#region License
/*
 * HttpConnection.cs
 *
 * This code is derived from HttpConnection.cs (System.Net) of Mono
 * (http://www.mono-project.com).
 *
 * The MIT License
 *
 * Copyright (c) 2005 Novell, Inc. (http://www.novell.com)
 * Copyright (c) 2012-2016 sta.blockhead
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */
#endregion

#region Authors
/*
 * Authors:
 * - Gonzalo Paniagua Javier <gonzalo@novell.com>
 */
#endregion

#region Contributors
/*
 * Contributors:
 * - Liryna <liryna.stark@gmail.com>
 * - Rohan Singh <rohan-singh@hotmail.com>
 */
#endregion

using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketSharp.Net
{
    internal sealed class HttpConnection
    {
        #region Private Fields

        private byte[] _buffer;
        private const int _bufferLength = 8192;
        private HttpListenerContext _context;
        private bool _contextRegistered;
        private StringBuilder _currentLine;
        private InputState _inputState;
        private RequestStream _inputStream;
        private HttpListener _lastListener;
        private LineState _lineState;
        private EndPointListener _listener;
        private EndPoint _localEndPoint;
        private ResponseStream _outputStream;
        private int _position;
        private EndPoint _remoteEndPoint;
        private MemoryStream _requestBuffer;
        private int _reuses;
        private bool _secure;
        private Socket _socket;
        private Stream _stream;
        private object _sync;
        private int _timeout;
        //private Dictionary<int, bool> _timeoutCanceled;
        //private Timer _timer;

        #endregion

        #region Internal Constructors

        internal HttpConnection(Socket socket, EndPointListener listener)
        {
            _socket = socket;
            _listener = listener;

            var netStream = new NetworkStream(socket, false);
            if (listener.IsSecure)
            {
                var sslConf = listener.SslConfiguration;
                var sslStream = new SslStream(
                    netStream,
                    false,
                    sslConf.ClientCertificateValidationCallback
                );

                sslStream.AuthenticateAsServer(
                    sslConf.ServerCertificate,
                    sslConf.ClientCertificateRequired,
                    sslConf.EnabledSslProtocols,
                    sslConf.CheckCertificateRevocation
                );

                _secure = true;
                _stream = sslStream;
            }
            else
            {
                _stream = netStream;
            }

            _localEndPoint = socket.LocalEndPoint;
            _remoteEndPoint = socket.RemoteEndPoint;
            _sync = new object();
            _timeout = 90000; // 90k ms for first request, 15k ms from then on.
            //_timeoutCanceled = new Dictionary<int, bool>();
            //_timer = new Timer(onTimeout, this, Timeout.Infinite, Timeout.Infinite);

            Init();
        }

        #endregion

        #region Public Properties

        public bool IsClosed
        {
            get
            {
                return _socket == null;
            }
        }

        public bool IsLocal
        {
            get
            {
                return ((IPEndPoint)_remoteEndPoint).Address.IsLocal();
            }
        }

        public bool IsSecure
        {
            get
            {
                return _secure;
            }
        }

        public IPEndPoint LocalEndPoint
        {
            get
            {
                return (IPEndPoint)_localEndPoint;
            }
        }

        public IPEndPoint RemoteEndPoint
        {
            get
            {
                return (IPEndPoint)_remoteEndPoint;
            }
        }

        public int Reuses
        {
            get
            {
                return _reuses;
            }
        }

        public Stream Stream
        {
            get
            {
                return _stream;
            }
        }

        #endregion

        #region Private Methods

        private void PrivateClose()
        {
            //lock (_sync)
            {
                if (_socket == null)
                    return;

                //disposeTimer();
                DisposeRequestBuffer();
                DisposeStream();
                PrivateCloseSocket();
            }

            UnregisterContext();
            RemoveConnection();
        }

        private void PrivateCloseSocket()
        {
            try
            {
                _socket.Shutdown(SocketShutdown.Both);
            }
            catch
            {
            }

            _socket.Close();
            _socket = null;
        }

        private void DisposeRequestBuffer()
        {
            if (_requestBuffer == null)
                return;

            _requestBuffer.Dispose();
            _requestBuffer = null;
        }

        private void DisposeStream()
        {
            if (_stream == null)
                return;

            _inputStream = null;
            _outputStream = null;

            _stream.Dispose();
            _stream = null;
        }

        private void DisposeTimer()
        {
            //if (_timer == null)
            //    return;
            //
            //try
            //{
            //    _timer.Change(Timeout.Infinite, Timeout.Infinite);
            //}
            //catch
            //{
            //}
            //
            //_timer.Dispose();
            //_timer = null;
        }

        private void Init()
        {
            _context = new HttpListenerContext(this);
            _inputState = InputState.RequestLine;
            _inputStream = null;
            _lineState = LineState.None;
            _outputStream = null;
            _position = 0;
            _requestBuffer = new MemoryStream();
        }

        private async Task OnReadAsync(int nread)
        {
            continueReading:
            //lock (conn._sync)
            //{
                int len;
                try
                {
                    int current = _reuses;
                    //if (!_timeoutCanceled[current])
                    //{
                    //    _timer.Change(Timeout.Infinite, Timeout.Infinite);
                    //    _timeoutCanceled[current] = true;
                    //}

                    _requestBuffer.Write(_buffer, 0, nread);
                    len = (int)_requestBuffer.Length;
                }
                catch (Exception ex)
                {
                    if (_requestBuffer != null && _requestBuffer.Length > 0)
                    {
                        await SendErrorAsync(ex.Message, 400);
                        return;
                    }

                    PrivateClose();
                    return;
                }

                if (nread <= 0)
                {
                    PrivateClose();
                    return;
                }

                if (ProcessInput(_requestBuffer.GetBuffer(), len))
                {
                    if (!_context.HasError)
                        _context.Request.FinishInitialization();

                    if (_context.HasError)
                    {
                        await SendErrorAsync();
                        return;
                    }

                    HttpListener lsnr;
                    if (!_listener.TrySearchHttpListener(_context.Request.Url, out lsnr))
                    {
                        await SendErrorAsync(null, 404);
                        return;
                    }

                    if (_lastListener != lsnr)
                    {
                        RemoveConnection();
                        if (!lsnr.AddConnection(this))
                        {
                            PrivateClose();
                            return;
                        }

                        _lastListener = lsnr;
                    }

                    _context.Listener = lsnr;
                    if (!_context.Authenticate())
                        return;

                    if (_context.Register())
                        _contextRegistered = true;

                    return;
                }

                //conn._stream.BeginRead(conn._buffer, 0, _bufferLength, onRead, conn);
                nread = await _stream.ReadAsync(_buffer, 0, _bufferLength);
                if (nread > 0)
                    goto continueReading;
                    //onReadAsync(nread);
            //}
        }

        private static async Task OnTimeout(object state) // TODO
        {
            //var conn = (HttpConnection)state;
            //var current = conn._reuses;
            //if (conn._socket == null)
            //    return;
            //
            ////lock (conn._sync)
            //{
            //    if (conn._socket == null)
            //        return;
            //
            //    if (conn._timeoutCanceled[current])
            //        return;
            //
            //    await conn.SendError(null, 408);
            //}
        }

        // true -> Done processing.
        // false -> Need more input.
        private bool ProcessInput(byte[] data, int length)
        {
            if (_currentLine == null)
                _currentLine = new StringBuilder(64);

            int nread;
            try
            {
                string line;
                while ((line = ReadLineFrom(data, _position, length, out nread)) != null)
                {
                    _position += nread;
                    if (line.Length == 0)
                    {
                        if (_inputState == InputState.RequestLine)
                            continue;

                        if (_position > 32768)
                            _context.ErrorMessage = "Headers too long";

                        _currentLine = null;
                        return true;
                    }

                    if (_inputState == InputState.RequestLine)
                    {
                        _context.Request.SetRequestLine(line);
                        _inputState = InputState.Headers;
                    }
                    else
                    {
                        _context.Request.AddHeader(line);
                    }

                    if (_context.HasError)
                        return true;
                }
            }
            catch (Exception ex)
            {
                _context.ErrorMessage = ex.Message;
                return true;
            }

            _position += nread;
            if (_position >= 32768)
            {
                _context.ErrorMessage = "Headers too long";
                return true;
            }

            return false;
        }

        private string ReadLineFrom(byte[] buffer, int offset, int length, out int read)
        {
            read = 0;

            for (var i = offset; i < length && _lineState != LineState.Lf; i++)
            {
                read++;

                var b = buffer[i];
                if (b == 13)
                    _lineState = LineState.Cr;
                else if (b == 10)
                    _lineState = LineState.Lf;
                else
                    _currentLine.Append((char)b);
            }

            if (_lineState != LineState.Lf)
                return null;

            var line = _currentLine.ToString();

            _currentLine.Length = 0;
            _lineState = LineState.None;

            return line;
        }

        private void RemoveConnection()
        {
            if (_lastListener != null)
                _lastListener.RemoveConnection(this);
            else
                _listener.RemoveConnection(this);
        }

        private void UnregisterContext()
        {
            if (!_contextRegistered)
                return;

            _context.Unregister();
            _contextRegistered = false;
        }

        #endregion

        #region Internal Methods

        internal async Task CloseAsync(bool force)
        {
            if (_socket == null)
                return;

            //lock (_sync)
            {
                if (_socket == null)
                    return;

                if (force)
                {
                    if (_outputStream != null)
                        await _outputStream.CloseAsync(true);

                    PrivateClose();
                    return;
                }

                await GetResponseStream().CloseAsync(false);

                if (_context.Response.CloseConnection)
                {
                    PrivateClose();
                    return;
                }

                if (!await _context.Request.FlushInputAsync())
                {
                    PrivateClose();
                    return;
                }

                DisposeRequestBuffer();
                UnregisterContext();
                Init();

                _reuses++;
                await BeginReadRequestAsync();
            }
        }

        #endregion

        #region Public Methods

        public async Task BeginReadRequestAsync()
        {
            if (_buffer == null)
                _buffer = new byte[_bufferLength];

            if (_reuses == 1)
                _timeout = 15000;

            try
            {
                //_timeoutCanceled.Add(_reuses, false);
                //_timer.Change(_timeout, Timeout.Infinite);
                //_stream.BeginRead(_buffer, 0, _bufferLength, onRead, this);
                int nread = await _stream.ReadAsync(_buffer, 0, _bufferLength);
                await OnReadAsync(nread);
            }
            catch
            {
                PrivateClose();
            }
        }

        public async Task CloseAsync()
        {
            await CloseAsync(false);
        }

        public RequestStream GetRequestStream(long contentLength, bool chunked)
        {
            //lock (_sync)
            {
                if (_socket == null)
                    return null;

                if (_inputStream != null)
                    return _inputStream;

                var buff = _requestBuffer.GetBuffer();
                var len = (int)_requestBuffer.Length;
                var cnt = len - _position;
                DisposeRequestBuffer();

                _inputStream = chunked
                    ? new ChunkedRequestStream(
                        _stream, buff, _position, cnt, _context
                    )
                    : new RequestStream(
                        _stream, buff, _position, cnt, contentLength
                    )
                ;

                return _inputStream;
            }
        }

        public ResponseStream GetResponseStream()
        {
            // TODO: Can we get this stream before reading the input?

            //lock (_sync)
            {
                if (_socket == null)
                    return null;

                if (_outputStream != null)
                    return _outputStream;

                var lsnr = _context.Listener;
                var ignore = lsnr != null ? lsnr.IgnoreWriteExceptions : true;
                _outputStream = new ResponseStream(_stream, _context.Response, ignore);

                return _outputStream;
            }
        }

        public async Task SendErrorAsync()
        {
            await SendErrorAsync(_context.ErrorMessage, _context.ErrorStatus);
        }

        public async Task SendErrorAsync(string message, int status)
        {
            if (_socket == null)
                return;

            //lock (_sync)
            {
                if (_socket == null)
                    return;

                try
                {
                    var res = _context.Response;
                    res.StatusCode = status;
                    res.ContentType = "text/html";

                    var content = new StringBuilder(64);
                    content.AppendFormat("<html><body><h1>{0} {1}", status, res.StatusDescription);
                    if (message != null && message.Length > 0)
                        content.AppendFormat(" ({0})</h1></body></html>", message);
                    else
                        content.Append("</h1></body></html>");

                    var enc = Encoding.UTF8;
                    var entity = enc.GetBytes(content.ToString());
                    res.ContentEncoding = enc;
                    res.ContentLength64 = entity.LongLength;

                    await res.CloseAsync(entity);
                }
                catch
                {
                    await CloseAsync(true);
                }
            }
        }

        #endregion
    }
}
