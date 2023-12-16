using Xunit;

namespace RedSharpNano.Tests;

[Collection("Redis collection")]
public class RedSharpNanoHashTests : RedSharpNanoBaseTests
{
    [Fact]
    public void Should_StoreAndRetrieveHashValue_When_HSetAndHGetCommandsAreUsed()
    {
        Client.Call("HSET", ElementId, "field1", "value1");
        var result = Client.Call("HGET", ElementId, "field1");
        Assert.Equal("value1", result);
    }

    [Fact]
    public void Should_RetrieveAllEntries_When_SettingMultipleHashValues()
    {
        foreach (var entry in MapValues)
        {
            Client.Call("HSET", ElementId, entry.Key, entry.Value);
        }

        var members = Client.Call("HGETALL", ElementId) as object[];

        Assert.True(AreHashesEqual(MapValues, members));
    }

    [Fact]
    public void Should_RemoveKeyFromHash_When_KeyExists()
    {
        foreach (var entry in MapValues)
        {
            Client.Call("HSET", ElementId, entry.Key, entry.Value);
        }

        var firstKey = MapValues.Keys.First();
        Client.Call("HDEL", ElementId, firstKey);

        MapValues.Remove(firstKey);

        var members = Client.Call("HGETALL", ElementId) as object[];
        Assert.True(AreHashesEqual(MapValues, members));
    }

    [Fact]
    public void Should_RetrieveSpecificValue_When_KeyExistsInHash()
    {
        foreach (var entry in MapValues)
        {
            Client.Call("HSET", ElementId, entry.Key, entry.Value);
        }

        var firstKey = MapValues.Keys.First();
        var hashValue = Client.Call("HGET", ElementId, firstKey);

        Assert.Equal(MapValues[firstKey], hashValue);
    }

    [Fact]
    public void Should_ReturnHashCount_When_ValuesAreAddedToHash()
    {
        foreach (var entry in MapValues)
        {
            Client.Call("HSET", ElementId, entry.Key, entry.Value);
        }

        var hashCount = Client.Call("HLEN", ElementId);
        Assert.Equal(MapValues.Count, Convert.ToInt32(hashCount));
    }


    [Fact]
    public void Should_CheckExistenceOfKeyInHash_When_KeysAreAdded()
    {
        foreach (var entry in MapValues)
        {
            Client.Call("HSET", ElementId, entry.Key, entry.Value);
        }

        var existingKey = MapValues.Keys.First();
        var nonExistingKey = "nonexistingkey";

        var containsExisting = Client.Call("HEXISTS", ElementId, existingKey);
        var containsNonExisting = Client.Call("HEXISTS", ElementId, nonExistingKey);

        Assert.Equal("1", containsExisting);
        Assert.Equal("0", containsNonExisting);
    }

    [Fact]
    public void Should_RetrieveHashKeys_When_HashHasMultipleKeys()
    {
        foreach (var entry in MapValues)
        {
            Client.Call("HSET", ElementId, entry.Key, entry.Value);
        }

        var expectedKeys = MapValues.Keys.ToList();
        var hashKeys = Client.Call("HKEYS", ElementId) as object[];

        Assert.True(Enumerable.SequenceEqual(expectedKeys, hashKeys));
    }

    [Fact]
    public void Should_RetrieveHashValues_When_HashHasMultipleValues()
    {
        foreach (var entry in MapValues)
        {
            Client.Call("HSET", ElementId, entry.Key, entry.Value);
        }

        var expectedValues = MapValues.Values.ToList();
        var hashValues = Client.Call("HVALS", ElementId) as object[];

        Assert.True(Enumerable.SequenceEqual(expectedValues, hashValues));
    }

    [Fact]
    public void Should_SetNewKeyValueInHash_When_KeyDoesNotExist()
    {
        foreach (var entry in MapValues)
        {
            Client.Call("HSET", ElementId, entry.Key, entry.Value);
        }

        var newKey = "newkey";
        var newValue = "newvalue";

        Client.Call("HSETNX", ElementId, newKey, newValue); // Should add new key-value pair

        MapValues[newKey] = newValue;

        var updatedItems = Client.Call("HGETALL", ElementId) as object[];
        Assert.True(AreHashesEqual(MapValues, updatedItems));
    }

    [Fact]
    public void Should_NotSetKeyValueInHash_When_KeyAlreadyExists()
    {
        var mapValues = CreateMap();
        foreach (var entry in mapValues)
        {
            Client.Call("HSET", ElementId, entry.Key, entry.Value);
        }

        var existingKey = mapValues.Keys.First();
        var newValue = "newvalue";

        Client.Call("HSETNX", ElementId, existingKey, newValue); // Should not update existing key

        var hashValue = Client.Call("HGET", ElementId, existingKey);
        Assert.NotEqual(newValue, hashValue);
    }

    [Fact]
    public void Should_ClearHash_When_HashHasMultipleKeys()
    {
        foreach (var entry in MapValues)
        {
            Client.Call("HSET", ElementId, entry.Key, entry.Value);
        }

        foreach (var key in MapValues.Keys.ToList())
        {
            Client.Call("HDEL", ElementId, key);
        }

        var hashCount = Client.Call("HLEN", ElementId);
        Assert.Equal("0", hashCount);
    }


    [Fact]
    public void Should_AddMembersToSet_When_SAddCommandIsUsed()
    {
        Client.Call("SADD", "myset", "member1");
        Client.Call("SADD", "myset", "member2");
        var result = (object[])Client.Call("SMEMBERS", "myset");

        Assert.Contains("member1", result[0].ToString());
        Assert.Contains("member2", result[1].ToString());
    }


}

