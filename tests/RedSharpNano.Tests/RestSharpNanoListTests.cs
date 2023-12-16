using Xunit;

namespace RedSharpNano.Tests;

[Collection("Redis collection")]
public class RedSharpNanoListTests : RedSharpNanoBaseTests
{
    [Fact]
    public void Should_AddToListAndRetrieveAll_When_ListOperationsPerformed()
    {
        var storeMembers = new List<string> { "one", "two", "three" };
        storeMembers.ForEach(member => Client.Call("RPUSH", ElementId, member));

        var listCount = Convert.ToInt32(Client.Call("LLEN", ElementId));
        Assert.Equal(storeMembers.Count, listCount);

        var members = Enumerable.Range(0, listCount)
            .Select(index => Client.Call("LINDEX", ElementId, index.ToString()))
            .ToList();

        Assert.Equal(storeMembers, members);
    }

    [Fact]
    public void Should_ReturnListCount_When_ElementsAddedToList()
    {
        Client.Call("RPUSH", ElementId, "one");
        Client.Call("RPUSH", ElementId, "two");

        var listCount = Convert.ToInt32(Client.Call("LLEN", ElementId));
        Assert.Equal(2, listCount);
    }

    [Fact]
    public void Should_AddElementsToList_When_RPushCommandIsUsed()
    {
        Client.Call("RPUSH", ElementId, "one");
        Client.Call("RPUSH", ElementId, "two");
        var result = (object[])Client.Call("LRANGE", ElementId, "0", "-1");

        Assert.True(Enumerable.SequenceEqual(new object[] { "one", "two" }, result));
    }

    [Fact]
    public void Should_ReturnCorrectItem_When_GettingItemFromList()
    {
        Client.Call("RPUSH", ElementId, "one");
        Client.Call("RPUSH", ElementId, "two");
        Client.Call("RPUSH", ElementId, "three");

        var item = Client.Call("LINDEX", ElementId, "1");
        Assert.Equal("two", item);
    }

    [Fact]
    public void Should_SetNewItem_When_UsingLSetCommand()
    {
        Client.Call("RPUSH", ElementId, "one");
        Client.Call("RPUSH", ElementId, "two");
        Client.Call("LSET", ElementId, "1", "new");

        var item = Client.Call("LINDEX", ElementId, "1");
        Assert.Equal("new", item);
    }

    [Fact]
    public void Should_RemoveAndReturnFirstItem_When_UsingLPopCommand()
    {
        Client.Call("RPUSH", ElementId, "one");
        Client.Call("RPUSH", ElementId, "two");
        Client.Call("RPUSH", ElementId, "three");

        var poppedItem = Client.Call("LPOP", ElementId);
        Assert.Equal("one", poppedItem);

        var listCount = Convert.ToInt32(Client.Call("LLEN", ElementId));
        Assert.Equal(2, listCount);
    }

    [Fact]
    public void Should_RemoveAndReturnLastItem_When_UsingRPopCommand()
    {
        Client.Call("RPUSH", ElementId, "one");
        Client.Call("RPUSH", ElementId, "two");
        Client.Call("RPUSH", ElementId, "three");

        var poppedItem = Client.Call("RPOP", ElementId);
        Assert.Equal("three", poppedItem);

        var listCount = Convert.ToInt32(Client.Call("LLEN", ElementId));
        Assert.Equal(2, listCount);
    }

    [Fact]
    public void Should_AddItemToLeft_When_UsingLPushCommand()
    {
        Client.Call("LPUSH", ElementId, "one");
        Client.Call("LPUSH", ElementId, "zero");

        var firstItem = Client.Call("LINDEX", ElementId, "0");
        Assert.Equal("zero", firstItem);
    }

    [Fact]
    public void Should_TrimList_When_UsingLTrimCommand()
    {
        for (int i = 0; i < 5; i++)
        {
            Client.Call("RPUSH", ElementId, $"value{i}");
        }

        Client.Call("LTRIM", ElementId, "1", "3");

        var listCount = Convert.ToInt32(Client.Call("LLEN", ElementId));
        Assert.Equal(3, listCount);

        var firstItem = Client.Call("LINDEX", ElementId, "0");
        Assert.Equal("value1", firstItem);
    }

    [Fact]
    public void Should_RemoveSpecificValue_When_UsingLRemCommand()
    {
        Client.Call("RPUSH", ElementId, "value");
        Client.Call("RPUSH", ElementId, "value");
        Client.Call("RPUSH", ElementId, "another");

        Client.Call("LREM", ElementId, "0", "value");

        var listCount = Convert.ToInt32(Client.Call("LLEN", ElementId));
        Assert.Equal(1, listCount);

        var remainingItem = Client.Call("LINDEX", ElementId, "0");
        Assert.Equal("another", remainingItem);
    }

    [Fact]
    public void Should_TransferLastItemAndReturnIt_When_UsingRPopLPushCommand()
    {
        string sourceList = GetId();
        string destinationList = GetId();

        Client.Call("RPUSH", sourceList, "item1");
        Client.Call("RPUSH", sourceList, "item2");
        Client.Call("RPUSH", sourceList, "item3");

        var result = Client.Call("RPOPLPUSH", sourceList, destinationList);

        Assert.Equal("item3", result);

        var sourceListCount = Convert.ToInt32(Client.Call("LLEN", sourceList));
        var destinationListCount = Convert.ToInt32(Client.Call("LLEN", destinationList));

        Assert.Equal(2, sourceListCount);
        Assert.Equal(1, destinationListCount);

        var destinationListItem = Client.Call("LINDEX", destinationList, "0");
        Assert.Equal("item3", destinationListItem);
    }

    [Fact]
    public async Task Should_BlockTransferLastItemAndReturnIt_When_UsingBRPopLPushCommand()
    {
        string sourceList = GetId();
        string destinationList = GetId();

        var blockedCallTask = Task.Run(() =>
        {
            var client2 = new Resp2Client("localhost", 6379);
            return client2.Call("BRPOPLPUSH", sourceList, destinationList, "5");
        });

        await Task.Delay(1000);

        Client.Call("RPUSH", sourceList, "item2");

        var result = await blockedCallTask;

        Assert.Equal("item2", result);

        var sourceListCount = Convert.ToInt32(Client.Call("LLEN", sourceList));
        var destinationListCount = Convert.ToInt32(Client.Call("LLEN", destinationList));

        Assert.Equal(0, sourceListCount);
        Assert.Equal(1, destinationListCount);

        var destinationListItem = Client.Call("LINDEX", destinationList, "0");
        Assert.Equal("item2", destinationListItem);
    }
}

