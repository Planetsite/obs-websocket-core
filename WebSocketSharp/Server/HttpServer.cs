using System;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp.Net;
using WebSocketSharp.Net.WebSockets;

namespace WebSocketSharp.Server;

/// <summary>
/// Provides a simple HTTP server that allows to accept
/// WebSocket handshake requests.
/// </summary>
/// <remarks>
/// This class can provide multiple WebSocket services.
/// </remarks>
public sealed class HttpServer
{
    //private Thread _receiveThread;
    private string _docRootPath;
    private string _hostname;
    private HttpListener _listener;
    private volatile ServerState _state;
    private object _sync;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpServer"/> class.
    /// </summary>
    /// <remarks>
    /// The new instance listens for incoming requests on
    /// <see cref="System.Net.IPAddress.Any"/> and port 80.
    /// </remarks>
    public HttpServer()
    {
        Init("*", System.Net.IPAddress.Any, 80, false);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpServer"/> class with
    /// the specified <paramref name="port"/>.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   The new instance listens for incoming requests on
    ///   <see cref="System.Net.IPAddress.Any"/> and <paramref name="port"/>.
    ///   </para>
    ///   <para>
    ///   It provides secure connections if <paramref name="port"/> is 443.
    ///   </para>
    /// </remarks>
    /// <param name="port">
    /// An <see cref="int"/> that represents the number of the port
    /// on which to listen.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="port"/> is less than 1 or greater than 65535.
    /// </exception>
    public HttpServer(int port)
      : this(port, port == 443)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpServer"/> class with
    /// the specified <paramref name="url"/>.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   The new instance listens for incoming requests on the IP address of the
    ///   host of <paramref name="url"/> and the port of <paramref name="url"/>.
    ///   </para>
    ///   <para>
    ///   Either port 80 or 443 is used if <paramref name="url"/> includes
    ///   no port. Port 443 is used if the scheme of <paramref name="url"/>
    ///   is https; otherwise, port 80 is used.
    ///   </para>
    ///   <para>
    ///   The new instance provides secure connections if the scheme of
    ///   <paramref name="url"/> is https.
    ///   </para>
    /// </remarks>
    /// <param name="url">
    /// A <see cref="string"/> that represents the HTTP URL of the server.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="url"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///   <para>
    ///   <paramref name="url"/> is empty.
    ///   </para>
    ///   <para>
    ///   -or-
    ///   </para>
    ///   <para>
    ///   <paramref name="url"/> is invalid.
    ///   </para>
    /// </exception>
    public HttpServer(string url)
    {
        if (url == null)
            throw new ArgumentNullException("url");

        if (url.Length == 0)
            throw new ArgumentException("An empty string.", "url");

        Uri uri;
        string msg;
        if (!TryCreateUri(url, out uri, out msg))
            throw new ArgumentException(msg, "url");

        var host = uri.GetDnsSafeHost(true);

        var addr = host.ToIPAddress();
        if (addr == null)
        {
            msg = "The host part could not be converted to an IP address.";
            throw new ArgumentException(msg, "url");
        }

        if (!addr.IsLocal())
        {
            msg = "The IP address of the host is not a local IP address.";
            throw new ArgumentException(msg, "url");
        }

        Init(host, addr, uri.Port, uri.Scheme == "https");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpServer"/> class with
    /// the specified <paramref name="port"/> and <paramref name="secure"/>.
    /// </summary>
    /// <remarks>
    /// The new instance listens for incoming requests on
    /// <see cref="System.Net.IPAddress.Any"/> and <paramref name="port"/>.
    /// </remarks>
    /// <param name="port">
    /// An <see cref="int"/> that represents the number of the port
    /// on which to listen.
    /// </param>
    /// <param name="secure">
    /// A <see cref="bool"/>: <c>true</c> if the new instance provides
    /// secure connections; otherwise, <c>false</c>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="port"/> is less than 1 or greater than 65535.
    /// </exception>
    public HttpServer(int port, bool secure)
    {
        if (!port.IsPortNumber())
        {
            var msg = "Less than 1 or greater than 65535.";
            throw new ArgumentOutOfRangeException("port", msg);
        }

        Init("*", System.Net.IPAddress.Any, port, secure);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpServer"/> class with
    /// the specified <paramref name="address"/> and <paramref name="port"/>.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   The new instance listens for incoming requests on
    ///   <paramref name="address"/> and <paramref name="port"/>.
    ///   </para>
    ///   <para>
    ///   It provides secure connections if <paramref name="port"/> is 443.
    ///   </para>
    /// </remarks>
    /// <param name="address">
    /// A <see cref="System.Net.IPAddress"/> that represents
    /// the local IP address on which to listen.
    /// </param>
    /// <param name="port">
    /// An <see cref="int"/> that represents the number of the port
    /// on which to listen.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="address"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="address"/> is not a local IP address.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="port"/> is less than 1 or greater than 65535.
    /// </exception>
    public HttpServer(System.Net.IPAddress address, int port)
      : this(address, port, port == 443)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpServer"/> class with
    /// the specified <paramref name="address"/>, <paramref name="port"/>,
    /// and <paramref name="secure"/>.
    /// </summary>
    /// <remarks>
    /// The new instance listens for incoming requests on
    /// <paramref name="address"/> and <paramref name="port"/>.
    /// </remarks>
    /// <param name="address">
    /// A <see cref="System.Net.IPAddress"/> that represents
    /// the local IP address on which to listen.
    /// </param>
    /// <param name="port">
    /// An <see cref="int"/> that represents the number of the port
    /// on which to listen.
    /// </param>
    /// <param name="secure">
    /// A <see cref="bool"/>: <c>true</c> if the new instance provides
    /// secure connections; otherwise, <c>false</c>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="address"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="address"/> is not a local IP address.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="port"/> is less than 1 or greater than 65535.
    /// </exception>
    public HttpServer(System.Net.IPAddress address, int port, bool secure)
    {
        if (address == null)
            throw new ArgumentNullException("address");

        if (!address.IsLocal())
            throw new ArgumentException("Not a local IP address.", "address");

        if (!port.IsPortNumber())
        {
            var msg = "Less than 1 or greater than 65535.";
            throw new ArgumentOutOfRangeException("port", msg);
        }

        Init(address.ToString(true), address, port, secure);
    }

    /// <summary>
    /// Gets the IP address of the server.
    /// </summary>
    /// <value>
    /// A <see cref="System.Net.IPAddress"/> that represents the local
    /// IP address on which to listen for incoming requests.
    /// </value>
    public System.Net.IPAddress Address { get; private set; }

    /// <summary>
    /// Gets or sets the scheme used to authenticate the clients.
    /// </summary>
    /// <remarks>
    /// The set operation does nothing if the server has already
    /// started or it is shutting down.
    /// </remarks>
    /// <value>
    ///   <para>
    ///   One of the <see cref="WebSocketSharp.Net.AuthenticationSchemes"/>
    ///   enum values.
    ///   </para>
    ///   <para>
    ///   It represents the scheme used to authenticate the clients.
    ///   </para>
    ///   <para>
    ///   The default value is
    ///   <see cref="WebSocketSharp.Net.AuthenticationSchemes.Anonymous"/>.
    ///   </para>
    /// </value>
    public AuthenticationSchemes AuthenticationSchemes
    {
        get => _listener.AuthenticationSchemes;

        set
        {
            if (!CanSet(out string msg))
            {
                Log.Warn(msg);
                return;
            }

            lock (_sync)
            {
                if (!CanSet(out msg))
                {
                    Log.Warn(msg);
                    return;
                }

                _listener.AuthenticationSchemes = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the path to the document folder of the server.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   '/' or '\' is trimmed from the end of the value if any.
    ///   </para>
    ///   <para>
    ///   The set operation does nothing if the server has already
    ///   started or it is shutting down.
    ///   </para>
    /// </remarks>
    /// <value>
    ///   <para>
    ///   A <see cref="string"/> that represents a path to the folder
    ///   from which to find the requested file.
    ///   </para>
    ///   <para>
    ///   The default value is "./Public".
    ///   </para>
    /// </value>
    /// <exception cref="ArgumentNullException">
    /// The value specified for a set operation is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///   <para>
    ///   The value specified for a set operation is an empty string.
    ///   </para>
    ///   <para>
    ///   -or-
    ///   </para>
    ///   <para>
    ///   The value specified for a set operation is an invalid path string.
    ///   </para>
    ///   <para>
    ///   -or-
    ///   </para>
    ///   <para>
    ///   The value specified for a set operation is an absolute root.
    ///   </para>
    /// </exception>
    public string DocumentRootPath
    {
        get => _docRootPath;

        set
        {
            if (value == null)
                throw new ArgumentNullException("value");

            if (value.Length == 0)
                throw new ArgumentException("An empty string.", "value");

            value = value.TrimSlashOrBackslashFromEnd();

            string full = null;
            try
            {
                full = Path.GetFullPath(value);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("An invalid path string.", "value", ex);
            }

            if (value == "/")
                throw new ArgumentException("An absolute root.", "value");

            if (value == "\\")
                throw new ArgumentException("An absolute root.", "value");

            if (value.Length == 2 && value[1] == ':')
                throw new ArgumentException("An absolute root.", "value");

            if (full == "/")
                throw new ArgumentException("An absolute root.", "value");

            full = full.TrimSlashOrBackslashFromEnd();
            if (full.Length == 2 && full[1] == ':')
                throw new ArgumentException("An absolute root.", "value");

            string msg;
            if (!CanSet(out msg))
            {
                Log.Warn(msg);
                return;
            }

            lock (_sync)
            {
                if (!CanSet(out msg))
                {
                    Log.Warn(msg);
                    return;
                }

                _docRootPath = value;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the server has started.
    /// </summary>
    /// <value>
    /// <c>true</c> if the server has started; otherwise, <c>false</c>.
    /// </value>
    public bool IsListening => _state == ServerState.Start;

    /// <summary>
    /// Gets a value indicating whether secure connections are provided.
    /// </summary>
    /// <value>
    /// <c>true</c> if this instance provides secure connections; otherwise,
    /// <c>false</c>.
    /// </value>
    public bool IsSecure { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether the server cleans up
    /// the inactive sessions periodically.
    /// </summary>
    /// <remarks>
    /// The set operation does nothing if the server has already
    /// started or it is shutting down.
    /// </remarks>
    /// <value>
    ///   <para>
    ///   <c>true</c> if the server cleans up the inactive sessions
    ///   every 60 seconds; otherwise, <c>false</c>.
    ///   </para>
    ///   <para>
    ///   The default value is <c>true</c>.
    ///   </para>
    /// </value>
    public bool KeepClean
    {
        get => WebSocketServices.KeepClean;

        set => WebSocketServices.KeepClean = value;
    }

    /// <summary>
    /// Gets the logging function for the server.
    /// </summary>
    /// <remarks>
    /// The default logging level is <see cref="LogLevel.Error"/>.
    /// </remarks>
    /// <value>
    /// A <see cref="Logger"/> that provides the logging function.
    /// </value>
    public Logger Log { get; private set; }

    /// <summary>
    /// Gets the port of the server.
    /// </summary>
    /// <value>
    /// An <see cref="int"/> that represents the number of the port
    /// on which to listen for incoming requests.
    /// </value>
    public int Port { get; private set; }

    /// <summary>
    /// Gets or sets the realm used for authentication.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   "SECRET AREA" is used as the realm if the value is
    ///   <see langword="null"/> or an empty string.
    ///   </para>
    ///   <para>
    ///   The set operation does nothing if the server has
    ///   already started or it is shutting down.
    ///   </para>
    /// </remarks>
    /// <value>
    ///   <para>
    ///   A <see cref="string"/> or <see langword="null"/> by default.
    ///   </para>
    ///   <para>
    ///   That string represents the name of the realm.
    ///   </para>
    /// </value>
    public string Realm
    {
        get => _listener.Realm;

        set
        {
            string msg;
            if (!CanSet(out msg))
            {
                Log.Warn(msg);
                return;
            }

            lock (_sync)
            {
                if (!CanSet(out msg))
                {
                    Log.Warn(msg);
                    return;
                }

                _listener.Realm = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the server is allowed to
    /// be bound to an address that is already in use.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   You should set this property to <c>true</c> if you would
    ///   like to resolve to wait for socket in TIME_WAIT state.
    ///   </para>
    ///   <para>
    ///   The set operation does nothing if the server has already
    ///   started or it is shutting down.
    ///   </para>
    /// </remarks>
    /// <value>
    ///   <para>
    ///   <c>true</c> if the server is allowed to be bound to an address
    ///   that is already in use; otherwise, <c>false</c>.
    ///   </para>
    ///   <para>
    ///   The default value is <c>false</c>.
    ///   </para>
    /// </value>
    public bool ReuseAddress
    {
        get => _listener.ReuseAddress;

        set
        {
            if (!CanSet(out string msg))
            {
                Log.Warn(msg);
                return;
            }

            lock (_sync)
            {
                if (!CanSet(out msg))
                {
                    Log.Warn(msg);
                    return;
                }

                _listener.ReuseAddress = value;
            }
        }
    }

    /// <summary>
    /// Gets the configuration for secure connection.
    /// </summary>
    /// <remarks>
    /// This configuration will be referenced when attempts to start,
    /// so it must be configured before the start method is called.
    /// </remarks>
    /// <value>
    /// A <see cref="ServerSslConfiguration"/> that represents
    /// the configuration used to provide secure connections.
    /// </value>
    /// <exception cref="InvalidOperationException">
    /// This instance does not provide secure connections.
    /// </exception>
    public ServerSslConfiguration SslConfiguration
    {
        get
        {
            if (!IsSecure)
            {
                var msg = "This instance does not provide secure connections.";
                throw new InvalidOperationException(msg);
            }

            return _listener.SslConfiguration;
        }
    }

    /// <summary>
    /// Gets or sets the delegate used to find the credentials
    /// for an identity.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   No credentials are found if the method invoked by
    ///   the delegate returns <see langword="null"/> or
    ///   the value is <see langword="null"/>.
    ///   </para>
    ///   <para>
    ///   The set operation does nothing if the server has
    ///   already started or it is shutting down.
    ///   </para>
    /// </remarks>
    /// <value>
    ///   <para>
    ///   A <c>Func&lt;<see cref="IIdentity"/>,
    ///   <see cref="NetworkCredential"/>&gt;</c> delegate or
    ///   <see langword="null"/> if not needed.
    ///   </para>
    ///   <para>
    ///   That delegate invokes the method called for finding
    ///   the credentials used to authenticate a client.
    ///   </para>
    ///   <para>
    ///   The default value is <see langword="null"/>.
    ///   </para>
    /// </value>
    public Func<IIdentity, NetworkCredential> UserCredentialsFinder
    {
        get => _listener.UserCredentialsFinder;

        set
        {
            string msg;
            if (!CanSet(out msg))
            {
                Log.Warn(msg);
                return;
            }

            lock (_sync)
            {
                if (!CanSet(out msg))
                {
                    Log.Warn(msg);
                    return;
                }

                _listener.UserCredentialsFinder = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the time to wait for the response to the WebSocket Ping or
    /// Close.
    /// </summary>
    /// <remarks>
    /// The set operation does nothing if the server has already started or
    /// it is shutting down.
    /// </remarks>
    /// <value>
    ///   <para>
    ///   A <see cref="TimeSpan"/> to wait for the response.
    ///   </para>
    ///   <para>
    ///   The default value is the same as 1 second.
    ///   </para>
    /// </value>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value specified for a set operation is zero or less.
    /// </exception>
    public TimeSpan WaitTime
    {
        get => WebSocketServices.WaitTime;

        set => WebSocketServices.WaitTime = value;
    }

    /// <summary>
    /// Gets the management function for the WebSocket services
    /// provided by the server.
    /// </summary>
    /// <value>
    /// A <see cref="WebSocketServiceManager"/> that manages
    /// the WebSocket services provided by the server.
    /// </value>
    public WebSocketServiceManager WebSocketServices { get; private set; }

    /// <summary>
    /// Occurs when the server receives an HTTP CONNECT request.
    /// </summary>
    public event EventHandler<HttpRequestEventArgs> OnConnect;

    /// <summary>
    /// Occurs when the server receives an HTTP DELETE request.
    /// </summary>
    public event EventHandler<HttpRequestEventArgs> OnDelete;

    /// <summary>
    /// Occurs when the server receives an HTTP GET request.
    /// </summary>
    public event EventHandler<HttpRequestEventArgs> OnGet;

    /// <summary>
    /// Occurs when the server receives an HTTP HEAD request.
    /// </summary>
    public event EventHandler<HttpRequestEventArgs> OnHead;

    /// <summary>
    /// Occurs when the server receives an HTTP OPTIONS request.
    /// </summary>
    public event EventHandler<HttpRequestEventArgs> OnOptions;

    /// <summary>
    /// Occurs when the server receives an HTTP POST request.
    /// </summary>
    public event EventHandler<HttpRequestEventArgs> OnPost;

    /// <summary>
    /// Occurs when the server receives an HTTP PUT request.
    /// </summary>
    public event EventHandler<HttpRequestEventArgs> OnPut;

    /// <summary>
    /// Occurs when the server receives an HTTP TRACE request.
    /// </summary>
    public event EventHandler<HttpRequestEventArgs> OnTrace;

    private async Task PrivateAbortAsync(CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            if (_state != ServerState.Start)
                return;

            _state = ServerState.ShuttingDown;
        }

        try
        {
            try
            {
                await WebSocketServices.StopAsync(1006, String.Empty, cancellationToken);
            }
            finally
            {
                await _listener.AbortAsync();
            }
        }
        catch
        {
        }

        _state = ServerState.Stop;
    }

    private bool CanSet(out string message)
    {
        message = null;

        if (_state == ServerState.Start)
        {
            message = "The server has already started.";
            return false;
        }

        if (_state == ServerState.ShuttingDown)
        {
            message = "The server is shutting down.";
            return false;
        }

        return true;
    }

    private bool PrivateCheckCertificate(out string message)
    {
        message = null;

        var byUser = _listener.SslConfiguration.ServerCertificate != null;

        var path = _listener.CertificateFolderPath;
        var withPort = EndPointListener.CertificateExists(Port, path);

        if (!(byUser || withPort))
        {
            message = "There is no server certificate for secure connection.";
            return false;
        }

        if (byUser && withPort)
            Log.Warn("The server certificate associated with the port is used.");

        return true;
    }

    private string PrivateCreateFilePath(string childPath)
    {
        childPath = childPath.TrimStart('/', '\\');
        return new StringBuilder(_docRootPath, 32)
            .AppendFormat("/{0}", childPath)
            .ToString()
            .Replace('\\', '/');
    }

    private static HttpListener PrivateCreateListener(string hostname, int port, bool secure)
    {
        var lsnr = new HttpListener();

        var schm = secure ? "https" : "http";
        var pref = String.Format("{0}://{1}:{2}/", schm, hostname, port);
        lsnr.Prefixes.Add(pref);

        return lsnr;
    }

    private void Init(string hostname, System.Net.IPAddress address, int port, bool secure)
    {
        _hostname = hostname;
        Address = address;
        Port = port;
        IsSecure = secure;

        _docRootPath = "./Public";
        _listener = PrivateCreateListener(_hostname, Port, IsSecure);
        Log = _listener.Log;
        WebSocketServices = new WebSocketServiceManager(Log);
        _sync = new object();
    }

    private async Task PrivateProcessRequestAsync(HttpListenerContext context)
    {
        var method = context.Request.HttpMethod;
        var evt = method switch
        {
            "GET" => OnGet,
            "HEAD" => OnHead,
            "POST" => OnPost,
            "PUT" => OnPut,
            "DELETE" => OnDelete,
            "CONNECT" => OnConnect,
            "OPTIONS" => OnOptions,
            "TRACE" => OnTrace,
            _ => null
        };

        if (evt != null)
            evt(this, new HttpRequestEventArgs(context, _docRootPath));
        else
            context.Response.StatusCode = 501; // Not Implemented

        await context.Response.CloseAsync();
    }

    private async Task PrivatePprocessRequestAsync(HttpListenerWebSocketContext context, CancellationToken cancellationToken)
    {
        var uri = context.RequestUri;
        if (uri == null)
        {
            context.Close(HttpStatusCode.BadRequest);
            return;
        }

        var path = uri.AbsolutePath;
        if (path.IndexOfAny(new[] { '%', '+' }) > -1)
            path = HttpUtility.UrlDecode(path, Encoding.UTF8);

        WebSocketServiceHost host;
        if (!WebSocketServices.InternalTryGetServiceHost(path, out host))
        {
            context.Close(HttpStatusCode.NotImplemented);
            return;
        }

        await host.StartSessionAsync(context, cancellationToken);
    }

    private async Task PrivateReceiveRequestAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            HttpListenerContext ctx = null;
            try
            {
                ctx = _listener.GetContext();
                try
                {
                    if (ctx.Request.IsUpgradeRequest("websocket"))
                    {
                        await PrivatePprocessRequestAsync(ctx.AcceptWebSocket(null), cancellationToken);
                        return;
                    }

                    await PrivateProcessRequestAsync(ctx);
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex.Message);
                    Log.Debug(ex.ToString());

                    await ctx.Connection.CloseAsync(true);
                }
            }
            catch (HttpListenerException)
            {
                Log.Info("The underlying listener is stopped.");
                break;
            }
            catch (InvalidOperationException)
            {
                Log.Info("The underlying listener is stopped.");
                break;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex.Message);
                Log.Debug(ex.ToString());

                if (ctx != null)
                    await ctx.Connection.CloseAsync(true);

                break;
            }
        }

        if (_state != ServerState.ShuttingDown)
            await PrivateAbortAsync(cancellationToken);
    }

    private async Task PrivateStartAsync(CancellationToken cancellationToken)
    {
        if (_state == ServerState.Start)
        {
            Log.Info("The server has already started.");
            return;
        }

        if (_state == ServerState.ShuttingDown)
        {
            Log.Warn("The server is shutting down.");
            return;
        }

        //lock (_sync)
        {
            if (_state == ServerState.Start)
            {
                Log.Info("The server has already started.");
                return;
            }

            if (_state == ServerState.ShuttingDown)
            {
                Log.Warn("The server is shutting down.");
                return;
            }

            WebSocketServices.Start(cancellationToken);

            try
            {
                await PrivateStartReceiving(cancellationToken);
            }
            catch
            {
                await WebSocketServices.StopAsync(1011, String.Empty, cancellationToken);
                throw;
            }

            _state = ServerState.Start;
        }
    }

    private async Task PrivateStartReceiving(CancellationToken cancellationToken)
    {
        try
        {
            await _listener.StartAsync();
        }
        catch (Exception ex)
        {
            var msg = "The underlying listener has failed to start.";
            throw new InvalidOperationException(msg, ex);
        }

#pragma warning disable CS4014
        /*await*/
        PrivateReceiveRequestAsync(cancellationToken);
#pragma warning restore CS4014
    }

    private async Task PrivateStopAsync(ushort code, string reason, CancellationToken stoppingToken)
    {
        if (_state == ServerState.Ready)
        {
            Log.Info("The server is not started.");
            return;
        }

        if (_state == ServerState.ShuttingDown)
        {
            Log.Info("The server is shutting down.");
            return;
        }

        if (_state == ServerState.Stop)
        {
            Log.Info("The server has already stopped.");
            return;
        }

        lock (_sync)
        {
            if (_state == ServerState.ShuttingDown)
            {
                Log.Info("The server is shutting down.");
                return;
            }

            if (_state == ServerState.Stop)
            {
                Log.Info("The server has already stopped.");
                return;
            }

            _state = ServerState.ShuttingDown;
        }

        try
        {
            var threw = false;
            try
            {
                await WebSocketServices.StopAsync(code, reason, stoppingToken);
            }
            catch
            {
                threw = true;
                throw;
            }
            finally
            {
                try
                {
                    await PrivateStopReceivingAsync(5000);
                }
                catch
                {
                    if (!threw)
                        throw;
                }
            }
        }
        finally
        {
            _state = ServerState.Stop;
        }
    }

    private async Task PrivateStopReceivingAsync(int millisecondsTimeout)
    {
        await _listener.StopAsync();
        //_receiveThread.Join(millisecondsTimeout);
    }

    private static bool TryCreateUri(string uriString, out Uri result, out string message)
    {
        result = null;
        message = null;

        var uri = uriString.ToUri();
        if (uri == null)
        {
            message = "An invalid URI string.";
            return false;
        }

        if (!uri.IsAbsoluteUri)
        {
            message = "A relative URI.";
            return false;
        }

        var schm = uri.Scheme;
        if (!(schm == "http" || schm == "https"))
        {
            message = "The scheme part is not 'http' or 'https'.";
            return false;
        }

        if (uri.PathAndQuery != "/")
        {
            message = "It includes either or both path and query components.";
            return false;
        }

        if (uri.Fragment.Length > 0)
        {
            message = "It includes the fragment component.";
            return false;
        }

        if (uri.Port == 0)
        {
            message = "The port part is zero.";
            return false;
        }

        result = uri;
        return true;
    }

    /// <summary>
    /// Adds a WebSocket service with the specified behavior, path,
    /// and delegate.
    /// </summary>
    /// <param name="path">
    ///   <para>
    ///   A <see cref="string"/> that represents an absolute path to
    ///   the service to add.
    ///   </para>
    ///   <para>
    ///   / is trimmed from the end of the string if present.
    ///   </para>
    /// </param>
    /// <param name="creator">
    ///   <para>
    ///   A <c>Func&lt;TBehavior&gt;</c> delegate.
    ///   </para>
    ///   <para>
    ///   It invokes the method called when creating a new session
    ///   instance for the service.
    ///   </para>
    ///   <para>
    ///   The method must create a new instance of the specified
    ///   behavior class and return it.
    ///   </para>
    /// </param>
    /// <typeparam name="TBehavior">
    ///   <para>
    ///   The type of the behavior for the service.
    ///   </para>
    ///   <para>
    ///   It must inherit the <see cref="WebSocketBehavior"/> class.
    ///   </para>
    /// </typeparam>
    /// <exception cref="ArgumentNullException">
    ///   <para>
    ///   <paramref name="path"/> is <see langword="null"/>.
    ///   </para>
    ///   <para>
    ///   -or-
    ///   </para>
    ///   <para>
    ///   <paramref name="creator"/> is <see langword="null"/>.
    ///   </para>
    /// </exception>
    /// <exception cref="ArgumentException">
    ///   <para>
    ///   <paramref name="path"/> is an empty string.
    ///   </para>
    ///   <para>
    ///   -or-
    ///   </para>
    ///   <para>
    ///   <paramref name="path"/> is not an absolute path.
    ///   </para>
    ///   <para>
    ///   -or-
    ///   </para>
    ///   <para>
    ///   <paramref name="path"/> includes either or both
    ///   query and fragment components.
    ///   </para>
    ///   <para>
    ///   -or-
    ///   </para>
    ///   <para>
    ///   <paramref name="path"/> is already in use.
    ///   </para>
    /// </exception>
    [Obsolete("This method will be removed. Use added one instead.")]
    public void AddWebSocketService<TBehavior>(string path, Func<TBehavior> creator, CancellationToken stoppingToken)
        where TBehavior : WebSocketBehavior
    {
        if (path == null)
            throw new ArgumentNullException("path");

        if (creator == null)
            throw new ArgumentNullException("creator");

        if (path.Length == 0)
            throw new ArgumentException("An empty string.", "path");

        if (path[0] != '/')
            throw new ArgumentException("Not an absolute path.", "path");

        if (path.IndexOfAny(new[] { '?', '#' }) > -1)
        {
            var msg = "It includes either or both query and fragment components.";
            throw new ArgumentException(msg, "path");
        }

        WebSocketServices.Add(path, creator, stoppingToken);
    }

    /// <summary>
    /// Adds a WebSocket service with the specified behavior and path.
    /// </summary>
    /// <param name="path">
    ///   <para>
    ///   A <see cref="string"/> that represents an absolute path to
    ///   the service to add.
    ///   </para>
    ///   <para>
    ///   / is trimmed from the end of the string if present.
    ///   </para>
    /// </param>
    /// <typeparam name="TBehaviorWithNew">
    ///   <para>
    ///   The type of the behavior for the service.
    ///   </para>
    ///   <para>
    ///   It must inherit the <see cref="WebSocketBehavior"/> class.
    ///   </para>
    ///   <para>
    ///   And also, it must have a public parameterless constructor.
    ///   </para>
    /// </typeparam>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="path"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///   <para>
    ///   <paramref name="path"/> is an empty string.
    ///   </para>
    ///   <para>
    ///   -or-
    ///   </para>
    ///   <para>
    ///   <paramref name="path"/> is not an absolute path.
    ///   </para>
    ///   <para>
    ///   -or-
    ///   </para>
    ///   <para>
    ///   <paramref name="path"/> includes either or both
    ///   query and fragment components.
    ///   </para>
    ///   <para>
    ///   -or-
    ///   </para>
    ///   <para>
    ///   <paramref name="path"/> is already in use.
    ///   </para>
    /// </exception>
    public void AddWebSocketService<TBehaviorWithNew>(string path, CancellationToken stoppingToken)
        where TBehaviorWithNew : WebSocketBehavior, new()
    {
        WebSocketServices.AddService<TBehaviorWithNew>(path, null, stoppingToken);
    }

    /// <summary>
    /// Adds a WebSocket service with the specified behavior, path,
    /// and delegate.
    /// </summary>
    /// <param name="path">
    ///   <para>
    ///   A <see cref="string"/> that represents an absolute path to
    ///   the service to add.
    ///   </para>
    ///   <para>
    ///   / is trimmed from the end of the string if present.
    ///   </para>
    /// </param>
    /// <param name="initializer">
    ///   <para>
    ///   An <c>Action&lt;TBehaviorWithNew&gt;</c> delegate or
    ///   <see langword="null"/> if not needed.
    ///   </para>
    ///   <para>
    ///   The delegate invokes the method called when initializing
    ///   a new session instance for the service.
    ///   </para>
    /// </param>
    /// <typeparam name="TBehaviorWithNew">
    ///   <para>
    ///   The type of the behavior for the service.
    ///   </para>
    ///   <para>
    ///   It must inherit the <see cref="WebSocketBehavior"/> class.
    ///   </para>
    ///   <para>
    ///   And also, it must have a public parameterless constructor.
    ///   </para>
    /// </typeparam>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="path"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///   <para>
    ///   <paramref name="path"/> is an empty string.
    ///   </para>
    ///   <para>
    ///   -or-
    ///   </para>
    ///   <para>
    ///   <paramref name="path"/> is not an absolute path.
    ///   </para>
    ///   <para>
    ///   -or-
    ///   </para>
    ///   <para>
    ///   <paramref name="path"/> includes either or both
    ///   query and fragment components.
    ///   </para>
    ///   <para>
    ///   -or-
    ///   </para>
    ///   <para>
    ///   <paramref name="path"/> is already in use.
    ///   </para>
    /// </exception>
    public void AddWebSocketService<TBehaviorWithNew>(string path, Action<TBehaviorWithNew> initializer, CancellationToken stoppingToken)
        where TBehaviorWithNew : WebSocketBehavior, new()
    {
        WebSocketServices.AddService(path, initializer, stoppingToken);
    }

    /// <summary>
    /// Gets the contents of the specified file from the document
    /// folder of the server.
    /// </summary>
    /// <returns>
    ///   <para>
    ///   An array of <see cref="byte"/> or <see langword="null"/>
    ///   if it fails.
    ///   </para>
    ///   <para>
    ///   That array represents the contents of the file.
    ///   </para>
    /// </returns>
    /// <param name="path">
    /// A <see cref="string"/> that represents a virtual path to
    /// find the file from the document folder.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="path"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///   <para>
    ///   <paramref name="path"/> is an empty string.
    ///   </para>
    ///   <para>
    ///   -or-
    ///   </para>
    ///   <para>
    ///   <paramref name="path"/> contains "..".
    ///   </para>
    /// </exception>
    [Obsolete("This method will be removed.")]
    public byte[] GetFile(string path)
    {
        if (path == null)
            throw new ArgumentNullException("path");

        if (path.Length == 0)
            throw new ArgumentException("An empty string.", "path");

        if (path.IndexOf("..") > -1)
            throw new ArgumentException("It contains '..'.", "path");

        path = PrivateCreateFilePath(path);
        return File.Exists(path) ? File.ReadAllBytes(path) : null;
    }

    /// <summary>
    /// Removes a WebSocket service with the specified path.
    /// </summary>
    /// <remarks>
    /// The service is stopped with close status 1001 (going away)
    /// if it has already started.
    /// </remarks>
    /// <returns>
    /// <c>true</c> if the service is successfully found and removed;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <param name="path">
    ///   <para>
    ///   A <see cref="string"/> that represents an absolute path to
    ///   the service to remove.
    ///   </para>
    ///   <para>
    ///   / is trimmed from the end of the string if present.
    ///   </para>
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="path"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///   <para>
    ///   <paramref name="path"/> is an empty string.
    ///   </para>
    ///   <para>
    ///   -or-
    ///   </para>
    ///   <para>
    ///   <paramref name="path"/> is not an absolute path.
    ///   </para>
    ///   <para>
    ///   -or-
    ///   </para>
    ///   <para>
    ///   <paramref name="path"/> includes either or both
    ///   query and fragment components.
    ///   </para>
    /// </exception>
    public async Task<bool> RemoveWebSocketServiceAsync(string path, CancellationToken stoppingToken)
    {
        return await WebSocketServices.RemoveServiceAsync(path, stoppingToken);
    }

    /// <summary>
    /// Starts receiving incoming requests.
    /// </summary>
    /// <remarks>
    /// This method does nothing if the server has already started or
    /// it is shutting down.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    ///   <para>
    ///   There is no server certificate for secure connection.
    ///   </para>
    ///   <para>
    ///   -or-
    ///   </para>
    ///   <para>
    ///   The underlying <see cref="HttpListener"/> has failed to start.
    ///   </para>
    /// </exception>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (IsSecure)
        {
            string msg;
            if (!PrivateCheckCertificate(out msg))
                throw new InvalidOperationException(msg);
        }

        await PrivateStartAsync(cancellationToken);
    }

    /// <summary>
    /// Stops receiving incoming requests.
    /// </summary>
    public async Task StopAsync(CancellationToken stoppingToken)
    {
        await PrivateStopAsync(1001, String.Empty, stoppingToken);
    }

    /// <summary>
    /// Stops receiving incoming requests and closes each connection.
    /// </summary>
    /// <param name="code">
    ///   <para>
    ///   A <see cref="ushort"/> that represents the status code indicating
    ///   the reason for the WebSocket connection close.
    ///   </para>
    ///   <para>
    ///   The status codes are defined in
    ///   <see href="http://tools.ietf.org/html/rfc6455#section-7.4">
    ///   Section 7.4</see> of RFC 6455.
    ///   </para>
    /// </param>
    /// <param name="reason">
    ///   <para>
    ///   A <see cref="string"/> that represents the reason for the WebSocket
    ///   connection close.
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
    ///   <paramref name="code"/> is 1010 (mandatory extension).
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
    public async Task StopAsync(ushort code, string reason, CancellationToken stoppingToken)
    {
        if (!code.IsCloseStatusCode())
        {
            var msg = "Less than 1000 or greater than 4999.";
            throw new ArgumentOutOfRangeException("code", msg);
        }

        if (code == 1010)
        {
            var msg = "1010 cannot be used.";
            throw new ArgumentException(msg, "code");
        }

        if (!reason.IsNullOrEmpty())
        {
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
        }

        await PrivateStopAsync(code, reason, stoppingToken);
    }

    /// <summary>
    /// Stops receiving incoming requests and closes each connection.
    /// </summary>
    /// <param name="code">
    ///   <para>
    ///   One of the <see cref="CloseStatusCode"/> enum values.
    ///   </para>
    ///   <para>
    ///   It represents the status code indicating the reason for the WebSocket
    ///   connection close.
    ///   </para>
    /// </param>
    /// <param name="reason">
    ///   <para>
    ///   A <see cref="string"/> that represents the reason for the WebSocket
    ///   connection close.
    ///   </para>
    ///   <para>
    ///   The size must be 123 bytes or less in UTF-8.
    ///   </para>
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The size of <paramref name="reason"/> is greater than 123 bytes.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///   <para>
    ///   <paramref name="code"/> is
    ///   <see cref="CloseStatusCode.MandatoryExtension"/>.
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
    public async Task StopAsync(CloseStatusCode code, string reason, CancellationToken stoppingToken)
    {
        if (code == CloseStatusCode.MandatoryExtension)
        {
            var msg = "MandatoryExtension cannot be used.";
            throw new ArgumentException(msg, "code");
        }

        if (!reason.IsNullOrEmpty())
        {
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
        }

        await PrivateStopAsync((ushort)code, reason, stoppingToken);
    }
}
