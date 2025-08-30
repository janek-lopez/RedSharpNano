using Xunit;

namespace RedSharpNano.Tests
{
    [Collection("Redis collection")]
    public class RedSharpNanoUtf8Tests : RedSharpNanoBaseTests
    {
        [Fact]
        public async Task Should_HandleUnicodeKeyAndValue_WithParamsOverload()
        {
            var key = "ключ:" + GetId() + "✓";
            var val = "値✓漢字";
            await Client.CallAsync("SET", key, val);
            var got = await Client.CallAsync("GET", key);
            Assert.Equal(val, got);
        }

        [Fact]
        public async Task Should_HandleUnicodeHashField()
        {
            var field = "フィールド✓";
            var value = "värde✓";
            await Client.CallAsync("HSET", ElementId, field, value);
            var got = await Client.CallAsync("HGET", ElementId, field);
            Assert.Equal(value, got);
        }

        [Fact]
        public async Task Should_HandleQuotedKeysWithPunctuation_When_StringCommandIsUsed()
        {
            await Client.CallAsync(""" SET "has:colons-and.dots" "val-1.2:3" """);
            var res = await Client.CallAsync(""" GET "has:colons-and.dots" """);
            Assert.Equal("val-1.2:3", res);
        }
    }
}