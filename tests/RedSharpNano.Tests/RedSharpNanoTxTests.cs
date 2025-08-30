using Xunit;

namespace RedSharpNano.Tests
{
    [Collection("Redis collection")]
    public class RedSharpNanoTxTests : RedSharpNanoBaseTests
    {
        [Fact]
        public async Task Should_QueueAndExecuteCommands_When_UsingMultiExec()
        {
            var k = GetId();
            Assert.Equal("OK", await Client.CallAsync("MULTI"));
            Assert.Equal("QUEUED", await Client.CallAsync("SET", k, "x"));
            Assert.Equal("QUEUED", await Client.CallAsync("GET", k));
            var exec = (object[])await Client.CallAsync("EXEC");
            Assert.Equal("OK", exec[0]);
            Assert.Equal("x", exec[1]);
        }

        [Fact]
        public async Task Should_DiscardQueuedCommands_When_DiscardIsUsed()
        {
            var k = GetId();
            Assert.Equal("OK", await Client.CallAsync("MULTI"));
            Assert.Equal("QUEUED", await Client.CallAsync("SET", k, "y"));
            Assert.Equal("OK", await Client.CallAsync("DISCARD"));
            var res = await Client.CallAsync("GET", k);
            Assert.Null(res);
        }
    }
}