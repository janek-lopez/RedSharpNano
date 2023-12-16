using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Text;

namespace RedSharpNano;

public class Resp2Client : IDisposable
{
    private TcpClient _tcpClient;
    private NetworkStream _stream;
    private StreamReader _reader;
    private List<string> _pipelineCommandBuffer = new List<string>();
    private bool _isPipelineActive = false;
    private bool _disposed = false;

    public Resp2Client(string server = "localhost", int port = 6379)
    {
        _tcpClient = new TcpClient(server, port);
        _stream = _tcpClient.GetStream();
        _reader = new StreamReader(_tcpClient.GetStream(), Encoding.UTF8);
    }

    public List<object> Pipeline(Action<Resp2Client> pipelineActions)
    {
        _pipelineCommandBuffer.Clear();
        _isPipelineActive = true;
        pipelineActions(this);
        return ExecutePipeline();
    }
    
    private List<object> ExecutePipeline()
    {
        var pipelinedCommand = string.Join("", _pipelineCommandBuffer);
        _stream.Write(Encoding.UTF8.GetBytes(pipelinedCommand), 0, pipelinedCommand.Length);
        var responses = new List<object>();

        for (int i = 0; i < _pipelineCommandBuffer.Count; i++)
            responses.Add(ParseResponse());

        _isPipelineActive = false;
        _pipelineCommandBuffer.Clear();
        return responses;
    }

    public object Call(string arg)
    {
        var args = Regex
            .Matches(arg, @"(?<match>\w+)|\""(?<match>[\w\s]*)""")
            .Select(m => m.Groups["match"].Value)
            .ToArray(); //https://stackoverflow.com/questions/554013/regular-expression-to-split-on-spaces-unless-in-quotes

        return Call(args[0], args[1..]);
    }

    public object Call(string method, params string[] args)
    {
        var cmd = PrepareCommand(method, args);

        if (!_isPipelineActive)
        {
            _tcpClient.GetStream().Write(Encoding.UTF8.GetBytes(cmd), 0, cmd.Length);
            return ParseResponse();
        }

        _pipelineCommandBuffer.Add(cmd);
        return null;
    }

    private string PrepareCommand(string method, params string[] args)
    {
        var sb = new StringBuilder().Append($"*{args.Length + 1}\r\n${method.Length}\r\n{method}\r\n");

        foreach (var arg in args)
            sb.Append($"${arg.Length}\r\n{arg}\r\n");

        return sb.ToString();
    }

    private object ParseResponse()
    {
        var line = _reader.ReadLine();

        if (line == null)
            throw new InvalidOperationException("No response from server.");

        var type = line[0];
        var result = line[1..];

        switch (type)
        {
            case '-': throw new Exception(result);
            case '$':
                if (result == "-1") return null;
                var length = int.Parse(result);
                var buffer = new char[length];
                _reader.ReadBlock(buffer, 0, length);
                _reader.ReadLine();
                return new string(buffer);
            case '*': return Enumerable.Range(0, int.Parse(result)).Select(_ => ParseResponse()).ToArray();
            default: return result;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _tcpClient?.Dispose();
        _stream?.Dispose();
        _reader?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}