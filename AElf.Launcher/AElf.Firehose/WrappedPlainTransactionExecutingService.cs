using AElf.Kernel;
using AElf.Kernel.FeatureDisable.Core;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.EventBus.Local;

namespace AElf.Firehose;

public class WrappedPlainTransactionExecutingService : PlainTransactionExecutingService
{
    private readonly FirehoseOptions _options;
    private readonly ILocalEventBus _localEventBus;
    private ILogger<WrappedPlainTransactionExecutingService> _logger;

    public WrappedPlainTransactionExecutingService(
        IOptionsSnapshot<FirehoseOptions> options,
        ISmartContractExecutiveService smartContractExecutiveService,
        IEnumerable<IPostExecutionPlugin> postPlugins, IEnumerable<IPreExecutionPlugin> prePlugins,
        ITransactionContextFactory transactionContextFactory, IFeatureDisableService featureDisableService,
        ILocalEventBus localEventBus, ILogger<WrappedPlainTransactionExecutingService> logger) : base(
        smartContractExecutiveService, postPlugins, prePlugins, transactionContextFactory, featureDisableService)
    {
        _options = options.Value;
        _localEventBus = localEventBus;
        _logger = logger;
    }

    protected override async Task<TransactionTrace> ExecuteOneAsync(
        SingleTransactionExecutingDto singleTxExecutingDto,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"WithInitialStateTracking {_options.WithInitialStateTracking}");
        if (_options.WithInitialStateTracking)
        {
            var chainContext = new ChainContextWrapperForTrackingOriginalStateValue(singleTxExecutingDto.ChainContext);
            singleTxExecutingDto.ChainContext = chainContext;

            var transactionTrace = await base.ExecuteOneAsync(singleTxExecutingDto, cancellationToken);
            await _localEventBus.PublishAsync(new ExtendedTransactionExecutedEventData()
            {
                TransactionTrace = transactionTrace,
                OriginalValues = chainContext.OriginalValues
            });
            _logger.LogTrace("Executed transaction and published trace with original sate values");
            return transactionTrace;
        }
        else
        {
            var transactionTrace = await base.ExecuteOneAsync(singleTxExecutingDto, cancellationToken);
            await _localEventBus.PublishAsync(new ExtendedTransactionExecutedEventData()
            {
                TransactionTrace = transactionTrace
            });
            _logger.LogTrace("Executed transaction and published trace");
            return transactionTrace;
        }
    }
}