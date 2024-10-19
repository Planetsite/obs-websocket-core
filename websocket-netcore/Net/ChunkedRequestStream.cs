using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketSharp.Net;

internal sealed class ChunkedRequestStream : RequestStream
{
    private const int _bufferLength = 8192;
    private HttpListenerContext _context;
    private bool _disposed;
    private bool _noMoreData;

    internal ChunkedRequestStream(
      Stream stream, byte[] buffer, int offset, int count, HttpListenerContext context)
      : base(stream, buffer, offset, count)
    {
        _context = context;
        Decoder = new ChunkStream((WebHeaderCollection)context.Request.Headers);
    }

    internal ChunkStream Decoder { get; set; }

    private async Task onRead(/*IAsyncResult asyncResult*/)
    {
        //var rstate = (ReadBufferState)asyncResult.AsyncState;
        //var ares = rstate.AsyncResult;

        try
        {
            int nread = 0; //  base.EndRead(asyncResult);
            //Decoder.Write(ares.Buffer, ares.Offset, nread);
            //nread = Decoder.Read(rstate.Buffer, rstate.Offset, rstate.Count);
            //rstate.Offset += nread;
            //rstate.Count -= nread;
            //if (rstate.Count == 0 || !Decoder.WantMore || nread == 0)
            //{
            //    _noMoreData = !Decoder.WantMore && nread == 0;
            //    ares.Count = rstate.InitialCount - rstate.Count;
            //    ares.CompleteAsync();
            //
            //    return;
            //}

            //ares.Offset = 0;
            //ares.Count = Math.Min(_bufferLength, Decoder.ChunkLeft + 6);
            //base.ReadAsync(ares.Buffer, ares.Offset, ares.Count, onRead, rstate);
        }
        catch (Exception ex)
        {
            await _context.Connection.SendErrorAsync(ex.Message, 400);
            //ares.CompleteAsync(ex);
        }
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (_disposed)
            throw new ObjectDisposedException(GetType().ToString());

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

        //var ares = new HttpStreamAsyncResult(callback, state);
        if (_noMoreData)
        {
            //await ares.CompleteAsync();
            //return ares;
        }

        var nread = Decoder.Read(buffer, offset, count);
        offset += nread;
        count -= nread;
        if (count == 0)
        {
            // Got all we wanted, no need to bother the decoder yet.
            //ares.Count = nread;
            //await ares.CompleteAsync();
            //return ares;
        }

        if (!Decoder.WantMore)
        {
            _noMoreData = nread == 0;
            //ares.Count = nread;
            //await ares.CompleteAsync();
            //return ares;
        }

        //ares.Buffer = new byte[_bufferLength];
        //ares.Offset = 0;
        //ares.Count = _bufferLength;

        //var rstate = new ReadBufferState(buffer, offset, count, ares);
        //rstate.InitialCount += nread;
        //await base.ReadAsync(ares.Buffer, ares.Offset, ares.Count, onRead, rstate);

        var aresBuffer = new byte[_bufferLength];
        return await base.ReadAsync(aresBuffer, 0, _bufferLength, cancellationToken);
    }

    public override void Close()
    {
        if (_disposed)
            return;

        _disposed = true;
        base.Close();
    }

    public override int EndRead(IAsyncResult asyncResult)
    {
        if (_disposed)
            throw new ObjectDisposedException(GetType().ToString());

        if (asyncResult == null)
            throw new ArgumentNullException("asyncResult");

        var ares = asyncResult as HttpStreamAsyncResult;
        if (ares == null)
            throw new ArgumentException("A wrong IAsyncResult.", "asyncResult");

        if (!ares.IsCompleted)
            ares.AsyncWaitHandle.WaitOne();

        if (ares.HasException)
            throw new HttpListenerException(400, "I/O operation aborted.");

        return ares.Count;
    }
}
