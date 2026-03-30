using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ThreadLocal;

public class ExampleScenarios
{
    public static void ThreadIsolationDemo()
    {
        Console.WriteLine("=== 线程隔离演示 ===");
        Console.WriteLine();

        SimpleThreadLocal<int> threadLocal = new SimpleThreadLocal<int>(() => 0);

        List<Thread> threads = new List<Thread>();

        int threadCount = 5;
        Console.WriteLine($"启动 {threadCount} 个线程进行线程隔离测试...\n");

        for (int i = 1; i <= threadCount; i++)
        {
            int threadNum = i;

            Thread thread = new Thread(() =>
            {
                int threadId = Thread.CurrentThread.ManagedThreadId;

                int setValue = threadNum * 100;
                threadLocal.Set(setValue);

                int getValue = threadLocal.Get();

                Console.WriteLine($"Thread {threadId}: Set={setValue}, Get={getValue}");

                if (setValue != getValue)
                {
                    Console.WriteLine($"  [错误] 线程隔离失效！预期: {setValue}, 实际: {getValue}");
                }
                else
                {
                    Console.WriteLine($"  [验证通过] 线程 {threadId} 独立存储工作正常");
                }
            });

            threads.Add(thread);
            thread.Start();
        }

        foreach (Thread thread in threads)
        {
            thread.Join();
        }

        Console.WriteLine();
        Console.WriteLine("=== 线程隔离演示结束 ===");
        Console.WriteLine("结论：每个线程都有独立的变量副本，线程间互不干扰");
    }

    public static void ThreadPoolReuseDemo()
    {
        Console.WriteLine("\n========== ThreadPool 复用污染演示 ==========");
        
        var userContext = new SimpleThreadLocal<string>(() => string.Empty);
        
        using var firstRequestDone = new ManualResetEvent(false);
        using var secondRequestDone = new ManualResetEvent(false);
        
        // 请求 A：设置用户上下文，但不清理
        ThreadPool.QueueUserWorkItem(_ =>
        {
            userContext.Set("user1");
            Console.WriteLine("Request 1: Set value = user1");
            
            Thread.Sleep(100);
            
            // 没有调用 Remove() - 线程池复用此线程时，数据会泄漏
            firstRequestDone.Set();
        });
        
        firstRequestDone.WaitOne();
        Thread.Sleep(50);
        
        // 请求 B：获取用户上下文（可能看到污染）
        ThreadPool.QueueUserWorkItem(_ =>
        {
            string? currentUser = userContext.Get();
            
            if (!string.IsNullOrEmpty(currentUser))
            {
                Console.WriteLine($"Request 2: sees previous value = {currentUser} [污染警告！]");
            }
            else
            {
                Console.WriteLine("Request 2: sees empty value [未发生污染]");
            }
            
            secondRequestDone.Set();
        });
        
        secondRequestDone.WaitOne();
        
        // 正确做法演示
        Console.WriteLine("\n--- 正确做法：使用 try-finally 确保清理 ---");
        DemonstrateTryFinallyCleanup();
        
        Console.WriteLine("========== 演示结束 ==========\n");
    }
    
    private static void DemonstrateTryFinallyCleanup()
    {
        var userContext = new SimpleThreadLocal<string>(() => string.Empty);
        
        using var done = new ManualResetEvent(false);
        
        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                userContext.Set("user2");
                Console.WriteLine("正确做法: Set value = user2");
                
                Thread.Sleep(50);
            }
            finally
            {
                userContext.Remove();
                Console.WriteLine("正确做法: Removed value (finally block)");
            }
            
