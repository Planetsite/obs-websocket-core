#region License
/*
 * WebSocket.cs
 *
 * This code is derived from WebSocket.java
 * (http://github.com/adamac/Java-WebSocket-client).
 *
 * The MIT License
 *
 * Copyright (c) 2009 Adam MacBeth
 * Copyright (c) 2010-2016 sta.blockhead
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

#region Contributors
/*
 * Contributors:
 * - Frank Razenberg <frank@zzattack.org>
 * - David Wood <dpwood@gmail.com>
 * - Liryna <liryna.stark@gmail.com>
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp.Net;
using WebSocketSharp.Net.WebSockets;

namespace WebSocketSharp
{
    /// <summary>
    /// Implements the WebSocket interface.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   This class provides a set of methods and properties for two-way
    ///   communication using the WebSocket protocol.
    ///   </para>
    ///   <para>
    ///   The WebSocket protocol is defined in
    ///   <see href="http://tools.ietf.org/html/rfc6455">RFC 6455</see>.
    ///   </para>
    /// </remarks>
    public class WebSocket : IDisposable
    {
        #region Private Fields

        private AuthenticationChallenge _authChallenge;
        private string _base64Key;
        private bool _client;
        private Func<Task> _closeContext;
        private CompressionMethod _compression;
        private WebSocketContext _context;
        private bool _enableRedirection;
        private string _extensions;
        private bool _extensionsRequested;
        //private object _forMessageEventQueue;
        //private object _forPing;
        //private object _forSend;
        //private object _forState;
        private MemoryStream _fragmentsBuffer;
        private bool _fragmentsCompressed;
        private Opcode _fragmentsOpcode;
        private const string _guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        private bool _inContinuation;
        //private volatile bool _inMessage;
        private volatile Logger _logger;
        private static readonly int _maxRetryCountForConnect;
        private Queue<MessageEventArgs> _messageEventQueue;
        private TaskCompletionSource<bool> _messageEventQueueRestart;
        private string _origin;
        private ManualResetEvent _pongReceived;
        private string _protocol;
        private string[] _protocols;
        private bool _protocolsRequested;
        private volatile WebSocketState _readyState;
        //private ManualResetEvent _receivingExited;
        private int _retryCountForConnect;
        private ClientSslConfiguration _sslConfig;
        private Stream _stream;
        private TcpClient _tcpClient;
        private Uri _uri;
        private const string _version = "13";
        private TimeSpan _waitTime;
        private CancellationTokenSource _receivingStoppingToken = new CancellationTokenSource();

        #endregion

        #region Internal Fields

        /// <summary>
        /// Represents the empty array of <see cref="byte"/> used internally.
        /// </summary>
        internal static readonly byte[] EmptyBytes;

        /// <summary>
        /// Represents the length used to determine whether the data should be fragmented in sending.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   The data will be fragmented if that length is greater than the value of this field.
        ///   </para>
        ///   <para>
        ///   If you would like to change the value, you must set it to a value between <c>125</c> and
        ///   <c>Int32.MaxValue - 14</c> inclusive.
        ///   </para>
        /// </remarks>
        internal static readonly int FragmentLength;

        /// <summary>
        /// Represents the random number generator used internally.
        /// </summary>
        internal static readonly RandomNumberGenerator RandomNumber;

        #endregion

        #region Static Constructor

        static WebSocket()
        {
            _maxRetryCountForConnect = 10;
            EmptyBytes = new byte[0];
            FragmentLength = 1016;
            RandomNumber = new RNGCryptoServiceProvider();
        }

        #endregion

        #region Internal Constructors

        // As server
        internal WebSocket(HttpListenerWebSocketContext context, string protocol)
        {
            _context = context;
            _protocol = protocol;

            _closeContext = context.CloseAsync;
            _logger = context.Log;
            IsSecure = context.IsSecureConnection;
            _stream = context.Stream;
            _waitTime = TimeSpan.FromSeconds(1);

            Init();
        }

        // As server
        internal WebSocket(TcpListenerWebSocketContext context, string protocol)
        {
            _context = context;
            _protocol = protocol;

            _closeContext = context.CloseAsync;
            _logger = context.Log;
            IsSecure = context.IsSecureConnection;
            _stream = context.Stream;
            _waitTime = TimeSpan.FromSeconds(1);

            Init();
        }

        #endregion

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocket"/> class with
        /// <paramref name="url"/> and optionally <paramref name="protocols"/>.
        /// </summary>
        /// <param name="url">
        ///   <para>
        ///   A <see cref="string"/> that specifies the URL to which to connect.
        ///   </para>
        ///   <para>
        ///   The scheme of the URL must be ws or wss.
        ///   </para>
        ///   <para>
        ///   The new instance uses a secure connection if the scheme is wss.
        ///   </para>
        /// </param>
        /// <param name="protocols">
        ///   <para>
        ///   An array of <see cref="string"/> that specifies the names of
        ///   the subprotocols if necessary.
        ///   </para>
        ///   <para>
        ///   Each value of the array must be a token defined in
        ///   <see href="http://tools.ietf.org/html/rfc2616#section-2.2">
        ///   RFC 2616</see>.
        ///   </para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="url"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="url"/> is an empty string.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="url"/> is an invalid WebSocket URL string.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="protocols"/> contains a value that is not a token.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="protocols"/> contains a value twice.
        ///   </para>
        /// </exception>
        public WebSocket(string url, params string[] protocols)
        {
            if (url == null)
                throw new ArgumentNullException("url");

            if (url.Length == 0)
                throw new ArgumentException("An empty string.", "url");

            string msg;
            if (!url.TryCreateWebSocketUri(out _uri, out msg))
                throw new ArgumentException(msg, "url");

            if (protocols != null && protocols.Length > 0)
            {
                if (!CheckProtocols(protocols, out msg))
                    throw new ArgumentException(msg, "protocols");

                _protocols = protocols;
            }

            _base64Key = CreateBase64Key();
            _client = true;
            _logger = new Logger();
            IsSecure = _uri.Scheme == "wss";
            _waitTime = TimeSpan.FromSeconds(5);

            Init();
        }

        #endregion

        #region Internal Properties

        internal CookieCollection CookieCollection { get; private set; }

        // As server
        internal Func<WebSocketContext, string> CustomHandshakeRequestChecker { get; set; }

        // As server
        internal bool IgnoreExtensions { get; set; }

        public bool IsConnected
        {
            get
            {
                return _readyState == WebSocketState.Open || _readyState == WebSocketState.Closing;
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the compression method used to compress a message.
        /// </summary>
        /// <remarks>
        /// The set operation does nothing if the connection has already been
        /// established or it is closing.
        /// </remarks>
        /// <value>
        ///   <para>
        ///   One of the <see cref="CompressionMethod"/> enum values.
        ///   </para>
        ///   <para>
        ///   It specifies the compression method used to compress a message.
        ///   </para>
        ///   <para>
        ///   The default value is <see cref="CompressionMethod.None"/>.
        ///   </para>
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The set operation is not available if this instance is not a client.
        /// </exception>
        public CompressionMethod Compression
        {
            get
            {
                return _compression;
            }

            set
            {
                string msg = null;

                if (!_client)
                {
                    msg = "This instance is not a client.";
                    throw new InvalidOperationException(msg);
                }

                if (!CanSet(out msg))
                {
                    _logger.Warn(msg);
                    return;
                }

                //lock (_forState)
                {
                    if (!CanSet(out msg))
                    {
                        _logger.Warn(msg);
                        return;
                    }

                    _compression = value;
                }
            }
        }

        /// <summary>
        /// Gets the HTTP cookies included in the handshake request/response.
        /// </summary>
        /// <value>
        ///   <para>
        ///   An <see cref="T:System.Collections.Generic.IEnumerable{WebSocketSharp.Net.Cookie}"/>
        ///   instance.
        ///   </para>
        ///   <para>
        ///   It provides an enumerator which supports the iteration over
        ///   the collection of the cookies.
        ///   </para>
        /// </value>
        public IEnumerable<Cookie> Cookies
        {
            get
            {
                lock (CookieCollection.SyncRoot)
                {
                    foreach (Cookie cookie in CookieCollection)
                        yield return cookie;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether a <see cref="OnMessage"/> event
        /// is emitted when a ping is received.
        /// </summary>
        /// <value>
        ///   <para>
        ///   <c>true</c> if this instance emits a <see cref="OnMessage"/> event
        ///   when receives a ping; otherwise, <c>false</c>.
        ///   </para>
        ///   <para>
        ///   The default value is <c>false</c>.
        ///   </para>
        /// </value>
        public bool EmitOnPing { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the URL redirection for
        /// the handshake request is allowed.
        /// </summary>
        /// <remarks>
        /// The set operation does nothing if the connection has already been
        /// established or it is closing.
        /// </remarks>
        /// <value>
        ///   <para>
        ///   <c>true</c> if this instance allows the URL redirection for
        ///   the handshake request; otherwise, <c>false</c>.
        ///   </para>
        ///   <para>
        ///   The default value is <c>false</c>.
        ///   </para>
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The set operation is not available if this instance is not a client.
        /// </exception>
        public bool EnableRedirection
        {
            get
            {
                return _enableRedirection;
            }

            set
            {
                string msg = null;

                if (!_client)
                {
                    msg = "This instance is not a client.";
                    throw new InvalidOperationException(msg);
                }

                if (!CanSet(out msg))
                {
                    _logger.Warn(msg);
                    return;
                }

                //lock (_forState)
                {
                    if (!CanSet(out msg))
                    {
                        _logger.Warn(msg);
                        return;
                    }

                    _enableRedirection = value;
                }
            }
        }

        /// <summary>
        /// Gets the extensions selected by server.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that will be a list of the extensions
        /// negotiated between client and server, or an empty string if
        /// not specified or selected.
        /// </value>
        public string Extensions
        {
            get
            {
                return _extensions ?? String.Empty;
            }
        }

        /// <summary>
        /// Gets a value indicating whether a secure connection is used.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance uses a secure connection; otherwise,
        /// <c>false</c>.
        /// </value>
        public bool IsSecure { get; private set; }

        /// <summary>
        /// Gets the logging function.
        /// </summary>
        /// <remarks>
        /// The default logging level is <see cref="LogLevel.Error"/>.
        /// </remarks>
        /// <value>
        /// A <see cref="Logger"/> that provides the logging function.
        /// </value>
        public Logger Log
        {
            get
            {
                return _logger;
            }

            internal set
            {
                _logger = value;
            }
        }

        /// <summary>
        /// Gets or sets the value of the HTTP Origin header to send with
        /// the handshake request.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   The HTTP Origin header is defined in
        ///   <see href="http://tools.ietf.org/html/rfc6454#section-7">
        ///   Section 7 of RFC 6454</see>.
        ///   </para>
        ///   <para>
        ///   This instance sends the Origin header if this property has any.
        ///   </para>
        ///   <para>
        ///   The set operation does nothing if the connection has already been
        ///   established or it is closing.
        ///   </para>
        /// </remarks>
        /// <value>
        ///   <para>
        ///   A <see cref="string"/> that represents the value of the Origin
        ///   header to send.
        ///   </para>
        ///   <para>
        ///   The syntax is &lt;scheme&gt;://&lt;host&gt;[:&lt;port&gt;].
        ///   </para>
        ///   <para>
        ///   The default value is <see langword="null"/>.
        ///   </para>
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The set operation is not available if this instance is not a client.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   The value specified for a set operation is not an absolute URI string.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The value specified for a set operation includes the path segments.
        ///   </para>
        /// </exception>
        public string Origin
        {
            get
            {
                return _origin;
            }

            set
            {
                string msg;

                if (!_client)
                {
                    msg = "This instance is not a client.";
                    throw new InvalidOperationException(msg);
                }

                if (!value.IsNullOrEmpty())
                {
                    Uri uri;
                    if (!Uri.TryCreate(value, UriKind.Absolute, out uri))
                    {
                        msg = "Not an absolute URI string.";
                        throw new ArgumentException(msg, "value");
                    }

                    if (uri.Segments.Length > 1)
                    {
                        msg = "It includes the path segments.";
                        throw new ArgumentException(msg, "value");
                    }
                }

                if (!CanSet(out msg))
                {
                    _logger.Warn(msg);
                    return;
                }

                //lock (_forState)
                {
                    if (!CanSet(out msg))
                    {
                        _logger.Warn(msg);
                        return;
                    }

                    _origin = !value.IsNullOrEmpty() ? value.TrimEnd('/') : value;
                }
            }
        }

        /// <summary>
        /// Gets the name of subprotocol selected by the server.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="string"/> that will be one of the names of
        ///   subprotocols specified by client.
        ///   </para>
        ///   <para>
        ///   An empty string if not specified or selected.
        ///   </para>
        /// </value>
        public string Protocol
        {
            get
            {
                return _protocol ?? String.Empty;
            }

            internal set
            {
                _protocol = value;
            }
        }

        /// <summary>
        /// Gets the current state of the connection.
        /// </summary>
        /// <value>
        ///   <para>
        ///   One of the <see cref="WebSocketState"/> enum values.
        ///   </para>
        ///   <para>
        ///   It indicates the current state of the connection.
        ///   </para>
        ///   <para>
        ///   The default value is <see cref="WebSocketState.Connecting"/>.
        ///   </para>
        /// </value>
        public WebSocketState ReadyState
        {
            get
            {
                return _readyState;
            }
        }

        /// <summary>
        /// Gets the configuration for secure connection.
        /// </summary>
        /// <remarks>
        /// This configuration will be referenced when attempts to connect,
        /// so it must be configured before any connect method is called.
        /// </remarks>
        /// <value>
        /// A <see cref="ClientSslConfiguration"/> that represents
        /// the configuration used to establish a secure connection.
        /// </value>
        /// <exception cref="InvalidOperationException">
        ///   <para>
        ///   This instance is not a client.
        ///   </para>
        ///   <para>
        ///   This instance does not use a secure connection.
        ///   </para>
        /// </exception>
        public ClientSslConfiguration SslConfiguration
        {
            get
            {
                if (!_client)
                {
                    var msg = "This instance is not a client.";
                    throw new InvalidOperationException(msg);
                }

                if (!IsSecure)
                {
                    var msg = "This instance does not use a secure connection.";
                    throw new InvalidOperationException(msg);
                }

                return GetSslConfiguration();
            }
        }

        /// <summary>
        /// Gets the URL to which to connect.
        /// </summary>
        /// <value>
        /// A <see cref="Uri"/> that represents the URL to which to connect.
        /// </value>
        public Uri Url
        {
            get
            {
                return _client ? _uri : _context.RequestUri;
            }
        }

        /// <summary>
        /// Gets or sets the time to wait for the response to the ping or close.
        /// </summary>
        /// <remarks>
        /// The set operation does nothing if the connection has already been
        /// established or it is closing.
        /// </remarks>
        /// <value>
        ///   <para>
        ///   A <see cref="TimeSpan"/> to wait for the response.
        ///   </para>
        ///   <para>
        ///   The default value is the same as 5 seconds if this instance is
        ///   a client.
        ///   </para>
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value specified for a set operation is zero or less.
        /// </exception>
        public TimeSpan WaitTime
        {
            get
            {
                return _waitTime;
            }

            set
            {
                if (value <= TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException("value", "Zero or less.");

                string msg;
                if (!CanSet(out msg))
                {
                    _logger.Warn(msg);
                    return;
                }

                //lock (_forState)
                {
                    if (!CanSet(out msg))
                    {
                        _logger.Warn(msg);
                        return;
                    }

                    _waitTime = value;
                }
            }
        }

        #endregion

        #region Public Events

        /// <summary>
        /// Occurs when the WebSocket connection has been closed.
        /// </summary>
        public event EventHandler<CloseEventArgs> OnClose;

        /// <summary>
        /// Occurs when the <see cref="WebSocket"/> gets an error.
        /// </summary>
        public event EventHandler<ErrorEventArgs> OnError;

        /// <summary>
        /// Occurs when the <see cref="WebSocket"/> receives a message.
        /// </summary>
        public event EventHandler<MessageEventArgs> OnMessage;

        /// <summary>
        /// Occurs when the WebSocket connection has been established.
        /// </summary>
        //public event EventHandler OnOpen;
        public Func<object, EventArgs, Task> OnOpen;

        #endregion

        #region Private Methods

        // As server
        private async Task<bool> PrivateAcceptAsync(CancellationToken cancellationToken)
        {
            if (_readyState == WebSocketState.Open)
            {
                var msg = "The handshake request has already been accepted.";
                _logger.Warn(msg);

                return false;
            }

            //lock (_forState)
            {
                if (_readyState == WebSocketState.Open)
                {
                    var msg = "The handshake request has already been accepted.";
                    _logger.Warn(msg);

                    return false;
                }

                if (_readyState == WebSocketState.Closing)
                {
                    var msg = "The close process has set in.";
                    _logger.Error(msg);

                    msg = "An interruption has occurred while attempting to accept.";
                    Error(msg, null);

                    return false;
                }

                if (_readyState == WebSocketState.Closed)
                {
                    var msg = "The connection has been closed.";
                    _logger.Error(msg);

                    msg = "An interruption has occurred while attempting to accept.";
                    Error(msg, null);

                    return false;
                }

                try
                {
                    if (!await AcceptHandshakeAsync(cancellationToken))
                        return false;
                }
                catch (Exception ex)
                {
                    _logger.Fatal(ex.Message);
                    _logger.Debug(ex.ToString());

                    var msg = "An exception has occurred while attempting to accept.";
                    await FatalAsync(msg, ex, cancellationToken);

                    return false;
                }

                _readyState = WebSocketState.Open;
                return true;
            }
        }

        // As server
        private async Task<bool> AcceptHandshakeAsync(CancellationToken cancellationToken)
        {
            _logger.Debug($"A handshake request from {_context.UserEndPoint}:\n{_context}");

            string msg;
            if (!CheckHandshakeRequest(_context, out msg))
            {
                _logger.Error(msg);

                await RefuseHandshakeAsync(
                    CloseStatusCode.ProtocolError,
                    "A handshake error has occurred while attempting to accept.",
                    cancellationToken
                );

                return false;
            }

            if (!CustomCheckHandshakeRequest(_context, out msg))
            {
                _logger.Error(msg);

                await RefuseHandshakeAsync(
                  CloseStatusCode.PolicyViolation,
                  "A handshake error has occurred while attempting to accept.",
                  cancellationToken
                );

                return false;
            }

            _base64Key = _context.Headers["Sec-WebSocket-Key"];

            if (_protocol != null)
            {
                var vals = _context.SecWebSocketProtocols;
                ProcessSecWebSocketProtocolClientHeader(vals);
            }

            if (!IgnoreExtensions)
            {
                var val = _context.Headers["Sec-WebSocket-Extensions"];
                ProcessSecWebSocketExtensionsClientHeader(val);
            }

            return await SendHttpResponseAsync(CreateHandshakeResponse(), cancellationToken);
        }

        private bool CanSet(out string message)
        {
            message = null;

            if (_readyState == WebSocketState.Open)
            {
                message = "The connection has already been established.";
                return false;
            }

            if (_readyState == WebSocketState.Closing)
            {
                message = "The connection is closing.";
                return false;
            }

            return true;
        }

        // As server
        private bool CheckHandshakeRequest(WebSocketContext context, out string message)
        {
            message = null;

            if (!context.IsWebSocketRequest)
            {
                message = "Not a handshake request.";
                return false;
            }

            if (context.RequestUri == null)
            {
                message = "It specifies an invalid Request-URI.";
                return false;
            }

            var headers = context.Headers;

            var key = headers["Sec-WebSocket-Key"];
            if (key == null)
            {
                message = "It includes no Sec-WebSocket-Key header.";
                return false;
            }

            if (key.Length == 0)
            {
                message = "It includes an invalid Sec-WebSocket-Key header.";
                return false;
            }

            var version = headers["Sec-WebSocket-Version"];
            if (version == null)
            {
                message = "It includes no Sec-WebSocket-Version header.";
                return false;
            }

            if (version != _version)
            {
                message = "It includes an invalid Sec-WebSocket-Version header.";
                return false;
            }

            var protocol = headers["Sec-WebSocket-Protocol"];
            if (protocol != null && protocol.Length == 0)
            {
                message = "It includes an invalid Sec-WebSocket-Protocol header.";
                return false;
            }

            if (!IgnoreExtensions)
            {
                var extensions = headers["Sec-WebSocket-Extensions"];
                if (extensions != null && extensions.Length == 0)
                {
                    message = "It includes an invalid Sec-WebSocket-Extensions header.";
                    return false;
                }
            }

            return true;
        }

        // As client
        private bool CheckHandshakeResponse(HttpResponse response, out string message)
        {
            message = null;

            if (response.IsRedirect)
            {
                message = "Indicates the redirection.";
                return false;
            }

            if (response.IsUnauthorized)
            {
                message = "Requires the authentication.";
                return false;
            }

            if (!response.IsWebSocketResponse)
            {
                message = "Not a WebSocket handshake response.";
                return false;
            }

            var headers = response.Headers;
            if (!ValidateSecWebSocketAcceptHeader(headers["Sec-WebSocket-Accept"]))
            {
                message = "Includes no Sec-WebSocket-Accept header, or it has an invalid value.";
                return false;
            }

            if (!ValidateSecWebSocketProtocolServerHeader(headers["Sec-WebSocket-Protocol"]))
            {
                message = "Includes no Sec-WebSocket-Protocol header, or it has an invalid value.";
                return false;
            }

            if (!ValidateSecWebSocketExtensionsServerHeader(headers["Sec-WebSocket-Extensions"]))
            {
                message = "Includes an invalid Sec-WebSocket-Extensions header.";
                return false;
            }

            if (!ValidateSecWebSocketVersionServerHeader(headers["Sec-WebSocket-Version"]))
            {
                message = "Includes an invalid Sec-WebSocket-Version header.";
                return false;
            }

            return true;
        }

        private static bool CheckProtocols(string[] protocols, out string message)
        {
            message = null;

            Func<string, bool> cond = protocol => protocol.IsNullOrEmpty()
                                                  || !protocol.IsToken();

            if (protocols.Contains(cond))
            {
                message = "It contains a value that is not a token.";
                return false;
            }

            if (protocols.ContainsTwice())
            {
                message = "It contains a value twice.";
                return false;
            }

            return true;
        }

        private bool CheckReceivedFrame(WebSocketFrame frame, out string message)
        {
            message = null;

            var masked = frame.IsMasked;
            if (_client && masked)
            {
                message = "A frame from the server is masked.";
                return false;
            }

            if (!_client && !masked)
            {
                message = "A frame from a client is not masked.";
                return false;
            }

            if (_inContinuation && frame.IsData)
            {
                message = "A data frame has been received while receiving continuation frames.";
                return false;
            }

            if (frame.IsCompressed && _compression == CompressionMethod.None)
            {
                message = "A compressed frame has been received without any agreement for it.";
                return false;
            }

            if (frame.Rsv2 == Rsv.On)
            {
                message = "The RSV2 of a frame is non-zero without any negotiation for it.";
                return false;
            }

            if (frame.Rsv3 == Rsv.On)
            {
                message = "The RSV3 of a frame is non-zero without any negotiation for it.";
                return false;
            }

            return true;
        }

        private async Task InternalCloseAsync(ushort code, string reason, CancellationToken cancellationToken)
        {
            if (_readyState == WebSocketState.Closing)
            {
                _logger.Info("The closing is already in progress.");
                return;
            }

            if (_readyState == WebSocketState.Closed)
            {
                _logger.Info("The connection has already been closed.");
                return;
            }

            if (code == 1005)
            { // == no status
                await InternalCloseAsync(PayloadData.Empty, true, true, false, cancellationToken);
                return;
            }

            var send = !code.IsReserved();
            await InternalCloseAsync(new PayloadData(code, reason), send, send, false, cancellationToken);
        }

        private async Task InternalCloseAsync(PayloadData payloadData, bool send, bool receive, bool received, CancellationToken cancellationToken)
        {
            //lock (_forState)
            {
                if (_readyState == WebSocketState.Closing)
                {
                    _logger.Info("The closing is already in progress.");
                    return;
                }

                if (_readyState == WebSocketState.Closed)
                {
                    _logger.Info("The connection has already been closed.");
                    return;
                }

                send = send && _readyState == WebSocketState.Open;
                receive = send && receive;

                _readyState = WebSocketState.Closing;
            }

            _logger.Trace("Begin closing the connection.");

            var res = await CloseHandshakeAsync(payloadData, send, receive, received, cancellationToken);
            await ReleaseResourcesAsync();

            _logger.Trace("End closing the connection.");

            _readyState = WebSocketState.Closed;

            var e = new CloseEventArgs(payloadData, res);

            try
            {
                OnClose.Emit(this, e);
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                _logger.Debug(ex.ToString());
            }
        }

        private async Task<bool> CloseHandshakeAsync(PayloadData payloadData, bool send, bool receive, bool received, CancellationToken stoppingToken)
        {
            using (var registration = stoppingToken.Register(() => _receivingStoppingToken.Cancel()))
            {
                var sent = false;
                if (send)
                {
                    var frame = WebSocketFrame.CreateCloseFrame(payloadData, _client);
                    sent = await SendBytesAsync(frame.ToArray(), stoppingToken);

                    if (_client)
                        frame.Unmask();
                }

                var wait = !received && sent && receive; // && _receivingExited != null;
                if (wait)
                {
                    try
                    {
                        await Task.Delay(-1, _receivingStoppingToken.Token);
                    }
                    catch
                    {
                    }
                    receive = !stoppingToken.IsCancellationRequested;
                }

                var ret = sent && received;

                _logger.Debug($"Was clean?: {ret}\n  sent: {sent}\n  received: {received}");

                return ret;
            }
        }

        // As client
        private async Task<bool> InternalConnectAsync(CancellationToken cancellationToken)
        {
            if (_readyState == WebSocketState.Open)
            {
                var msg = "The connection has already been established.";
                _logger.Warn(msg);

                return false;
            }

            //lock (_forState)
            {
                if (_readyState == WebSocketState.Open)
                {
                    var msg = "The connection has already been established.";
                    _logger.Warn(msg);

                    return false;
                }

                if (_readyState == WebSocketState.Closing)
                {
                    var msg = "The close process has set in.";
                    _logger.Error(msg);

                    msg = "An interruption has occurred while attempting to connect.";
                    Error(msg, null);

                    return false;
                }

                if (_retryCountForConnect > _maxRetryCountForConnect)
                {
                    var msg = "An opportunity for reconnecting has been lost.";
                    _logger.Error(msg);

                    msg = "An interruption has occurred while attempting to connect.";
                    Error(msg, null);

                    return false;
                }

                _readyState = WebSocketState.Connecting;

                try
                {
                    await DoHandshakeAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _retryCountForConnect++;

                    _logger.Fatal(ex.Message);
                    _logger.Debug(ex.ToString());

                    var msg = "An exception has occurred while attempting to connect.";
                    await FatalAsync(msg, ex, cancellationToken);

                    return false;
                }

                _retryCountForConnect = 1;
                _readyState = WebSocketState.Open;

                return true;
            }
        }

        // As client
        private string CreateExtensions()
        {
            var buff = new StringBuilder(80);

            if (_compression != CompressionMethod.None)
            {
                var str = _compression.ToExtensionString(
                  "server_no_context_takeover", "client_no_context_takeover");

                buff.AppendFormat("{0}, ", str);
            }

            var len = buff.Length;
            if (len > 2)
            {
                buff.Length = len - 2;
                return buff.ToString();
            }

            return null;
        }

        // As server
        private HttpResponse CreateHandshakeFailureResponse(HttpStatusCode code)
        {
            var ret = HttpResponse.CreateCloseResponse(code);
            ret.Headers["Sec-WebSocket-Version"] = _version;

            return ret;
        }

        // As client
        private HttpRequest CreateHandshakeRequest()
        {
            var ret = HttpRequest.CreateWebSocketRequest(_uri);

            var headers = ret.Headers;
            if (!_origin.IsNullOrEmpty())
                headers["Origin"] = _origin;

            headers["Sec-WebSocket-Key"] = _base64Key;

            _protocolsRequested = _protocols != null;
            if (_protocolsRequested)
                headers["Sec-WebSocket-Protocol"] = _protocols.ToString(", ");

            _extensionsRequested = _compression != CompressionMethod.None;
            if (_extensionsRequested)
                headers["Sec-WebSocket-Extensions"] = CreateExtensions();

            headers["Sec-WebSocket-Version"] = _version;

            if (CookieCollection.Count > 0)
                ret.SetCookies(CookieCollection);

            return ret;
        }

        // As server
        private HttpResponse CreateHandshakeResponse()
        {
            var ret = HttpResponse.CreateWebSocketResponse();

            var headers = ret.Headers;
            headers["Sec-WebSocket-Accept"] = CreateResponseKey(_base64Key);

            if (_protocol != null)
                headers["Sec-WebSocket-Protocol"] = _protocol;

            if (_extensions != null)
                headers["Sec-WebSocket-Extensions"] = _extensions;

            if (CookieCollection.Count > 0)
                ret.SetCookies(CookieCollection);

            return ret;
        }

        // As server
        private bool CustomCheckHandshakeRequest(WebSocketContext context, out string message)
        {
            message = null;

            if (CustomHandshakeRequestChecker == null)
                return true;

            message = CustomHandshakeRequestChecker(context);
            return message == null;
        }

        // As client
        private async Task DoHandshakeAsync(CancellationToken cancellationToken)
        {
            await SetClientStreamAsync();
            var res = await SendHandshakeRequestAsync(cancellationToken);

            string msg;
            if (!CheckHandshakeResponse(res, out msg))
                throw new WebSocketException(CloseStatusCode.ProtocolError, msg);

            if (_protocolsRequested)
                _protocol = res.Headers["Sec-WebSocket-Protocol"];

            if (_extensionsRequested)
                ProcessSecWebSocketExtensionsServerHeader(res.Headers["Sec-WebSocket-Extensions"]);

            ProcessCookies(res.Cookies);
        }

        private void EnqueueToMessageEventQueue(MessageEventArgs e)
        {
            //lock (_forMessageEventQueue)
                _messageEventQueue.Enqueue(e);
        }

        private void Error(string message, Exception exception)
        {
            try
            {
                OnError.Emit(this, new ErrorEventArgs(message, exception));
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                _logger.Debug(ex.ToString());
            }
        }

        private async Task FatalAsync(string message, Exception exception, CancellationToken cancellationToken)
        {
            var code = exception is WebSocketException
                ? ((WebSocketException)exception).Code
                : CloseStatusCode.Abnormal
            ;

            await FatalAsync(message, (ushort)code, cancellationToken);
        }

        private async Task FatalAsync(string message, ushort code, CancellationToken cancellationToken)
        {
            var payload = new PayloadData(code, message);
            await InternalCloseAsync(payload, !code.IsReserved(), false, false, cancellationToken);
        }

        private async Task FatalAsync(string message, CloseStatusCode code, CancellationToken cancellationToken)
        {
            await FatalAsync(message, (ushort)code, cancellationToken);
        }

        private ClientSslConfiguration GetSslConfiguration()
        {
            if (_sslConfig == null)
                _sslConfig = new ClientSslConfiguration(_uri.DnsSafeHost);

            return _sslConfig;
        }

        private void Init()
        {
            _compression = CompressionMethod.None;
            CookieCollection = new CookieCollection();
            //_forPing = new object();
            //_forSend = new object();
            //_forState = new object();
            _messageEventQueue = new Queue<MessageEventArgs>();
            //_forMessageEventQueue = ((ICollection)_messageEventQueue).SyncRoot;
            _readyState = WebSocketState.Connecting;
        }

        //CancellationTokenSource eventQueueRestartToken;
        //private void WakeUpEventQueue()
        //{
        //    if (eventQueueRestartToken.IsCancellationRequested) return;
        //    eventQueueRestartToken.Cancel();
        //}

        private async Task StartReceivingDispatcherTaskAsync()
        {
            do
            {
                if (_messageEventQueue.Count == 0 || _readyState != WebSocketState.Open)
                {
                    if (await _messageEventQueueRestart.Task == false)
                        break;
                    _messageEventQueueRestart = new TaskCompletionSource<bool>();

                    //try {
                    //    await Task.Delay(-1, eventQueueRestartToken.Token);
                    //}
                    //catch
                    //{
                    //}
                    //
                    //eventQueueRestartToken = new CancellationTokenSource();
                }

                MessageEventArgs msg;
                if (_messageEventQueue.Count == 0)
                    continue;
                msg = _messageEventQueue.Dequeue();

                try
                {
                    OnMessage.Emit(this, msg);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.ToString());
                    Error("An error has occurred during an OnMessage event.", ex);
                }
            }
            while (true);
        }

        private async Task OpenAsync(CancellationToken cancellationToken)
        {
            _messageEventQueueRestart = new TaskCompletionSource<bool>();

            #pragma warning disable CS4014
            /*await*/ StartReceivingAccumulatorTaskAsync(cancellationToken);
            /*await*/ StartReceivingDispatcherTaskAsync();
            #pragma warning restore CS4014

            try
            {
                if (OnOpen != null)
                    await OnOpen(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
                Error("An error has occurred during the OnOpen event.", ex);
            }
        }

        private async Task<bool> PingAsync(byte[] data, CancellationToken cancellationToken)
        {
            if (_readyState != WebSocketState.Open)
                return false;

            var pongReceived = _pongReceived;
            if (pongReceived == null)
                return false;

            //lock (_forPing)
            {
                try
                {
                    pongReceived.Reset();
                    if (!await SendAsync(Fin.Final, Opcode.Ping, data, false, cancellationToken))
                        return false;

                    return pongReceived.WaitOne(_waitTime);
                }
                catch (ObjectDisposedException)
                {
                    return false;
                }
            }
        }

        private async Task<bool> ProcessCloseFrameAsync(WebSocketFrame frame, CancellationToken cancellationToken)
        {
            var payload = frame.PayloadData;
            await InternalCloseAsync(payload, !payload.HasReservedCode, false, true, cancellationToken);

            return false;
        }

        // As client
        private void ProcessCookies(CookieCollection cookies)
        {
            if (cookies.Count == 0)
                return;

            CookieCollection.SetOrRemove(cookies);
        }

        private bool ProcessDataFrame(WebSocketFrame frame)
        {
            EnqueueToMessageEventQueue(
                frame.IsCompressed
                ? new MessageEventArgs(frame.Opcode, frame.PayloadData.ApplicationData.Decompress(_compression))
                : new MessageEventArgs(frame)
            );

            return true;
        }

        private async Task<bool> ProcessFragmentFrameAsync(WebSocketFrame frame)
        {
            if (!_inContinuation)
            {
                // Must process first fragment.
                if (frame.IsContinuation)
                    return true;

                _fragmentsOpcode = frame.Opcode;
                _fragmentsCompressed = frame.IsCompressed;
                _fragmentsBuffer = new MemoryStream();
                _inContinuation = true;
            }

            await _fragmentsBuffer.WriteBytesAsync(frame.PayloadData.ApplicationData, 1024);
            if (frame.IsFinal)
            {
                using (_fragmentsBuffer)
                {
                    var data = _fragmentsCompressed
                        ? _fragmentsBuffer.DecompressToArray(_compression)
                        : _fragmentsBuffer.ToArray();

                    EnqueueToMessageEventQueue(new MessageEventArgs(_fragmentsOpcode, data));
                }

                _fragmentsBuffer = null;
                _inContinuation = false;
            }

            return true;
        }

        private async Task<bool> ProcessPingFrameAsync(WebSocketFrame frame, CancellationToken cancellationToken)
        {
            _logger.Trace("A ping was received.");

            var pong = WebSocketFrame.CreatePongFrame(frame.PayloadData, _client);

            //lock (_forState)
            {
                if (_readyState != WebSocketState.Open)
                {
                    _logger.Error("The connection is closing.");
                    return true;
                }

                if (!await SendBytesAsync(pong.ToArray(), cancellationToken))
                    return false;
            }

            _logger.Trace("A pong to this ping has been sent.");

            if (EmitOnPing)
            {
                if (_client)
                    pong.Unmask();

                EnqueueToMessageEventQueue(new MessageEventArgs(frame));
            }

            return true;
        }

        private bool ProcessPongFrame(WebSocketFrame frame)
        {
            _logger.Trace("A pong was received.");

            try
            {
                _pongReceived.Set();
            }
            catch (NullReferenceException ex)
            {
                _logger.Error(ex.Message);
                _logger.Debug(ex.ToString());

                return false;
            }
            catch (ObjectDisposedException ex)
            {
                _logger.Error(ex.Message);
                _logger.Debug(ex.ToString());

                return false;
            }

            _logger.Trace("It has been signaled.");

            return true;
        }

        private async Task<bool> ProcessReceivedFrameAsync(WebSocketFrame frame, CancellationToken cancellationToken)
        {
            string msg;
            if (!CheckReceivedFrame(frame, out msg))
                throw new WebSocketException(CloseStatusCode.ProtocolError, msg);

            frame.Unmask();
            return frame.IsFragment
                ? await ProcessFragmentFrameAsync(frame)
                : frame.IsData

                ? ProcessDataFrame(frame)
                : frame.IsPing

                ? await ProcessPingFrameAsync(frame, cancellationToken)
                : frame.IsPong

                ? ProcessPongFrame(frame)
                : frame.IsClose

                ? await ProcessCloseFrameAsync(frame, cancellationToken)
                : await ProcessUnsupportedFrameAsync(frame, cancellationToken)
            ;
        }

        // As server
        private void ProcessSecWebSocketExtensionsClientHeader(string value)
        {
            if (value == null)
                return;

            var buff = new StringBuilder(80);
            var comp = false;

            foreach (var elm in value.SplitHeaderValue(','))
            {
                var extension = elm.Trim();
                if (extension.Length == 0)
                    continue;

                if (!comp)
                {
                    if (extension.IsCompressionExtension(CompressionMethod.Deflate))
                    {
                        _compression = CompressionMethod.Deflate;

                        buff.AppendFormat(
                          "{0}, ",
                          _compression.ToExtensionString(
                            "client_no_context_takeover", "server_no_context_takeover"
                          )
                        );

                        comp = true;
                    }
                }
            }

            var len = buff.Length;
            if (len <= 2)
                return;

            buff.Length = len - 2;
            _extensions = buff.ToString();
        }

        // As client
        private void ProcessSecWebSocketExtensionsServerHeader(string value)
        {
            if (value == null)
            {
                _compression = CompressionMethod.None;
                return;
            }

            _extensions = value;
        }

        // As server
        private void ProcessSecWebSocketProtocolClientHeader(IEnumerable<string> values)
        {
            if (values.Contains(val => val == _protocol))
                return;

            _protocol = null;
        }

        private async Task<bool> ProcessUnsupportedFrameAsync(WebSocketFrame frame, CancellationToken cancellationToken)
        {
            _logger.Fatal("An unsupported frame:" + frame.PrintToString(false));
            await FatalAsync("There is no way to handle it.", CloseStatusCode.PolicyViolation, cancellationToken);

            return false;
        }

        // As server
        private async Task RefuseHandshakeAsync(CloseStatusCode code, string reason, CancellationToken cancellationToken)
        {
            _readyState = WebSocketState.Closing;

            var res = CreateHandshakeFailureResponse(HttpStatusCode.BadRequest);
            await SendHttpResponseAsync(res, cancellationToken);

            await ReleaseServerResourcesAsync();

            _readyState = WebSocketState.Closed;

            var e = new CloseEventArgs((ushort)code, reason, false);

            try
            {
                OnClose.Emit(this, e);
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                _logger.Debug(ex.ToString());
            }
        }

        // As client
        private void ReleaseClientResources()
        {
            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }

            if (_tcpClient != null)
            {
                _tcpClient.Close();
                _tcpClient = null;
            }
        }

        private void ReleaseCommonResources()
        {
            if (_fragmentsBuffer != null)
            {
                _fragmentsBuffer.Dispose();
                _fragmentsBuffer = null;
                _inContinuation = false;
            }

            if (_pongReceived != null)
            {
                _pongReceived.Close();
                _pongReceived = null;
            }
        }

        private async Task ReleaseResourcesAsync()
        {
            if (_client)
                ReleaseClientResources();
            else
                await ReleaseServerResourcesAsync();

            ReleaseCommonResources();
        }

        // As server
        private async Task ReleaseServerResourcesAsync()
        {
            if (_closeContext == null)
                return;

            await _closeContext();
            _closeContext = null;
            _stream = null;
            _context = null;
        }

        private async Task<bool> SendAsync(Opcode opcode, Stream stream, CancellationToken cancellationToken)
        {
            //lock (_forSend)
            {
                var src = stream;
                var compressed = false;
                var sent = false;
                try
                {
                    if (_compression != CompressionMethod.None)
                    {
                        stream = stream.Compress(_compression);
                        compressed = true;
                    }

                    sent = await SendAsync(opcode, stream, compressed, cancellationToken);
                    if (!sent)
                        Error("A send has been interrupted.", null);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.ToString());
                    Error("An error has occurred during a send.", ex);
                }
                finally
                {
                    if (compressed)
                        stream.Dispose();

                    src.Dispose();
                }

                return sent;
            }
        }

        private async Task<bool> SendAsync(Opcode opcode, Stream stream, bool compressed, CancellationToken cancellationToken)
        {
            var len = stream.Length;
            if (len == 0)
                return await SendAsync(Fin.Final, opcode, EmptyBytes, false, cancellationToken);

            var quo = len / FragmentLength;
            var rem = (int)(len % FragmentLength);

            byte[] buff = null;
            if (quo == 0)
            {
                buff = new byte[rem];
                return stream.Read(buff, 0, rem) == rem
                       && await SendAsync(Fin.Final, opcode, buff, compressed, cancellationToken);
            }

            if (quo == 1 && rem == 0)
            {
                buff = new byte[FragmentLength];
                return stream.Read(buff, 0, FragmentLength) == FragmentLength
                       && await SendAsync(Fin.Final, opcode, buff, compressed, cancellationToken);
            }

            /* Send fragments */

            // Begin
            buff = new byte[FragmentLength];
            var sent = stream.Read(buff, 0, FragmentLength) == FragmentLength
                       && await SendAsync(Fin.More, opcode, buff, compressed, cancellationToken);

            if (!sent)
                return false;

            var n = rem == 0 ? quo - 2 : quo - 1;
            for (long i = 0; i < n; i++)
            {
                sent = stream.Read(buff, 0, FragmentLength) == FragmentLength
                       && await SendAsync(Fin.More, Opcode.Cont, buff, false, cancellationToken);

                if (!sent)
                    return false;
            }

            // End
            if (rem == 0)
                rem = FragmentLength;
            else
                buff = new byte[rem];

            return stream.Read(buff, 0, rem) == rem
                && await SendAsync(Fin.Final, Opcode.Cont, buff, false, cancellationToken);
        }

        private async Task<bool> SendAsync(Fin fin, Opcode opcode, byte[] data, bool compressed, CancellationToken cancellationToken)
        {
            //lock (_forState)
            {
                if (_readyState != WebSocketState.Open)
                {
                    _logger.Error("The connection is closing.");
                    return false;
                }

                var frame = new WebSocketFrame(fin, opcode, data, compressed, _client);
                return await SendBytesAsync(frame.ToArray(), cancellationToken);
            }
        }

        private async Task<bool> SendBytesAsync(byte[] bytes, CancellationToken cancellationToken)
        {
            try
            {
                await _stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                _logger.Debug(ex.ToString());

                return false;
            }

            return true;
        }

        // As client
        private async Task<HttpResponse> SendHandshakeRequestAsync(CancellationToken cancellationToken)
        {
            var req = CreateHandshakeRequest();
            var res = await SendHttpRequestAsync(req, 90000, cancellationToken);
            if (res.IsUnauthorized)
            {
                var chal = res.Headers["WWW-Authenticate"];
                _logger.Warn("Received an authentication requirement for '{chal}'.");
                if (chal.IsNullOrEmpty())
                {
                    _logger.Error("No authentication challenge is specified.");
                    return res;
                }

                _authChallenge = AuthenticationChallenge.Parse(chal);
                if (_authChallenge == null)
                {
                    _logger.Error("An invalid authentication challenge is specified.");
                    return res;
                }
            }

            if (res.IsRedirect)
            {
                var url = res.Headers["Location"];
                _logger.Warn("Received a redirection to '{url}'.");
                if (_enableRedirection)
                {
                    if (url.IsNullOrEmpty())
                    {
                        _logger.Error("No url to redirect is located.");
                        return res;
                    }

                    Uri uri;
                    string msg;
                    if (!url.TryCreateWebSocketUri(out uri, out msg))
                    {
                        _logger.Error("An invalid url to redirect is located: " + msg);
                        return res;
                    }

                    ReleaseClientResources();

                    _uri = uri;
                    IsSecure = uri.Scheme == "wss";

                    await SetClientStreamAsync();
                    return await SendHandshakeRequestAsync(cancellationToken);
                }
            }

            return res;
        }

        // As client
        private async Task<HttpResponse> SendHttpRequestAsync(HttpRequest request, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            _logger.Debug("A request to the server:\n" + request.ToString());
            var res = await request.GetResponseAsync(_stream, millisecondsTimeout, cancellationToken);
            _logger.Debug("A response to this request:\n" + res.ToString());

            return res;
        }

        // As server
        private async Task<bool> SendHttpResponseAsync(HttpResponse response, CancellationToken cancellationToken)
        {
            _logger.Debug($"A response to {_context.UserEndPoint}:\n{response}");

            return await SendBytesAsync(response.ToByteArray(), cancellationToken);
        }

        // As client
        private async Task SetClientStreamAsync()
        {
            _tcpClient = new TcpClient(_uri.DnsSafeHost, _uri.Port);
            _stream = _tcpClient.GetStream();

            if (IsSecure)
            {
                var conf = GetSslConfiguration();
                var host = conf.TargetHost;
                if (host != _uri.DnsSafeHost)
                    throw new WebSocketException(
                      CloseStatusCode.TlsHandshakeFailure, "An invalid host name is specified.");

                try
                {
                    var sslStream = new SslStream(
                      _stream,
                      false,
                      conf.ServerCertificateValidationCallback,
                      conf.ClientCertificateSelectionCallback);

                    await sslStream.AuthenticateAsClientAsync(
                      host,
                      conf.ClientCertificates,
                      conf.EnabledSslProtocols,
                      conf.CheckCertificateRevocation
                      //,
                      //cancellationToken
                    );

                    _stream = sslStream;
                }
                catch (Exception ex)
                {
                    throw new WebSocketException(CloseStatusCode.TlsHandshakeFailure, ex);
                }
            }
        }

        private async Task StartReceivingAccumulatorTaskAsync(CancellationToken stoppingToken)
        {
            if (_messageEventQueue.Count > 0)
                _messageEventQueue.Clear();

            _pongReceived = new ManualResetEvent(false);

            do
            {
                try
                {
                    var frame = await WebSocketFrame.ReadFrameAsync(_stream, false, stoppingToken);

                    if (!await ProcessReceivedFrameAsync(frame, stoppingToken) || _readyState == WebSocketState.Closed)
                    {
                        _receivingStoppingToken.Cancel();
                        if (!_messageEventQueueRestart.Task.IsCompleted)
                            _messageEventQueueRestart.SetCanceled();
                        break;
                    }

                    // Receive next asap because the Ping or Close needs a response to it.
                    // temporaneamente disabilitato
                    // chiamava se stesso;
                    if (!_messageEventQueueRestart.Task.IsCompleted)
                        _messageEventQueueRestart.SetResult(true);
                }
                catch (Exception ex)
                {
                    _logger.Fatal(ex.ToString());
                    await FatalAsync("An exception has occurred while receiving.", ex, stoppingToken);
                }
            } while (!_receivingStoppingToken.IsCancellationRequested);
        }

        // As client
        private bool ValidateSecWebSocketAcceptHeader(string value)
        {
            return value != null && value == CreateResponseKey(_base64Key);
        }

        // As client
        private bool ValidateSecWebSocketExtensionsServerHeader(string value)
        {
            if (value == null)
                return true;

            if (value.Length == 0)
                return false;

            if (!_extensionsRequested)
                return false;

            var comp = _compression != CompressionMethod.None;
            foreach (var e in value.SplitHeaderValue(','))
            {
                var ext = e.Trim();
                if (comp && ext.IsCompressionExtension(_compression))
                {
                    if (!ext.Contains("server_no_context_takeover"))
                    {
                        _logger.Error("The server hasn't sent back 'server_no_context_takeover'.");
                        return false;
                    }

                    if (!ext.Contains("client_no_context_takeover"))
                        _logger.Warn("The server hasn't sent back 'client_no_context_takeover'.");

                    var method = _compression.ToExtensionString();
                    var invalid =
                      ext.SplitHeaderValue(';').Contains(
                        t =>
                        {
                            t = t.Trim();
                            return t != method
                         && t != "server_no_context_takeover"
                         && t != "client_no_context_takeover";
                        }
                      );

                    if (invalid)
                        return false;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        // As client
        private bool ValidateSecWebSocketProtocolServerHeader(string value)
        {
            if (value == null)
                return !_protocolsRequested;

            if (value.Length == 0)
                return false;

            return _protocolsRequested && _protocols.Contains(p => p == value);
        }

        // As client
        private bool ValidateSecWebSocketVersionServerHeader(string value)
        {
            return value == null || value == _version;
        }

        #endregion

        #region Internal Methods

        // As server
        internal async Task InternalCloseAsync(HttpResponse response, CancellationToken cancellationToken)
        {
            _readyState = WebSocketState.Closing;

            await SendHttpResponseAsync(response, cancellationToken);
            await ReleaseServerResourcesAsync();

            _readyState = WebSocketState.Closed;
        }

        // As server
        internal async Task InternalCloseAsync(HttpStatusCode code, CancellationToken cancellationToken)
        {
            await InternalCloseAsync(CreateHandshakeFailureResponse(code), cancellationToken);
        }

        // As server
        internal async Task InternalCloseAsync(PayloadData payloadData, byte[] frameAsBytes, CancellationToken stoppingToken)
        {
            //lock (_forState)
            {
                if (_readyState == WebSocketState.Closing)
                {
                    _logger.Info("The closing is already in progress.");
                    return;
                }

                if (_readyState == WebSocketState.Closed)
                {
                    _logger.Info("The connection has already been closed.");
                    return;
                }

                _readyState = WebSocketState.Closing;
            }

            _logger.Trace("Begin closing the connection.");

            using (var registration = stoppingToken.Register(() => _receivingStoppingToken.Cancel()))
            {
                bool sent = frameAsBytes != null && await SendBytesAsync(frameAsBytes, stoppingToken);
                bool received = false;

                if (sent)
                {
                    try
                    {
                        await Task.Delay(-1, _receivingStoppingToken.Token);
                    }
                    catch
                    {
                    }
                    received = !stoppingToken.IsCancellationRequested;
                }

                bool res = sent && received;

                _logger.Debug($"Was clean?: {res}\n  sent: {sent}\n  received: {received}");

                await ReleaseServerResourcesAsync();
                ReleaseCommonResources();

                _logger.Trace("End closing the connection.");

                _readyState = WebSocketState.Closed;

                var e = new CloseEventArgs(payloadData, res);

                try
                {
                    OnClose.Emit(this, e);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.Message);
                    _logger.Debug(ex.ToString());
                }
            }
        }

        // As client
        internal static string CreateBase64Key()
        {
            var src = new byte[16];
            RandomNumber.GetBytes(src);

            return Convert.ToBase64String(src);
        }

        internal static string CreateResponseKey(string base64Key)
        {
            var buff = new StringBuilder(base64Key, 64);
            buff.Append(_guid);
            SHA1 sha1 = new SHA1CryptoServiceProvider();
            var src = sha1.ComputeHash(buff.ToString().GetUTF8EncodedBytes());

            return Convert.ToBase64String(src);
        }

        // As server
        internal async Task InternalAcceptAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!await AcceptHandshakeAsync(cancellationToken))
                    return;
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex.Message);
                _logger.Debug(ex.ToString());

                var msg = "An exception has occurred while attempting to accept.";
                await FatalAsync(msg, ex, cancellationToken);

                return;
            }

            _readyState = WebSocketState.Open;

            await OpenAsync(cancellationToken);
        }

        // As server
        internal async Task<bool> InternalPingAsync(byte[] frameAsBytes, TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (_readyState != WebSocketState.Open)
                return false;

            var pongReceived = _pongReceived;
            if (pongReceived == null)
                return false;

            //lock (_forPing)
            {
                try
                {
                    pongReceived.Reset();

                    //lock (_forState)
                    {
                        if (_readyState != WebSocketState.Open)
                            return false;

                        if (!await SendBytesAsync(frameAsBytes, cancellationToken))
                            return false;
                    }

                    return pongReceived.WaitOne(timeout);
                }
                catch (ObjectDisposedException)
                {
                    return false;
                }
            }
        }

        // As server
        internal async Task InternalSendAsync(Opcode opcode, byte[] data, Dictionary<CompressionMethod, byte[]> cache, CancellationToken cancellationToken)
        {
            //lock (_forSend)
            {
                //lock (_forState)
                {
                    if (_readyState != WebSocketState.Open)
                    {
                        _logger.Error("The connection is closing.");
                        return;
                    }

                    byte[] found;
                    if (!cache.TryGetValue(_compression, out found))
                    {
                        found = new WebSocketFrame(
                            Fin.Final,
                            opcode,
                            data.Compress(_compression),
                            _compression != CompressionMethod.None,
                            false
                        )
                        .ToArray();

                        cache.Add(_compression, found);
                    }

                    await SendBytesAsync(found, cancellationToken);
                }
            }
        }

        // As server
        internal async Task InternalSendAsync(Opcode opcode, Stream stream, Dictionary<CompressionMethod, Stream> cache, CancellationToken cancellationToken)
        {
            //lock (_forSend)
            {
                Stream found;
                if (!cache.TryGetValue(_compression, out found))
                {
                    found = stream.Compress(_compression);
                    cache.Add(_compression, found);
                }
                else
                {
                    found.Position = 0;
                }

                await SendAsync(opcode, found, _compression != CompressionMethod.None, cancellationToken);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Accepts the handshake request.
        /// </summary>
        /// <remarks>
        /// This method does nothing if the handshake request has already been
        /// accepted.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///   <para>
        ///   This instance is a client.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The close process is in progress.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The connection has already been closed.
        ///   </para>
        /// </exception>
        public async Task AcceptAsync(CancellationToken cancellationToken)
        {
            if (_client)
            {
                var msg = "This instance is a client.";
                throw new InvalidOperationException(msg);
            }

            if (_readyState == WebSocketState.Closing)
            {
                var msg = "The close process is in progress.";
                throw new InvalidOperationException(msg);
            }

            if (_readyState == WebSocketState.Closed)
            {
                var msg = "The connection has already been closed.";
                throw new InvalidOperationException(msg);
            }

            if (await PrivateAcceptAsync(cancellationToken))
                await OpenAsync(cancellationToken);
        }

        /// <summary>
        /// Closes the connection.
        /// </summary>
        /// <remarks>
        /// This method does nothing if the current state of the connection is
        /// Closing or Closed.
        /// </remarks>
        public async Task CloseAsync(CancellationToken cancellationToken)
        {
            await CloseAsync(1005, String.Empty, cancellationToken);
        }

        /// <summary>
        /// Closes the connection with the specified code.
        /// </summary>
        /// <remarks>
        /// This method does nothing if the current state of the connection is
        /// Closing or Closed.
        /// </remarks>
        /// <param name="code">
        ///   <para>
        ///   One of the <see cref="CloseStatusCode"/> enum values.
        ///   </para>
        ///   <para>
        ///   It represents the status code indicating the reason for the close.
        ///   </para>
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="code"/> is
        ///   <see cref="CloseStatusCode.ServerError"/>.
        ///   It cannot be used by clients.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="code"/> is
        ///   <see cref="CloseStatusCode.MandatoryExtension"/>.
        ///   It cannot be used by servers.
        ///   </para>
        /// </exception>
        public async Task CloseAsync(CloseStatusCode code, CancellationToken cancellationToken)
        {
            if (_client && code == CloseStatusCode.ServerError)
            {
                var msg = "ServerError cannot be used.";
                throw new ArgumentException(msg, "code");
            }

            if (!_client && code == CloseStatusCode.MandatoryExtension)
            {
                var msg = "MandatoryExtension cannot be used.";
                throw new ArgumentException(msg, "code");
            }

            await CloseAsync((ushort)code, String.Empty, cancellationToken);
        }

        /// <summary>
        /// Closes the connection with the specified code and reason.
        /// </summary>
        /// <remarks>
        /// This method does nothing if the current state of the connection is
        /// Closing or Closed.
        /// </remarks>
        /// <param name="code">
        ///   <para>
        ///   A <see cref="ushort"/> that represents the status code indicating
        ///   the reason for the close.
        ///   </para>
        ///   <para>
        ///   The status codes are defined in
        ///   <see href="http://tools.ietf.org/html/rfc6455#section-7.4">
        ///   Section 7.4</see> of RFC 6455.
        ///   </para>
        /// </param>
        /// <param name="reason">
        ///   <para>
        ///   A <see cref="string"/> that represents the reason for the close.
        ///   </para>
        ///   <para>
        ///   The size must be 123 bytes or less in UTF-8.
        ///   </para>
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <para>
        ///   <paramref name="code"/> is less than 1000 or greater than 4999.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The size of <paramref name="reason"/> is greater than 123 bytes.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="code"/> is 1011 (server error).
        ///   It cannot be used by clients.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="code"/> is 1010 (mandatory extension).
        ///   It cannot be used by servers.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="code"/> is 1005 (no status) and there is reason.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="reason"/> could not be UTF-8-encoded.
        ///   </para>
        /// </exception>
        public async Task CloseAsync(ushort code, string reason, CancellationToken cancellationToken)
        {
            if (!code.IsCloseStatusCode())
            {
                var msg = "Less than 1000 or greater than 4999.";
                throw new ArgumentOutOfRangeException("code", msg);
            }

            if (_client && code == 1011)
            {
                var msg = "1011 cannot be used.";
                throw new ArgumentException(msg, "code");
            }

            if (!_client && code == 1010)
            {
                var msg = "1010 cannot be used.";
                throw new ArgumentException(msg, "code");
            }

            if (reason.IsNullOrEmpty())
            {
                await InternalCloseAsync(code, String.Empty, cancellationToken);
                return;
            }

            if (code == 1005)
            {
                var msg = "1005 cannot be used.";
                throw new ArgumentException(msg, "code");
            }

            byte[] bytes;
            if (!reason.TryGetUTF8EncodedBytes(out bytes))
            {
                var msg = "It could not be UTF-8-encoded.";
                throw new ArgumentException(msg, "reason");
            }

            if (bytes.Length > 123)
            {
                var msg = "Its size is greater than 123 bytes.";
                throw new ArgumentOutOfRangeException("reason", msg);
            }

            await InternalCloseAsync(code, reason, cancellationToken);
        }

        /// <summary>
        /// Closes the connection with the specified code and reason.
        /// </summary>
        /// <remarks>
        /// This method does nothing if the current state of the connection is
        /// Closing or Closed.
        /// </remarks>
        /// <param name="code">
        ///   <para>
        ///   One of the <see cref="CloseStatusCode"/> enum values.
        ///   </para>
        ///   <para>
        ///   It represents the status code indicating the reason for the close.
        ///   </para>
        /// </param>
        /// <param name="reason">
        ///   <para>
        ///   A <see cref="string"/> that represents the reason for the close.
        ///   </para>
        ///   <para>
        ///   The size must be 123 bytes or less in UTF-8.
        ///   </para>
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="code"/> is
        ///   <see cref="CloseStatusCode.ServerError"/>.
        ///   It cannot be used by clients.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="code"/> is
        ///   <see cref="CloseStatusCode.MandatoryExtension"/>.
        ///   It cannot be used by servers.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="code"/> is
        ///   <see cref="CloseStatusCode.NoStatus"/> and there is reason.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="reason"/> could not be UTF-8-encoded.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The size of <paramref name="reason"/> is greater than 123 bytes.
        /// </exception>
        public async Task CloseAsync(CloseStatusCode code, string reason, CancellationToken cancellationToken)
        {
            if (_client && code == CloseStatusCode.ServerError)
            {
                var msg = "ServerError cannot be used.";
                throw new ArgumentException(msg, "code");
            }

            if (!_client && code == CloseStatusCode.MandatoryExtension)
            {
                var msg = "MandatoryExtension cannot be used.";
                throw new ArgumentException(msg, "code");
            }

            if (reason.IsNullOrEmpty())
            {
                await CloseAsync((ushort)code, String.Empty, cancellationToken);
                return;
            }

            if (code == CloseStatusCode.NoStatus)
            {
                var msg = "NoStatus cannot be used.";
                throw new ArgumentException(msg, "code");
            }

            byte[] bytes;
            if (!reason.TryGetUTF8EncodedBytes(out bytes))
            {
                var msg = "It could not be UTF-8-encoded.";
                throw new ArgumentException(msg, "reason");
            }

            if (bytes.Length > 123)
            {
                var msg = "Its size is greater than 123 bytes.";
                throw new ArgumentOutOfRangeException("reason", msg);
            }

            await CloseAsync((ushort)code, reason, cancellationToken);
        }

        /// <summary>
        /// Establishes a connection.
        /// </summary>
        /// <remarks>
        /// This method does nothing if the connection has already been established.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///   <para>
        ///   This instance is not a client.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The close process is in progress.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   A series of reconnecting has failed.
        ///   </para>
        /// </exception>
        public async Task ConnectAsync(CancellationToken stoppingToken)
        {
            if (!_client)
            {
                var msg = "This instance is not a client.";
                throw new InvalidOperationException(msg);
            }

            if (_readyState == WebSocketState.Closing)
            {
                var msg = "The close process is in progress.";
                throw new InvalidOperationException(msg);
            }

            if (_retryCountForConnect > _maxRetryCountForConnect)
            {
                var msg = "A series of reconnecting has failed.";
                throw new InvalidOperationException(msg);
            }

            if (await InternalConnectAsync(stoppingToken))
                await OpenAsync(stoppingToken);
            else
                throw new Exception("could not connect");
        }

        /// <summary>
        /// Sends a ping using the WebSocket connection.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the send has done with no error and a pong has been
        /// received within a time; otherwise, <c>false</c>.
        /// </returns>
        public async Task<bool> PingAsync(CancellationToken cancellationToken)
        {
            return await PingAsync(EmptyBytes, cancellationToken);
        }

        /// <summary>
        /// Sends a ping with <paramref name="message"/> using the WebSocket
        /// connection.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the send has done with no error and a pong has been
        /// received within a time; otherwise, <c>false</c>.
        /// </returns>
        /// <param name="message">
        ///   <para>
        ///   A <see cref="string"/> that represents the message to send.
        ///   </para>
        ///   <para>
        ///   The size must be 125 bytes or less in UTF-8.
        ///   </para>
        /// </param>
        /// <exception cref="ArgumentException">
        /// <paramref name="message"/> could not be UTF-8-encoded.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The size of <paramref name="message"/> is greater than 125 bytes.
        /// </exception>
        public async Task<bool> PingAsync(string message, CancellationToken cancellationToken)
        {
            if (message.IsNullOrEmpty())
                return await PingAsync(EmptyBytes, cancellationToken);

            byte[] bytes;
            if (!message.TryGetUTF8EncodedBytes(out bytes))
            {
                var msg = "It could not be UTF-8-encoded.";
                throw new ArgumentException(msg, "message");
            }

            if (bytes.Length > 125)
            {
                var msg = "Its size is greater than 125 bytes.";
                throw new ArgumentOutOfRangeException("message", msg);
            }

            return await PingAsync(bytes, cancellationToken);
        }

        /// <summary>
        /// Sends the specified data using the WebSocket connection.
        /// </summary>
        /// <param name="data">
        /// An array of <see cref="byte"/> that represents the binary data to send.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The current state of the connection is not Open.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="data"/> is <see langword="null"/>.
        /// </exception>
        public async Task SendAsync(byte[] data, CancellationToken cancellationToken)
        {
            if (_readyState != WebSocketState.Open)
            {
                var msg = "The current state of the connection is not Open.";
                throw new InvalidOperationException(msg);
            }

            if (data == null)
                throw new ArgumentNullException("data");

            await SendAsync(Opcode.Binary, new MemoryStream(data), cancellationToken);
        }

        /// <summary>
        /// Sends the specified file using the WebSocket connection.
        /// </summary>
        /// <param name="fileInfo">
        ///   <para>
        ///   A <see cref="FileInfo"/> that specifies the file to send.
        ///   </para>
        ///   <para>
        ///   The file is sent as the binary data.
        ///   </para>
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The current state of the connection is not Open.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="fileInfo"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   The file does not exist.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The file could not be opened.
        ///   </para>
        /// </exception>
        public async Task SendAsync(FileInfo fileInfo, CancellationToken cancellationToken)
        {
            if (_readyState != WebSocketState.Open)
            {
                var msg = "The current state of the connection is not Open.";
                throw new InvalidOperationException(msg);
            }

            if (fileInfo == null)
                throw new ArgumentNullException("fileInfo");

            if (!fileInfo.Exists)
            {
                var msg = "The file does not exist.";
                throw new ArgumentException(msg, "fileInfo");
            }

            FileStream stream;
            if (!fileInfo.TryOpenRead(out stream))
            {
                var msg = "The file could not be opened.";
                throw new ArgumentException(msg, "fileInfo");
            }

            await SendAsync(Opcode.Binary, stream, cancellationToken);
        }

        /// <summary>
        /// Sends the specified data using the WebSocket connection.
        /// </summary>
        /// <param name="data">
        /// A <see cref="string"/> that represents the text data to send.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The current state of the connection is not Open.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="data"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="data"/> could not be UTF-8-encoded.
        /// </exception>
        public async Task SendAsync(string data, CancellationToken cancellationToken)
        {
            if (_readyState != WebSocketState.Open)
            {
                var msg = "The current state of the connection is not Open.";
                throw new InvalidOperationException(msg);
            }

            if (data == null)
                throw new ArgumentNullException("data");

            byte[] bytes;
            if (!data.TryGetUTF8EncodedBytes(out bytes))
            {
                var msg = "It could not be UTF-8-encoded.";
                throw new ArgumentException(msg, "data");
            }

            await SendAsync(Opcode.Text, new MemoryStream(bytes), cancellationToken);
        }

        /// <summary>
        /// Sends the data from the specified stream using the WebSocket connection.
        /// </summary>
        /// <param name="stream">
        ///   <para>
        ///   A <see cref="Stream"/> instance from which to read the data to send.
        ///   </para>
        ///   <para>
        ///   The data is sent as the binary data.
        ///   </para>
        /// </param>
        /// <param name="length">
        /// An <see cref="int"/> that specifies the number of bytes to send.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The current state of the connection is not Open.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="stream"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="stream"/> cannot be read.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="length"/> is less than 1.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   No data could be read from <paramref name="stream"/>.
        ///   </para>
        /// </exception>
        public async Task SendAsync(Stream stream, int length, CancellationToken cancellationToken)
        {
            if (_readyState != WebSocketState.Open)
            {
                var msg = "The current state of the connection is not Open.";
                throw new InvalidOperationException(msg);
            }

            if (stream == null)
                throw new ArgumentNullException("stream");

            if (!stream.CanRead)
            {
                var msg = "It cannot be read.";
                throw new ArgumentException(msg, "stream");
            }

            if (length < 1)
            {
                var msg = "Less than 1.";
                throw new ArgumentException(msg, "length");
            }

            var bytes = await Ext.ExtReadBytesAsync(stream, length, CancellationToken.None);

            var len = bytes.Length;
            if (len == 0)
            {
                var msg = "No data could be read from it.";
                throw new ArgumentException(msg, "stream");
            }

            if (len < length)
            {
                _logger.Warn($"Only {len} byte(s) of data could be read from the stream.");
            }

            await SendAsync(Opcode.Binary, new MemoryStream(bytes), cancellationToken);
        }

        #endregion

        #region Explicit Interface Implementations

        /// <summary>
        /// Closes the connection and releases all associated resources.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   This method closes the connection with close status 1001 (going away).
        ///   </para>
        ///   <para>
        ///   And this method does nothing if the current state of the connection is
        ///   Closing or Closed.
        ///   </para>
        /// </remarks>
        public void Dispose()
        {
            CloseAsync(1001, String.Empty, CancellationToken.None).Wait();
        }

        #endregion
    }
}
