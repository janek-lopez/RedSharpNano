using System.Net.Sockets;
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
        public async Task Should_ReturnMessage_When_PingWithMessageIsCalled()
        {
            var result = await Client.CallAsync("PING", "hello");
            Assert.Equal("hello", result);
        }

        [Fact]
        public async Task Should_EchoUnicode_When_EchoCommandIsUsed()
        {
            var payload = "héłłø ✓ 漢字";
            var result = await Client.CallAsync("ECHO", payload);
            Assert.Equal(payload, result);
        }

        [Fact]
        public async Task Should_ReturnArrayWithNulls_When_MGetHasMissingKeys()
        {
            var k1 = GetId(); var k2 = GetId();
            await Client.CallAsync("SET", k1, "v1");
            var res = (object[])await Client.CallAsync("MGET", k1, k2);
            Assert.Equal(2, res.Length);
            Assert.Equal("v1", res[0]);
            Assert.Null(res[1]);
        }

        [Fact]
        public async Task Should_DeleteMultipleKeys_When_DelWithMultipleKeysIsUsed()
        {
            var k1 = GetId(); var k2 = GetId();
            await Client.CallAsync("SET", k1, "a");
            await Client.CallAsync("SET", k2, "b");
            var deleted = await Client.CallAsync("DEL", k1, k2);
            Assert.Equal("2", deleted);
        }

        [Fact]
        public async Task Should_ReturnTwoNumbers_When_TimeCommandIsCalled()
        {
            var arr = (object[])await Client.CallAsync("TIME");
            Assert.Equal(2, arr.Length);
            Assert.True(long.TryParse((string)arr[0], out _));
            Assert.True(long.TryParse((string)arr[1], out _));
        }

        [Fact]
        public async Task Should_ThrowWrongType_When_ListOpIsUsedOnStringKey()
        {
            var k = GetId();
            await Client.CallAsync("SET", k, "value");
            await Assert.ThrowsAsync<Exception>(() => Client.CallAsync("LPUSH", k, "x"));
        }

        [Fact]
        public async Task Should_StoreAndRetrieveLargeUnicodeValue_When_ValueIsLarge()
        {
            var k = GetId();
            var large = new string('ü', 200_000); // multi-byte char stresses byte lengths
            await Client.CallAsync("SET", k, large);
            var back = await Client.CallAsync("GET", k);
            Assert.Equal(large, back);
        }

        [Fact]
        public async Task Should_ExecuteMixedReplyTypes_When_PipelineHasVariousCommands()
        {
            var k = GetId(); var list = GetId();
            var results = await Client.PipelineAsync(async c =>
            {
                await c.CallAsync("SET", k, "v");         // "OK"
                await c.CallAsync("INCR", GetId());       // "1" (integer as string)
                await c.CallAsync("GET", k);              // "v"
                await c.CallAsync("RPUSH", list, "a");    // "1"
                await c.CallAsync("LRANGE", list, "0", "-1"); // ["a"]
            });

            Assert.Equal(5, results.Count);
            Assert.Equal("OK", results[0]);
            Assert.Equal("1", results[1]);
            Assert.Equal("v", results[2]);
            Assert.Equal("1", results[3]);
            var last = Assert.IsType<object[]>(results[4]);
            Assert.Single(last);
            Assert.Equal("a", last[0]);
        }

        [Fact]
        public async Task Should_HandleManyPipelinedIncrements_InOrder()
        {
            var k = GetId();
            var n = 100;
            var results = await Client.PipelineAsync(async c =>
            {
                await c.CallAsync("SET", k, "0");
                for (int i = 0; i < n; i++) await c.CallAsync("INCR", k);
                await c.CallAsync("GET", k);
            });

            Assert.Equal(n + 2, results.Count);
            Assert.Equal("OK", results[0]);
            for (int i = 1; i <= n; i++) Assert.Equal(i.ToString(), results[i]);
            Assert.Equal(n.ToString(), results[^1]);
        }

        [Fact]
        public async Task Should_HandleQuotedEmptyValue_When_StringCommandIsUsed()
        {
            await Client.CallAsync(""" SET "empty-key" "" """);
            var result = await Client.CallAsync(""" GET "empty-key" """);
            Assert.Equal("", result);
        }

        [Fact]
        public async Task Should_CheckExistenceAndExpiry_When_UsingExpireAndTtl()
        {
            var k = GetId();
            await Client.CallAsync("SET", k, "x");
            var exists = await Client.CallAsync("EXISTS", k);
            Assert.Equal("1", exists);

            var ttlBefore = await Client.CallAsync("TTL", k); // -1 (no expire)
            Assert.Equal("-1", ttlBefore);

            var setExpiry = await Client.CallAsync("EXPIRE", k, "10");
            Assert.Equal("1", setExpiry);

            var ttlAfter = await Client.CallAsync("TTL", k); // >=1
            Assert.True(int.Parse((string)ttlAfter) >= 1);
        }

        [Fact]
        public async Task Should_AppendAndReportLength_When_UsingAppend()
        {
            var k = GetId();
            var len1 = await Client.CallAsync("APPEND", k, "a");
            var len2 = await Client.CallAsync("APPEND", k, "bc");
            Assert.Equal("1", len1);
            Assert.Equal("3", len2);
            Assert.Equal("abc", await Client.CallAsync("GET", k));
        }

        [Fact]
        public async Task Should_IncrementFloat_When_IncrByFloatIsUsed()
        {
            var k = GetId();
            await Client.CallAsync("SET", k, "1.0");
            var r = await Client.CallAsync("INCRBYFLOAT", k, "0.5");
            Assert.Equal("1.5", r);
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