            done.Set();
        });
        
        done.WaitOne();
        
        using var verifyDone = new ManualResetEvent(false);
        ThreadPool.QueueUserWorkItem(_ =>
        {
            string? value = userContext.Get();
            Console.WriteLine($"正确做法验证: Next request sees = '{(string.IsNullOrEmpty(value) ? "(empty)" : value)}'");
            verifyDone.Set();
        });
        verifyDone.WaitOne();
    }

    public static void ThreadLocalComparisonDemo()
    {
        Console.WriteLine("\n========== ThreadLocal 对比演示 ==========");
        Console.WriteLine("对比：SimpleThreadLocal vs .NET 内置 ThreadLocal<T>");
        Console.WriteLine();

        // ========================================
        // 第一部分：SimpleThreadLocal 演示
        // ========================================
        Console.WriteLine("【SimpleThreadLocal 实现】");
        Console.WriteLine("使用 Dictionary + lock 实现线程隔离");
        Console.WriteLine();

        SimpleThreadLocal<int> simple = new SimpleThreadLocal<int>(() => 0);

        List<Thread> simpleThreads = new List<Thread>();
        int[] simpleValues = { 100, 200, 300 };

        for (int i = 0; i < 3; i++)
        {
            int index = i;
            Thread t = new Thread(() =>
            {
                int threadId = Thread.CurrentThread.ManagedThreadId;
                simple.Set(simpleValues[index]);
                Console.WriteLine($"  Thread {threadId}: Set({simpleValues[index]}) -> Get() = {simple.Get()}");
            });
            simpleThreads.Add(t);
            t.Start();
        }

        foreach (Thread t in simpleThreads) t.Join();

        // ========================================
        // 第二部分：.NET 内置 ThreadLocal 演示
        // ========================================
        Console.WriteLine();
        Console.WriteLine("【.NET 内置 ThreadLocal<T>】");
        Console.WriteLine("使用 .NET Framework 4.0+ 内置的 ThreadLocal<T> 类");
        Console.WriteLine();

        // .NET 内置 ThreadLocal<T>
        // 构造函数接受 Func<T> 工厂方法，用于初始化每个线程的初始值
        ThreadLocal<int> dotNet = new ThreadLocal<int>(() => 0);

        List<Thread> dotNetThreads = new List<Thread>();
        int[] dotNetValues = { 111, 222, 333 };

        for (int i = 0; i < 3; i++)
        {
            int index = i;
            Thread t = new Thread(() =>
            {
                int threadId = Thread.CurrentThread.ManagedThreadId;
                dotNet.Value = dotNetValues[index];  // 使用 .Value 属性
                Console.WriteLine($"  Thread {threadId}: Set({dotNetValues[index]}) -> Value = {dotNet.Value}");
            });
            dotNetThreads.Add(t);
            t.Start();
        }

        foreach (Thread t in dotNetThreads) t.Join();

        // ========================================
        // 第三部分：Remove 演示
        // ========================================
        Console.WriteLine();
        Console.WriteLine("【Remove 演示】");

        // SimpleThreadLocal 的 Remove
        var simpleRemove = new SimpleThreadLocal<int>(() => 0);
        Thread removeThread1 = new Thread(() =>
        {
            simpleRemove.Set(999);
            Console.WriteLine($"  SimpleThreadLocal: Set(999) -> Get() = {simpleRemove.Get()}");
            simpleRemove.Remove();
            Console.WriteLine($"  SimpleThreadLocal: Remove 后 Get() = {simpleRemove.Get()} (重新创建初始值)");
        });
        removeThread1.Start();
        removeThread1.Join();

        // .NET ThreadLocal 的 Remove - 通过 IsValueCreated 判断
        ThreadLocal<int> dotNetRemove = new ThreadLocal<int>(() => 0);
        Thread removeThread2 = new Thread(() =>
        {
            dotNetRemove.Value = 888;
            Console.WriteLine($"  ThreadLocal<T>: Set(888) -> Value = {dotNetRemove.Value}");
            // 注意：ThreadLocal<T> 没有 Remove 方法，需要使用 Dispose
            Console.WriteLine($"  ThreadLocal<T>: 调用 Dispose() 清理资源");
            dotNetRemove.Dispose();
        });
        removeThread2.Start();
        removeThread2.Join();

        // ========================================
        // 总结：内置 ThreadLocal 优势
        // ========================================
        Console.WriteLine();
        Console.WriteLine("========== 对比总结 ==========");
        Console.WriteLine();
        Console.WriteLine("【.NET 内置 ThreadLocal<T> 优势】");
        Console.WriteLine("  1. 性能优化：使用 Thread 静态存储（TLS），无需 Dictionary 查找");
        Console.WriteLine("  2. 线程安全：底层 TLS 是线程安全的，无锁设计");
        Console.WriteLine("  3. Dispose 支持：实现 IDisposable，显式释放资源");
        Console.WriteLine("  4. IsValueCreated：可查询值是否已初始化");
        Console.WriteLine("  5. .NET 原生支持：有完善的文档和社区支持");
        Console.WriteLine();
        Console.WriteLine("【SimpleThreadLocal 适用场景】");
        Console.WriteLine("  - 学习理解 ThreadLocal 原理");
        Console.WriteLine("  - 需要自定义存储逻辑的特例");
        Console.WriteLine("  - 旧版 .NET Framework（4.0 以下）兼容");
        Console.WriteLine();
        Console.WriteLine("========== 演示结束 ==========\n");
    }

    public static void AsyncLocalComparisonDemo()
    {
        Console.WriteLine("\n========== AsyncLocal<T> 流转对比演示 ==========");
        Console.WriteLine();
        Console.WriteLine("原理说明：AsyncLocal<T> 基于 ExecutionContext 进行值流转，");
        Console.WriteLine("          ExecutionContext 会跨 async/await 边界传递");
        Console.WriteLine("          而 ThreadLocal<T> 仅绑定到线程，await 切换线程后值丢失");
        Console.WriteLine();

        // 创建 AsyncLocal<string> 实例
        // AsyncLocal<T> 特点：值会随 ExecutionContext 跨 async/await 流转
        AsyncLocal<string> asyncLocal = new AsyncLocal<string>();

        // 创建 ThreadLocal<string> 实例用于对比
        // ThreadLocal<T> 特点：值仅绑定到当前线程，线程切换后丢失
        ThreadLocal<string> threadLocal = new ThreadLocal<string>(() => string.Empty);

        // 调用异步方法执行对比测试
        RunAsyncFlowComparison(asyncLocal, threadLocal).Wait();

        Console.WriteLine();
        Console.WriteLine("========== 演示结束 ==========\n");
    }

    private static async Task RunAsyncFlowComparison(
        AsyncLocal<string> asyncLocal, 
        ThreadLocal<string> threadLocal)
    {
        // ========== AsyncLocal 演示 ==========
        asyncLocal.Value = "AsyncLocal_Value";
        Console.WriteLine("【AsyncLocal 测试】");

        string beforeAwaitAsyncLocal = asyncLocal.Value;
        Console.WriteLine($"  Before await: {beforeAwaitAsyncLocal} (Thread {Thread.CurrentThread.ManagedThreadId})");

        // await 会挂起当前方法，释放线程，ExecutionContext 会流转到恢复后的执行
        await Task.Delay(100);

        string afterAwaitAsyncLocal = asyncLocal.Value;
        Console.WriteLine($"  After await: {afterAwaitAsyncLocal} (Thread {Thread.CurrentThread.ManagedThreadId}, preserved)");
        Console.WriteLine($"  结果: {(beforeAwaitAsyncLocal == afterAwaitAsyncLocal ? "✓ 值已保留" : "✗ 值丢失")}");
        Console.WriteLine();

        // ========== ThreadLocal 演示 ==========
        // 关键：ThreadPool 线程复用是不可预测的！
        // - 如果是同一线程：值保留（污染问题！）
        // - 如果是不同线程：值丢失（预期行为）
        // 这正是 ThreadLocal 的"陷阱" - 依赖线程池行为，不可靠
        
        // 多次测试，展示不可预测性
        Console.WriteLine("【ThreadLocal 测试 - 多次运行观察不可预测性】");
        Console.WriteLine("  运行 3 次测试，观察 ThreadPool 线程复用行为...\n");
        
        for (int i = 0; i < 3; i++)
        {
            using var setDone = new ManualResetEvent(false);
            using var getDone = new ManualResetEvent(false);
            
            int setThreadId = 0;
            int getThreadId = 0;
            
            // 设置值
            ThreadPool.QueueUserWorkItem(_ =>
            {
                setThreadId = Thread.CurrentThread.ManagedThreadId;
                threadLocal.Value = "ThreadLocal_Value";
                setDone.Set();
            });
            
            setDone.WaitOne();
            
            // 等待一段时间（让 ThreadPool 有机会复用或分配新线程）
            await Task.Delay(100);
            
            // 读取值
            ThreadPool.QueueUserWorkItem(_ =>
            {
                getThreadId = Thread.CurrentThread.ManagedThreadId;
                var value = threadLocal.Value;
                getDone.Set();
            });
            
            getDone.WaitOne();
            
            bool threadSwitched = getThreadId != setThreadId;
            string result = threadSwitched ? "✓ 值已丢失（线程切换）" : "✗ 值仍存在（线程复用=污染）";
            
            Console.WriteLine($"  测试 {i + 1}: 线程 {setThreadId} -> {getThreadId} = {(threadSwitched ? "切换" : "复用")}");
            Console.WriteLine($"    结果: {result}");
        }
        
        Console.WriteLine();
        Console.WriteLine("  结论: ThreadLocal 依赖线程池行为，结果不可预测！");
        Console.WriteLine("        推荐使用 try-finally + Remove() 或 使用 AsyncLocal");
        
        // 释放 ThreadLocal 资源
        threadLocal.Dispose();
    }

    public static void SimpleAsyncLocalDemo()
    {
        Console.WriteLine("\n========== SimpleAsyncLocal 演示 ==========");
        Console.WriteLine("使用自定义 SimpleAsyncLocal 实现展示 ExecutionContext 流转");
        Console.WriteLine();

        // 创建自定义 SimpleAsyncLocal 实例
        SimpleAsyncLocal<string> simpleAsyncLocal = new SimpleAsyncLocal<string>();

        // 测试：设置值 -> await -> 验证值是否保留
        TestAsyncFlow(simpleAsyncLocal).Wait();

        Console.WriteLine("========== 演示结束 ==========\n");
    }

    private static async Task TestAsyncFlow(SimpleAsyncLocal<string> simpleAsyncLocal)
    {
        simpleAsyncLocal.Value = "SimpleAsyncLocal_Value";
        Console.WriteLine("【SimpleAsyncLocal 测试】");

        string beforeAwait = simpleAsyncLocal.Value;
        Console.WriteLine($"  Before await: {beforeAwait}");

        // 等待一段时间（模拟异步操作）
        await Task.Delay(100);

        string afterAwait = simpleAsyncLocal.Value;
        Console.WriteLine($"  After await: {afterAwait}");
        Console.WriteLine($"  结果: {(beforeAwait == afterAwait ? "✓ 值已保留（流转成功）" : "✗ 值丢失")}");
        Console.WriteLine();

        // 清理
        simpleAsyncLocal.Remove();
    }
}