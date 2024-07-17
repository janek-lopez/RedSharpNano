namespace RedSharpNano.Demo.DistributedLock;
class Program
{
    const int LockTimeout = 5;
    const int TaskTime = 1000;
    const int RetryDelay = 2000;

    static async Task Main() =>
        await Task.WhenAll(Enumerable.Range(1, 6).Select(i => PerformTaskWithLockAsync(new Resp2Client(), $"Task{i}")));

    static async Task PerformTaskWithLockAsync(Resp2Client client, string taskName)
    {
        string lockKey = "my_lock_key", lockValue = Guid.NewGuid().ToString();
        while (true)
        {
            if (await client.CallAsync("SET", lockKey, lockValue, "NX", "EX", LockTimeout.ToString()) is "OK")
            {
                try
                {
                    Console.WriteLine($"{taskName} acquired the lock.");
                    await Task.Delay(1000);
                    Console.WriteLine($"{taskName} completed its task.");
                }
                finally
                {
                    await client.CallAsync("EVAL", @"if redis.call('get',KEYS[1])==ARGV[1] then return redis.call('del',KEYS[1]) else return 0 end", "1", lockKey, lockValue);
                }
                break;
            }
            Console.WriteLine($"{taskName} could not acquire the lock, retrying...");
            await Task.Delay(RetryDelay);
        }
    }
}