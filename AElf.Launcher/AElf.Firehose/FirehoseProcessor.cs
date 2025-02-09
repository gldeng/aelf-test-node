using System.Collections.Concurrent;
using AElf.Firehose.Pb;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.EventBus;
using Hash = AElf.Types.Hash;

namespace AElf.Firehose;

/// <summary>
/// Prints messages required by firehose to stdin. We need to break it down into two steps
/// as the event <see cref="BlockAcceptedEvent"/> carries the block but LIB info is updated
/// only after the event is fired (hence we cannot get the correct LIB if we naively process
/// <see cref="BlockAcceptedEvent"/>.
/// </summary>
public class FirehoseProcessor : ILocalEventHandler<BlockAcceptedEvent>, ILocalEventHandler<BlockAttachedEvent>,
    ILocalEventHandler<ExtendedTransactionExecutedEventData>, ILocalEventHandler<PreBlockExecuteEvent>,
    ILocalEventHandler<MiningEvent>
{
    private readonly FirehoseOptions _options;

    private MiningEvent? _miningEvent = null;
    private List<BlockAcceptedEvent> _acceptedEvents = new List<BlockAcceptedEvent>();

    // current block -> irreversible block
    private readonly ConcurrentDictionary<long, long> _irreverisbleBlocks = new ConcurrentDictionary<long, long>();
    private readonly IBlockchainService _blockchainService;

    private readonly ConcurrentDictionary<AElf.Types.Hash, ExtendedTransactionExecutedEventData>
        _transactionExecutedEventData;

    private readonly ILogger<FirehoseProcessor> _logger;

    public FirehoseProcessor(IOptionsSnapshot<FirehoseOptions> options, IBlockchainService blockchainService,
        ILogger<FirehoseProcessor> logger)
    {
        _blockchainService = blockchainService;
        _logger = logger;
        _options = options.Value;
        _transactionExecutedEventData = new ConcurrentDictionary<Hash, ExtendedTransactionExecutedEventData>();
        Console.WriteLine($"FIRE INIT 3.0 {Pb.Block.Descriptor.FullName}");
    }

    public Task HandleEventAsync(BlockAcceptedEvent? eventData)
    {
        if (eventData == null)
            return Task.CompletedTask;
        _logger.LogTrace("Handle BlockAcceptedEvent Height: {}, Hash: {}", eventData.Block.Height,
            eventData.Block.GetHash().ToHex());
        _acceptedEvents.Add(eventData);
        return Task.CompletedTask;
    }

    public async Task HandleEventAsync(BlockAttachedEvent eventData)
    {
        _logger.LogTrace("Handle BlockAttachedEvent Height: {}, Hash: {}, Existing: {}",
            eventData.Height, eventData.Hash.ToHex(), eventData.ExistingBlock);

        if (eventData.ExistingBlock && _miningEvent == null) return;
        await CheckIrreversibleBlockAsync(eventData.Height);
        _logger.LogTrace("lib dict size {}", _irreverisbleBlocks.Count);
        var lastBlockAcceptedEvent = _acceptedEvents.Last();
        if (
            // ReSharper disable once ComplexConditionExpression
            lastBlockAcceptedEvent.Block.Header.Height != eventData.Height ||
            lastBlockAcceptedEvent.Block.Header.GetHash() != eventData.Hash
        )
        {
            _logger.LogError("firehose block discrepancy");
            _acceptedEvents = new List<BlockAcceptedEvent>();
            _irreverisbleBlocks.Clear();
            _transactionExecutedEventData.Clear();
            _miningEvent = null;
            return;
        }

        _logger.LogTrace("Exporting {} blocks", _acceptedEvents.Count);
        foreach (var @event in _acceptedEvents)
        {
            PrepareAndPrintBlock(@event);
        }

        _acceptedEvents = new List<BlockAcceptedEvent>();
        _irreverisbleBlocks.Clear();
        _transactionExecutedEventData.Clear();
        _miningEvent = null;
    }

    private void PrepareAndPrintBlock(BlockAcceptedEvent @event)
    {
        var pbBlock = PreparePbBlock(@event);
        if (pbBlock == null) return;
        var blockPayloadBase64 = Convert.ToBase64String(pbBlock.ToByteArray());
        // Convert DateTime to Unix time in nanoseconds
        var unixTimeNanos = (
            @event.Block.Header.Time.ToDateTime() -
            new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        ).TotalNanoseconds;

        var libHeight = _irreverisbleBlocks[@event.Block.Height];

        _logger.LogTrace("Exporting block Height: {}, Hash: {}", @event.Block.Height, @event.Block.GetHash().ToHex());
        var blockLine = string.Format(
            "FIRE BLOCK {0} {1} {2} {3} {4} {5} {6}",
            @event.Block.Height,
            @event.Block.GetHash().ToHex(),
            @event.Block.Height - 1,
            @event.Block.Header.PreviousBlockHash.ToHex(),
            libHeight,
            (long)unixTimeNanos, // Cast to long for formatting
            blockPayloadBase64
        );
        Console.WriteLine(blockLine);
    }

    private Pb.Block? PreparePbBlock(BlockAcceptedEvent @event)
    {
        _logger.LogTrace("Preparing Pb Block Height: {}, Hash: {}", @event.Block.Height,
            @event.Block.GetHash().ToHex());
        var pbBlock = Pb.Block.Parser.ParseFrom(@event.Block.ToByteArray());
        pbBlock.FirehoseBody = new FirehoseBlockBody();
        pbBlock.FirehoseBody.Transactions.AddRange(
            @event.BlockExecutedSet.TransactionMap.Values.Select(
                t => Pb.Transaction.Parser.ParseFrom(t.ToByteArray()))
        );
        pbBlock.FirehoseBody.TrasanctionResults.AddRange(
            @event.BlockExecutedSet.TransactionResultMap.Values.Select(
                ts => Pb.TransactionResult.Parser.ParseFrom(ts.ToByteArray()))
        );
        // if (_acceptedEvent.Block.Height > 1)
        pbBlock.FirehoseBody.TransactionTraces.AddRange(
            @event.Block.TransactionIds.Select(
                txId => Pb.TransactionTrace.Parser.ParseFrom(_transactionExecutedEventData[txId].TransactionTrace
                    .ToByteArray())
            )
        );
        if (_options.WithInitialStateTracking)
        {
            pbBlock.FirehoseBody.InitialStates.AddRange(
                @event.Block.TransactionIds.Select(
                    txId =>
                    {
                        var state = new InitialStateSet();
                        var originalValues = _transactionExecutedEventData[txId].OriginalValues;
                        foreach (var kvp in originalValues
                                     .Select(kvp => new
                                     {
                                         Key = kvp.Key.ToStateKey(), Value = kvp.Value
                                     })
                                     .OrderBy(kvp => kvp.Key))
                        {
                            state.Values.Add(kvp.Key, ByteString.CopyFrom(kvp.Value ?? new byte[0]));
                        }

                        return state;
                    })
            );
        }

        return pbBlock;
    }

    public Task HandleEventAsync(ExtendedTransactionExecutedEventData eventData)
    {
        _transactionExecutedEventData[eventData.TransactionTrace.TransactionId] = eventData;
        return Task.CompletedTask;
    }

    public async Task HandleEventAsync(PreBlockExecuteEvent eventData)
    {
        _logger.LogTrace("Handle PreBlockExecuteEvent Height: {}, Hash: {}", eventData.Height, eventData.Hash);
        await CheckIrreversibleBlockAsync(eventData.Height - 1);
    }

    private async Task CheckIrreversibleBlockAsync(long lastHeight)
    {
        if (!_irreverisbleBlocks.ContainsKey(lastHeight))
        {
            var chain = await _blockchainService.GetChainAsync();
            _irreverisbleBlocks[lastHeight] = chain.LastIrreversibleBlockHeight;
        }
    }

    public Task HandleEventAsync(MiningEvent eventData)
    {
        _miningEvent = eventData;
        return Task.CompletedTask;
    }
}