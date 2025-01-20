using AElf.Types;

namespace AElf.Firehose;

public class BlockAttachedEvent
{
    public long Height { get; set; }
    public Hash Hash { get; set; }
    public bool ExistingBlock { get; set; } 
}