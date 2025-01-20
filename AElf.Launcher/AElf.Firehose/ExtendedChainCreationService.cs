using AElf.Kernel;
using AElf.Kernel.ChainController.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Volo.Abp.EventBus.Local;

namespace AElf.Firehose;

public class ExtendedChainCreationService : IChainCreationService
{
    private readonly ChainCreationService _inner;

    private readonly ILocalEventBus _localEventBus;
    private readonly ILogger<ExtendedChainCreationService> _logger;

    public ExtendedChainCreationService(
        ChainCreationService inner, ILocalEventBus localEventBus, ILogger<ExtendedChainCreationService> logger)
    {
        _inner = inner;
        _localEventBus = localEventBus;
        _logger = logger;
    }

    public async Task<Chain> CreateNewChainAsync(IEnumerable<Transaction> genesisTransactions)
    {
        _logger.LogTrace("Pre CreateNewChainAsync");
        var chain = await _inner.CreateNewChainAsync(genesisTransactions);
        _logger.LogTrace("Post CreateNewChainAsync");
        await _localEventBus.PublishAsync(new BlockAttachedEvent()
        {
            Height = AElfConstants.GenesisBlockHeight,
            Hash = chain.GenesisBlockHash
        });
        _logger.LogTrace("Published BlockAttachedEvent");
        return chain;
    }
}