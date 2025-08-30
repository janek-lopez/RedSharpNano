using Xunit;

namespace RedSharpNano.Tests
{
    [Collection("Redis collection")]
    public class RedSharpNanoSetTests : RedSharpNanoBaseTests
    {
        [Fact]
        public async Task Should_HandleMembershipCardinalityAndRemoval_ForSets()
        {
            var set = GetId();
            await Client.CallAsync("SADD", set, "a");
            await Client.CallAsync("SADD", set, "b");
            var card = await Client.CallAsync("SCARD", set);
            Assert.Equal("2", card);

            var m1 = await Client.CallAsync("SISMEMBER", set, "a");
            var m2 = await Client.CallAsync("SISMEMBER", set, "z");
            Assert.Equal("1", m1);
            Assert.Equal("0", m2);

            var rem = await Client.CallAsync("SREM", set, "a");
            Assert.Equal("1", rem);
            var card2 = await Client.CallAsync("SCARD", set);
            Assert.Equal("1", card2);
        }


        [Fact]
        public async Task Should_AddMembersToSet_When_SAddCommandIsUsed()
        {
            await Client.CallAsync("SADD", "myset", "member1");
            await Client.CallAsync("SADD", "myset", "member2");
            var result = await Client.CallAsync("SMEMBERS", "myset") as object[];

            Assert.Contains("member1", result);
            Assert.Contains("member2", result);
        }

        [Fact]
        public async Task Should_ComputeUnionIntersectionAndDifference()
        {
            var a = GetId(); var b = GetId();
            await Client.CallAsync("SADD", a, "1");
            await Client.CallAsync("SADD", a, "2");
            await Client.CallAsync("SADD", b, "2");
            await Client.CallAsync("SADD", b, "3");

            var sunion = ((object[])await Client.CallAsync("SUNION", a, b)).Cast<string>().ToHashSet();
            Assert.True(new[] { "1", "2", "3" }.All(sunion.Contains));

            var sinter = ((object[])await Client.CallAsync("SINTER", a, b)).Cast<string>().ToArray();
            Assert.Single(sinter); Assert.Equal("2", sinter[0]);

            var sdiff = ((object[])await Client.CallAsync("SDIFF", a, b)).Cast<string>().ToArray();
            Assert.Single(sdiff); Assert.Equal("1", sdiff[0]);
        }

        [Fact]
        public async Task Should_SScanAllMembers()
        {
            var set = GetId();
            foreach (var v in new[] { "a", "b", "c" })
                await Client.CallAsync("SADD", set, v);

            var seen = new HashSet<string>(); var cursor = "0";
            do
            {
                var resp = (object[])await Client.CallAsync("SSCAN", set, cursor, "COUNT", "2");
                cursor = (string)resp[0];
                foreach (var m in (object[])resp[1]) seen.Add((string)m);
            } while (cursor != "0");

            Assert.True(new[] { "a", "b", "c" }.All(seen.Contains));
        }
    }
}