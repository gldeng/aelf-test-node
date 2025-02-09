using AElf.Types;

namespace AElf.Firehose;

public class PreBlockExecuteEvent
{
    public long Height { get; set; }
    public Hash Hash { get; set; }
}