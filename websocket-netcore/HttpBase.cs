#region License
/*
 * HttpBase.cs
 *
 * The MIT License
 *
 * Copyright (c) 2012-2014 sta.blockhead
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
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp.Net;

namespace WebSocketSharp
{
    internal abstract class HttpBase
    {
        #region Private Fields

        private const int _headersMaxLength = 8192;

        #endregion

        #region Internal Fields

        internal byte[] EntityBodyData;

        #endregion

        #region Protected Fields

        protected const string CrLf = "\r\n";

        #endregion

        #region Protected Constructors

        protected HttpBase(Version version, NameValueCollection headers)
        {
            ProtocolVersion = version;
            Headers = headers;
        }

        #endregion

        #region Public Properties

        public string EntityBody
        {
            get
            {
                if (EntityBodyData == null || EntityBodyData.LongLength == 0)
                    return String.Empty;

                Encoding enc = null;

                var contentType = Headers["Content-Type"];
                if (contentType != null && contentType.Length > 0)
                    enc = HttpUtility.GetEncoding(contentType);

                return (enc ?? Encoding.UTF8).GetString(EntityBodyData);
            }
        }

        public NameValueCollection Headers { get; }

        public Version ProtocolVersion { get; }

        #endregion

        #region Private Methods

        private static async Task<byte[]> readEntityBodyAsync(Stream stream, string length, CancellationToken cancellationToken)
        {
            long len;
            if (!Int64.TryParse(length, out len))
                throw new ArgumentException("Cannot be parsed.", "length");

            if (len < 0)
                throw new ArgumentOutOfRangeException("length", "Less than zero.");

            return len > 1024
                ? await Ext.ExtReadBytesAsync(stream, (int)len, cancellationToken)
                : len > 0

                ? await Ext.ExtReadBytesAsync(stream, (int)len, cancellationToken)
                : null
            ;
        }

        private static async Task<string[]> readHeadersAsync(Stream stream, int maxLength, CancellationToken cancellationToken)
        {
            var buff = new List<byte>(100);
            var temp = new byte[1];
            var cnt = 0;

            var read = false;
            while (cnt < maxLength)
            {
                int nread = await stream.ReadAsync(temp, 0, 1, cancellationToken);
                if (nread == 0) throw new WebSocketException("Connection was closed.");
                buff.Add(temp[0]);
                ++cnt;
                int size = buff.Count;
                if (size > 4 && buff[size-1] == '\n' && buff[size-2] == '\r' && buff[size-3] == '\n' && buff[size-4] == '\r')
                {
                    read = true;
                    break;
                }
            }

            if (!read)
                throw new WebSocketException("The length of header part is greater than the max length.");

            return Encoding.UTF8
                .GetString(buff.ToArray())
                .Replace(CrLf + " ", " ")
                .Replace(CrLf + "\t", " ")
                .Split(new[] { CrLf }, StringSplitOptions.RemoveEmptyEntries)
            ;
        }

        #endregion

        #region Protected Methods

        protected static async Task<T> ReadAsync<T>(Stream stream, Func<string[], T> parser, int millisecondsTimeout)
          where T : HttpBase
        {
            var timeout = false;
            var cancellation = new CancellationTokenSource(millisecondsTimeout);

            T http = null;
            Exception exception = null;
            try
            {
                http = parser(await readHeadersAsync(stream, _headersMaxLength, cancellation.Token));
                var contentLen = http.Headers["Content-Length"];
                if (contentLen != null && contentLen.Length > 0)
                    http.EntityBodyData = await readEntityBodyAsync(stream, contentLen, cancellation.Token);
            }
            catch (TaskCanceledException)
            {
                timeout = true;
                stream.Close();
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            var msg = timeout
                ? "A timeout has occurred while reading an HTTP request/response."
                : exception != null

                ? "An exception has occurred while reading an HTTP request/response."
                : null
            ;

            if (msg != null)
                throw new WebSocketException(msg, exception);

            return http;
        }

        #endregion

        #region Public Methods

        public byte[] ToByteArray()
        {
            return Encoding.UTF8.GetBytes(ToString());
        }

        #endregion
    }
}
