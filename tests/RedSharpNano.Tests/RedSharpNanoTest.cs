using System.Net.Sockets;
using Xunit;

namespace RedSharpNano.Tests;

[Collection("Redis collection")]
public class RedSharpNanoTest : RedSharpNanoBaseTests
{
    [Fact]
    public void Should_ReturnPong_When_PingCommandIsCalled()
    {
        var result = Client.Call("PING");
        Assert.Equal("PONG", result);
    }

    [Fact]
    public void Should_StoreAndRetrieveValue_When_SetAndGetCommandsAreUsed()
    {
        Client.Call("SET", "mykey", "myvalue");
        var result = Client.Call("GET", "mykey");
        Assert.Equal("myvalue", result);
    }


    [Fact]
    public void Should_DeleteKey_When_DelCommandIsUsed()
    {
        Client.Call("SET", "tempkey", "tempvalue");
        var result = Client.Call("DEL", "tempkey");
        Assert.Equal("1", result);
    }

    [Fact]
    public void Should_ExecuteMultipleCommands_When_PipelineIsUsed()
    {
        var results = Client.Pipeline(client =>
        {
            client.Call("SET", "pipelinekey", "pipelinevalue");
            client.Call("GET", "pipelinekey");
            client.Call("DEL", "pipelinekey");
        });

        Assert.NotNull(results);
        Assert.Equal(3, results.Count);
        Assert.Equal("OK", results[0]); // SET command response
        Assert.Equal("pipelinevalue", results[1]); // GET command response
        Assert.Equal("1", results[2]); // DEL command response
    }

    [Fact]
    public void Should_ThrowException_When_UnknownCommandIsCalled()
    {
        Assert.Throws<Exception>(() => Client.Call("UNKNOWNCOMMAND"));
    }
    
    [Fact]
    public void Should_CleanUpResources_When_DisposeIsCalled()
    {
        var client = new Resp2Client("localhost", 6379);
        client.Dispose();

        // Check if resources are released by attempting an operation and expecting a failure
        var exception = Record.Exception(() => client.Call("PING"));
        Assert.NotNull(exception);
        Assert.IsType<ObjectDisposedException>(exception);
    }

    [Fact]
    public void Should_Support_CallingDispose_Twice()
    {
        var client = new Resp2Client("localhost", 6379);

        client.Dispose();
        client.Dispose();

        // Check if resources are released by attempting an operation and expecting a failure
        var exception = Record.Exception(() => client.Call("PING"));
        Assert.NotNull(exception);
        Assert.IsType<ObjectDisposedException>(exception);
    }

    [Fact]
    public void Should_ReturnNull_When_GetCommandIsCalledOnNonexistentKey()
    {
        var result = Client.Call("GET", "nonexistentkey");
        Assert.Null(result);
    }

    [Fact]
    public void Should_ThrowException_When_ConnectingToInvalidPort()
    {
        Assert.Throws<SocketException>(() => new Resp2Client("localhost:9999"));
    }

    [Fact]
    public void Should_ThrowException_When_InvalidArgumentsArePassedToCommand()
    {
        Assert.Throws<Exception>(() => Client.Call("SET", "onlyOneArgument"));
    }

    [Fact]
    public void Should_IncrementValue_When_IncrCommandIsUsed()
    {
        Client.Call("SET", "counter", "1");
        var result = Client.Call("INCR", "counter");
        Assert.Equal("2", result);
    }

    [Fact]
    public void Should_DecrementValue_When_DecrCommandIsUsed()
    {
        Client.Call("SET", "counter", "5");
        var result = Client.Call("DECR", "counter");
        Assert.Equal("4", result);
    }

    [Fact]
    public void Should_ReturnCorrectLength_When_StrLenCommandIsUsed()
    {
        Client.Call("SET", "mykey", "Hello, World!");
        var result = Client.Call("STRLEN", "mykey");
        Assert.Equal("13", result);
    }

    [Fact]
    public void Should_ThrowException_When_PipelinedCommands_AreInvalid()
    {
        Assert.Throws<Exception>(() =>
        {
            var results = Client.Pipeline(pipeline =>
            {
                pipeline.Call("SET", "key1", "value1");
                pipeline.Call("INVALIDCOMMAND");
                pipeline.Call("GET", "key1");
            });
        });
    }

    [Fact]
    public void Should_HandleEmptyPipeline_When_NoCommandsAreAdded()
    {
        var results = Client.Pipeline(pipeline => { });
        Assert.Empty(results);
    }
}