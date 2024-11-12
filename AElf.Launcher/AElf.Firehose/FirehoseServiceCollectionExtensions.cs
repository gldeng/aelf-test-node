using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.ChainController.Application;
using AElf.Kernel.SmartContractExecution.Application;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Firehose;

public static class FirehoseServiceCollectionExtensions
{
    public static IServiceCollection AddFirehose(this IServiceCollection services)
    {
        services.AddSingleton<ILocalEventHandler<BlockAcceptedEvent>, FirehoseProcessor>();
        services.AddSingleton<ILocalEventHandler<BlockAttachedEvent>, FirehoseProcessor>();
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
        return services;
    }
}