using System;
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
    /// 
    /// 实现方式：
    /// - 内部包装 .NET 的 AsyncLocal<T>，确保正确的 ExecutionContext 流转行为
    /// - 提供简化的 API（Value 属性 + Remove 方法）
    /// </summary>
    /// <typeparam name="T">存储的数据类型</typeparam>
    public class SimpleAsyncLocal<T>
    {
        /// <summary>
        /// 内部使用 .NET AsyncLocal<T> 来正确实现 ExecutionContext 流转
        /// </summary>
        private readonly AsyncLocal<T> _innerAsyncLocal = new AsyncLocal<T>();

        /// <summary>
        /// 获取或设置当前 ExecutionContext 关联的值
        /// </summary>
        public T Value
        {
            get => _innerAsyncLocal.Value;
            set => _innerAsyncLocal.Value = value;
        }

        /// <summary>
        /// 移除当前 ExecutionContext 关联的值
        /// 注意：AsyncLocal 没有 Remove 方法，这里通过设置为 default(T) 来"清除"
        /// </summary>
        public void Remove()
        {
            _innerAsyncLocal.Value = default(T);
        }
    }
}