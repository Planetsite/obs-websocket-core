using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketSharp.Net.WebSockets;

/// <summary>
/// Provides the access to the information in a WebSocket handshake request to
/// a <see cref="TcpListener"/> instance.
/// </summary>
internal sealed class TcpListenerWebSocketContext : WebSocketContext
{
    private NameValueCollection _queryString;
    private HttpRequest _request;
    private Uri _requestUri;
    private bool _secure;
    private System.Net.EndPoint _serverEndPoint;
    private TcpClient _tcpClient;
    private IPrincipal _user;
    private System.Net.EndPoint _userEndPoint;
    private WebSocket _websocket;

    internal TcpListenerWebSocketContext(
      TcpClient tcpClient,
      string protocol,
      bool secure,
      ServerSslConfiguration sslConfig,
      Logger log
    )
    {
        _tcpClient = tcpClient;
        _secure = secure;
        Log = log;

        var netStream = tcpClient.GetStream();
        if (secure)
        {
            var sslStream = new SslStream(
                netStream,
                false,
                sslConfig.ClientCertificateValidationCallback
            );

            sslStream.AuthenticateAsServer(
              sslConfig.ServerCertificate,
              sslConfig.ClientCertificateRequired,
              sslConfig.EnabledSslProtocols,
              sslConfig.CheckCertificateRevocation
            );

            Stream = sslStream;
        }
        else
        {
            Stream = netStream;
        }

        var sock = tcpClient.Client;
        _serverEndPoint = sock.LocalEndPoint;
        _userEndPoint = sock.RemoteEndPoint;

        _request = HttpRequest.ReadAsync(Stream, 90000).Result;
        _websocket = new WebSocket(this, protocol);
    }

    internal Logger Log { get; }

    internal Stream Stream { get; }

    /// <summary>
    /// Gets the HTTP cookies included in the handshake request.
    /// </summary>
    /// <value>
    ///   <para>
    ///   A <see cref="WebSocketSharp.Net.CookieCollection"/> that contains
    ///   the cookies.
    ///   </para>
    ///   <para>
    ///   An empty collection if not included.
    ///   </para>
    /// </value>
    public override CookieCollection CookieCollection => _request.Cookies;

    /// <summary>
    /// Gets the HTTP headers included in the handshake request.
    /// </summary>
    /// <value>
    /// A <see cref="NameValueCollection"/> that contains the headers.
    /// </value>
    public override NameValueCollection Headers => _request.Headers;

    /// <summary>
    /// Gets the value of the Host header included in the handshake request.
    /// </summary>
    /// <value>
    ///   <para>
    ///   A <see cref="string"/> that represents the server host name requested
    ///   by the client.
    ///   </para>
    ///   <para>
    ///   It includes the port number if provided.
    ///   </para>
    /// </value>
    public override string Host => _request.Headers["Host"];

    /// <summary>
    /// Gets a value indicating whether the client is authenticated.
    /// </summary>
    /// <value>
    /// <c>true</c> if the client is authenticated; otherwise, <c>false</c>.
    /// </value>
    public override bool IsAuthenticated => _user != null;

    /// <summary>
    /// Gets a value indicating whether the handshake request is sent from
    /// the local computer.
    /// </summary>
    /// <value>
    /// <c>true</c> if the handshake request is sent from the same computer
    /// as the server; otherwise, <c>false</c>.
    /// </value>
    public override bool IsLocal => UserEndPoint.Address.IsLocal();

    /// <summary>
    /// Gets a value indicating whether a secure connection is used to send
    /// the handshake request.
    /// </summary>
    /// <value>
    /// <c>true</c> if the connection is secure; otherwise, <c>false</c>.
    /// </value>
    public override bool IsSecureConnection => _secure;

    /// <summary>
    /// Gets a value indicating whether the request is a WebSocket handshake
    /// request.
    /// </summary>
    /// <value>
    /// <c>true</c> if the request is a WebSocket handshake request; otherwise,
    /// <c>false</c>.
    /// </value>
    public override bool IsWebSocketRequest => _request.IsWebSocketRequest;

    /// <summary>
    /// Gets the value of the Origin header included in the handshake request.
    /// </summary>
    /// <value>
    ///   <para>
    ///   A <see cref="string"/> that represents the value of the Origin header.
    ///   </para>
    ///   <para>
    ///   <see langword="null"/> if the header is not present.
    ///   </para>
    /// </value>
    public override string Origin => _request.Headers["Origin"];

    /// <summary>
    /// Gets the query string included in the handshake request.
    /// </summary>
    /// <value>
    ///   <para>
    ///   A <see cref="NameValueCollection"/> that contains the query
    ///   parameters.
    ///   </para>
    ///   <para>
    ///   An empty collection if not included.
    ///   </para>
    /// </value>
    public override NameValueCollection QueryString
    {
        get
        {
            if (_queryString == null)
            {
                var uri = RequestUri;
                _queryString = QueryStringCollection.Parse(uri?.Query, Encoding.UTF8);
            }

            return _queryString;
        }
    }

    /// <summary>
    /// Gets the URI requested by the client.
    /// </summary>
    /// <value>
    ///   <para>
    ///   A <see cref="Uri"/> that represents the URI parsed from the request.
    ///   </para>
    ///   <para>
    ///   <see langword="null"/> if the URI cannot be parsed.
    ///   </para>
    /// </value>
    public override Uri RequestUri
    {
        get
        {
            if (_requestUri == null)
            {
                _requestUri = HttpUtility.CreateRequestUrl(
                    _request.RequestUri,
                    _request.Headers["Host"],
                    _request.IsWebSocketRequest,
                    _secure
                );
            }

            return _requestUri;
        }
    }

