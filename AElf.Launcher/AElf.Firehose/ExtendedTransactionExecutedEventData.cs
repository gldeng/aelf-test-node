using AElf.Kernel.SmartContractExecution.Events;
using AElf.Types;

namespace AElf.Firehose;

public class ExtendedTransactionExecutedEventData : TransactionExecutedEventData
{
    public Dictionary<ScopedStatePath, byte[]> OriginalValues { get; set; }
}