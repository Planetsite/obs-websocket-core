using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketSharp.Net;

internal class RequestStream : Stream
{
    private long _bodyLeft;
    private byte[] _buffer;
    private int _count;
    private bool _disposed;
    private int _offset;
    private Stream _stream;

    internal RequestStream(Stream stream, byte[] buffer, int offset, int count)
        : this(stream, buffer, offset, count, -1)
    {
    }

    internal RequestStream(Stream stream, byte[] buffer, int offset, int count, long contentLength)
    {
        _stream = stream;
        _buffer = buffer;
        _offset = offset;
        _count = count;
        _bodyLeft = contentLength;
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();

        set => throw new NotSupportedException();
    }

    // Returns 0 if we can keep reading from the base stream,
    // > 0 if we read something from the buffer,
    // -1 if we had a content length set and we finished reading that many bytes.
    private int fillFromBuffer(byte[] buffer, int offset, int count)
    {
        if (buffer == null)
            throw new ArgumentNullException("buffer");

        if (offset < 0)
            throw new ArgumentOutOfRangeException("offset", "A negative value.");

        if (count < 0)
            throw new ArgumentOutOfRangeException("count", "A negative value.");

        var len = buffer.Length;
        if (offset + count > len)
            throw new ArgumentException(
              "The sum of 'offset' and 'count' is greater than 'buffer' length.");

        if (_bodyLeft == 0)
            return -1;

        if (_count == 0 || count == 0)
            return 0;

        if (count > _count)
            count = _count;

        if (_bodyLeft > 0 && count > _bodyLeft)
            count = (int)_bodyLeft;

        Buffer.BlockCopy(_buffer, _offset, buffer, offset, count);
        _offset += count;
        _count -= count;
        if (_bodyLeft > 0)
            _bodyLeft -= count;

        return count;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (_disposed)
            throw new ObjectDisposedException(GetType().ToString());

        var nread = fillFromBuffer(buffer, offset, count);
        if (nread > 0 || nread == -1)
        {
            //var ares = new HttpStreamAsyncResult(callback, state);
            //ares.Buffer = buffer;
            //ares.Offset = offset;
            //ares.Count = count;
            //ares.SyncRead = nread > 0 ? nread : 0;
            //await ares.CompleteAsync();

            return 0;
        }

        // Avoid reading past the end of the request to allow for HTTP pipelining.
        if (_bodyLeft >= 0 && count > _bodyLeft)
            count = (int)_bodyLeft;

        nread = await _stream.ReadAsync(buffer, offset, count);
        return 0;
    }

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
    {
        throw new NotSupportedException();
    }

    public override void Close()
    {
        _disposed = true;
    }

    public override int EndRead(IAsyncResult asyncResult)
    {
        if (_disposed)
            throw new ObjectDisposedException(GetType().ToString());

        if (asyncResult == null)
            throw new ArgumentNullException("asyncResult");

        if (asyncResult is HttpStreamAsyncResult)
        {
            var ares = (HttpStreamAsyncResult)asyncResult;
            if (!ares.IsCompleted)
                ares.AsyncWaitHandle.WaitOne();

            return ares.SyncRead;
        }

        // Close on exception?
        var nread = _stream.EndRead(asyncResult);
        if (nread > 0 && _bodyLeft > 0)
            _bodyLeft -= nread;

        return nread;
    }

    public override void EndWrite(IAsyncResult asyncResult)
    {
        throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }
}
