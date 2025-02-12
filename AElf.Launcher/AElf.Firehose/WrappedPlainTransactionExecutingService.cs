using System.Reflection;
using AElf.Kernel;
using AElf.Kernel.FeatureDisable.Core;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Types;
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
        var transactionTrace = await base.ExecuteOneAsync(singleTxExecutingDto, cancellationToken);
        if (_options.WithInitialStateTracking)
        {
            var ctx = singleTxExecutingDto.ChainContext as ChainContextWithTieredStateCache;
            if (ctx == null)
            {
                throw new InvalidOperationException("ChainContext is not of type ChainContextWithTieredStateCache.");
            }

            await _localEventBus.PublishAsync(new ExtendedTransactionExecutedEventData()
            {
                TransactionTrace = transactionTrace,
                OriginalValues = ctx.StateCache.GetOriginalValues()
            });
            _logger.LogTrace("Executed transaction and published trace with original state values");
        }
        else
        {
            await _localEventBus.PublishAsync(new ExtendedTransactionExecutedEventData()
            {
                TransactionTrace = transactionTrace
            });
            _logger.LogTrace("Executed transaction and published trace");
        }

        return transactionTrace;
    }
}

public static class TieredStateCacheExtension
{
    private static readonly FieldInfo? OriginalValuesField = 
        typeof(TieredStateCache).GetField("_originalValues", BindingFlags.NonPublic | BindingFlags.Instance);

    public static Dictionary<ScopedStatePath, byte[]> GetOriginalValues(this TieredStateCache cache)
    {
        if (OriginalValuesField != null)
        {
            return OriginalValuesField.GetValue(cache) as Dictionary<ScopedStatePath, byte[]>
                   ?? throw new InvalidOperationException("Original values are not of the expected type.");
        }

        throw new InvalidOperationException("Unable to access the original values.");
    }
}