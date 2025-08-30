using Xunit;

namespace RedSharpNano.Tests
{
    [Collection("Redis collection")]
    public class RedSharpNanoSortedSetTests : RedSharpNanoBaseTests
    {
        [Fact]
        public async Task Should_WorkWithSortedSets_AddRangeWithScoresAndQuery()
        {
            var z = GetId();
            var added = await Client.CallAsync("ZADD", z, "1", "a", "2", "b");
            Assert.Equal("2", added);

            var card = await Client.CallAsync("ZCARD", z);
            Assert.Equal("2", card);

            var withScores = (object[])await Client.CallAsync("ZRANGE", z, "0", "-1", "WITHSCORES");
            Assert.Equal(4, withScores.Length);
            Assert.Equal("a", withScores[0]); Assert.Equal("1", withScores[1]);
            Assert.Equal("b", withScores[2]); Assert.Equal("2", withScores[3]);

            var scoreA = await Client.CallAsync("ZSCORE", z, "a");
            Assert.Equal("1", scoreA);

            var rem = await Client.CallAsync("ZREM", z, "a");
            Assert.Equal("1", rem);
            var card2 = await Client.CallAsync("ZCARD", z);
            Assert.Equal("1", card2);
        }

        [Fact]
        public async Task Should_IncrementScoreAndCountRanges()
        {
            var z = GetId();
            await Client.CallAsync("ZADD", z, "1", "a", "2", "b", "3", "c");
            Assert.Equal("3", await Client.CallAsync("ZCOUNT", z, "1", "3"));

            var newScore = await Client.CallAsync("ZINCRBY", z, "2.5", "a");
            Assert.Equal("3.5", newScore);

            var byScore = (object[])await Client.CallAsync("ZRANGEBYSCORE", z, "3", "+inf");
            Assert.True(byScore.Cast<string>().Contains("a"));
        }

        [Fact]
        public async Task Should_RemoveByScoreRange()
        {
            var z = GetId();
            await Client.CallAsync("ZADD", z, "1", "a", "2", "b", "5", "c");
            var removed = await Client.CallAsync("ZREMRANGEBYSCORE", z, "2", "5");
            Assert.Equal("2", removed);
            Assert.Equal("1", await Client.CallAsync("ZCARD", z));
        }

        [Fact]
        public async Task Should_ZScanAllMembers()
        {
            var z = GetId();
            await Client.CallAsync("ZADD", z, "1", "a", "2", "b", "3", "c");

            var seen = new HashSet<string>(); var cursor = "0";
            do
            {
                var resp = (object[])await Client.CallAsync("ZSCAN", z, cursor, "COUNT", "2");
                cursor = (string)resp[0];
                var kvs = (object[])resp[1];
                for (int i = 0; i + 1 < kvs.Length; i += 2)
                    seen.Add((string)kvs[i]); // member name
            } while (cursor != "0");

            Assert.True(new[] { "a", "b", "c" }.All(seen.Contains));
        }
    }
}