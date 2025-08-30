using Xunit;

namespace RedSharpNano.Tests
{
    [Collection("Redis collection")]
    public class RedSharpNanoListTests : RedSharpNanoBaseTests
    {

        [Fact]
        public async Task Should_ReturnEmptyArray_When_LRangeOnMissingList()
        {
            var res = (object[])await Client.CallAsync("LRANGE", GetId(), "0", "-1");
            Assert.Empty(res);
        }

        [Fact]
        public async Task Should_ReturnNull_When_LIndexOutOfRange()
        {
            await Client.CallAsync("RPUSH", ElementId, "one");
            var res = await Client.CallAsync("LINDEX", ElementId, "5");
            Assert.Null(res);
        }

        [Fact]
        public async Task Should_ReturnLast_When_LIndexNegative()
        {
            await Client.CallAsync("RPUSH", ElementId, "one");
            await Client.CallAsync("RPUSH", ElementId, "two");
            await Client.CallAsync("RPUSH", ElementId, "three");
            var res = await Client.CallAsync("LINDEX", ElementId, "-1");
            Assert.Equal("three", res);
        }

        [Fact]
        public async Task Should_ReturnNull_When_BLPopTimesOut()
        {
            var list = GetId();
            var res = await Client.CallAsync("BLPOP", list, "1"); // timeout 1s, empty list
            Assert.Null(res); // Null multi-bulk -> client maps to null
        }

        [Fact]
        public async Task Should_InsertRelativeToPivot_When_LInsertUsed()
        {
            await Client.CallAsync("RPUSH", ElementId, "a");
            await Client.CallAsync("RPUSH", ElementId, "c");
            var count = await Client.CallAsync("LINSERT", ElementId, "BEFORE", "c", "b");
            Assert.Equal("3", count);
            var all = (object[])await Client.CallAsync("LRANGE", ElementId, "0", "-1");
            Assert.True(Enumerable.SequenceEqual(new object[] { "a", "b", "c" }, all));
        }

        [Fact]
        public async Task Should_AddToListAndRetrieveAll_When_ListOperationsPerformed()
        {
            var storeMembers = new List<string> { "one", "two", "three" };
            foreach (var member in storeMembers)
            {
                await Client.CallAsync("RPUSH", ElementId, member);
            }

            var listCount = Convert.ToInt32(await Client.CallAsync("LLEN", ElementId));
            Assert.Equal(storeMembers.Count, listCount);

            var members = new List<object>();
            for (int index = 0; index < listCount; index++)
            {
                members.Add(await Client.CallAsync("LINDEX", ElementId, index.ToString()));
            }

            Assert.Equal(storeMembers, members);
        }

        [Fact]
        public async Task Should_ReturnListCount_When_ElementsAddedToList()
        {
            await Client.CallAsync("RPUSH", ElementId, "one");
            await Client.CallAsync("RPUSH", ElementId, "two");

            var listCount = Convert.ToInt32(await Client.CallAsync("LLEN", ElementId));
            Assert.Equal(2, listCount);
        }

        [Fact]
        public async Task Should_AddElementsToList_When_RPushCommandIsUsed()
        {
            await Client.CallAsync("RPUSH", ElementId, "one");
            await Client.CallAsync("RPUSH", ElementId, "two");
            var result = (object[])await Client.CallAsync("LRANGE", ElementId, "0", "-1");

            Assert.True(Enumerable.SequenceEqual(new object[] { "one", "two" }, result));
        }

        [Fact]
        public async Task Should_ReturnCorrectItem_When_GettingItemFromList()
        {
            await Client.CallAsync("RPUSH", ElementId, "one");
            await Client.CallAsync("RPUSH", ElementId, "two");
            await Client.CallAsync("RPUSH", ElementId, "three");

            var item = await Client.CallAsync("LINDEX", ElementId, "1");
            Assert.Equal("two", item);
        }

        [Fact]
        public async Task Should_SetNewItem_When_UsingLSetCommand()
        {
            await Client.CallAsync("RPUSH", ElementId, "one");
            await Client.CallAsync("RPUSH", ElementId, "two");
            await Client.CallAsync("LSET", ElementId, "1", "new");

            var item = await Client.CallAsync("LINDEX", ElementId, "1");
            Assert.Equal("new", item);
        }

