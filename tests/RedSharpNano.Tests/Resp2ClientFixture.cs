using Xunit;

namespace RedSharpNano.Tests;

public class Resp2ClientFixture : IDisposable
{
    //Global setup
    public Resp2ClientFixture()
    {
        using var client = new Resp2Client();
        client.Call("FLUSHALL");
    }

    //Global TearDown
    public void Dispose()
    {
        using var client = new Resp2Client();
        client.Call("FLUSHALL");
    }
}

[CollectionDefinition("Redis collection")]
public class DatabaseCollection : ICollectionFixture<Resp2ClientFixture>
{
}