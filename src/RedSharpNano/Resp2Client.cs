using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace RedSharpNano;

public class Resp2Client : IDisposable
{
    private readonly TcpClient _tcp;
    private readonly NetworkStream _stream;
    private readonly StreamReader _reader;
    private readonly List<string> _pipeline = new();
    private bool _isPipelining;

    public Resp2Client(string host = "localhost", int port = 6379)
    {
        _tcp = new TcpClient(host, port);
        _stream = _tcp.GetStream();
        _reader = new StreamReader(_stream, Encoding.UTF8);
    }

    public async Task<object> CallAsync(params string[] args)
    {
        var cmd = $"*{args.Length}\r\n{string.Join("", args.Select(a => $"${a.Length}\r\n{a}\r\n"))}";
        if (_isPipelining) { _pipeline.Add(cmd); return null; }
        await _stream.WriteAsync(Encoding.UTF8.GetBytes(cmd));
        return await ParseResponseAsync();
    }

    public async Task<object> CallAsync(string arg)
    {
        var args = Regex
            .Matches(arg, @"(?<match>\w+)|\""(?<match>[\w\s]*)""")
            .Select(m => m.Groups["match"].Value)
            .ToArray(); //https://stackoverflow.com/questions/554013/regular-expression-to-split-on-spaces-unless-in-quotes

        return await CallAsync(args);
    }

    public async Task<List<object>> PipelineAsync(Func<Resp2Client, Task> actions)
    {
        _isPipelining = true;
        await actions(this);
        await _stream.WriteAsync(Encoding.UTF8.GetBytes(string.Join("", _pipeline)));
        var responses = new List<object>();
        foreach (var _ in _pipeline)
        {
            responses.Add(await ParseResponseAsync());
        }
        _pipeline.Clear();
        _isPipelining = false;
        return responses;
    }

    private async Task<object> ParseResponseAsync()
    {
        var line = await _reader.ReadLineAsync();
        if (string.IsNullOrEmpty(line)) throw new Exception("Empty response");
        return line[0] switch
        {
            '-' => throw new Exception(line[1..]),
            '$' => await ReadBulkString(int.Parse(line[1..])),
            '*' => await ReadArray(int.Parse(line[1..])),
            _ => line[1..]
        };
    }

    private async Task<string> ReadBulkString(int length)
    {
        if (length == -1) return null;
        var buffer = new char[length];
        await _reader.ReadBlockAsync(buffer, 0, length);
        await _reader.ReadLineAsync(); // Consume CRLF
        return new string(buffer);
    }

    private async Task<object[]> ReadArray(int length)
    {
        var result = new object[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = await ParseResponseAsync();
        }
        return result;
    }

    public void Dispose()
    {
        _tcp?.Dispose();
        _stream?.Dispose();
        _reader?.Dispose();
    }
}