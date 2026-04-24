using Xunit;

namespace LFUCache;

public class LFUCacheTests
{
    [Fact]
    public void Get_ReturnsNull_WhenKeyNotExists()
    {
        var cache = new LFUCache<string, int>(5);
        
        var result = cache.Get("nonexistent");
        
        Assert.Equal(0, result);
    }

    [Fact]
    public void PutAndGet_ReturnsValue_WhenKeyExists()
    {
        var cache = new LFUCache<string, int>(5);
        
        cache.Put("key1", 100);
        var result = cache.Get("key1");
        
        Assert.Equal(100, result);
    }

    [Fact]
    public void Put_UpdatesValue_WhenKeyExists()
    {
        var cache = new LFUCache<string, int>(5);
        
        cache.Put("key1", 100);
        cache.Put("key1", 200);
        var result = cache.Get("key1");
        
        Assert.Equal(200, result);
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void Put_EvictsLFUItem_WhenCapacityExceeded()
    {
        var cache = new LFUCache<string, int>(3);
        
        // Insert 3 items
        cache.Put("key1", 1);
        cache.Put("key2", 2);
        cache.Put("key3", 3);
        
        // Access key1 and key2 to increase their frequency
        cache.Get("key1");
        cache.Get("key2");
        
        // key3 has lowest frequency (1), should be evicted
        cache.Put("key4", 4);
        
        Assert.Equal(3, cache.Count);
        Assert.Equal(0, cache.Get("key3"));
        Assert.Equal(1, cache.Get("key1"));
        Assert.Equal(2, cache.Get("key2"));
        Assert.Equal(4, cache.Get("key4"));
    }

    [Fact]
    public void Get_IncrementsFrequency()
    {
        var cache = new LFUCache<string, int>(3);
        
        cache.Put("key1", 1);
        cache.Put("key2", 2);
        cache.Put("key3", 3);
        
        // Access key1 twice
        cache.Get("key1");
        cache.Get("key1");
        
        // key2 and key3 have frequency 1, key1 has frequency 3
        // Add new item, should evict key2 or key3 (lowest frequency)
        cache.Put("key4", 4);
        
        Assert.Equal(3, cache.Count);
        Assert.Equal(1, cache.Get("key1"));
        // Either key2 or key3 should be evicted
        var key2Exists = cache.Get("key2") != 0;
        var key3Exists = cache.Get("key3") != 0;
        Assert.False(key2Exists && key3Exists);
    }

    [Fact]
    public void Put_IncrementsFrequency()
    {
        var cache = new LFUCache<string, int>(3);
        
        cache.Put("key1", 1);
        cache.Put("key2", 2);
        cache.Put("key3", 3);
        
        // Update key1 to increase its frequency
        cache.Put("key1", 10);
        cache.Put("key1", 20);
        
        // key2 and key3 have frequency 1, key1 has frequency 3
        // Add new item, should evict key2 or key3
        cache.Put("key4", 4);
        
        Assert.Equal(3, cache.Count);
        Assert.Equal(20, cache.Get("key1"));
        // Either key2 or key3 should be evicted
        var key2Exists = cache.Get("key2") != 0;
        var key3Exists = cache.Get("key3") != 0;
        Assert.False(key2Exists && key3Exists);
    }

    [Fact]
    public void Remove_ReturnsFalse_WhenKeyNotExists()
    {
        var cache = new LFUCache<int, string>(5);
        
        var result = cache.Remove(999);
        
        Assert.False(result);
    }

    [Fact]
    public void Remove_ReturnsTrue_WhenKeyExists()
    {
        var cache = new LFUCache<int, string>(5);
        
        cache.Put(1, "value1");
        var result = cache.Remove(1);
        
        Assert.True(result);
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void Clear_RemovesAllItems()
    {
        var cache = new LFUCache<string, int>(5);
        
        cache.Put("key1", 1);
        cache.Put("key2", 2);
        cache.Put("key3", 3);
        
        cache.Clear();
        
        Assert.Equal(0, cache.Count);
        Assert.Equal(0, cache.Get("key1"));
        Assert.Equal(0, cache.Get("key2"));
        Assert.Equal(0, cache.Get("key3"));
    }

    [Fact]
    public void Count_ReturnsCorrectValue()
    {
        var cache = new LFUCache<string, int>(5);
        
        Assert.Equal(0, cache.Count);
        
        cache.Put("key1", 1);
        Assert.Equal(1, cache.Count);
        
        cache.Put("key2", 2);
        Assert.Equal(2, cache.Count);
        
        cache.Put("key1", 100); // Update existing
        Assert.Equal(2, cache.Count);
        
        cache.Remove("key1");
        Assert.Equal(1, cache.Count);
    }
}