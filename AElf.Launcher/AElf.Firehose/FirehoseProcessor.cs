using System.Collections.Concurrent;
using AElf.Firehose.Pb;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContractExecution.Events;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Volo.Abp.EventBus;
using Hash = AElf.Types.Hash;
using TransactionTrace = AElf.Firehose.Pb.TransactionTrace;

namespace AElf.Firehose;

/// <summary>
/// Prints messages required by firehose to stdin. We need to break it down into two steps
/// as the event <see cref="BlockAcceptedEvent"/> carries the block but LIB info is updated
/// only after the event is fired (hence we cannot get the correct LIB if we naively process
/// <see cref="BlockAcceptedEvent"/>.
/// </summary>
public class FirehoseProcessor : ILocalEventHandler<BlockAcceptedEvent>, ILocalEventHandler<BlockAttachedEvent>,
    ILocalEventHandler<TransactionExecutedEventData>
{
    private BlockAcceptedEvent? _acceptedEvent = null;
    private readonly IBlockchainService _blockchainService;
    private ConcurrentDictionary<AElf.Types.Hash, TransactionTrace> _transactionTraces;
    private ILogger<FirehoseProcessor> _logger;

    public FirehoseProcessor(IBlockchainService blockchainService, ILogger<FirehoseProcessor> logger)
    {
        _blockchainService = blockchainService;
        _logger = logger;
        _transactionTraces = new ConcurrentDictionary<Hash, TransactionTrace>();
        Console.WriteLine($"FIRE INIT 3.0 {Pb.Block.Descriptor.FullName}");
    }

    public Task HandleEventAsync(BlockAcceptedEvent? eventData)
    {
        _acceptedEvent = eventData;
        return Task.CompletedTask;
    }

    public async Task HandleEventAsync(BlockAttachedEvent eventData)
    {
        var pbBlock = PreparePbBlock(eventData);
        if (pbBlock == null) return;
        var blockPayloadBase64 = Convert.ToBase64String(pbBlock.ToByteArray());
        // Convert DateTime to Unix time in nanoseconds
        var unixTimeNanos = (
            _acceptedEvent.Block.Header.Time.ToDateTime() -
            new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        ).TotalNanoseconds;
        var chain = await _blockchainService.GetChainAsync();

        var blockLine = string.Format(
            "FIRE BLOCK {0} {1} {2} {3} {4} {5} {6}",
            _acceptedEvent.Block.Height,
            _acceptedEvent.Block.GetHash().ToHex(),
            _acceptedEvent.Block.Height - 1,
            _acceptedEvent.Block.Header.PreviousBlockHash.ToHex(),
            chain.LastIrreversibleBlockHeight,
            (long)unixTimeNanos, // Cast to long for formatting
            blockPayloadBase64
        );
        Console.WriteLine(blockLine);
    }

    private Pb.Block? PreparePbBlock(BlockAttachedEvent eventData)
    {
        if (_acceptedEvent == null)
        {
            Console.WriteLine("FIRE EXCEPTION NO_ACCEPTED_EVENT");
            return null;
        }

        if (
            // ReSharper disable once ComplexConditionExpression
            _acceptedEvent.Block.Header.Height != eventData.Height ||
            _acceptedEvent.Block.Header.GetHash() != eventData.Hash
        )
        {
            _acceptedEvent = null;
            Console.WriteLine("FIRE EXCEPTION DISCREPANCY");
            return null;
        }

        var pbBlock = Pb.Block.Parser.ParseFrom(_acceptedEvent.Block.ToByteArray());
        pbBlock.FirehoseBody = new FirehoseBlockBody();
        pbBlock.FirehoseBody.Transactions.AddRange(
            _acceptedEvent.BlockExecutedSet.TransactionMap.Values.Select(
                t => Pb.Transaction.Parser.ParseFrom(t.ToByteArray()))
        );
        pbBlock.FirehoseBody.TrasanctionResults.AddRange(
            _acceptedEvent.BlockExecutedSet.TransactionResultMap.Values.Select(
                ts => Pb.TransactionResult.Parser.ParseFrom(ts.ToByteArray()))
        );
        // if (_acceptedEvent.Block.Height > 1)
            pbBlock.FirehoseBody.TransactionTraces.AddRange(
                _acceptedEvent.Block.TransactionIds.Select(txId => _transactionTraces[txId])
            );
        _transactionTraces.Clear();
        return pbBlock;
    }

    public Task HandleEventAsync(TransactionExecutedEventData eventData)
    {
        var transactionTrace = TransactionTrace.Parser.ParseFrom(eventData.TransactionTrace.ToByteArray());
        _transactionTraces[eventData.TransactionTrace.TransactionId] = transactionTrace;
        return Task.CompletedTask;
    }
}