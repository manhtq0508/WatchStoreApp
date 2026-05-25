using StackExchange.Redis;

namespace WatchStoreApp.Data;

public class RedisContext(IConnectionMultiplexer redis)
{
    private IDatabase Database => redis.GetDatabase();

    public async Task SetStringAsync(string key, string value, TimeSpan? expiry = null)
    {
        await Database.StringSetAsync(key, value, expiry, When.Always);
    }
    
    public async Task<string?> GetStringAsync(string key)
    {
        return await Database.StringGetAsync(key);
    }
    
    public async Task<bool> DeleteKeyAsync(string key)
    {
        return await Database.KeyDeleteAsync(key);
    }
}