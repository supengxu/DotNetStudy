using System;
using System.Collections.Generic;

namespace LRUCache.Implementations
{
    public class LRUCacheByOrderedDictionary<TKey, TValue> where TKey : notnull
    {
        private readonly int _capacity;
        private readonly Dictionary<TKey, TValue> _cache;
        private readonly List<TKey> _keys;

        public LRUCacheByOrderedDictionary(int capacity)
        {
            _capacity = capacity;
            _cache = new Dictionary<TKey, TValue>(capacity);
            _keys = new List<TKey>(capacity);
        }

        public TValue? Get(TKey key)
        {
            if (!_cache.TryGetValue(key, out var value))
            {
                return default;
            }

            _keys.Remove(key);
            _keys.Add(key);

            return value;
        }

        public void Put(TKey key, TValue value)
        {
            if (_cache.ContainsKey(key))
            {
                _keys.Remove(key);
            }
            else if (_cache.Count >= _capacity)
            {
                var firstKey = _keys[0];
                _keys.RemoveAt(0);
                _cache.Remove(firstKey);
            }

            _cache[key] = value;
            _keys.Add(key);
        }

        public bool Contains(TKey key) => _cache.ContainsKey(key);

        public int Count => _cache.Count;

        public void Clear()
        {
            _cache.Clear();
            _keys.Clear();
        }

        public void PrintOrder()
        {
            Console.Write("LRU Order (oldest -> newest): ");
            foreach (var key in _keys)
            {
                Console.Write(key + " ");
            }
            Console.WriteLine();
        }
    }
}
