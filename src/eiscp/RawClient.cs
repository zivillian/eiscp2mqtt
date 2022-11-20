using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace eiscp;

public class RawClient:IDisposable
{
    private readonly string _hostname;
    private readonly TcpClient _client;
    private readonly Pipe _pipe;
    private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);

    public RawClient(string hostname)
    {
        _hostname = hostname;
        _client = new TcpClient();
        _pipe = new Pipe();
    }

    public event EventHandler<RawCommandEventArgs> RawCommand;

    public event EventHandler<DebugEventArgs> Debug;

    public ValueTask ConnectAsync(CancellationToken cancellationToken)
    {
        return _client.ConnectAsync(_hostname, 60128, cancellationToken);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var fillTask = FillPipeAsync(_pipe.Writer, cancellationToken);
        var readTask = ReadPipeAsync(_pipe.Reader, cancellationToken);
        await Task.WhenAll(fillTask, readTask);
    }

    private static readonly byte[] _sendHeader = { (byte)'I', (byte)'S', (byte)'C', (byte)'P',
        0, 0, 0, 16,
        0, 0, 0, 0,
        1, 0, 0, 0,
        (byte)'!',(byte)'1' };

    private static readonly byte[] _sendNewline = { (byte)'\r',(byte)'\n' };
    public virtual async Task SendCommandAsync(string command, CancellationToken cancellationToken)
    {
        try
        {
            var length = Encoding.UTF8.GetByteCount(command) + 4;
            var packet = new byte[length - 4 + _sendHeader.Length + _sendNewline.Length];
            _sendHeader.CopyTo(packet.AsSpan());
            BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(8), (uint)length);
            Encoding.UTF8.GetBytes(command).CopyTo(packet.AsSpan(18));
            _sendNewline.CopyTo(packet.AsSpan(packet.Length - _sendNewline.Length));

            await _writeLock.WaitAsync(cancellationToken);
            OnDebugSend(command);
            var stream = _client.GetStream();
            await stream.WriteAsync(packet.AsMemory(), cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public void Disconnect()
    {
        _pipe.Writer.Complete();
        _pipe.Reader.Complete();
    }

    private async Task FillPipeAsync(PipeWriter target, CancellationToken cancellationToken)
    {
        const int bufferSize = 512;
        try
        {
            Stream stream = _client.GetStream();
            while (!cancellationToken.IsCancellationRequested)
            {
                var buffer = target.GetMemory(bufferSize);
                var read = await stream.ReadAsync(buffer, cancellationToken);
                if (read == 0)
                    break;
                target.Advance(read);
                var result = await target.FlushAsync(cancellationToken);
                if (result.IsCanceled || result.IsCompleted)
                    break;
            }
            await target.CompleteAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            await target.CompleteAsync(ex);
        }
    }

    private async Task ReadPipeAsync(PipeReader source, CancellationToken cancellationToken)
    {
        try
        {
            var headerSize = 16;
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await source.ReadAsync(cancellationToken);
                var buffer = result.Buffer;
                while (buffer.Length >= headerSize)
                {
                    var length = GetHeaderLength(buffer);
                    if (buffer.Length < length + headerSize) break;
                    var payloadBuffer = buffer.Slice(headerSize, length);
                    var payload = Encoding.UTF8.GetString(payloadBuffer);
                    OnDebugReceive(payload);
                    OnRawCommand(payload);
                    buffer = buffer.Slice(payloadBuffer.End);

                }
                source.AdvanceTo(buffer.Start, buffer.End);
                if (result.IsCompleted)
                {
                    break;
                }
            }
            await source.CompleteAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            await source.CompleteAsync(ex);
        }
    }

    private static readonly byte[] _headerPrefix = { (byte)'I', (byte)'S', (byte)'C', (byte)'P', 0, 0, 0, 16 };
    private static readonly byte[] _headerSuffix = { 1, 0, 0, 0 };
    private uint GetHeaderLength(ReadOnlySequence<byte> source)
    {
        if (source.Length < 16) throw new Exception("Invalid eISCP  Header");
        source = source.Slice(0, 16);
        if (source.IsSingleSegment)
        {
            var header = source.FirstSpan;
            if (!header.StartsWith(_headerPrefix)) throw new Exception("Invalid eISCP  Header");
            if (!header.EndsWith(_headerSuffix)) throw new Exception("Invalid eISCP  Header");
            return BinaryPrimitives.ReadUInt32BigEndian(header.Slice(8, 4));
        }
        else
        {
            Span<byte> header = stackalloc byte[16];
            source.CopyTo(header);
            if (!header.StartsWith(_headerPrefix)) throw new Exception("Invalid eISCP  Header");
            if (!header.EndsWith(_headerSuffix)) throw new Exception("Invalid eISCP  Header");
            return BinaryPrimitives.ReadUInt32BigEndian(header.Slice(8, 4));
        }
    }

    protected virtual void OnRawCommand(string command)
    {
        var handler = RawCommand;
        if (handler is null) return;
        command = ParseCommand(command);
        var eventArgs = new RawCommandEventArgs(command);
        handler.Invoke(this, eventArgs);
    }

    protected string ParseCommand(string command)
    {
        if (!command.StartsWith("!1")) throw new Exception("invalid command prefix");
        var eof = command.IndexOf('\x1a');
        if (eof < 0)
        {
            //TX-L20D sends '!1TUN\r\n' when switching to tuner
            command = command.Substring(2).TrimEnd();
        }
        else
        {
            command = command.Substring(2, eof - 2);
        }
        return command;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _client.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void OnDebugSend(string command)
    {
        Debug?.Invoke(this, new DebugEventArgs{Command = command, Send = true});
    }

    protected virtual void OnDebugReceive(string command)
    {
        Debug?.Invoke(this, new DebugEventArgs{Command = command, Send = false});
    }
}