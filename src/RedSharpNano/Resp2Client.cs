using System.Net.Sockets;
using System.Text.RegularExpressions;
using static System.Text.Encoding;
namespace RedSharpNano;
public sealed class Resp2Client(string host = "localhost", int port = 6379) : IDisposable
{
    readonly TcpClient _tcp = new(host, port) { NoDelay = true };
    NetworkStream NetStream => _tcp.GetStream();
    List<string>? _pipeline;
    public async Task<object?> CallAsync(params string[] args)
    {
        var command = $"*{args.Length}\r\n{string.Concat(args.Select(arg => $"${UTF8.GetByteCount(arg)}\r\n{arg}\r\n"))}";
        if (_pipeline != null) { _pipeline.Add(command); return null; }
        await NetStream.WriteAsync(UTF8.GetBytes(command));
        return await ParseAsync();
    }
    public Task<object?> CallAsync(string commandLine) =>
        CallAsync([.. Regex.Matches(commandLine, @"[^\s""]+|""([^""]*)""")
                           .Select(match => match.Groups[1].Success ? match.Groups[1].Value : match.Value)]);
    public async Task<List<object?>> PipelineAsync(Func<Resp2Client, Task> actions)
    {
        _pipeline = []; await actions(this);
        await NetStream.WriteAsync(UTF8.GetBytes(string.Concat(_pipeline)));
        var responses = new List<object?>();
        while (responses.Count < _pipeline.Count) responses.Add(await ParseAsync());
        _pipeline = null; return responses;
    }
    async Task<object?> ParseAsync()
    {
        var line = await ReadLineAsync();
        switch (line[0])
        {
            case '-': throw new Exception(line[1..]);
            case '+' or ':': return line[1..]; // integer & simple string replies as strings (matches tests)
            case '$' or '*' when line[1] == '-': return null; // $-1 / *-1 null replies
            case '$':
                var buffer = new byte[int.Parse(line[1..]) + 2]; await NetStream.ReadExactlyAsync(buffer);
                if (buffer[^2] != '\r' || buffer[^1] != '\n') throw new FormatException("Missing CRLF");
                return UTF8.GetString(buffer, 0, buffer.Length - 2);
            case '*':
                var items = new object?[int.Parse(line[1..])];
                for (var i = 0; i < items.Length; i++) items[i] = await ParseAsync();
                return items;
            default: throw new NotSupportedException("Unknown RESP: " + line);
        }
    }
    async Task<string> ReadLineAsync()
    {
        List<byte> bytes = [];
        var single = new byte[1];
        while (await NetStream.ReadAsync(single) == 1 ? single[0] != '\n' : throw new EndOfStreamException())
            if (single[0] != '\r') bytes.Add(single[0]);
        return UTF8.GetString([.. bytes]);
    }
    public void Dispose() => _tcp.Dispose();
}