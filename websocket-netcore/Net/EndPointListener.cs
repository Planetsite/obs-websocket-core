#region License
/*
 * EndPointListener.cs
 *
 * This code is derived from EndPointListener.cs (System.Net) of Mono
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


#region Authors
/*
 * Authors:
 * - Gonzalo Paniagua Javier <gonzalo@novell.com>
 */


#region Contributors
/*
 * Contributors:
 * - Liryna <liryna.stark@gmail.com>
 * - Nicholas Devenish
 */


using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketSharp.Net
{
    internal sealed class EndPointListener
    {
        #region Private Fields

        private List<HttpListenerPrefix> _all; // host == '+'
        private static readonly string _defaultCertFolderPath;
        private IPEndPoint _endpoint;
        private Dictionary<HttpListenerPrefix, HttpListener> _prefixes;
        private bool _secure;
        private Socket _socket;
        private ServerSslConfiguration _sslConfig;
        private List<HttpListenerPrefix> _unhandled; // host == '*'
        private Dictionary<HttpConnection, HttpConnection> _unregistered;
        private object _unregisteredSync;

        

        #region Static Constructor

        static EndPointListener()
        {
            _defaultCertFolderPath =
              Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }

        

        #region Internal Constructors

        internal EndPointListener(
          IPEndPoint endpoint,
          bool secure,
          string certificateFolderPath,
          ServerSslConfiguration sslConfig,
          bool reuseAddress
        )
        {
            if (secure)
            {
                var cert =
                  getCertificate(endpoint.Port, certificateFolderPath, sslConfig.ServerCertificate);

                if (cert == null)
                    throw new ArgumentException("No server certificate could be found.");

                _secure = true;
                _sslConfig = new ServerSslConfiguration(sslConfig);
                _sslConfig.ServerCertificate = cert;
            }

            _endpoint = endpoint;
            _prefixes = new Dictionary<HttpListenerPrefix, HttpListener>();
            _unregistered = new Dictionary<HttpConnection, HttpConnection>();
            _unregisteredSync = ((ICollection)_unregistered).SyncRoot;
            _socket =
              new Socket(endpoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            if (reuseAddress)
                _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            _socket.Bind(endpoint);
            _socket.Listen(500);
            //_socket.BeginAccept(onAccept, this);

            #pragma warning disable CS4014
            /*await*/ AcceptConnection();
            #pragma warning restore CS4014
        }

        

        #region Public Properties

        public IPAddress Address
        {
            get
            {
                return _endpoint.Address;
            }
        }

        public bool IsSecure
        {
            get
            {
                return _secure;
            }
        }

        public int Port
        {
            get
            {
                return _endpoint.Port;
            }
        }

        public ServerSslConfiguration SslConfiguration
        {
            get
            {
                return _sslConfig;
            }
        }

        

        #region Private Methods

        private async Task AcceptConnection()
        {
            do
            {
                var newSocket = await _socket.AcceptAsync();
                await onAccept(newSocket);
                //Task.Delay(1);
            }
            while (_socket.Connected);
        }

        private static void addSpecial(List<HttpListenerPrefix> prefixes, HttpListenerPrefix prefix)
        {
            var path = prefix.Path;
            foreach (var pref in prefixes)
            {
                if (pref.Path == path)
                    throw new HttpListenerException(87, "The prefix is already in use.");
            }

            prefixes.Add(prefix);
        }

        private static RSACryptoServiceProvider createRSAFromFile(string filename)
        {
            byte[] pvk = null;
            using (var fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                pvk = new byte[fs.Length];
                fs.Read(pvk, 0, pvk.Length);
            }

            var rsa = new RSACryptoServiceProvider();
            rsa.ImportCspBlob(pvk);

            return rsa;
        }

        private static X509Certificate2 getCertificate(
          int port, string folderPath, X509Certificate2 defaultCertificate
        )
        {
            if (folderPath == null || folderPath.Length == 0)
                folderPath = _defaultCertFolderPath;

            try
            {
                var cer = Path.Combine(folderPath, String.Format("{0}.cer", port));
                var key = Path.Combine(folderPath, String.Format("{0}.key", port));
                if (File.Exists(cer) && File.Exists(key))
                {
                    var cert = new X509Certificate2(cer);
                    cert.PrivateKey = createRSAFromFile(key);

                    return cert;
                }
            }
            catch
            {
            }

            return defaultCertificate;
        }

        private async Task leaveIfNoPrefixAsync()
        {
            if (_prefixes.Count > 0)
                return;

            var prefs = _unhandled;
            if (prefs != null && prefs.Count > 0)
                return;

            prefs = _all;
            if (prefs != null && prefs.Count > 0)
                return;

            await EndPointManager.RemoveEndPointAsync(_endpoint);
        }

        private async Task onAccept(Socket newSocket)
        {
            await processAcceptedAsync(newSocket);
        }

        private async Task processAcceptedAsync(Socket socket)
        {
            HttpConnection conn = null;
            try
            {
                conn = new HttpConnection(socket, this);
                //lock (listener._unregisteredSync)
                    _unregistered[conn] = conn;

                await conn.BeginReadRequestAsync();
            }
            catch
            {
                if (conn != null)
                {
                    await conn.CloseAsync(true);
                    return;
                }

                socket.Close();
            }
        }

        private static bool removeSpecial(List<HttpListenerPrefix> prefixes, HttpListenerPrefix prefix)
        {
            var path = prefix.Path;
            var cnt = prefixes.Count;
            for (var i = 0; i < cnt; i++)
            {
                if (prefixes[i].Path == path)
                {
                    prefixes.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        private static HttpListener searchHttpListenerFromSpecial(
          string path, List<HttpListenerPrefix> prefixes
        )
        {
            if (prefixes == null)
                return null;

            HttpListener bestMatch = null;

            var bestLen = -1;
            foreach (var pref in prefixes)
            {
                var prefPath = pref.Path;

                var len = prefPath.Length;
                if (len < bestLen)
                    continue;

                if (path.StartsWith(prefPath))
                {
                    bestLen = len;
                    bestMatch = pref.Listener;
                }
            }

            return bestMatch;
        }

        

        #region Internal Methods

        internal static bool CertificateExists(int port, string folderPath)
        {
            if (folderPath == null || folderPath.Length == 0)
                folderPath = _defaultCertFolderPath;

            var cer = Path.Combine(folderPath, String.Format("{0}.cer", port));
            var key = Path.Combine(folderPath, String.Format("{0}.key", port));

            return File.Exists(cer) && File.Exists(key);
        }

        internal void RemoveConnection(HttpConnection connection)
        {
            lock (_unregisteredSync)
                _unregistered.Remove(connection);
        }

        internal bool TrySearchHttpListener(Uri uri, out HttpListener listener)
        {
            listener = null;

            if (uri == null)
                return false;

            var host = uri.Host;
            var dns = Uri.CheckHostName(host) == UriHostNameType.Dns;
            var port = uri.Port.ToString();
            var path = HttpUtility.UrlDecode(uri.AbsolutePath);
            var pathSlash = path[path.Length - 1] != '/' ? path + "/" : path;

            if (host != null && host.Length > 0)
            {
                var bestLen = -1;
                foreach (var pref in _prefixes.Keys)
                {
                    if (dns)
                    {
                        var prefHost = pref.Host;
                        if (Uri.CheckHostName(prefHost) == UriHostNameType.Dns && prefHost != host)
                            continue;
                    }

                    if (pref.Port != port)
                        continue;

                    var prefPath = pref.Path;

                    var len = prefPath.Length;
                    if (len < bestLen)
                        continue;

                    if (path.StartsWith(prefPath) || pathSlash.StartsWith(prefPath))
                    {
                        bestLen = len;
                        listener = _prefixes[pref];
                    }
                }

                if (bestLen != -1)
                    return true;
            }

            var prefs = _unhandled;
            listener = searchHttpListenerFromSpecial(path, prefs);
            if (listener == null && pathSlash != path)
                listener = searchHttpListenerFromSpecial(pathSlash, prefs);

            if (listener != null)
                return true;

            prefs = _all;
            listener = searchHttpListenerFromSpecial(path, prefs);
            if (listener == null && pathSlash != path)
                listener = searchHttpListenerFromSpecial(pathSlash, prefs);

            return listener != null;
        }

        

        #region Public Methods

        public void AddPrefix(HttpListenerPrefix prefix, HttpListener listener)
        {
            List<HttpListenerPrefix> current, future;
            if (prefix.Host == "*")
            {
                do
                {
                    current = _unhandled;
                    future = current != null
                             ? new List<HttpListenerPrefix>(current)
                             : new List<HttpListenerPrefix>();

                    prefix.Listener = listener;
                    addSpecial(future, prefix);
                }
                while (Interlocked.CompareExchange(ref _unhandled, future, current) != current);

                return;
            }

            if (prefix.Host == "+")
            {
                do
                {
                    current = _all;
                    future = current != null
                             ? new List<HttpListenerPrefix>(current)
                             : new List<HttpListenerPrefix>();

                    prefix.Listener = listener;
                    addSpecial(future, prefix);
                }
                while (Interlocked.CompareExchange(ref _all, future, current) != current);

                return;
            }

            Dictionary<HttpListenerPrefix, HttpListener> prefs, prefs2;
            do
            {
                prefs = _prefixes;
                if (prefs.ContainsKey(prefix))
                {
                    if (prefs[prefix] != listener)
                    {
                        throw new HttpListenerException(
                          87, String.Format("There's another listener for {0}.", prefix)
                        );
                    }

                    return;
                }

                prefs2 = new Dictionary<HttpListenerPrefix, HttpListener>(prefs);
                prefs2[prefix] = listener;
            }
            while (Interlocked.CompareExchange(ref _prefixes, prefs2, prefs) != prefs);
        }

        public async Task CloseAsync()
        {
            _socket.Close();

            HttpConnection[] conns = null;
            lock (_unregisteredSync)
            {
                if (_unregistered.Count == 0)
                    return;

                var keys = _unregistered.Keys;
                conns = new HttpConnection[keys.Count];
                keys.CopyTo(conns, 0);
                _unregistered.Clear();
            }

            for (var i = conns.Length - 1; i >= 0; i--)
                await conns[i].CloseAsync(true);
        }

        public async Task RemovePrefixAsync(HttpListenerPrefix prefix, HttpListener listener)
        {
            List<HttpListenerPrefix> current, future;
            if (prefix.Host == "*")
            {
                do
                {
                    current = _unhandled;
                    if (current == null)
                        break;

                    future = new List<HttpListenerPrefix>(current);
                    if (!removeSpecial(future, prefix))
                        break; // The prefix wasn't found.
                }
                while (Interlocked.CompareExchange(ref _unhandled, future, current) != current);

                await leaveIfNoPrefixAsync();
                return;
            }

            if (prefix.Host == "+")
            {
                do
                {
                    current = _all;
                    if (current == null)
                        break;

                    future = new List<HttpListenerPrefix>(current);
                    if (!removeSpecial(future, prefix))
                        break; // The prefix wasn't found.
                }
                while (Interlocked.CompareExchange(ref _all, future, current) != current);

                await leaveIfNoPrefixAsync();
                return;
            }

            Dictionary<HttpListenerPrefix, HttpListener> prefs, prefs2;
            do
            {
                prefs = _prefixes;
                if (!prefs.ContainsKey(prefix))
                    break;

                prefs2 = new Dictionary<HttpListenerPrefix, HttpListener>(prefs);
                prefs2.Remove(prefix);
            }
            while (Interlocked.CompareExchange(ref _prefixes, prefs2, prefs) != prefs);

            await leaveIfNoPrefixAsync();
        }

        
    }
}
