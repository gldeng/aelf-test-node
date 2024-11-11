using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using Google.Protobuf;
using Volo.Abp.EventBus;

namespace AElf.Firehose;

/// <summary>
/// Prints messages required by firehose to stdin. We need to break it down into two steps
/// as the event <see cref="BlockAcceptedEvent"/> carries the block but LIB info is updated
/// only after the event is fired (hence we cannot get the correct LIB if we naively process
/// <see cref="BlockAcceptedEvent"/>.
/// </summary>
public class FirehoseProcessor : ILocalEventHandler<BlockAcceptedEvent>, ILocalEventHandler<BlockAttachedEvent>
{
    private BlockAcceptedEvent? _acceptedEvent = null;
    private readonly IBlockchainService _blockchainService;

    public FirehoseProcessor(IBlockchainService blockchainService)
    {
        _blockchainService = blockchainService;
        Console.WriteLine($"FIRE INIT 3.0 {Block.Descriptor.FullName}");
    }

    public Task HandleEventAsync(BlockAcceptedEvent? eventData)
    {
        _acceptedEvent = eventData;
        return Task.CompletedTask;
    }

    public async Task HandleEventAsync(BlockAttachedEvent eventData)
    {
        if (_acceptedEvent == null)
        {
            Console.WriteLine("FIRE EXCEPTION NO_ACCEPTED_EVENT");
            return;
        }

        if (
            // ReSharper disable once ComplexConditionExpression
            _acceptedEvent.Block.Header.Height != eventData.Height ||
            _acceptedEvent.Block.Header.GetHash() != eventData.Hash
        )
        {
            _acceptedEvent = null;
            Console.WriteLine("FIRE EXCEPTION DISCREPANCY");
            return;
        }

        var blockPayloadBase64 = Convert.ToBase64String(_acceptedEvent.Block.ToByteArray());

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
}