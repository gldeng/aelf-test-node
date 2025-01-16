using AElf.Kernel;
using AElf.Types;

namespace AElf.Firehose;

public class StateCacheProxy : IStateCache
{
    private readonly Dictionary<ScopedStatePath, byte[]> _originalValues = new();
    private readonly IStateCache _inner;

    public StateCacheProxy(IStateCache inner)
    {
        _inner = inner;
    }

    public bool TryGetValue(ScopedStatePath key, out byte[] value)
    {
        var found = _inner.TryGetValue(key, out var innerValue);
        if (found)
        {
            _originalValues.TryAdd(key, innerValue);
        }

        value = innerValue;

        return found;
    }

    public byte[] this[ScopedStatePath key]
    {
        set => _inner[key] = value;
    }

    public Dictionary<ScopedStatePath, byte[]> OriginalValues => _originalValues;
}

public class ChainContextWrapperForTrackingOriginalStateValue : IChainContext
{
    private readonly StateCacheProxy _stateCache;
    private readonly IChainContext _inner;
    public Hash BlockHash => _inner.BlockHash;
    public long BlockHeight => _inner.BlockHeight;

    public IStateCache StateCache
    {
        get => _stateCache;
        set
        {
            throw new InvalidOperationException(
                $"{typeof(ChainContextWrapperForTrackingOriginalStateValue)} doesn't support setting of StateCache.");
        }
    }
    public Dictionary<ScopedStatePath, byte[]> OriginalValues => _stateCache.OriginalValues;
    public ChainContextWrapperForTrackingOriginalStateValue(IChainContext inner)
    {
        _inner = inner;
        _stateCache = new StateCacheProxy(_inner.StateCache);
    }
}