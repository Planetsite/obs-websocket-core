#region License
/*
 * WebSocketSessionManager.cs
 *
 * The MIT License
 *
 * Copyright (c) 2012-2015 sta.blockhead
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketSharp.Server
{
    /// <summary>
    /// Provides the management function for the sessions in a WebSocket service.
    /// </summary>
    /// <remarks>
    /// This class manages the sessions in a WebSocket service provided by
    /// the <see cref="WebSocketServer"/> or <see cref="HttpServer"/>.
    /// </remarks>
    public class WebSocketSessionManager
    {
        #region Private Fields

        private volatile bool _clean;
        //private object _forSweep;
        private Logger _log;
        private Dictionary<string, IWebSocketSession> _sessions;
        private volatile ServerState _state;
        private volatile bool _sweeping;
        //private System.Timers.Timer _sweepTimer;
        private CancellationTokenSource _sweepStoppingToken = new CancellationTokenSource();
        //private object _sync;
        private TimeSpan _waitTime;

        #endregion

        #region Internal Constructors

        internal WebSocketSessionManager(Logger log)
        {
            _log = log;

            _clean = true;
            //_forSweep = new object();
            _sessions = new Dictionary<string, IWebSocketSession>();
            _state = ServerState.Ready;
            //_sync = ((ICollection)_sessions).SyncRoot;
            _waitTime = TimeSpan.FromSeconds(1);

            //setSweepTimerNotAsync(60000, CancellationToken.None);
        }

        #endregion

        #region Internal Properties

        internal ServerState State
        {
            get
            {
                return _state;
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the IDs for the active sessions in the WebSocket service.
        /// </summary>
        /// <value>
        ///   <para>
        ///   An <c>IEnumerable&lt;string&gt;</c> instance.
        ///   </para>
        ///   <para>
        ///   It provides an enumerator which supports the iteration over
        ///   the collection of the IDs for the active sessions.
        ///   </para>
        /// </value>
        public async IAsyncEnumerable<string> GetActiveIDsAsync(CancellationToken cancellationToken)
        {
            foreach (var res in await PrivateBroadpingAsync(WebSocketFrame.EmptyPingBytes, cancellationToken))
            {
                if (res.Value)
                    yield return res.Key;
            }
        }

        /// <summary>
        /// Gets the number of the sessions in the WebSocket service.
        /// </summary>
        /// <value>
        /// An <see cref="int"/> that represents the number of the sessions.
        /// </value>
        public int Count
        {
            get
            {
                //lock (_sync)
                    return _sessions.Count;
            }
        }

        /// <summary>
        /// Gets the IDs for the sessions in the WebSocket service.
        /// </summary>
        /// <value>
        ///   <para>
        ///   An <c>IEnumerable&lt;string&gt;</c> instance.
        ///   </para>
        ///   <para>
        ///   It provides an enumerator which supports the iteration over
        ///   the collection of the IDs for the sessions.
        ///   </para>
        /// </value>
        public IEnumerable<string> IDs
        {
            get
            {
                if (_state != ServerState.Start)
                    return Enumerable.Empty<string>();

                //lock (_sync)
                {
                    if (_state != ServerState.Start)
                        return Enumerable.Empty<string>();

                    return _sessions.Keys.ToList();
                }
            }
        }

        /// <summary>
        /// Gets the IDs for the inactive sessions in the WebSocket service.
        /// </summary>
        /// <value>
        ///   <para>
        ///   An <c>IEnumerable&lt;string&gt;</c> instance.
        ///   </para>
        ///   <para>
        ///   It provides an enumerator which supports the iteration over
        ///   the collection of the IDs for the inactive sessions.
        ///   </para>
        /// </value>
        public async IAsyncEnumerable<string> GetInactiveIDsAsync(CancellationToken cancellationToken)
        {
            foreach (var res in await PrivateBroadpingAsync(WebSocketFrame.EmptyPingBytes, cancellationToken))
            {
                if (!res.Value)
                    yield return res.Key;
            }
        }

        /// <summary>
        /// Gets the session instance with <paramref name="id"/>.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="IWebSocketSession"/> instance or <see langword="null"/>
        ///   if not found.
        ///   </para>
        ///   <para>
        ///   The session instance provides the function to access the information
        ///   in the session.
        ///   </para>
        /// </value>
        /// <param name="id">
        /// A <see cref="string"/> that represents the ID of the session to find.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="id"/> is an empty string.
        /// </exception>
        public IWebSocketSession this[string id]
        {
            get
            {
                if (id == null)
                    throw new ArgumentNullException("id");

                if (id.Length == 0)
                    throw new ArgumentException("An empty string.", "id");

                IWebSocketSession session;
                PrivateTryGetSession(id, out session);

                return session;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the inactive sessions in
        /// the WebSocket service are cleaned up periodically.
        /// </summary>
        /// <remarks>
        /// The set operation does nothing if the service has already started or
        /// it is shutting down.
        /// </remarks>
        /// <value>
        /// <c>true</c> if the inactive sessions are cleaned up every 60 seconds;
        /// otherwise, <c>false</c>.
        /// </value>
        public bool KeepClean
        {
            get
            {
                return _clean;
            }

            set
            {
                string msg;
                if (!CanSet(out msg))
                {
                    _log.Warn(msg);
                    return;
                }

                //lock (_sync)
                {
                    if (!CanSet(out msg))
                    {
                        _log.Warn(msg);
                        return;
                    }

                    _clean = value;
                }
            }
        }

        /// <summary>
        /// Gets the session instances in the WebSocket service.
        /// </summary>
        /// <value>
        ///   <para>
        ///   An <c>IEnumerable&lt;IWebSocketSession&gt;</c> instance.
        ///   </para>
        ///   <para>
        ///   It provides an enumerator which supports the iteration over
        ///   the collection of the session instances.
        ///   </para>
        /// </value>
        public IEnumerable<IWebSocketSession> Sessions
        {
            get
            {
                if (_state != ServerState.Start)
                    return Enumerable.Empty<IWebSocketSession>();

                //lock (_sync)
                {
                    if (_state != ServerState.Start)
                        return Enumerable.Empty<IWebSocketSession>();

                    return _sessions.Values.ToList();
                }
            }
        }

        /// <summary>
        /// Gets or sets the time to wait for the response to the WebSocket Ping or
        /// Close.
        /// </summary>
        /// <remarks>
        /// The set operation does nothing if the service has already started or
        /// it is shutting down.
        /// </remarks>
        /// <value>
        /// A <see cref="TimeSpan"/> to wait for the response.
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
                    _log.Warn(msg);
                    return;
                }

                //lock (_sync)
                {
                    if (!CanSet(out msg))
                    {
                        _log.Warn(msg);
                        return;
                    }

                    _waitTime = value;
                }
            }
        }

        #endregion

        #region Private Methods

        private async Task PrivateBroadcastAsync(Opcode opcode, byte[] data, Action completed, CancellationToken cancellationToken)
        {
            var cache = new Dictionary<CompressionMethod, byte[]>();

            try
            {
                foreach (var session in Sessions)
                {
                    if (_state != ServerState.Start)
                    {
                        _log.Error("The service is shutting down.");
                        break;
                    }

                    await session.Context.WebSocket.InternalSendAsync(opcode, data, cache, cancellationToken);
                }

                if (completed != null)
                    completed();
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                _log.Debug(ex.ToString());
            }
            finally
            {
                cache.Clear();
            }
        }

        private async Task PrivateBroadcastAsync(Opcode opcode, Stream stream, Action completed, CancellationToken cancellationToken)
        {
            var cache = new Dictionary<CompressionMethod, Stream>();

            try
            {
                foreach (var session in Sessions)
                {
                    if (_state != ServerState.Start)
                    {
                        _log.Error("The service is shutting down.");
                        break;
                    }

                    await session.Context.WebSocket.InternalSendAsync(opcode, stream, cache, cancellationToken);
                }

                if (completed != null)
                    completed();
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                _log.Debug(ex.ToString());
            }
            finally
            {
                foreach (var cached in cache.Values)
                    cached.Dispose();

                cache.Clear();
            }
        }

        private async Task<Dictionary<string, bool>> PrivateBroadpingAsync(byte[] frameAsBytes, CancellationToken cancellationToken)
        {
            var ret = new Dictionary<string, bool>();

            foreach (var session in Sessions)
            {
                if (_state != ServerState.Start)
                {
                    _log.Error("The service is shutting down.");
                    break;
                }

                var res = await session.Context.WebSocket.InternalPingAsync(frameAsBytes, _waitTime, cancellationToken);
                ret.Add(session.ID, res);
            }

            return ret;
        }

        private bool CanSet(out string message)
        {
            message = null;

            if (_state == ServerState.Start)
            {
                message = "The service has already started.";
                return false;
            }

            if (_state == ServerState.ShuttingDown)
            {
                message = "The service is shutting down.";
                return false;
            }

            return true;
        }

        private static string CreateID()
        {
            return Guid.NewGuid().ToString("N");
        }

        private async Task SetSweepTimerNotAsync(double interval, CancellationToken stoppingToken)
        {
            int sleepMillisec = (int)interval;
            var stop = _sweepStoppingToken.Token;
            using var disposeRegistration = stoppingToken.Register(() => _sweepStoppingToken.Cancel());

            try
            {
                do
                {
                    using var cts = new CancellationTokenSource(sleepMillisec);
                    await SweepAsync(cts.Token);
                    await Task.Delay(sleepMillisec);
                }
                while (!stop.IsCancellationRequested);
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception sweepErr)
            {
                _log.Error($"WebSocketSessionManager Sweep loop crash: {sweepErr.Message}\n{sweepErr.StackTrace}");
            }
        }

        private async Task PrivateStopAsync(PayloadData payloadData, bool send, CancellationToken stoppingToken)
        {
            var bytes = send
                ? WebSocketFrame.CreateCloseFrame(payloadData, false).ToArray()
                : null;

            //lock (_sync)
            {
                _state = ServerState.ShuttingDown;

                _sweepStoppingToken.Cancel();
                foreach (var session in _sessions.Values.ToList())
                    await session.Context.WebSocket.InternalCloseAsync(payloadData, bytes, stoppingToken);

                _state = ServerState.Stop;
            }
        }

        private bool PrivateTryGetSession(string id, out IWebSocketSession session)
        {
            session = null;

            if (_state != ServerState.Start)
                return false;

            //lock (_sync)
            {
                if (_state != ServerState.Start)
                    return false;

                return _sessions.TryGetValue(id, out session);
            }
        }

        #endregion

        #region Internal Methods

        internal string Add(IWebSocketSession session)
        {
            //lock (_sync)
            {
                if (_state != ServerState.Start)
                    return null;

                var id = CreateID();
                _sessions.Add(id, session);

                return id;
            }
        }

        internal async Task BroadcastAsync(Opcode opcode, byte[] data, Dictionary<CompressionMethod, byte[]> cache, CancellationToken cancellationToken)
        {
            foreach (var session in Sessions)
            {
                if (_state != ServerState.Start)
                {
                    _log.Error("The service is shutting down.");
                    break;
                }

                await session.Context.WebSocket.InternalSendAsync(opcode, data, cache, cancellationToken);
            }
        }

        internal async Task BroadcastAsync(Opcode opcode, Stream stream, Dictionary<CompressionMethod, Stream> cache, CancellationToken cancellationToken)
        {
            foreach (var session in Sessions)
            {
                if (_state != ServerState.Start)
                {
                    _log.Error("The service is shutting down.");
                    break;
                }

                await session.Context.WebSocket.InternalSendAsync(opcode, stream, cache, cancellationToken);
            }
        }

        internal async Task<Dictionary<string, bool>> BroadpingAsync(byte[] frameAsBytes, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var ret = new Dictionary<string, bool>();

            foreach (var session in Sessions)
            {
                if (_state != ServerState.Start)
                {
                    _log.Error("The service is shutting down.");
                    break;
                }

                var res = await session.Context.WebSocket.InternalPingAsync(frameAsBytes, timeout, cancellationToken);
                ret.Add(session.ID, res);
            }

            return ret;
        }

        internal bool Remove(string id)
        {
            //lock (_sync)
                return _sessions.Remove(id);
        }

        internal void Start(CancellationToken stoppingToken)
        {
            //lock (_sync)
            {
                if (_clean)
                    #pragma warning disable CS4014
                    /*await*/ SetSweepTimerNotAsync(60000, stoppingToken);
                    #pragma warning restore CS4014
                _state = ServerState.Start;
            }
        }

        internal async Task StopAsync(ushort code, string reason, CancellationToken stoppingToken)
        {
            if (code == 1005)
            { // == no status
                await PrivateStopAsync(PayloadData.Empty, true, stoppingToken);
                return;
            }

            await PrivateStopAsync(new PayloadData(code, reason), !code.IsReserved(), stoppingToken);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sends <paramref name="data"/> to every client in the WebSocket service.
        /// </summary>
        /// <param name="data">
        /// An array of <see cref="byte"/> that represents the binary data to send.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The current state of the manager is not Start.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="data"/> is <see langword="null"/>.
        /// </exception>
        public async Task BroadcastAsync(byte[] data, CancellationToken cancellationToken)
        {
            if (_state != ServerState.Start)
            {
                var msg = "The current state of the manager is not Start.";
                throw new InvalidOperationException(msg);
            }

            if (data == null)
                throw new ArgumentNullException("data");

            if (data.LongLength <= WebSocket.FragmentLength)
                await PrivateBroadcastAsync(Opcode.Binary, data, null, cancellationToken);
            else
                await PrivateBroadcastAsync(Opcode.Binary, new MemoryStream(data), null, cancellationToken);
        }

        /// <summary>
        /// Sends <paramref name="data"/> to every client in the WebSocket service.
        /// </summary>
        /// <param name="data">
        /// A <see cref="string"/> that represents the text data to send.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The current state of the manager is not Start.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="data"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="data"/> could not be UTF-8-encoded.
        /// </exception>
        public async Task BroadcastAsync(string data, CancellationToken cancellationToken)
        {
            if (_state != ServerState.Start)
            {
                var msg = "The current state of the manager is not Start.";
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

            if (bytes.LongLength <= WebSocket.FragmentLength)
                await PrivateBroadcastAsync(Opcode.Text, bytes, null, cancellationToken);
            else
                await PrivateBroadcastAsync(Opcode.Text, new MemoryStream(bytes), null, cancellationToken);
        }

        /// <summary>
        /// Sends the data from <paramref name="stream"/> to every client in
        /// the WebSocket service.
        /// </summary>
        /// <remarks>
        /// The data is sent as the binary data.
        /// </remarks>
        /// <param name="stream">
        /// A <see cref="Stream"/> instance from which to read the data to send.
        /// </param>
        /// <param name="length">
        /// An <see cref="int"/> that specifies the number of bytes to send.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The current state of the manager is not Start.
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
        public async Task BroadcastAsync(Stream stream, int length, CancellationToken cancellationToken)
        {
            if (_state != ServerState.Start)
            {
                var msg = "The current state of the manager is not Start.";
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
                _log.Warn(
                  String.Format(
                    "Only {0} byte(s) of data could be read from the stream.",
                    len
                  )
                );
            }

            if (len <= WebSocket.FragmentLength)
                await PrivateBroadcastAsync(Opcode.Binary, bytes, null, cancellationToken);
            else
                await PrivateBroadcastAsync(Opcode.Binary, new MemoryStream(bytes), null, cancellationToken);
        }

        /// <summary>
        /// Sends <paramref name="data"/> asynchronously to every client in
        /// the WebSocket service.
        /// </summary>
        /// <remarks>
        /// This method does not wait for the send to be complete.
        /// </remarks>
        /// <param name="data">
        /// An array of <see cref="byte"/> that represents the binary data to send.
        /// </param>
        /// <param name="completed">
        ///   <para>
        ///   An <see cref="Action"/> delegate or <see langword="null"/>
        ///   if not needed.
        ///   </para>
        ///   <para>
        ///   The delegate invokes the method called when the send is complete.
        ///   </para>
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The current state of the manager is not Start.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="data"/> is <see langword="null"/>.
        /// </exception>
        public async Task BroadcastAsync(byte[] data, Action completed, CancellationToken cancellationToken)
        {
            if (_state != ServerState.Start)
            {
                var msg = "The current state of the manager is not Start.";
                throw new InvalidOperationException(msg);
            }

            if (data == null)
                throw new ArgumentNullException("data");

            if (data.LongLength <= WebSocket.FragmentLength)
                await PrivateBroadcastAsync(Opcode.Binary, data, completed, cancellationToken);
            else
                await PrivateBroadcastAsync(Opcode.Binary, new MemoryStream(data), completed, cancellationToken);
        }

        /// <summary>
        /// Sends <paramref name="data"/> asynchronously to every client in
        /// the WebSocket service.
        /// </summary>
        /// <remarks>
        /// This method does not wait for the send to be complete.
        /// </remarks>
        /// <param name="data">
        /// A <see cref="string"/> that represents the text data to send.
        /// </param>
        /// <param name="completed">
        ///   <para>
        ///   An <see cref="Action"/> delegate or <see langword="null"/>
        ///   if not needed.
        ///   </para>
        ///   <para>
        ///   The delegate invokes the method called when the send is complete.
        ///   </para>
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The current state of the manager is not Start.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="data"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="data"/> could not be UTF-8-encoded.
        /// </exception>
        public async Task BroadcastAsync(string data, Action completed, CancellationToken cancellationToken)
        {
            if (_state != ServerState.Start)
            {
                var msg = "The current state of the manager is not Start.";
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

            if (bytes.LongLength <= WebSocket.FragmentLength)
                await PrivateBroadcastAsync(Opcode.Text, bytes, completed, cancellationToken);
            else
                await PrivateBroadcastAsync(Opcode.Text, new MemoryStream(bytes), completed, cancellationToken);
        }

        /// <summary>
        /// Sends the data from <paramref name="stream"/> asynchronously to
        /// every client in the WebSocket service.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   The data is sent as the binary data.
        ///   </para>
        ///   <para>
        ///   This method does not wait for the send to be complete.
        ///   </para>
        /// </remarks>
        /// <param name="stream">
        /// A <see cref="Stream"/> instance from which to read the data to send.
        /// </param>
        /// <param name="length">
        /// An <see cref="int"/> that specifies the number of bytes to send.
        /// </param>
        /// <param name="completed">
        ///   <para>
        ///   An <see cref="Action"/> delegate or <see langword="null"/>
        ///   if not needed.
        ///   </para>
        ///   <para>
        ///   The delegate invokes the method called when the send is complete.
        ///   </para>
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The current state of the manager is not Start.
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
        public async Task BroadcastAsync(Stream stream, int length, Action completed, CancellationToken cancellationToken)
        {
            if (_state != ServerState.Start)
            {
                var msg = "The current state of the manager is not Start.";
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
                _log.Warn($"Only {len} byte(s) of data could be read from the stream.");
            }

            if (len <= WebSocket.FragmentLength)
                await PrivateBroadcastAsync(Opcode.Binary, bytes, completed, cancellationToken);
            else
                await PrivateBroadcastAsync(Opcode.Binary, new MemoryStream(bytes), completed, cancellationToken);
        }

        /// <summary>
        /// Sends a ping to every client in the WebSocket service.
        /// </summary>
        /// <returns>
        ///   <para>
        ///   A <c>Dictionary&lt;string, bool&gt;</c>.
        ///   </para>
        ///   <para>
        ///   It represents a collection of pairs of a session ID and
        ///   a value indicating whether a pong has been received from
        ///   the client within a time.
        ///   </para>
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The current state of the manager is not Start.
        /// </exception>
        [Obsolete("This method will be removed.")]
        public async Task<Dictionary<string, bool>> BroadpingAsync(CancellationToken cancellationToken)
        {
            if (_state != ServerState.Start)
            {
                var msg = "The current state of the manager is not Start.";
                throw new InvalidOperationException(msg);
            }

            return await BroadpingAsync(WebSocketFrame.EmptyPingBytes, _waitTime, cancellationToken);
        }

        /// <summary>
        /// Sends a ping with <paramref name="message"/> to every client in
        /// the WebSocket service.
        /// </summary>
        /// <returns>
        ///   <para>
        ///   A <c>Dictionary&lt;string, bool&gt;</c>.
        ///   </para>
        ///   <para>
        ///   It represents a collection of pairs of a session ID and
        ///   a value indicating whether a pong has been received from
        ///   the client within a time.
        ///   </para>
        /// </returns>
        /// <param name="message">
        ///   <para>
        ///   A <see cref="string"/> that represents the message to send.
        ///   </para>
        ///   <para>
        ///   The size must be 125 bytes or less in UTF-8.
        ///   </para>
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The current state of the manager is not Start.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="message"/> could not be UTF-8-encoded.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The size of <paramref name="message"/> is greater than 125 bytes.
        /// </exception>
        [Obsolete("This method will be removed.")]
        public async Task<Dictionary<string, bool>> BroadpingAsync(string message, CancellationToken cancellationToken)
        {
            if (_state != ServerState.Start)
            {
                var msg = "The current state of the manager is not Start.";
                throw new InvalidOperationException(msg);
            }

            if (message.IsNullOrEmpty())
                return await BroadpingAsync(WebSocketFrame.EmptyPingBytes, _waitTime, cancellationToken);

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

            var frame = WebSocketFrame.CreatePingFrame(bytes, false);
            return await BroadpingAsync(frame.ToArray(), _waitTime, cancellationToken);
        }

        /// <summary>
        /// Closes the specified session.
        /// </summary>
        /// <param name="id">
        /// A <see cref="string"/> that represents the ID of the session to close.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="id"/> is an empty string.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The session could not be found.
        /// </exception>
        public async Task CloseSessionAsync(string id, CancellationToken cancellationToken)
        {
            IWebSocketSession session;
            if (!PrivateTryGetSession(id, out session))
            {
                var msg = "The session could not be found.";
                throw new InvalidOperationException(msg);
            }

            await session.Context.WebSocket.CloseAsync(cancellationToken);
        }

        /// <summary>
        /// Closes the specified session with <paramref name="code"/> and
        /// <paramref name="reason"/>.
        /// </summary>
        /// <param name="id">
        /// A <see cref="string"/> that represents the ID of the session to close.
        /// </param>
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
        /// <exception cref="ArgumentNullException">
        /// <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="id"/> is an empty string.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="code"/> is 1010 (mandatory extension).
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="code"/> is 1005 (no status) and there is
        ///   <paramref name="reason"/>.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="reason"/> could not be UTF-8-encoded.
        ///   </para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The session could not be found.
        /// </exception>
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
        public async Task CloseSessionAsync(string id, ushort code, string reason, CancellationToken cancellationToken)
        {
            IWebSocketSession session;
            if (!PrivateTryGetSession(id, out session))
            {
                var msg = "The session could not be found.";
                throw new InvalidOperationException(msg);
            }

            await session.Context.WebSocket.CloseAsync(code, reason, cancellationToken);
        }

        /// <summary>
        /// Closes the specified session with <paramref name="code"/> and
        /// <paramref name="reason"/>.
        /// </summary>
        /// <param name="id">
        /// A <see cref="string"/> that represents the ID of the session to close.
        /// </param>
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
        /// <exception cref="ArgumentNullException">
        /// <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="id"/> is an empty string.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="code"/> is
        ///   <see cref="CloseStatusCode.MandatoryExtension"/>.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="code"/> is
        ///   <see cref="CloseStatusCode.NoStatus"/> and there is
        ///   <paramref name="reason"/>.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="reason"/> could not be UTF-8-encoded.
        ///   </para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The session could not be found.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The size of <paramref name="reason"/> is greater than 123 bytes.
        /// </exception>
        public async Task CloseSessionAsync(string id, CloseStatusCode code, string reason, CancellationToken cancellationToken)
        {
            IWebSocketSession session;
            if (!PrivateTryGetSession(id, out session))
            {
                var msg = "The session could not be found.";
                throw new InvalidOperationException(msg);
            }

            await session.Context.WebSocket.CloseAsync(code, reason, cancellationToken);
        }

        /// <summary>
        /// Sends a ping to the client using the specified session.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the send has done with no error and a pong has been
        /// received from the client within a time; otherwise, <c>false</c>.
        /// </returns>
        /// <param name="id">
        /// A <see cref="string"/> that represents the ID of the session.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="id"/> is an empty string.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The session could not be found.
        /// </exception>
        public async Task<bool> PingToAsync(string id, CancellationToken cancellationToken)
        {
            IWebSocketSession session;
            if (!PrivateTryGetSession(id, out session))
            {
                var msg = "The session could not be found.";
                throw new InvalidOperationException(msg);
            }

            return await session.Context.WebSocket.PingAsync(cancellationToken);
        }

        /// <summary>
        /// Sends a ping with <paramref name="message"/> to the client using
        /// the specified session.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the send has done with no error and a pong has been
        /// received from the client within a time; otherwise, <c>false</c>.
        /// </returns>
        /// <param name="message">
        ///   <para>
        ///   A <see cref="string"/> that represents the message to send.
        ///   </para>
        ///   <para>
        ///   The size must be 125 bytes or less in UTF-8.
        ///   </para>
        /// </param>
        /// <param name="id">
        /// A <see cref="string"/> that represents the ID of the session.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="id"/> is an empty string.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="message"/> could not be UTF-8-encoded.
        ///   </para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The session could not be found.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The size of <paramref name="message"/> is greater than 125 bytes.
        /// </exception>
        public async Task<bool> PingToAsync(string message, string id, CancellationToken cancellationToken)
        {
            IWebSocketSession session;
            if (!PrivateTryGetSession(id, out session))
            {
                var msg = "The session could not be found.";
                throw new InvalidOperationException(msg);
            }

            return await session.Context.WebSocket.PingAsync(message, cancellationToken);
        }

        /// <summary>
        /// Sends <paramref name="data"/> to the client using the specified session.
        /// </summary>
        /// <param name="data">
        /// An array of <see cref="byte"/> that represents the binary data to send.
        /// </param>
        /// <param name="id">
        /// A <see cref="string"/> that represents the ID of the session.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <para>
        ///   <paramref name="id"/> is <see langword="null"/>.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="data"/> is <see langword="null"/>.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="id"/> is an empty string.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   <para>
        ///   The session could not be found.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The current state of the WebSocket connection is not Open.
        ///   </para>
        /// </exception>
        public async Task SendToAsync(byte[] data, string id, CancellationToken cancellationToken)
        {
            IWebSocketSession session;
            if (!PrivateTryGetSession(id, out session))
            {
                var msg = "The session could not be found.";
                throw new InvalidOperationException(msg);
            }

            await session.Context.WebSocket.SendAsync(data, cancellationToken);
        }

        /// <summary>
        /// Sends <paramref name="data"/> to the client using the specified session.
        /// </summary>
        /// <param name="data">
        /// A <see cref="string"/> that represents the text data to send.
        /// </param>
        /// <param name="id">
        /// A <see cref="string"/> that represents the ID of the session.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <para>
        ///   <paramref name="id"/> is <see langword="null"/>.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="data"/> is <see langword="null"/>.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="id"/> is an empty string.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="data"/> could not be UTF-8-encoded.
        ///   </para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   <para>
        ///   The session could not be found.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The current state of the WebSocket connection is not Open.
        ///   </para>
        /// </exception>
        public async Task SendToAsync(string data, string id, CancellationToken cancellationToken)
        {
            IWebSocketSession session;
            if (!PrivateTryGetSession(id, out session))
            {
                var msg = "The session could not be found.";
                throw new InvalidOperationException(msg);
            }

            await session.Context.WebSocket.SendAsync(data, cancellationToken);
        }

        /// <summary>
        /// Sends the data from <paramref name="stream"/> to the client using
        /// the specified session.
        /// </summary>
        /// <remarks>
        /// The data is sent as the binary data.
        /// </remarks>
        /// <param name="stream">
        /// A <see cref="Stream"/> instance from which to read the data to send.
        /// </param>
        /// <param name="length">
        /// An <see cref="int"/> that specifies the number of bytes to send.
        /// </param>
        /// <param name="id">
        /// A <see cref="string"/> that represents the ID of the session.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <para>
        ///   <paramref name="id"/> is <see langword="null"/>.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="stream"/> is <see langword="null"/>.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="id"/> is an empty string.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
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
        /// <exception cref="InvalidOperationException">
        ///   <para>
        ///   The session could not be found.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The current state of the WebSocket connection is not Open.
        ///   </para>
        /// </exception>
        public async Task SendToAsync(Stream stream, int length, string id, CancellationToken cancellationToken)
        {
            IWebSocketSession session;
            if (!PrivateTryGetSession(id, out session))
            {
                var msg = "The session could not be found.";
                throw new InvalidOperationException(msg);
            }

            await session.Context.WebSocket.SendAsync(stream, length, cancellationToken);
        }

        /// <summary>
        /// Cleans up the inactive sessions in the WebSocket service.
        /// </summary>
        public async Task SweepAsync(CancellationToken cancellationToken)
        {
            if (_sweeping)
            {
                _log.Info("The sweeping is already in progress.");
                return;
            }

            //lock (_forSweep)
            {
                if (_sweeping)
                {
                    _log.Info("The sweeping is already in progress.");
                    return;
                }

                _sweeping = true;
            }

            await foreach (var id in GetInactiveIDsAsync(cancellationToken))
            {
                if (_state != ServerState.Start)
                    break;

                //lock (_sync)
                {
                    if (_state != ServerState.Start)
                        break;

                    IWebSocketSession session;
                    if (_sessions.TryGetValue(id, out session))
                    {
                        var state = session.ConnectionState;
                        if (state == WebSocketState.Open)
                            await session.Context.WebSocket.CloseAsync(CloseStatusCode.Abnormal, cancellationToken);
                        else if (state == WebSocketState.Closing)
                            continue;
                        else
                            _sessions.Remove(id);
                    }
                }
            }

            _sweeping = false;
        }

        /// <summary>
        /// Tries to get the session instance with <paramref name="id"/>.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the session is successfully found; otherwise,
        /// <c>false</c>.
        /// </returns>
        /// <param name="id">
        /// A <see cref="string"/> that represents the ID of the session to find.
        /// </param>
        /// <param name="session">
        ///   <para>
        ///   When this method returns, a <see cref="IWebSocketSession"/>
        ///   instance or <see langword="null"/> if not found.
        ///   </para>
        ///   <para>
        ///   The session instance provides the function to access
        ///   the information in the session.
        ///   </para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="id"/> is an empty string.
        /// </exception>
        public bool TryGetSession(string id, out IWebSocketSession session)
        {
            if (id == null)
                throw new ArgumentNullException("id");

            if (id.Length == 0)
                throw new ArgumentException("An empty string.", "id");

            return PrivateTryGetSession(id, out session);
        }

        #endregion
    }
}
