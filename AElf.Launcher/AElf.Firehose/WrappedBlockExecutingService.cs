using AElf.Kernel;
using AElf.Kernel.Blockchain;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Types;
using Volo.Abp.EventBus.Local;

namespace AElf.Firehose;

public class WrappedBlockExecutingService : IBlockExecutingService
{
    private readonly IBlockExecutingService _inner;
    private readonly ILocalEventBus _localEventBus;

    public WrappedBlockExecutingService(IBlockExecutingService inner, ILocalEventBus localEventBus)
    {
        _inner = inner;
        _localEventBus = localEventBus;
    }

    public Task<BlockExecutedSet> ExecuteBlockAsync(
        BlockHeader blockHeader,
        List<Transaction> nonCancellableTransactions)
    {
        _localEventBus.PublishAsync(new PreBlockExecuteEvent
        {
            Height = blockHeader.Height,
            Hash = blockHeader.GetHash()
        });
        return _inner.ExecuteBlockAsync(blockHeader, nonCancellableTransactions);
    }

    public Task<BlockExecutedSet> ExecuteBlockAsync(
        BlockHeader blockHeader,
        IEnumerable<Transaction> nonCancellableTransactions,
        IEnumerable<Transaction> cancellableTransactions,
        CancellationToken cancellationToken)
    {
        _localEventBus.PublishAsync(new PreBlockExecuteEvent
        {
            Height = blockHeader.Height,
            Hash = blockHeader.GetHash()
        });
        return _inner.ExecuteBlockAsync(blockHeader, nonCancellableTransactions, cancellableTransactions,
            cancellationToken);
    }
}