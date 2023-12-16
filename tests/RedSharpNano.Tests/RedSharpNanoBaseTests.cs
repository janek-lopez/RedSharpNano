using Xunit;

namespace RedSharpNano.Tests;

[Collection("Redis collection")]
public class RedSharpNanoBaseTests : IDisposable
{
    protected Resp2Client Client;
    protected Dictionary<string, string> MapValues;
    protected string ElementId;

    public RedSharpNanoBaseTests()
    {
        Client = new Resp2Client();
        MapValues = CreateMap();
        ElementId = GetId();
    }
    protected string GetId()
    {
        return Guid.NewGuid().ToString();
    }

    protected Dictionary<string, string> CreateMap()
    {
        // Replace this with your own way of creating a map of string keys to string values
        return new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" },
            { "key3", "value3" }
        };
    }

    protected bool AreHashesEqual(Dictionary<string, string> dictionary, object[] array)
    {
        var areHashesEqual = true;

        for (var i = 0; i < array.Length; i += 2)
        {
            if (array[i] is not string key || array[i + 1] is not string value)
            {
                return false;
            }

            if (!dictionary.ContainsKey(key) || dictionary[key] != value)
            {
                areHashesEqual = false;
                break;
            }
        }

        return areHashesEqual;
    }


    public void Dispose()
    {
        Client.Dispose();
    }
}