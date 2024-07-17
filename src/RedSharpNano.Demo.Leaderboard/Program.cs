namespace RedSharpNano.Demo.Leaderboard;

class Program
{
    static async Task Main()
    {
        using var client = new Resp2Client();

        await client.CallAsync("DEL", "leaderboard");

        Console.WriteLine("Leaderboard cleared. Starting new game.\n");

        await Task.WhenAll(Enumerable.Range(1, 5).Select(i => SimulatePlayer(new Resp2Client(), $"Player{i}")));
        await DisplayTopPlayers(client);
    }

    static async Task SimulatePlayer(Resp2Client client, string playerName)
    {
        var random = new Random();
        for (int i = 0; i < 3; i++)
        {
            int score = random.Next(1, 100);
            await client.CallAsync("ZINCRBY", "leaderboard", score.ToString(), playerName);
            Console.WriteLine($"{playerName} scored {score} points.");
            await Task.Delay(500);
        }

        var results = await client.PipelineAsync(async c =>
        {
            await c.CallAsync("ZREVRANK", "leaderboard", playerName);
            await c.CallAsync("ZCARD", "leaderboard");
        });

        var rank = results[0] != null ? int.Parse(results[0].ToString()) : -1;
        var totalPlayers = results[1] != null ? int.Parse(results[1].ToString()) : 0;

        Console.WriteLine($"{playerName} finished at rank {rank + 1} out of {totalPlayers}.");
    }

    static async Task DisplayTopPlayers(Resp2Client client)
    {
        var topScores = await client.CallAsync("ZREVRANGE", "leaderboard", "0", "2", "WITHSCORES") as object[];
        Console.WriteLine("\nTop 3 Players:");
        for (int i = 0; i < topScores.Length; i += 2)
        {
            Console.WriteLine($"{topScores[i]}: {topScores[i + 1]} points");
        }
    }
}