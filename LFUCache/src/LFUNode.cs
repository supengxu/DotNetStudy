namespace LFUCache;

internal sealed class LFUNode<TKey, TValue>
{
    public TKey Key { get; }
    public TValue Value { get; set; }
    public int Frequency { get; set; }
    public LFUNode<TKey, TValue>? Prev { get; set; }
    public LFUNode<TKey, TValue>? Next { get; set; }
    
    public LFUNode(TKey key, TValue value)
    {
        Key = key;
        Value = value;
        Frequency = 1;
    }
}