using System.Collections.Generic;
using System.Threading;

namespace LRUCache.Implementations
{
    /// <summary>
    /// LRU 缓存实现方式4: 线程安全版本（使用 ReaderWriterLockSlim）
    /// 
    /// 原理:
    /// - 包装基础 LRU 实现，添加线程安全访问
    /// - 使用 ReaderWriterLockSlim 实现读写分离锁
    /// - 多个线程可以同时读取，写入时独占访问
    /// 
    /// 复杂度:
    /// - Get: O(1) + 锁开销
    /// - Put: O(1) + 锁开销
    /// 
    /// 优点: 线程安全，支持高并发读取
    /// 缺点: 锁开销较大
    /// </summary>
    public class LRUCacheThreadSafe<TKey, TValue> where TKey : notnull
    {
        private readonly LRUCacheByHashLinkedList<TKey, TValue> _cache;
        private readonly ReaderWriterLockSlim _lock = new();

        public LRUCacheThreadSafe(int capacity)
        {
            _cache = new LRUCacheByHashLinkedList<TKey, TValue>(capacity);
        }

        public TValue? Get(TKey key)
        {
            _lock.EnterReadLock();
            try
            {
                return _cache.Get(key);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void Put(TKey key, TValue value)
        {
            _lock.EnterWriteLock();
            try
            {
                _cache.Put(key, value);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool Contains(TKey key)
        {
            _lock.EnterReadLock();
            try
            {
                return _cache.Contains(key);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public int Count
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _cache.Count;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                _cache.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void PrintOrder()
        {
            _lock.EnterReadLock();
            try
            {
                _cache.PrintOrder();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }
}
