using Xunit;

namespace RedSharpNano.Tests;

[Collection("Redis collection")]
public class RedSharpNanoHashTests : RedSharpNanoBaseTests
{
    [Fact]
    public async Task Should_StoreAndRetrieveHashValue_When_HSetAndHGetCommandsAreUsed()
    {
        await Client.CallAsync("HSET", ElementId, "field1", "value1");
        var result = await Client.CallAsync("HGET", ElementId, "field1");
        Assert.Equal("value1", result);
    }

    [Fact]
    public async Task Should_RetrieveAllEntries_When_SettingMultipleHashValues()
    {
        foreach (var entry in MapValues)
        {
            await Client.CallAsync("HSET", ElementId, entry.Key, entry.Value);
        }

        var members = await Client.CallAsync("HGETALL", ElementId) as object[];

        Assert.True(AreHashesEqual(MapValues, members));
    }

    [Fact]
    public async Task Should_RemoveKeyFromHash_When_KeyExists()
    {
        foreach (var entry in MapValues)
        {
            await Client.CallAsync("HSET", ElementId, entry.Key, entry.Value);
        }

        var firstKey = MapValues.Keys.First();
        await Client.CallAsync("HDEL", ElementId, firstKey);

        MapValues.Remove(firstKey);

        var members = await Client.CallAsync("HGETALL", ElementId) as object[];
        Assert.True(AreHashesEqual(MapValues, members));
    }

    [Fact]
    public async Task Should_RetrieveSpecificValue_When_KeyExistsInHash()
    {
        foreach (var entry in MapValues)
        {
            await Client.CallAsync("HSET", ElementId, entry.Key, entry.Value);
        }

        var firstKey = MapValues.Keys.First();
        var hashValue = await Client.CallAsync("HGET", ElementId, firstKey);

        Assert.Equal(MapValues[firstKey], hashValue);
    }

    [Fact]
    public async Task Should_ReturnHashCount_When_ValuesAreAddedToHash()
    {
        foreach (var entry in MapValues)
        {
            await Client.CallAsync("HSET", ElementId, entry.Key, entry.Value);
        }

        var hashCount = await Client.CallAsync("HLEN", ElementId);
        Assert.Equal(MapValues.Count, Convert.ToInt32(hashCount));
    }


    [Fact]
    public async Task Should_CheckExistenceOfKeyInHash_When_KeysAreAdded()
    {
        foreach (var entry in MapValues)
        {
            await Client.CallAsync("HSET", ElementId, entry.Key, entry.Value);
        }

        var existingKey = MapValues.Keys.First();
        var nonExistingKey = "nonexistingkey";

        var containsExisting = await Client.CallAsync("HEXISTS", ElementId, existingKey);
        var containsNonExisting = await Client.CallAsync("HEXISTS", ElementId, nonExistingKey);

        Assert.Equal("1", containsExisting);
        Assert.Equal("0", containsNonExisting);
    }

    [Fact]
    public async Task Should_RetrieveHashKeys_When_HashHasMultipleKeys()
    {
        foreach (var entry in MapValues)
        {
            await Client.CallAsync("HSET", ElementId, entry.Key, entry.Value);
        }

        var expectedKeys = MapValues.Keys.ToList();
        var hashKeys = await Client.CallAsync("HKEYS", ElementId) as object[];

        Assert.True(Enumerable.SequenceEqual(expectedKeys, hashKeys));
    }

    [Fact]
    public async Task Should_RetrieveHashValues_When_HashHasMultipleValues()
    {
        foreach (var entry in MapValues)
        {
            await Client.CallAsync("HSET", ElementId, entry.Key, entry.Value);
        }

        var expectedValues = MapValues.Values.ToList();
        var hashValues = await Client.CallAsync("HVALS", ElementId) as object[];

        Assert.True(Enumerable.SequenceEqual(expectedValues, hashValues));
    }

    [Fact]
    public async Task Should_SetNewKeyValueInHash_When_KeyDoesNotExist()
    {
        foreach (var entry in MapValues)
        {
            await Client.CallAsync("HSET", ElementId, entry.Key, entry.Value);
        }

        var newKey = "newkey";
        var newValue = "newvalue";

        await Client.CallAsync("HSETNX", ElementId, newKey, newValue); // Should add new key-value pair

        MapValues[newKey] = newValue;

        var updatedItems = await Client.CallAsync("HGETALL", ElementId) as object[];
        Assert.True(AreHashesEqual(MapValues, updatedItems));
    }

    [Fact]
    public async Task Should_NotSetKeyValueInHash_When_KeyAlreadyExists()
    {
        var mapValues = CreateMap();
        foreach (var entry in mapValues)
        {
            await Client.CallAsync("HSET", ElementId, entry.Key, entry.Value);
        }

        var existingKey = mapValues.Keys.First();
        var newValue = "newvalue";

        await Client.CallAsync("HSETNX", ElementId, existingKey, newValue); // Should not update existing key

        var hashValue = await Client.CallAsync("HGET", ElementId, existingKey);
        Assert.NotEqual(newValue, hashValue);
    }

    [Fact]
    public async Task Should_ClearHash_When_HashHasMultipleKeys()
    {
        foreach (var entry in MapValues)
        {
            await Client.CallAsync("HSET", ElementId, entry.Key, entry.Value);
        }

        foreach (var key in MapValues.Keys.ToList())
        {
            await Client.CallAsync("HDEL", ElementId, key);
        }

        var hashCount = await Client.CallAsync("HLEN", ElementId);
        Assert.Equal("0", hashCount);
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
}
