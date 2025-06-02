using System.Diagnostics;
using System.Runtime.CompilerServices;
 
namespace SimpleTest;
 
public class Downloader
{
    private sealed class FetchDataAsyncStateMachine : IAsyncStateMachine
    {
        // 当前状态（-1: 初始状态，0: 挂起状态，-2: 完成状态）
        public int state;
 
        // 异步任务的构建器，用于管理任务的生命周期
        public AsyncTaskMethodBuilder<string> taskBuilder;
 
        // 输入参数：目标 URL
        public string url;
 
        // 内部变量：用于 HTTP 请求的 HttpClient
        private HttpClient client;
        private string fetchedData; // 保存从 URL 获取的数据
        private TaskAwaiter<string> awaiter; // 用于管理 GetStringAsync 的等待状态
 
        public void MoveNext()
        {
            int currentState = state; // 保存当前状态
            string result;
 
            try
            {
                if (currentState != 0) // 状态为初始状态
                {
                    client = new HttpClient(); // 创建 HttpClient 实例
                }
 
                try
                {
                    TaskAwaiter<string> taskAwaiter;
 
                    if (currentState != 0) // 状态为初始状态
                    {
                        // 开始异步操作，获取 URL 的内容
                        taskAwaiter = client.GetStringAsync(url).GetAwaiter();
 
                        // 如果异步操作未完成，挂起当前状态机
                        if (!taskAwaiter.IsCompleted)
                        {
                            state = 0; // 设置状态为挂起状态
                            awaiter = taskAwaiter; // 保存当前的 TaskAwaiter
                            FetchDataAsyncStateMachine stateMachine = this;
 
                            // 将状态机挂起，等待异步操作完成后继续
                            taskBuilder.AwaitUnsafeOnCompleted(ref taskAwaiter, ref stateMachine);
                            return; // 返回以挂起当前逻辑
                        }
                    }
                    else // 从挂起状态恢复执行
                    {
                        taskAwaiter = awaiter; // 恢复挂起时保存的 TaskAwaiter
                        awaiter = default; // 清空挂起状态
                        state = -1; // 设置状态为已恢复
                    }
 
                    // 获取异步操作的结果
                    fetchedData = taskAwaiter.GetResult();
                    result = fetchedData;
                }
                finally
                {
                    // 在操作完成后释放 HttpClient 资源
                    if (state < 0 && client != null)
                    {
                        client.Dispose();
                    }
                }
            }
            catch (Exception exception)
            {
                // 异常处理：设置状态为完成并报告异常
                state = -2;
                client = null;
                taskBuilder.SetException(exception);
                return;
            }
 
            // 设置状态为完成并返回结果
            state = -2;
            client = null;
            taskBuilder.SetResult(result);
        }
 
        // 必须实现的接口方法，当前示例中未使用
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    }
 
    private sealed class SaveDataAsyncStateMachine : IAsyncStateMachine
    {
        // 当前状态（-1: 初始状态，0: 挂起状态，-2: 完成状态）
        public int state;
 
        // 异步任务的构建器，用于管理任务的生命周期
        public AsyncTaskMethodBuilder taskBuilder;
 
        // 输入参数：要写入文件的 HTML 数据
        public string html;
 
        // 内部变量：管理 WriteAllTextAsync 的等待状态
        private TaskAwaiter awaiter;
 
        public void MoveNext()
        {
            int currentState = state; // 保存当前状态
 
            try
            {
                TaskAwaiter taskAwaiter;
 
                if (currentState != 0) // 初始状态
                {
                    // 开始异步写入操作
                    taskAwaiter = File.WriteAllTextAsync("index.html", html).GetAwaiter();
 
                    // 如果写入操作未完成，挂起当前状态机
                    if (!taskAwaiter.IsCompleted)
                    {
                        state = 0; // 设置状态为挂起状态
                        awaiter = taskAwaiter; // 保存当前的 TaskAwaiter
                        SaveDataAsyncStateMachine stateMachine = this;
 
                        // 将状态机挂起，等待写入操作完成后继续
                        taskBuilder.AwaitUnsafeOnCompleted(ref taskAwaiter, ref stateMachine);
                        return; // 返回以挂起当前逻辑
                    }
                }
                else // 从挂起状态恢复执行
                {
                    taskAwaiter = awaiter; // 恢复挂起时保存的 TaskAwaiter
                    awaiter = default; // 清空挂起状态
                    state = -1; // 设置状态为已恢复
                }
 
                // 获取异步操作的结果（此处无返回值，单纯确保无异常）
                taskAwaiter.GetResult();
            }
            catch (Exception exception)
            {
                // 异常处理：设置状态为完成并报告异常
                state = -2;
                taskBuilder.SetException(exception);
                return;
            }
 
            // 设置状态为完成
            state = -2;
            taskBuilder.SetResult();
        }
 
        // 必须实现的接口方法，当前示例中未使用
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    }
 