    /// <summary>
    /// Gets the value of the Sec-WebSocket-Key header included in
    /// the handshake request.
    /// </summary>
    /// <value>
    ///   <para>
    ///   A <see cref="string"/> that represents the value of
    ///   the Sec-WebSocket-Key header.
    ///   </para>
    ///   <para>
    ///   The value is used to prove that the server received
    ///   a valid WebSocket handshake request.
    ///   </para>
    ///   <para>
    ///   <see langword="null"/> if the header is not present.
    ///   </para>
    /// </value>
    public override string SecWebSocketKey => _request.Headers["Sec-WebSocket-Key"];

    /// <summary>
    /// Gets the names of the subprotocols from the Sec-WebSocket-Protocol
    /// header included in the handshake request.
    /// </summary>
    /// <value>
    ///   <para>
    ///   An <see cref="T:System.Collections.Generic.IEnumerable{string}"/>
    ///   instance.
    ///   </para>
    ///   <para>
    ///   It provides an enumerator which supports the iteration over
    ///   the collection of the names of the subprotocols.
    ///   </para>
    /// </value>
    public override IEnumerable<string> SecWebSocketProtocols
    {
        get
        {
            var val = _request.Headers["Sec-WebSocket-Protocol"];
            if (val == null || val.Length == 0)
                yield break;

            foreach (var elm in val.Split(','))
            {
                var protocol = elm.Trim();
                if (protocol.Length == 0)
                    continue;

                yield return protocol;
            }
        }
    }

    /// <summary>
    /// Gets the value of the Sec-WebSocket-Version header included in
    /// the handshake request.
    /// </summary>
    /// <value>
    ///   <para>
    ///   A <see cref="string"/> that represents the WebSocket protocol
    ///   version specified by the client.
    ///   </para>
    ///   <para>
    ///   <see langword="null"/> if the header is not present.
    ///   </para>
    /// </value>
    public override string SecWebSocketVersion => _request.Headers["Sec-WebSocket-Version"];

    /// <summary>
    /// Gets the endpoint to which the handshake request is sent.
    /// </summary>
    /// <value>
    /// A <see cref="System.Net.IPEndPoint"/> that represents the server IP
    /// address and port number.
    /// </value>
    public override System.Net.IPEndPoint ServerEndPoint => (System.Net.IPEndPoint)_serverEndPoint;

    /// <summary>
    /// Gets the client information.
    /// </summary>
    /// <value>
    ///   <para>
    ///   A <see cref="IPrincipal"/> instance that represents identity,
    ///   authentication, and security roles for the client.
    ///   </para>
    ///   <para>
    ///   <see langword="null"/> if the client is not authenticated.
    ///   </para>
    /// </value>
    public override IPrincipal User => _user;

    /// <summary>
    /// Gets the endpoint from which the handshake request is sent.
    /// </summary>
    /// <value>
    /// A <see cref="System.Net.IPEndPoint"/> that represents the client IP
    /// address and port number.
    /// </value>
    public override System.Net.IPEndPoint UserEndPoint => (System.Net.IPEndPoint)_userEndPoint;

    /// <summary>
    /// Gets the WebSocket instance used for two-way communication between
    /// the client and server.
    /// </summary>
    /// <value>
    /// A <see cref="WebSocketSharp.WebSocket"/>.
    /// </value>
    public override WebSocket WebSocket => _websocket;

    private async Task<HttpRequest> sendAuthenticationChallengeAsync(string challenge)
    {
        var res = HttpResponse.CreateUnauthorizedResponse(challenge);
        var bytes = res.ToByteArray();
        await Stream.WriteAsync(bytes, 0, bytes.Length);

        return await HttpRequest.ReadAsync(Stream, 15000);
    }

    internal async Task<bool> AuthenticateAsync(AuthenticationSchemes scheme, string realm, Func<IIdentity, NetworkCredential> credentialsFinder)
    {
        var chal = new AuthenticationChallenge(scheme, realm).ToString();

        for (int retry = -1; retry < 99; ++retry)
        {
            var user = HttpUtility.CreateUser(
                _request.Headers["Authorization"],
                scheme,
                realm,
                _request.HttpMethod,
                credentialsFinder
            );

            if (user != null && user.Identity.IsAuthenticated)
            {
                _user = user;
                return true;
            }

            _request = await sendAuthenticationChallengeAsync(chal);
        };

        return false;
    }

    internal Task CloseAsync()
    {
        Stream.Close();
        _tcpClient.Close();
        return Task.CompletedTask;
    }

    internal async Task CloseAsync(HttpStatusCode code, CancellationToken stoppingToken)
    {
        var res = HttpResponse.CreateCloseResponse(code);
        var bytes = res.ToByteArray();
        await Stream.WriteAsync(bytes, 0, bytes.Length, stoppingToken);

        Stream.Close();
        _tcpClient.Close();
    }

    /// <summary>
    /// Returns a string that represents the current instance.
    /// </summary>
    /// <returns>
    /// A <see cref="string"/> that contains the request line and headers
    /// included in the handshake request.
    /// </returns>
    public override string ToString()
    {
        return _request.ToString();
    }
}
