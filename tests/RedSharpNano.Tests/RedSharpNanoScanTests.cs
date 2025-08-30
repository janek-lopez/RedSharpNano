using Xunit;

namespace RedSharpNano.Tests
{
    [Collection("Redis collection")]
    public class RedSharpNanoScanTests : RedSharpNanoBaseTests
    {
        [Fact]
        public async Task Should_ScanKeys_WithMatch()
        {
            var prefix = "pfx:" + GetId() + ":";
            var keys = new List<string> { prefix + "k1", prefix + "k2", prefix + "k3" };
            foreach (var k in keys) await Client.CallAsync("SET", k, "x");

            var found = new HashSet<string>();
            var cursor = "0";
            do
            {
                var resp = (object[])await Client.CallAsync("SCAN", cursor, "MATCH", prefix + "*", "COUNT", "100");
                cursor = (string)resp[0];
                var batch = (object[])resp[1];
                foreach (var o in batch) found.Add((string)o);
            } while (cursor != "0");

            Assert.True(keys.TrueForAll(found.Contains));
        }

        [Fact]
        public async Task Should_ScanEmptyDatabase_AndReturnEmptyBatches()
        {
            await Client.CallAsync("FLUSHALL");
            var cursor = "0"; bool seenAny = false;
            do
            {
                var resp = (object[])await Client.CallAsync("SCAN", cursor, "COUNT", "5");
                cursor = (string)resp[0];
                var batch = (object[])resp[1];
                seenAny |= batch.Length > 0;
            } while (cursor != "0");
            Assert.False(seenAny);
        }
    }
}