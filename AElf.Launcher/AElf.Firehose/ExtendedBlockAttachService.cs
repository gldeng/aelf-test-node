using AElf.Kernel;
using AElf.Kernel.SmartContractExecution.Application;
using Volo.Abp.EventBus.Local;

namespace AElf.Firehose;

public class ExtendedBlockAttachService : IBlockAttachService
{
    private BlockAttachService _inner;

    private ILocalEventBus _localEventBus;

    public ExtendedBlockAttachService(BlockAttachService inner, ILocalEventBus localEventBus)
    {
        _inner = inner;
        _localEventBus = localEventBus;
    }


    public async Task AttachBlockAsync(Block block)
    {
        await _inner.AttachBlockAsync(block);
        await _localEventBus.PublishAsync(new BlockAttachedEvent()
        {
            Height = block.Height,
            Hash = block.GetHash()
        });
    }
}