    private sealed class SaveDataFromUrlAsyncStateMachine : IAsyncStateMachine
    {
        // 当前状态（-1: 初始状态，0,1,2: 挂起状态，-2: 完成状态）
        public int state;
        public AsyncTaskMethodBuilder taskBuilder;
 
        // 内部变量
        private string url; // 请求的 URL
        private string html; // 获取的 HTML 数据
        private string processedHtml; // 处理后的 HTML 数据
 
        // 临时变量
        private string tempHtmlResult;
        private string tempProcessedResult;
 
        // 用于管理多个异步操作的 Awaiter
        private TaskAwaiter<string> awaiter3; // 对应 FetchDataAsync
        private TaskAwaiter<string> awaiter2; // 对应 ProcessDataAsync
        private TaskAwaiter awaiter; // 对应 SaveDataAsync
 
        public void MoveNext()
        {
            int currentState = state; // 保存当前状态机的状态。初始值为 -1，表示尚未开始执行。
 
            try
            {
                TaskAwaiter<string> stringTaskAwaiter; // 用于管理异步操作 `FetchDataAsync` 和 `ProcessDataAsync` 的结果。
                TaskAwaiter simpleAwaiter; // 用于管理异步操作 `SaveDataAsync` 的结果。
 
                // 根据当前状态执行不同的逻辑。
                switch (currentState)
                {
                    default: // 初始状态（state = -1）
                        url = "https://www.baidu.com"; // 初始化 URL 变量，表示目标地址。
 
                        // 调用 FetchDataAsync 方法以获取 HTML 数据，并获取其 Awaiter。
                        stringTaskAwaiter = FetchDataAsync(url).GetAwaiter();
                        if (!stringTaskAwaiter.IsCompleted) // 如果异步操作尚未完成，则需要挂起状态机。
                        {
                            state = 0; // 将状态设置为 0，表示挂起点在 FetchDataAsync 处。
                            awaiter3 = stringTaskAwaiter; // 保存当前的 Awaiter（对应 FetchDataAsync）。
                            SaveDataFromUrlAsyncStateMachine stateMachine = this; // 保存当前状态机实例。
 
                            // 挂起状态机，并在异步操作完成后恢复执行。
                            taskBuilder.AwaitUnsafeOnCompleted(ref stringTaskAwaiter, ref stateMachine);
                            return; // 退出方法，等待异步操作完成时重新进入。
                        }
                        goto Case_FetchCompleted; // 如果异步操作已完成，直接跳转到 FetchDataAsync 完成后的逻辑。
 
                    case 0: // 从 FetchDataAsync 挂起点恢复
                        stringTaskAwaiter = awaiter3; // 恢复之前保存的 Awaiter。
                        awaiter3 = default; // 清除 Awaiter 的引用。
                        state = -1; // 重置状态为 -1，表示状态机当前未挂起。
                        goto Case_FetchCompleted; // 跳转到 FetchDataAsync 完成后的逻辑。
 
                    case 1: // 从 ProcessDataAsync 挂起点恢复
                        stringTaskAwaiter = awaiter2; // 恢复之前保存的 Awaiter。
                        awaiter2 = default; // 清除 Awaiter 的引用。
                        state = -1; // 重置状态为 -1，表示状态机当前未挂起。
                        goto Case_ProcessCompleted; // 跳转到 ProcessDataAsync 完成后的逻辑。
 
                    case 2: // 从 SaveDataAsync 挂起点恢复
                        simpleAwaiter = awaiter; // 恢复之前保存的 Awaiter。
                        awaiter = default; // 清除 Awaiter 的引用。
                        state = -1; // 重置状态为 -1，表示状态机当前未挂起。
                        break;
 
                    Case_FetchCompleted: // FetchDataAsync 操作完成，处理结果
                        tempHtmlResult = stringTaskAwaiter.GetResult(); // 获取 FetchDataAsync 的返回结果（HTML 数据）。
                        html = tempHtmlResult; // 将结果赋值给 html 变量。
                        tempHtmlResult = null; // 清理临时变量。
 
                        // 调用 ProcessDataAsync 方法以处理 HTML 数据，并获取其 Awaiter。
                        stringTaskAwaiter = ProcessDataAsync(html).GetAwaiter();
                        if (!stringTaskAwaiter.IsCompleted) // 如果异步操作尚未完成，则需要挂起状态机。
                        {
                            state = 1; // 将状态设置为 1，表示挂起点在 ProcessDataAsync 处。
                            awaiter2 = stringTaskAwaiter; // 保存当前的 Awaiter（对应 ProcessDataAsync）。
                            SaveDataFromUrlAsyncStateMachine stateMachine = this; // 保存当前状态机实例。
 
                            // 挂起状态机，并在异步操作完成后恢复执行。
                            taskBuilder.AwaitUnsafeOnCompleted(ref stringTaskAwaiter, ref stateMachine);
                            return; // 退出方法，等待异步操作完成时重新进入。
                        }
                        goto Case_ProcessCompleted; // 如果异步操作已完成，直接跳转到 ProcessDataAsync 完成后的逻辑。
 
                    Case_ProcessCompleted: // ProcessDataAsync 操作完成，处理结果
                        tempProcessedResult = stringTaskAwaiter.GetResult(); // 获取 ProcessDataAsync 的返回结果（处理后的 HTML 数据）。
                        processedHtml = tempProcessedResult; // 将结果赋值给 processedHtml 变量。
                        tempProcessedResult = null; // 清理临时变量。
 
                        // 调用 SaveDataAsync 方法以保存处理后的数据，并获取其 Awaiter。
                        simpleAwaiter = SaveDataAsync(processedHtml).GetAwaiter();
                        if (!simpleAwaiter.IsCompleted) // 如果异步操作尚未完成，则需要挂起状态机。
                        {
                            state = 2; // 将状态设置为 2，表示挂起点在 SaveDataAsync 处。
                            awaiter = simpleAwaiter; // 保存当前的 Awaiter（对应 SaveDataAsync）。
                            SaveDataFromUrlAsyncStateMachine stateMachine = this; // 保存当前状态机实例。
 
                            // 挂起状态机，并在异步操作完成后恢复执行。
                            taskBuilder.AwaitUnsafeOnCompleted(ref simpleAwaiter, ref stateMachine);
                            return; // 退出方法，等待异步操作完成时重新进入。
                        }
                        break; // 如果异步操作已完成，直接执行 SaveDataAsync 完成后的逻辑。
                }
 
                // SaveDataAsync 操作完成，确保任务成功结束
                simpleAwaiter.GetResult(); // 调用 GetResult 确保 SaveDataAsync 没有抛出异常。
            }
            catch (Exception exception) // 捕获异步操作中可能抛出的任何异常
            {
                state = -2; // 将状态机的状态设置为 -2，表示已完成且发生异常。
                taskBuilder.SetException(exception); // 将捕获的异常传递给 TaskBuilder，通知调用方任务失败。
                return; // 退出方法，状态机终止。
            }
 
            // 异步任务成功完成
            state = -2; // 将状态机的状态设置为 -2，表示已完成且没有异常。
            taskBuilder.SetResult(); // 标记任务完成并通知调用方。
        }
 
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    }
 
