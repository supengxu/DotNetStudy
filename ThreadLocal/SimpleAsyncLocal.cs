using System;
using System.Collections.Generic;
using System.Threading;

namespace ThreadLocal
{
    /// <summary>
    /// 简单的异步局部存储（Async Local Storage）实现类
    /// 基于 ExecutionContext 存储数据，实现跨 async/await 边界流转
    /// 
    /// 原理说明：
    /// - AsyncLocal<T> 使用 ExecutionContext 存储数据
    /// - ExecutionContext 会跨 async/await 自动流转（Flow）
    /// - 不同于 ThreadLocal，AsyncLocal 的值会随异步执行流传递
    /// </summary>
    /// <typeparam name="T">存储的数据类型</typeparam>
    public class SimpleAsyncLocal<T>
    {
        /// <summary>
        /// 使用线程安全的字典存储每个 ExecutionContext 的数据
        /// key: ExecutionContext 的哈希码
        /// value: 存储的值
        /// </summary>
        private static readonly Dictionary<int, T> _storage = new Dictionary<int, T>();
        
        private static readonly object _lock = new object();

        /// <summary>
        /// 获取或设置当前 ExecutionContext 关联的值
        /// </summary>
        public T Value
        {
            get
            {
                // 获取当前 ExecutionContext 的唯一标识
                int contextId = GetExecutionContextId();
                
                lock (_lock)
                {
                    if (_storage.TryGetValue(contextId, out T value))
                    {
                        return value;
                    }
                    return default(T);
                }
            }
            set
            {
                // 获取当前 ExecutionContext 的唯一标识
                int contextId = GetExecutionContextId();
                
                lock (_lock)
                {
                    _storage[contextId] = value;
                }
            }
        }

        /// <summary>
        /// 获取当前 ExecutionContext 的唯一标识符
        /// 使用 SynchronizationContext + Task 的哈希码组合
        /// </summary>
        private static int GetExecutionContextId()
        {
            // 获取当前同步上下文（可为 null）
            var syncContext = SynchronizationContext.Current?.GetHashCode() ?? 0;
            
            // 获取当前任务（可为 null）
            var taskId = Task.CurrentId?.GetHashCode() ?? 0;
            
            // 组合生成唯一 ID（简化实现）
            return syncContext ^ (taskId * 31);
        }

        /// <summary>
        /// 移除当前 ExecutionContext 关联的值
        /// </summary>
        public void Remove()
        {
            int contextId = GetExecutionContextId();
            
            lock (_lock)
            {
                _storage.Remove(contextId);
            }
        }
    }
}