using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;

namespace RedSharpNano.Tests
{
    [Collection("Redis collection")]
    public class RedSharpNanoTest : RedSharpNanoBaseTests
    {
        [Fact]
        public async Task Should_ReturnPong_When_PingCommandIsCalled()
        {
            var result = await Client.CallAsync("PING");
            Assert.Equal("PONG", result);
        }

        [Fact]
        public async Task Should_StoreAndRetrieveValue_When_SetAndGetCommandsAreUsed()
        {
            await Client.CallAsync("SET", "mykey", "myvalue");
            var result = await Client.CallAsync("GET", "mykey");
            Assert.Equal("myvalue", result);
        }

        [Fact]
        public async Task Should_ExecuteStringAsCommand_When_StringIsUsedAsCommand()
        {
            await Client.CallAsync("SET mykey_str myvalue");
            var result = await Client.CallAsync("GET mykey_str");
            Assert.Equal("myvalue", result);
        }

        [Fact]
        public async Task Should_ExecuteStringWithSpacesAsCommand_When_StringWithSpacesIsUsedAsCommand()
        {
            await Client.CallAsync(""" SET "mykey with spaces" "my value also with spaces" """);
            var result = await Client.CallAsync(""" GET "mykey with spaces" """);
            Assert.Equal("my value also with spaces", result);
        }

        [Fact]
        public async Task Should_DeleteKey_When_DelCommandIsUsed()
        {
            await Client.CallAsync("SET", "tempkey", "tempvalue");
            var result = await Client.CallAsync("DEL", "tempkey");
            Assert.Equal("1", result);
        }

        [Fact]
        public async Task Should_ExecuteMultipleCommands_When_PipelineIsUsed()
        {
            var results = await Client.PipelineAsync(async client =>
            {
                await client.CallAsync("SET", "pipelinekey", "pipelinevalue");
                await client.CallAsync("GET", "pipelinekey");
                await client.CallAsync("DEL", "pipelinekey");
            });

            Assert.NotNull(results);
            Assert.Equal(3, results.Count);
            Assert.Equal("OK", results[0]);
            Assert.Equal("pipelinevalue", results[1]);
            Assert.Equal("1", results[2]);
        }

        [Fact]
        public async Task Should_ThrowException_When_UnknownCommandIsCalled()
        {
            await Assert.ThrowsAsync<Exception>(() => Client.CallAsync("UNKNOWNCOMMAND"));
        }

        [Fact]
        public async Task Should_CleanUpResources_When_DisposeIsCalled()
        {
            var client = new Resp2Client("localhost", 6379);
            client.Dispose();

            await Assert.ThrowsAsync<ObjectDisposedException>(() => client.CallAsync("PING"));
        }

        [Fact]
        public async Task Should_Support_CallingDispose_Twice()
        {
            var client = new Resp2Client("localhost", 6379);

            client.Dispose();
            client.Dispose();

            await Assert.ThrowsAsync<ObjectDisposedException>(() => client.CallAsync("PING"));
        }

        [Fact]
        public async Task Should_ReturnNull_When_GetCommandIsCalledOnNonexistentKey()
        {
            var result = await Client.CallAsync("GET", "nonexistentkey");
            Assert.Null(result);
        }

        [Fact]
        public void Should_ThrowException_When_ConnectingToInvalidPort()
        {
            Assert.Throws<SocketException>(() => new Resp2Client("localhost", 9999));
        }

        [Fact]
        public async Task Should_ThrowException_When_InvalidArgumentsArePassedToCommand()
        {
            await Assert.ThrowsAsync<Exception>(() => Client.CallAsync("SET", "onlyOneArgument"));
        }

        [Fact]
        public async Task Should_IncrementValue_When_IncrCommandIsUsed()
        {
            await Client.CallAsync("SET", "counter", "1");
            var result = await Client.CallAsync("INCR", "counter");
            Assert.Equal("2", result);
        }

        [Fact]
        public async Task Should_DecrementValue_When_DecrCommandIsUsed()
        {
            await Client.CallAsync("SET", "counter", "5");
            var result = await Client.CallAsync("DECR", "counter");
            Assert.Equal("4", result);
        }

        [Fact]
        public async Task Should_ReturnCorrectLength_When_StrLenCommandIsUsed()
        {
            await Client.CallAsync("SET", "mykey", "Hello, World!");
            var result = await Client.CallAsync("STRLEN", "mykey");
            Assert.Equal("13", result);
        }

        [Fact]
        public async Task Should_ThrowException_When_PipelinedCommands_AreInvalid()
        {
            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await Client.PipelineAsync(async pipeline =>
                {
                    await pipeline.CallAsync("SET", "key1", "value1");
                    await pipeline.CallAsync("INVALIDCOMMAND");
                    await pipeline.CallAsync("GET", "key1");
                });
            });
        }

        [Fact]
        public async Task Should_HandleEmptyPipeline_When_NoCommandsAreAdded()
        {
            var results = await Client.PipelineAsync(async _ => { });
            Assert.Empty(results);
        }
    }
}