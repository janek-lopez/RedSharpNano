using RedSharpNano;
namespace RedisClientDemo;

class Program
{
    static async Task Main(string[] args)
    {

        var tasks = new List<Task>();

        for (int i = 1; i <= 6; i++)
        {
            int taskId = i;

            var task = Task.Run(() =>
            {
                PerformTaskWithLock(new Resp2Client(), $"Task{taskId}");
            });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        Console.WriteLine("All tasks completed.");
    }

    static void PerformTaskWithLock(Resp2Client client, string taskName)
    {
        string lockKey = "my_lock_key";
        string lockValue = Guid.NewGuid().ToString();
        int lockTimeout = 5;
        int retryDelay = 3000;

        while (true)
        {
            if (AcquireLock(client, lockKey, lockValue, lockTimeout))
            {
                try
                {
                    Console.WriteLine($"{taskName} acquired the lock.");
                    PerformTask(taskName);
                }
                finally
                {
                    ReleaseLock(client, lockKey, lockValue);
                }
                break;
            }

            Console.WriteLine($"{taskName} could not acquire the lock, retrying...");
            Task.Delay(retryDelay).Wait();
        }
    }

    static bool AcquireLock(Resp2Client client, string key, string value, int timeout)
    {
        var response = client.Call("SET", key, value, "NX", "EX", timeout.ToString());
        return response?.ToString() == "OK";
    }

    static void ReleaseLock(Resp2Client client, string key, string value)
    {
        //https://redis.io/commands/set/
        string script = @"
                if redis.call('get', KEYS[1]) == ARGV[1] then
                    return redis.call('del', KEYS[1])
                else
                    return 0
                end";
        client.Call("EVAL", script, "1", key, value);
    }

    static void PerformTask(string taskName)
    {
        Console.WriteLine($"{taskName} is performing a task...");
        Thread.Sleep(2000);
        Console.WriteLine($"{taskName} has completed its task.");
    }
}
