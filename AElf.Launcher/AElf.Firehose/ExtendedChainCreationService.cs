using AElf.Kernel;
using AElf.Kernel.ChainController.Application;
using AElf.Types;
using Volo.Abp.EventBus.Local;

namespace AElf.Firehose;

public class ExtendedChainCreationService : IChainCreationService
{
    private readonly ChainCreationService _inner;

    private readonly ILocalEventBus _localEventBus;

    public ExtendedChainCreationService(ChainCreationService inner, ILocalEventBus localEventBus)
    {
        _inner = inner;
        _localEventBus = localEventBus;
    }

    public async Task<Chain> CreateNewChainAsync(IEnumerable<Transaction> genesisTransactions)
    {
        var chain = await _inner.CreateNewChainAsync(genesisTransactions);
        await _localEventBus.PublishAsync(new BlockAttachedEvent()
        {
            Height = AElfConstants.GenesisBlockHeight,
            Hash = chain.GenesisBlockHash
        });
        return chain;
    }
}