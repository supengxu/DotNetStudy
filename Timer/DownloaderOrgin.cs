// 下载器
public class Downloader
{
    // 异步方法拉取数据
    static async Task<string> FetchDataAsync(string url)
    {
        using var client = new HttpClient();
        return await client.GetStringAsync(url); // 网卡 I/O 异步获取数据
    }
 
    // 处理数据，虽然返回Task<T>，但是是同步方法，只不过方法启动并返回了一个Task对象，并不执行任何异步操作（await ...）
    static Task<string> ProcessDataAsyncAsync(string html)
    {
        return Task.Run(() =>
        {
            Thread.Sleep(1000);// 阻塞线程线程池线程1s，模拟 CPU 密集耗时操作
            return html.ToUpper(); // 示例处理逻辑
        });
    }
 
    // 异步保存数据
    static async Task SaveDataAsync(string html)
    {
        await File.WriteAllTextAsync("index.html", html); // 磁盘 I/O 异步保存
    }
 
    // 异步从url拉取数据、处理、保存数据
    public static async Task SaveDataFromUrlAsync()
    {
        string url = "https://www.baidu.com";
        string html = await FetchDataAsync(url);// 拉取数据
        string processedHtml = await ProcessDataAsyncAsync(html);// 处理数据
        await SaveDataAsync(processedHtml);// 保存数据
    }
}