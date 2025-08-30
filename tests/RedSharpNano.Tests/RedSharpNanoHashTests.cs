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
    public async Task Should_ReturnNull_When_HGetOnMissingField()
    {
        var field = "missing";
        var res = await Client.CallAsync("HGET", ElementId, field);
        Assert.Null(res);
    }

    [Fact]
    public async Task Should_ReturnAddedCount_When_HSetNewVsUpdate()
    {
        var field = "f"; var v1 = "1"; var v2 = "2";
        var added = await Client.CallAsync("HSET", ElementId, field, v1);
        Assert.Equal("1", added);
        var updated = await Client.CallAsync("HSET", ElementId, field, v2);
        Assert.Equal("0", updated);
        var back = await Client.CallAsync("HGET", ElementId, field);
        Assert.Equal(v2, back);
    }

    [Fact]
    public async Task Should_IncrementIntegerField_When_HIncrByIsUsed()
    {
        await Client.CallAsync("HSET", ElementId, "count", "10");
        var r1 = await Client.CallAsync("HINCRBY", ElementId, "count", "5");
        Assert.Equal("15", r1);
        var r2 = await Client.CallAsync("HINCRBY", ElementId, "count", "-3");
        Assert.Equal("12", r2);
    }

    [Fact]
    public async Task Should_ScanAllFields_When_HScanIsUsed()
    {
        // seed
        foreach (var kv in MapValues)
            await Client.CallAsync("HSET", ElementId, kv.Key, kv.Value);

        var seen = new Dictionary<string, string>();
        var cursor = "0";
        do
        {
            var reply = (object[])await Client.CallAsync("HSCAN", ElementId, cursor);
            cursor = (string)reply[0];
            var kvs = (object[])reply[1];
            for (int i = 0; i + 1 < kvs.Length; i += 2)
                seen[(string)kvs[i]] = (string)kvs[i + 1];
        } while (cursor != "0");

        Assert.Equal(MapValues.Count, seen.Count);
        foreach (var kv in MapValues) Assert.Equal(kv.Value, seen[kv.Key]);
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
}