        [Fact]
        public async Task Should_RemoveAndReturnFirstItem_When_UsingLPopCommand()
        {
            await Client.CallAsync("RPUSH", ElementId, "one");
            await Client.CallAsync("RPUSH", ElementId, "two");
            await Client.CallAsync("RPUSH", ElementId, "three");

            var poppedItem = await Client.CallAsync("LPOP", ElementId);
            Assert.Equal("one", poppedItem);

            var listCount = Convert.ToInt32(await Client.CallAsync("LLEN", ElementId));
            Assert.Equal(2, listCount);
        }

        [Fact]
        public async Task Should_RemoveAndReturnLastItem_When_UsingRPopCommand()
        {
            await Client.CallAsync("RPUSH", ElementId, "one");
            await Client.CallAsync("RPUSH", ElementId, "two");
            await Client.CallAsync("RPUSH", ElementId, "three");

            var poppedItem = await Client.CallAsync("RPOP", ElementId);
            Assert.Equal("three", poppedItem);

            var listCount = Convert.ToInt32(await Client.CallAsync("LLEN", ElementId));
            Assert.Equal(2, listCount);
        }

        [Fact]
        public async Task Should_AddItemToLeft_When_UsingLPushCommand()
        {
            await Client.CallAsync("LPUSH", ElementId, "one");
            await Client.CallAsync("LPUSH", ElementId, "zero");

            var firstItem = await Client.CallAsync("LINDEX", ElementId, "0");
            Assert.Equal("zero", firstItem);
        }

        [Fact]
        public async Task Should_TrimList_When_UsingLTrimCommand()
        {
            for (int i = 0; i < 5; i++)
            {
                await Client.CallAsync("RPUSH", ElementId, $"value{i}");
            }

            await Client.CallAsync("LTRIM", ElementId, "1", "3");

            var listCount = Convert.ToInt32(await Client.CallAsync("LLEN", ElementId));
            Assert.Equal(3, listCount);

            var firstItem = await Client.CallAsync("LINDEX", ElementId, "0");
            Assert.Equal("value1", firstItem);
        }

        [Fact]
        public async Task Should_RemoveSpecificValue_When_UsingLRemCommand()
        {
            await Client.CallAsync("RPUSH", ElementId, "value");
            await Client.CallAsync("RPUSH", ElementId, "value");
            await Client.CallAsync("RPUSH", ElementId, "another");

            await Client.CallAsync("LREM", ElementId, "0", "value");

            var listCount = Convert.ToInt32(await Client.CallAsync("LLEN", ElementId));
            Assert.Equal(1, listCount);

            var remainingItem = await Client.CallAsync("LINDEX", ElementId, "0");
            Assert.Equal("another", remainingItem);
        }

        [Fact]
        public async Task Should_TransferLastItemAndReturnIt_When_UsingRPopLPushCommand()
        {
            string sourceList = GetId();
            string destinationList = GetId();

            await Client.CallAsync("RPUSH", sourceList, "item1");
            await Client.CallAsync("RPUSH", sourceList, "item2");
            await Client.CallAsync("RPUSH", sourceList, "item3");

            var result = await Client.CallAsync("RPOPLPUSH", sourceList, destinationList);

            Assert.Equal("item3", result);

            var sourceListCount = Convert.ToInt32(await Client.CallAsync("LLEN", sourceList));
            var destinationListCount = Convert.ToInt32(await Client.CallAsync("LLEN", destinationList));

            Assert.Equal(2, sourceListCount);
            Assert.Equal(1, destinationListCount);

            var destinationListItem = await Client.CallAsync("LINDEX", destinationList, "0");
            Assert.Equal("item3", destinationListItem);
        }

        [Fact]
        public async Task Should_BlockTransferLastItemAndReturnIt_When_UsingBRPopLPushCommand()
        {
            string sourceList = GetId();
            string destinationList = GetId();

            var blockedCallTask = Task.Run(async () =>
            {
                var client2 = new Resp2Client("localhost", 6379);
                return await client2.CallAsync("BRPOPLPUSH", sourceList, destinationList, "5");
            });

            await Task.Delay(1000);

            await Client.CallAsync("RPUSH", sourceList, "item2");

            var result = await blockedCallTask;

            Assert.Equal("item2", result);

            var sourceListCount = Convert.ToInt32(await Client.CallAsync("LLEN", sourceList));
            var destinationListCount = Convert.ToInt32(await Client.CallAsync("LLEN", destinationList));

            Assert.Equal(0, sourceListCount);
            Assert.Equal(1, destinationListCount);

            var destinationListItem = await Client.CallAsync("LINDEX", destinationList, "0");
            Assert.Equal("item2", destinationListItem);
        }
    }
}
