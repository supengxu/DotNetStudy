using System;
using System.Collections.Generic;

namespace LRUCache.Implementations
{
    public class LRUCacheByHashLinkedList<TKey, TValue> where TKey : notnull
    {
        private readonly int _capacity;
        private readonly Dictionary<TKey, LinkedListNode<CacheEntry<TKey, TValue>>> _cache;
        private readonly LinkedList<CacheEntry<TKey, TValue>> _linkedList;

        public LRUCacheByHashLinkedList(int capacity)
        {
            _capacity = capacity;
            _cache = new Dictionary<TKey, LinkedListNode<CacheEntry<TKey, TValue>>>(capacity);
            _linkedList = new LinkedList<CacheEntry<TKey, TValue>>();
        }

        public TValue? Get(TKey key)
        {
            if (!_cache.TryGetValue(key, out var node))
            {
                return default;
            }

            _linkedList.Remove(node);
            _linkedList.AddLast(node);

            return node.Value.Value;
        }

        public void Put(TKey key, TValue value)
        {
            if (_cache.TryGetValue(key, out var existingNode))
            {
                _linkedList.Remove(existingNode);
                existingNode.Value = new CacheEntry<TKey, TValue>(key, value);
                _linkedList.AddLast(existingNode);
                return;
            }

            var newNode = new LinkedListNode<CacheEntry<TKey, TValue>>(
                new CacheEntry<TKey, TValue>(key, value));

            if (_cache.Count >= _capacity)
            {
                var firstNode = _linkedList.First;
                if (firstNode != null)
                {
                    _linkedList.RemoveFirst();
                    _cache.Remove(firstNode.Value.Key);
                }
            }

            _linkedList.AddLast(newNode);
            _cache[key] = newNode;
        }

        public bool Contains(TKey key) => _cache.ContainsKey(key);

        public int Count => _cache.Count;

        public void Clear()
        {
            _cache.Clear();
            _linkedList.Clear();
        }

        public void PrintOrder()
        {
            Console.Write("LRU Order (oldest -> newest): ");
            foreach (var node in _linkedList)
            {
                Console.Write(node.Key + " ");
            }
            Console.WriteLine();
        }

        private readonly struct CacheEntry<TK, TV>
        {
            public TK Key { get; }
            public TV Value { get; }

            public CacheEntry(TK key, TV value)
            {
                Key = key;
                Value = value;
            }
        }
    }
}
