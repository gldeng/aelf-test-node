using AElf.Kernel;
using AElf.Kernel.FeatureDisable.Core;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Events;
using Volo.Abp.EventBus.Local;

namespace AElf.Firehose;

public class WrappedPlainTransactionExecutingService : PlainTransactionExecutingService
{
    private readonly ILocalEventBus _localEventBus;

    public WrappedPlainTransactionExecutingService(ISmartContractExecutiveService smartContractExecutiveService,
        IEnumerable<IPostExecutionPlugin> postPlugins, IEnumerable<IPreExecutionPlugin> prePlugins,
        ITransactionContextFactory transactionContextFactory, IFeatureDisableService featureDisableService,
        ILocalEventBus localEventBus) : base(
        smartContractExecutiveService, postPlugins, prePlugins, transactionContextFactory, featureDisableService)
    {
        _localEventBus = localEventBus;
    }

    protected override async Task<TransactionTrace> ExecuteOneAsync(
        SingleTransactionExecutingDto singleTxExecutingDto,
        CancellationToken cancellationToken)
    {
        var transactionTrace = await base.ExecuteOneAsync(singleTxExecutingDto, cancellationToken);
        await _localEventBus.PublishAsync(new TransactionExecutedEventData()
        {
            TransactionTrace = transactionTrace
        });
        return transactionTrace;
    }
}