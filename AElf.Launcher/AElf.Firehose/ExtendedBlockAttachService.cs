using AElf.Kernel;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContractExecution.Application;
using Microsoft.Extensions.Logging;
using Volo.Abp.EventBus.Local;

namespace AElf.Firehose;

public class ExtendedBlockAttachService : IBlockAttachService
{
    private BlockAttachService _inner;
    private readonly IBlockStateSetManger _blockStateSetManger;

    private ILocalEventBus _localEventBus;
    private readonly ILogger<ExtendedBlockAttachService> _logger;

    public ExtendedBlockAttachService(
        BlockAttachService inner, IBlockStateSetManger blockStateSetManger, ILocalEventBus localEventBus,
        ILogger<ExtendedBlockAttachService> logger)
    {
        _inner = inner;
        _blockStateSetManger = blockStateSetManger;
        _localEventBus = localEventBus;
        _logger = logger;
    }


    public async Task AttachBlockAsync(Block block)
    {
        var blockState = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
        var blockStateExists = blockState != null;
        _logger.LogTrace($"Pre AttachBlockAsync blockHeight: {block.Height}");
        await _inner.AttachBlockAsync(block);
        _logger.LogTrace($"Post AttachBlockAsync blockHeight: {block.Height}");

        await _localEventBus.PublishAsync(new BlockAttachedEvent()
        {
            Height = block.Height,
            Hash = block.GetHash(),
            ExistingBlock = blockStateExists
        });
        _logger.LogTrace($"Published BlockAttachedEvent blockHeight: {block.Height}");
    }
}