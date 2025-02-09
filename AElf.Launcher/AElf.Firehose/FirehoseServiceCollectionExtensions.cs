using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.ChainController.Application;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Firehose;

public static class FirehoseServiceCollectionExtensions
{
    public static IServiceCollection AddFirehose(this IServiceCollection services,
        Action<FirehoseOptions>? setupAction = null)
    {
        services.AddSingleton<ILocalEventHandler<BlockAcceptedEvent>, FirehoseProcessor>();
        services.AddSingleton<ILocalEventHandler<BlockAttachedEvent>, FirehoseProcessor>();
        services.AddSingleton<ILocalEventHandler<ExtendedTransactionExecutedEventData>, FirehoseProcessor>();
        services.AddSingleton<FirehoseProcessor>();
        services.RemoveAll(
            x => x.ImplementationType == typeof(ChainCreationService) &&
                 x.ServiceType == typeof(IChainCreationService)
        );
        services.AddTransient<IChainCreationService, ExtendedChainCreationService>();
        services.RemoveAll(
            x => x.ImplementationType == typeof(BlockAttachService) &&
                 x.ServiceType == typeof(IBlockAttachService)
        );
        services.AddTransient<IBlockAttachService, ExtendedBlockAttachService>();
        services.RemoveAll(
            x => x.ImplementationType == typeof(PlainTransactionExecutingService) &&
                 x.ServiceType == typeof(ITransactionExecutingService));
        services.RemoveAll(
            x => x.ImplementationType == typeof(PlainTransactionExecutingService) &&
                 x.ServiceType == typeof(IPlainTransactionExecutingService)
        );
        services.AddSingleton<IPlainTransactionExecutingService, WrappedPlainTransactionExecutingService>();

        services.RemoveAll(
            x => x.ImplementationType == typeof(FullBlockchainExecutingService) &&
                 x.ServiceType == typeof(IBlockchainExecutingService)
        );
        services.AddSingleton<IBlockchainExecutingService, WrappedFullBlockchainExecutingService>();

        services.RemoveAll(
            x => x.ImplementationType == typeof(MinerService) &&
                 x.ServiceType == typeof(IMinerService)
        );
        services.AddSingleton<IMinerService, WrappedMinerService>();
        if (setupAction != null)
            services.Configure(setupAction);
        return services;
    }
}