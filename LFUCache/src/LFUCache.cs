using System.Collections.Generic;

namespace LFUCache;

public sealed class LFUCache<TKey, TValue> : ILFUCache<TKey, TValue>
{
    private readonly Dictionary<TKey, LFUNode<TKey, TValue>> _nodeMap;
    private readonly Dictionary<int, DoublyLinkedList<TKey, TValue>> _frequencyMap;
    private readonly int _capacity;
    private int _minFrequency;
    private readonly object _lock;
    
    public int Capacity => _capacity;
    
    public int Count => _nodeMap.Count;
    
    public LFUCache(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentException("Capacity must be greater than 0", nameof(capacity));
        }
        
        _capacity = capacity;
        _nodeMap = new Dictionary<TKey, LFUNode<TKey, TValue>>();
        _frequencyMap = new Dictionary<int, DoublyLinkedList<TKey, TValue>>();
        _minFrequency = 0;
        _lock = new object();
    }
    
    public TValue? Get(TKey key)
    {
        lock (_lock)
        {
            if (!_nodeMap.TryGetValue(key, out var node))
            {
                return default;
            }
            
            UpdateFrequency(node);
            return node.Value;
        }
    }
    
    public void Put(TKey key, TValue value)
    {
        lock (_lock)
        {
            if (_nodeMap.TryGetValue(key, out var existingNode))
            {
                existingNode.Value = value;
                UpdateFrequency(existingNode);
                return;
            }
            
            if (_nodeMap.Count >= _capacity)
            {
                Evict();
            }
            
            var newNode = new LFUNode<TKey, TValue>(key, value);
            _nodeMap[key] = newNode;
            
            if (!_frequencyMap.TryGetValue(1, out var list))
            {
                list = new DoublyLinkedList<TKey, TValue>();
                _frequencyMap[1] = list;
            }
            
            list.AddToHead(newNode);
            _minFrequency = 1;
        }
    }
    
    public bool Remove(TKey key)
    {
        lock (_lock)
        {
            if (!_nodeMap.TryGetValue(key, out var node))
            {
                return false;
            }
            
            if (_frequencyMap.TryGetValue(node.Frequency, out var list))
            {
                list.Remove(node);
                
                if (list.Count == 0)
                {
                    _frequencyMap.Remove(node.Frequency);
                    
                    if (_minFrequency == node.Frequency)
                    {
                        UpdateMinFrequency();
                    }
                }
            }
            
            _nodeMap.Remove(key);
            return true;
        }
    }
    
    public void Clear()
    {
        lock (_lock)
        {
            _nodeMap.Clear();
            _frequencyMap.Clear();
            _minFrequency = 0;
        }
    }
    
    private void UpdateFrequency(LFUNode<TKey, TValue> node)
    {
        var oldFrequency = node.Frequency;
        
        if (_frequencyMap.TryGetValue(oldFrequency, out var oldList))
        {
            oldList.Remove(node);
            
            if (oldList.Count == 0)
            {
                _frequencyMap.Remove(oldFrequency);
                
                if (_minFrequency == oldFrequency)
                {
                    _minFrequency = oldFrequency + 1;
                }
            }
        }
        
        node.Frequency++;
        
        if (!_frequencyMap.TryGetValue(node.Frequency, out var newList))
        {
            newList = new DoublyLinkedList<TKey, TValue>();
            _frequencyMap[node.Frequency] = newList;
        }
        
        newList.AddToHead(node);
    }
    
    private void Evict()
    {
        if (!_frequencyMap.TryGetValue(_minFrequency, out var list))
        {
            return;
        }
        
        var node = list.RemoveFromTail();
        
        if (node != null)
        {
            _nodeMap.Remove(node.Key);
            
            if (list.Count == 0)
            {
                _frequencyMap.Remove(_minFrequency);
            }
        }
    }
    
    private void UpdateMinFrequency()
    {
        _minFrequency = 0;
        
        foreach (var frequency in _frequencyMap.Keys)
        {
            if (_minFrequency == 0 || frequency < _minFrequency)
            {
                _minFrequency = frequency;
            }
        }
    }
}