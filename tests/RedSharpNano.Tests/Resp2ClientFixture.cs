using Xunit;

namespace RedSharpNano.Tests;

public class Resp2ClientFixture : IAsyncLifetime
{
    //Global setup
    public async Task InitializeAsync()
    {
        using var client = new Resp2Client();
        await client.CallAsync("FLUSHALL");
    }

    //Global TearDown
    public async Task DisposeAsync()
    {
        using var client = new Resp2Client();
        await client.CallAsync("FLUSHALL");
    }
}

[CollectionDefinition("Redis collection")]
public class DatabaseCollection : ICollectionFixture<Resp2ClientFixture>;