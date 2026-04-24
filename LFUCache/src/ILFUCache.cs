namespace LFUCache;

/// <summary>
/// LFU (Least Frequently Used) 缓存接口
/// </summary>
/// <typeparam name="TKey">键类型</typeparam>
/// <typeparam name="TValue">值类型</typeparam>
public interface ILFUCache<TKey, TValue> where TKey : notnull
{
    /// <summary>
    /// 缓存容量
    /// </summary>
    int Capacity { get; }
    
    /// <summary>
    /// 当前缓存中的元素数量
    /// </summary>
    int Count { get; }
    
    /// <summary>
    /// 获取指定键的值，如果不存在则返回 null
    /// </summary>
    TValue? Get(TKey key);
    
    /// <summary>
    /// 添加或更新键值对
    /// </summary>
    void Put(TKey key, TValue value);
    
    /// <summary>
    /// 移除指定键的元素
    /// </summary>
    bool Remove(TKey key);
    
    /// <summary>
    /// 清空缓存
    /// </summary>
    void Clear();
}