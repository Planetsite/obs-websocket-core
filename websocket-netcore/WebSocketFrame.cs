#region License
/*
 * WebSocketFrame.cs
 *
 * The MIT License
 *
 * Copyright (c) 2012-2019 sta.blockhead
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
 * - Chris Swiedler
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketSharp
{
    internal class WebSocketFrame : IEnumerable<byte>
    {
        #region Internal Fields

        /// <summary>
        /// Represents the ping frame without the payload data as an array of
        /// <see cref="byte"/>.
        /// </summary>
        /// <remarks>
        /// The value of this field is created from a non masked ping frame,
        /// so it can only be used to send a ping from the server.
        /// </remarks>
        internal static readonly byte[] EmptyPingBytes;

        #endregion

        #region Static Constructor

        static WebSocketFrame()
        {
            EmptyPingBytes = CreatePingFrame(false).ToArray();
        }

        #endregion

        #region Private Constructors

        private WebSocketFrame()
        {
        }

        #endregion

        #region Internal Constructors

        internal WebSocketFrame(Opcode opcode, PayloadData payloadData, bool mask)
          : this(Fin.Final, opcode, payloadData, false, mask)
        {
        }

        internal WebSocketFrame(
          Fin fin, Opcode opcode, byte[] data, bool compressed, bool mask
        )
          : this(fin, opcode, new PayloadData(data), compressed, mask)
        {
        }

        internal WebSocketFrame(
          Fin fin,
          Opcode opcode,
          PayloadData payloadData,
          bool compressed,
          bool mask
        )
        {
            Fin = fin;
            Opcode = opcode;

            Rsv1 = opcode.IsData() && compressed ? Rsv.On : Rsv.Off;
            Rsv2 = Rsv.Off;
            Rsv3 = Rsv.Off;

            var len = payloadData.Length;
            if (len < 126)
            {
                PayloadLength = (byte)len;
                ExtendedPayloadLength = WebSocket.EmptyBytes;
            }
            else if (len < 0x010000)
            {
                PayloadLength = (byte)126;
                ExtendedPayloadLength = ((ushort)len).InternalToByteArray(ByteOrder.Big);
            }
            else
            {
                PayloadLength = (byte)127;
                ExtendedPayloadLength = len.InternalToByteArray(ByteOrder.Big);
            }

            if (mask)
            {
                Mask = Mask.On;
                MaskingKey = createMaskingKey();
                payloadData.Mask(MaskingKey);
            }
            else
            {
                Mask = Mask.Off;
                MaskingKey = WebSocket.EmptyBytes;
            }

            PayloadData = payloadData;
        }

        #endregion

        #region Internal Properties

        internal ulong ExactPayloadLength
        {
            get
            {
                return PayloadLength < 126
                    ? PayloadLength
                    : PayloadLength == 126
                        ? ExtendedPayloadLength.ToUInt16(ByteOrder.Big)
                        : ExtendedPayloadLength.ToUInt64(ByteOrder.Big);
            }
        }

        internal int ExtendedPayloadLengthWidth
        {
            get
            {
                return PayloadLength < 126
                    ? 0
                    : PayloadLength == 126
                        ? 2
                        : 8;
            }
        }

        #endregion

        #region Public Properties

        public byte[] ExtendedPayloadLength { get; private set; }

        public Fin Fin { get; private set; }

        public bool IsBinary
        {
            get
            {
                return Opcode == Opcode.Binary;
            }
        }

        public bool IsClose
        {
            get
            {
                return Opcode == Opcode.Close;
            }
        }

        public bool IsCompressed
        {
            get
            {
                return Rsv1 == Rsv.On;
            }
        }

        public bool IsContinuation
        {
            get
            {
                return Opcode == Opcode.Cont;
            }
        }

        public bool IsControl
        {
            get
            {
                return Opcode >= Opcode.Close;
            }
        }

        public bool IsData
        {
            get
            {
                return Opcode == Opcode.Text || Opcode == Opcode.Binary;
            }
        }

        public bool IsFinal
        {
            get
            {
                return Fin == Fin.Final;
            }
        }

        public bool IsFragment
        {
            get
            {
                return Fin == Fin.More || Opcode == Opcode.Cont;
            }
        }

        public bool IsMasked
        {
            get
            {
                return Mask == Mask.On;
            }
        }

        public bool IsPing
        {
            get
            {
                return Opcode == Opcode.Ping;
            }
        }

        public bool IsPong
        {
            get
            {
                return Opcode == Opcode.Pong;
            }
        }

        public bool IsText
        {
            get
            {
                return Opcode == Opcode.Text;
            }
        }

        public ulong Length
        {
            get
            {
                return 2
                    + (ulong)(ExtendedPayloadLength.Length + MaskingKey.Length)
                    + PayloadData.Length;
            }
        }

        public Mask Mask { get; private set; }

        public byte[] MaskingKey { get; private set; }

        public Opcode Opcode { get; private set; }

        public PayloadData PayloadData { get; private set; }

        public byte PayloadLength { get; private set; }

        public Rsv Rsv1 { get; private set; }

        public Rsv Rsv2 { get; private set; }

        public Rsv Rsv3 { get; private set; }

        #endregion

        #region Private Methods

        private static byte[] createMaskingKey()
        {
            var key = new byte[4];
            WebSocket.RandomNumber.GetBytes(key);

            return key;
        }

        private static string dump(WebSocketFrame frame)
        {
            var len = frame.Length;
            var cnt = (long)(len / 4);
            var rem = (int)(len % 4);

            int cntDigit;
            string cntFmt;
            if (cnt < 10000)
            {
                cntDigit = 4;
                cntFmt = "{0,4}";
            }
            else if (cnt < 0x010000)
            {
                cntDigit = 4;
                cntFmt = "{0,4:X}";
            }
            else if (cnt < 0x0100000000)
            {
                cntDigit = 8;
                cntFmt = "{0,8:X}";
            }
            else
            {
                cntDigit = 16;
                cntFmt = "{0,16:X}";
            }

            var spFmt = $"{{0,{cntDigit}}}";

            var headerFmt = String.Format(
                @"
{0} 01234567 89ABCDEF 01234567 89ABCDEF
{0}+--------+--------+--------+--------+\n",
                spFmt
            );

            var lineFmt = String.Format("{0}|{{1,8}} {{2,8}} {{3,8}} {{4,8}}|\n", cntFmt);

            var footerFmt = String.Format("{0}+--------+--------+--------+--------+", spFmt);

            var buff = new StringBuilder(64);

            Func<Action<string, string, string, string>> linePrinter =
              () =>
              {
                  long lineCnt = 0;
                  return (arg1, arg2, arg3, arg4) =>
                  {
                      buff.AppendFormat(
                  lineFmt, ++lineCnt, arg1, arg2, arg3, arg4
                );
                  };
              };

            var printLine = linePrinter();
            var bytes = frame.ToArray();

            buff.AppendFormat(headerFmt, String.Empty);

            for (long i = 0; i <= cnt; i++)
            {
                var j = i * 4;

                if (i < cnt)
                {
                    printLine(
                      Convert.ToString(bytes[j], 2).PadLeft(8, '0'),
                      Convert.ToString(bytes[j + 1], 2).PadLeft(8, '0'),
                      Convert.ToString(bytes[j + 2], 2).PadLeft(8, '0'),
                      Convert.ToString(bytes[j + 3], 2).PadLeft(8, '0')
                    );

                    continue;
                }

                if (rem > 0)
                {
                    printLine(
                      Convert.ToString(bytes[j], 2).PadLeft(8, '0'),
                      rem >= 2
                      ? Convert.ToString(bytes[j + 1], 2).PadLeft(8, '0')
                      : String.Empty,
                      rem == 3
                      ? Convert.ToString(bytes[j + 2], 2).PadLeft(8, '0')
                      : String.Empty,
                      String.Empty
                    );
                }
            }

            buff.AppendFormat(footerFmt, String.Empty);
            return buff.ToString();
        }

        private static string print(WebSocketFrame frame)
        {
            // Payload Length
            var payloadLen = frame.PayloadLength;

            // Extended Payload Length
            var extPayloadLen = payloadLen > 125
                                ? frame.ExactPayloadLength.ToString()
                                : String.Empty;

            // Masking Key
            var maskingKey = BitConverter.ToString(frame.MaskingKey);

            // Payload Data
            var payload = payloadLen == 0
                          ? String.Empty
                          : payloadLen > 125
                            ? "---"
                            : !frame.IsText
                              || frame.IsFragment
                              || frame.IsMasked
                              || frame.IsCompressed
                              ? frame.PayloadData.ToString()
                              : utf8Decode(frame.PayloadData.ApplicationData);

            var fmt = @"
                    FIN: {0}
                   RSV1: {1}
                   RSV2: {2}
                   RSV3: {3}
                 Opcode: {4}
                   MASK: {5}
         Payload Length: {6}
Extended Payload Length: {7}
            Masking Key: {8}
           Payload Data: {9}";

            return String.Format(
                fmt,
                frame.Fin,
                frame.Rsv1,
                frame.Rsv2,
                frame.Rsv3,
                frame.Opcode,
                frame.Mask,
                payloadLen,
                extPayloadLen,
                maskingKey,
                payload
            );
        }

        private static WebSocketFrame processHeader(byte[] header)
        {
            if (header.Length != 2)
            {
                var msg = "The header part of a frame could not be read.";
                throw new WebSocketException(msg);
            }

            // FIN
            var fin = (header[0] & 0x80) == 0x80 ? Fin.Final : Fin.More;

            // RSV1
            var rsv1 = (header[0] & 0x40) == 0x40 ? Rsv.On : Rsv.Off;

            // RSV2
            var rsv2 = (header[0] & 0x20) == 0x20 ? Rsv.On : Rsv.Off;

            // RSV3
            var rsv3 = (header[0] & 0x10) == 0x10 ? Rsv.On : Rsv.Off;

            // Opcode
            var opcode = (byte)(header[0] & 0x0f);

            // MASK
            var mask = (header[1] & 0x80) == 0x80 ? Mask.On : Mask.Off;

            // Payload Length
            var payloadLen = (byte)(header[1] & 0x7f);

            if (!opcode.IsSupported())
            {
                var msg = "A frame has an unsupported opcode.";
                throw new WebSocketException(CloseStatusCode.ProtocolError, msg);
            }

            if (!opcode.IsData() && rsv1 == Rsv.On)
            {
                var msg = "A non data frame is compressed.";
                throw new WebSocketException(CloseStatusCode.ProtocolError, msg);
            }

            if (opcode.IsControl())
            {
                if (fin == Fin.More)
                {
                    var msg = "A control frame is fragmented.";
                    throw new WebSocketException(CloseStatusCode.ProtocolError, msg);
                }

                if (payloadLen > 125)
                {
                    var msg = "A control frame has too long payload length.";
                    throw new WebSocketException(CloseStatusCode.ProtocolError, msg);
                }
            }

            var frame = new WebSocketFrame();
            frame.Fin = fin;
            frame.Rsv1 = rsv1;
            frame.Rsv2 = rsv2;
            frame.Rsv3 = rsv3;
            frame.Opcode = (Opcode)opcode;
            frame.Mask = mask;
            frame.PayloadLength = payloadLen;

            return frame;
        }

        private static async Task readExtendedPayloadLengthAsync(Stream stream, WebSocketFrame frame)
        {
            var len = frame.ExtendedPayloadLengthWidth;
            if (len == 0)
            {
                frame.ExtendedPayloadLength = WebSocket.EmptyBytes;
                return;
            }

            byte[] bytes = await Ext.ExtReadBytesRetryAsync(stream, len);
            if (bytes.Length != len)
            {
                var msg = "The extended payload length of a frame could not be read.";
                throw new WebSocketException(msg);
            }

            frame.ExtendedPayloadLength = bytes;
        }

        private static async Task<WebSocketFrame> readHeaderAsync(Stream stream)
        {
            var data = await Ext.ExtReadBytesAsync(stream, 2, CancellationToken.None);
            return processHeader(data);
        }

        //private static async Task<WebSocketFrame> readHeaderAsync(Stream stream)
        //{
        //    byte[] bytes = await Ext.ExtReadBytesRetryAsync(stream, 2);
        //    return processHeader(bytes);
        //}

        private static async Task readMaskingKeyAsync(Stream stream, WebSocketFrame frame)
        {
            if (!frame.IsMasked)
            {
                frame.MaskingKey = WebSocket.EmptyBytes;
                return;
            }

            var len = 4;
            var bytes = await Ext.ExtReadBytesAsync(stream, len, CancellationToken.None);

            if (bytes.Length != len)
            {
                var msg = "The masking key of a frame could not be read.";
                throw new WebSocketException(msg);
            }

            frame.MaskingKey = bytes;
        }

        private static async Task<WebSocketFrame> readPayloadData(Stream stream, WebSocketFrame frame)
        {
            var exactLen = frame.ExactPayloadLength;
            if (exactLen > PayloadData.MaxLength)
            {
                var msg = "A frame has too long payload length.";
                throw new WebSocketException(CloseStatusCode.TooBig, msg);
            }

            if (exactLen == 0)
            {
                frame.PayloadData = PayloadData.Empty;
                return frame;
            }

            long len = (long)exactLen;
            var bytes = frame.PayloadLength < 127
                ? await Ext.ExtReadBytesAsync(stream, (int)exactLen, CancellationToken.None)
                : await Ext.ExtReadBytesAsync(stream, (int)len, CancellationToken.None)
            ;

            if (bytes.LongLength != len)
            {
                var msg = "The payload data of a frame could not be read.";
                throw new WebSocketException(msg);
            }

            frame.PayloadData = new PayloadData(bytes, len);
            return frame;
        }

        private static async Task readPayloadDataAsync(Stream stream, WebSocketFrame frame)
        {
            var exactLen = frame.ExactPayloadLength;
            if (exactLen > PayloadData.MaxLength)
            {
                var msg = "A frame has too long payload length.";
                throw new WebSocketException(CloseStatusCode.TooBig, msg);
            }

            if (exactLen == 0)
            {
                frame.PayloadData = PayloadData.Empty;
                return;
            }

            long len = (long)exactLen;

            if (frame.PayloadLength < 127)
            {
                var smallBytes = await Ext.ExtReadBytesAsync(stream, (int)exactLen, CancellationToken.None);
                frame.PayloadData = new PayloadData(smallBytes, len);
                return;
            }

            var bytes = await Ext.ExtReadBytesRetryAsync(stream, (int)len);
            if (bytes.LongLength != len)
            {
                var msg = "The payload data of a frame could not be read.";
                throw new WebSocketException(msg);
            }

            frame.PayloadData = new PayloadData(bytes, len);
        }

        private static string utf8Decode(byte[] bytes)
        {
            try
            {
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Internal Methods

        internal static WebSocketFrame CreateCloseFrame(
          PayloadData payloadData, bool mask
        )
        {
            return new WebSocketFrame(Fin.Final, Opcode.Close, payloadData, false, mask);
        }

        internal static WebSocketFrame CreatePingFrame(bool mask)
        {
            return new WebSocketFrame(Fin.Final, Opcode.Ping, PayloadData.Empty, false, mask);
        }

        internal static WebSocketFrame CreatePingFrame(byte[] data, bool mask)
        {
            return new WebSocketFrame(Fin.Final, Opcode.Ping, new PayloadData(data), false, mask);
        }

        internal static WebSocketFrame CreatePongFrame(PayloadData payloadData, bool mask)
        {
            return new WebSocketFrame(Fin.Final, Opcode.Pong, payloadData, false, mask);
        }

        internal static async Task<WebSocketFrame> ReadFrameAsync(Stream stream, bool unmask)
        {
            var frame = await readHeaderAsync(stream);
            await readExtendedPayloadLengthAsync(stream, frame);
            await readMaskingKeyAsync(stream, frame);
            await readPayloadDataAsync(stream, frame);

            if (unmask)
                frame.Unmask();

            return frame;
         }

        internal void Unmask()
        {
            if (Mask == Mask.Off)
                return;

            Mask = Mask.Off;
            PayloadData.Mask(MaskingKey);
            MaskingKey = WebSocket.EmptyBytes;
        }

        #endregion

        #region Public Methods

        public IEnumerator<byte> GetEnumerator()
        {
            foreach (var b in ToArray())
                yield return b;
        }

        public void Print(bool dumped)
        {
            Console.WriteLine(dumped ? dump(this) : print(this));
        }

        public string PrintToString(bool dumped)
        {
            return dumped ? dump(this) : print(this);
        }

        public byte[] ToArray()
        {
            using (var buff = new MemoryStream())
            {
                var header = (int)Fin;
                header = (header << 1) + (int)Rsv1;
                header = (header << 1) + (int)Rsv2;
                header = (header << 1) + (int)Rsv3;
                header = (header << 4) + (int)Opcode;
                header = (header << 1) + (int)Mask;
                header = (header << 7) + (int)PayloadLength;

                buff.Write(
                  ((ushort)header).InternalToByteArray(ByteOrder.Big), 0, 2
                );

                if (PayloadLength > 125)
                    buff.Write(ExtendedPayloadLength, 0, PayloadLength == 126 ? 2 : 8);

                if (Mask == Mask.On)
                    buff.Write(MaskingKey, 0, 4);

                if (PayloadLength > 0)
                {
                    var bytes = PayloadData.ToArray();

                    if (PayloadLength < 127)
                        buff.Write(bytes, 0, bytes.Length);
                    else
                    {
                        using (var src = new MemoryStream(bytes))
                            src.CopyTo(buff, 1024);
                    }
                }

                buff.Close();
                return buff.ToArray();
            }
        }

        public override string ToString()
        {
            return BitConverter.ToString(ToArray());
        }

        #endregion

        #region Explicit Interface Implementations

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
