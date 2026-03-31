// ThreadLocal 示例程序主入口
// 本程序演示 .NET 中 ThreadLocal<T> 的各种使用场景

using System;
using ThreadLocal;

namespace ThreadLocalDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== ThreadLocal<T> 示例程序 ===");
            Console.WriteLine();

            // 演示1：线程隔离 - 展示 ThreadLocal 如何在不同线程间保持独立数据
            Console.WriteLine("\n=== Demo 1: 线程隔离 ===");
            ExampleScenarios.ThreadIsolationDemo();

            // 演示2：ThreadPool 复用污染问题 - 展示线程池复用导致的潜在问题
            Console.WriteLine("\n=== Demo 2: 线程池复用污染 ===");
            ExampleScenarios.ThreadPoolReuseDemo();

            // 演示3：ThreadLocal vs 普通变量的性能对比
            Console.WriteLine("\n=== Demo 3: ThreadLocal vs 普通变量性能对比 ===");
            ExampleScenarios.ThreadLocalComparisonDemo();

            // 演示4：AsyncLocal vs ThreadLocal 的区别和应用场景
            Console.WriteLine("\n=== Demo 4: AsyncLocal vs ThreadLocal 对比 ===");
            ExampleScenarios.AsyncLocalComparisonDemo();

            // 演示5：自定义 SimpleAsyncLocal 实现
            Console.WriteLine("\n=== Demo 5: SimpleAsyncLocal 自定义实现 ===");
            ExampleScenarios.SimpleAsyncLocalDemo();

            
            ThreadLocal<string> threadLocal = new ThreadLocal<string>(() => "ThreadLocal");
            
            
            ThreadLocal<string> threadLocal2 = new ThreadLocal<string>(() => "ThreadLocal");

            threadLocal2.Value = "222";
            // threadLocal.Value = 
            Console.WriteLine();
            Console.WriteLine("=== 程序结束 ===");
        }
    }
}