    private static Task<string> FetchDataAsync(string url) // 异步方法，接收一个 URL 参数，返回一个包含字符串结果的任务。
    {
        var stateMachine = new FetchDataAsyncStateMachine // 创建 FetchDataAsyncStateMachine 状态机实例。
        {
            taskBuilder = AsyncTaskMethodBuilder<string>.Create(), // 初始化 TaskBuilder，用于构建异步任务。
            url = url, // 将调用方传入的 URL 参数赋值到状态机中。
            state = -1 // 初始化状态为 -1，表示状态机尚未开始执行。
        };
 
        stateMachine.taskBuilder.Start(ref stateMachine); // 启动状态机，开始执行其 MoveNext 方法。
        return stateMachine.taskBuilder.Task; // 返回由 TaskBuilder 创建的任务，供调用方等待异步操作完成。
    }
 
    private static Task<string> ProcessDataAsync(string html) // 异步方法，接收 HTML 字符串，返回处理后的字符串任务。
    {
        return Task.Run(() => // 使用 Task.Run 在线程池中执行同步代码，模拟异步操作。
        {
            Thread.Sleep(1000); // 模拟耗时操作，例如数据处理或计算。
            return html.ToUpper(); // 将输入 HTML 转换为大写后返回。
        });
    }
 
    private static Task SaveDataAsync(string html) // 异步方法，接收 HTML 字符串，返回一个任务。
    {
        var stateMachine = new SaveDataAsyncStateMachine // 创建 SaveDataAsyncStateMachine 状态机实例。
        {
            taskBuilder = AsyncTaskMethodBuilder.Create(), // 初始化 TaskBuilder，用于构建不返回值的异步任务。
            html = html, // 将调用方传入的 HTML 参数赋值到状态机中。
            state = -1 // 初始化状态为 -1，表示状态机尚未开始执行。
        };
 
        stateMachine.taskBuilder.Start(ref stateMachine); // 启动状态机，开始执行其 MoveNext 方法。
        return stateMachine.taskBuilder.Task; // 返回由 TaskBuilder 创建的任务，供调用方等待异步操作完成。
    }
 
    public static Task SaveDataFromUrlAsync() // 异步方法，无参数，返回一个任务。
    {
        var stateMachine = new SaveDataFromUrlAsyncStateMachine // 创建 SaveDataFromUrlAsyncStateMachine 状态机实例。
        {
            taskBuilder = AsyncTaskMethodBuilder.Create(), // 初始化 TaskBuilder，用于构建不返回值的异步任务。
            state = -1 // 初始化状态为 -1，表示状态机尚未开始执行。
        };
 
        stateMachine.taskBuilder.Start(ref stateMachine); // 启动状态机，开始执行其 MoveNext 方法。
        return stateMachine.taskBuilder.Task; // 返回由 TaskBuilder 创建的任务，供调用方等待异步操作完成。
    }
}