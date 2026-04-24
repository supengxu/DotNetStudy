using System.Threading.Tasks;
using Xunit;

namespace LFUCache;

public class ThreadSafetyTests
{
    [Fact]
    public async Task ConcurrentPut_NoDataLoss()
    {
        var cache = new LFUCache<int, string>(10);
        var tasks = new Task[10];
        
        for (int i = 0; i < 10; i++)
        {
            int key = i;
            tasks[i] = Task.Run(() => cache.Put(key, $"value{key}"));
        }
        
        await Task.WhenAll(tasks);
        
        int expectedCount = Math.Min(10, cache.Capacity);
        Assert.Equal(expectedCount, cache.Count);
        
        for (int i = 0; i < expectedCount; i++)
        {
            var value = cache.Get(i);
            Assert.Equal($"value{i}", value);
        }
    }

    [Fact]
    public async Task ConcurrentGetPut_NoExceptions()
    {
        var cache = new LFUCache<string, int>(10);
        var exceptions = new List<Exception>();
        
        for (int i = 0; i < 10; i++)
        {
            cache.Put($"key{i}", i);
        }
        
        var putTasks = new Task[5];
        var getTasks = new Task[5];
        
        for (int i = 0; i < 5; i++)
        {
            int index = i;
            putTasks[i] = Task.Run(() =>
            {
                try
                {
                    for (int j = 0; j < 100; j++)
                    {
                        cache.Put($"newKey{index}_{j}", index + j);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });
            
            getTasks[i] = Task.Run(() =>
            {
                try
                {
                    for (int j = 0; j < 100; j++)
                    {
                        cache.Get($"key{j % 10}");
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });
        }
        
        await Task.WhenAll(putTasks);
        await Task.WhenAll(getTasks);
        
        Assert.Empty(exceptions);
    }

    [Fact]
    public async Task ConcurrentMixedOperations_ThreadSafe()
    {
        var cache = new LFUCache<int, string>(5);
        var exceptions = new List<Exception>();
        
        var putTasks = new Task[3];
        var getTasks = new Task[3];
        var removeTasks = new Task[2];
        
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            putTasks[i] = Task.Run(() =>
            {
                try
                {
                    for (int j = 0; j < 50; j++)
                    {
                        cache.Put(index * 10 + j, $"value{index}_{j}");
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });
            
            getTasks[i] = Task.Run(() =>
            {
                try
                {
                    for (int j = 0; j < 50; j++)
                    {
                        cache.Get(j % 5);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });
        }
        
        for (int i = 0; i < 2; i++)
        {
            int index = i;
            removeTasks[i] = Task.Run(() =>
            {
                try
                {
                    for (int j = 0; j < 25; j++)
                    {
                        cache.Remove(index * 10 + j);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });
        }
        
        await Task.WhenAll(putTasks);
        await Task.WhenAll(getTasks);
        await Task.WhenAll(removeTasks);
        
        Assert.Empty(exceptions);
        Assert.True(cache.Count >= 0);
        Assert.True(cache.Count <= cache.Capacity);
    }
}