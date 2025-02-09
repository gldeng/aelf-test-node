using AElf.Kernel.Blockchain;
using AElf.Kernel.Miner.Application;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.EventBus.Local;

namespace AElf.Firehose;

public class WrappedMinerService : IMinerService
{
    private readonly MinerService _inner;
    private readonly ILocalEventBus _localEventBus;

    public WrappedMinerService(
        MinerService inner,
        ILocalEventBus localEventBus
    )
    {
        _inner = inner;
        _localEventBus = localEventBus;
    }


    public async Task<BlockExecutedSet> MineAsync(
        Hash previousBlockHash, long previousBlockHeight, Timestamp blockTime, Duration blockExecutionTime)
    {
        await _localEventBus.PublishAsync(new MiningEvent()
        {
            Height = previousBlockHeight + 1
        });
        return await _inner.MineAsync(previousBlockHash, previousBlockHeight, blockTime, blockExecutionTime);
    }
}