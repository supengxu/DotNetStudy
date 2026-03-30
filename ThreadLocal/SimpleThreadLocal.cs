using System;
using System.Collections.Generic;
using System.Threading;

namespace ThreadLocal
{
    /// <summary>
    /// 简单的线程局部存储（Thread Local Storage）实现类
    /// 使用 Dictionary 存储每个线程独立的数据副本
    /// </summary>
    /// <typeparam name="T">存储的数据类型</typeparam>
    public class SimpleThreadLocal<T>
    {
        /// <summary>
        /// 线程局部数据存储，使用线程 ID 作为键
        /// </summary>
        private readonly Dictionary<int, T> _storage = new Dictionary<int, T>();

        /// <summary>
        /// 用于初始化线程局部数据初始值的委托
        /// </summary>
        private readonly Func<T> _valueFactory;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="valueFactory">用于创建初始值的工厂方法</param>
        public SimpleThreadLocal(Func<T> valueFactory)
        {
            _valueFactory = valueFactory ?? throw new ArgumentNullException(nameof(valueFactory));
        }

        /// <summary>
        /// 获取当前线程的局部数据
        /// 如果当前线程没有存储值，则调用 _valueFactory 创建初始值
        /// </summary>
        /// <returns>当前线程存储的值</returns>
        public T Get()
        {
            // 获取当前线程的 ID
            int threadId = Thread.CurrentThread.ManagedThreadId;
            
            // 使用 lock 保护字典访问，确保线程安全
            lock (_storage)
            {
                // 检查当前线程是否已有存储的值
                if (_storage.TryGetValue(threadId, out T value))
                {
                    return value;
                }
                
                // 线程不存在，创建初始值
                T initialValue = _valueFactory();
                
                // 存储到字典中并返回
                _storage[threadId] = initialValue;
                return initialValue;
            }
        }

        /// <summary>
        /// 设置当前线程的局部数据
        /// </summary>
        /// <param name="value">要存储的值</param>
        public void Set(T value)
        {
            // 获取当前线程的 ID
            int threadId = Thread.CurrentThread.ManagedThreadId;
            
            // 使用 lock 保护字典访问，确保线程安全
            lock (_storage)
            {
                // 将值存储到字典中（如果键已存在则覆盖）
                _storage[threadId] = value;
            }
        }

        /// <summary>
        /// 移除当前线程的局部数据
        /// </summary>
        public void Remove()
        {
            // 获取当前线程的 ID
            int threadId = Thread.CurrentThread.ManagedThreadId;
            
            // 使用 lock 保护字典访问，确保线程安全
            lock (_storage)
            {
                // 从字典中移除当前线程的数据
                _storage.Remove(threadId);
            }
        }
    }
}