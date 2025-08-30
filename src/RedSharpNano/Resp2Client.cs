using System.Net.Sockets;
using System.Text.RegularExpressions;
using static System.Text.Encoding;
using static System.Globalization.CultureInfo;

namespace RedSharpNano;

public sealed class Resp2Client(string host = "localhost", int port = 6379) : IDisposable
{
    const byte CR = (byte)'\r', LF = (byte)'\n';
    const string CRLF = "\r\n";

    readonly TcpClient _tcp = new(host, port) { NoDelay = true };
    NetworkStream NetStream => _tcp.GetStream();
    readonly List<string> _pipelineBuffer = [];
    bool _isPipelining;

    public async Task<object?> CallAsync(params string[] args)
    {
        var command = $"*{args.Length}{CRLF}{string.Concat(args.Select(arg => $"${UTF8.GetByteCount(arg)}{CRLF}{arg}{CRLF}"))}";
        if (_isPipelining) { _pipelineBuffer.Add(command); return null; }
        await NetStream.WriteAsync(UTF8.GetBytes(command));
        return await ParseAsync();
    }

    public Task<object?> CallAsync(string commandLine) =>
        CallAsync(Regex.Matches(commandLine, @"[^\s""]+|""([^""]*)""")
                       .Select(match => match.Groups[1].Success ? match.Groups[1].Value : match.Value)
                       .ToArray());

    public async Task<List<object?>> PipelineAsync(Func<Resp2Client, Task> actions)
    {
        _isPipelining = true; await actions(this); _isPipelining = false;
        await NetStream.WriteAsync(UTF8.GetBytes(string.Concat(_pipelineBuffer)));
        var responses = new List<object?>(_pipelineBuffer.Count);
        for (int i = 0; i < _pipelineBuffer.Count; i++) responses.Add(await ParseAsync());
        _pipelineBuffer.Clear(); return responses;
    }

    async Task<object?> ParseAsync()
    {
        var line = await ReadLineAsync();
        if (line.Length == 0) throw new Exception("Empty response");
        var type = line[0]; var payload = line.Length > 1 ? line[1..] : "";
        switch (type)
        {
            case '-': throw new Exception(payload);
            case '+' or ':': return payload; // integer & simple string replies as strings (matches tests)
            case '$':
                var length = int.Parse(payload, InvariantCulture);
                if (length == -1) return null;
                var buffer = new byte[length + 2]; await NetStream.ReadExactlyAsync(buffer);
                if (buffer[^2] != CR || buffer[^1] != LF) throw new FormatException("Missing CRLF");
                return UTF8.GetString(buffer, 0, length);
            case '*':
                var count = int.Parse(payload, InvariantCulture);
                if (count == -1) return null;
                var items = new object?[count];
                for (int i = 0; i < count; i++) items[i] = await ParseAsync();
                return items;
            default: throw new NotSupportedException("Unknown RESP: " + type);
        }
    }

    async Task<string> ReadLineAsync()
    {
        using var lineBuffer = new MemoryStream();
        var oneByte = new byte[1];
        while (true)
        {
            if (await NetStream.ReadAsync(oneByte, 0, 1) == 0) throw new EndOfStreamException();
            if (oneByte[0] == LF) break;
            if (oneByte[0] != CR) lineBuffer.WriteByte(oneByte[0]);
        }
        return UTF8.GetString(lineBuffer.ToArray());
    }

    public void Dispose() => _tcp.Dispose();
}
