using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContractExecution.Application;
using Volo.Abp.EventBus.Local;

namespace AElf.Firehose;

public class WrappedFullBlockchainExecutingService : IBlockchainExecutingService
{
    private readonly FullBlockchainExecutingService _inner;
    private readonly ILocalEventBus _localEventBus;

    public WrappedFullBlockchainExecutingService(
        IBlockchainService blockchainService,
        IBlockValidationService blockValidationService,
        IBlockExecutingService blockExecutingService,
        ITransactionResultService transactionResultService,
        IBlockStateSetManger blockStateSetManger,
        ILocalEventBus localEventBus)
    {
        var wrappedBlockExecutingService = new WrappedBlockExecutingService(blockExecutingService, localEventBus);
        _inner = new FullBlockchainExecutingService(
            blockchainService,
            blockValidationService,
            wrappedBlockExecutingService,
            transactionResultService,
            blockStateSetManger
        );
        _localEventBus = localEventBus;
    }

    public async Task<BlockExecutionResult> ExecuteBlocksAsync(IEnumerable<Block> blocks)
    {
        return await _inner.ExecuteBlocksAsync(blocks);
    }
}