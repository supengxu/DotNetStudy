using System;
using System.Collections.Generic;

namespace LRUCache.Implementations
{
    public class LRUCacheByLinkedDictionary<TKey, TValue> where TKey : notnull
    {
        private readonly int _capacity;
        private readonly Dictionary<TKey, TValue> _cache;
        private readonly LinkedList<TKey> _order;

        public LRUCacheByLinkedDictionary(int capacity)
        {
            _capacity = capacity;
            _cache = new Dictionary<TKey, TValue>(capacity);
            _order = new LinkedList<TKey>();
        }

        public TValue? Get(TKey key)
        {
            if (!_cache.TryGetValue(key, out var value))
            {
                return default;
            }

            _order.Remove(key);
            _order.AddLast(key);

            return value;
        }

        public void Put(TKey key, TValue value)
        {
            if (_cache.ContainsKey(key))
            {
                _cache[key] = value;
                _order.Remove(key);
                _order.AddLast(key);
                return;
            }

            if (_cache.Count >= _capacity)
            {
                var first = _order.First;
                if (first != null)
                {
                    _order.RemoveFirst();
                    _cache.Remove(first.Value);
                }
            }

            _cache[key] = value;
            _order.AddLast(key);
        }

        public bool Contains(TKey key) => _cache.ContainsKey(key);

        public int Count => _cache.Count;

        public void Clear()
        {
            _cache.Clear();
            _order.Clear();
        }

        public void PrintOrder()
        {
            Console.Write("LRU Order (oldest -> newest): ");
            foreach (var key in _order)
            {
                Console.Write(key + " ");
            }
            Console.WriteLine();
        }
    }
}
