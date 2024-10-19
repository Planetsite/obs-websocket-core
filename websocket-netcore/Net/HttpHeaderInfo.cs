/*
 * HttpHeaderInfo.cs
 *
 * The MIT License
 *
 * Copyright (c) 2013-2020 sta.blockhead
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

namespace WebSocketSharp.Net;

internal class HttpHeaderInfo
{
    internal HttpHeaderInfo(string headerName, HttpHeaderType headerType)
    {
        HeaderName = headerName;
        HeaderType = headerType;
    }

    internal bool IsMultiValueInRequest => (HeaderType & HttpHeaderType.MultiValueInRequest) == HttpHeaderType.MultiValueInRequest;

    internal bool IsMultiValueInResponse => (HeaderType & HttpHeaderType.MultiValueInResponse) == HttpHeaderType.MultiValueInResponse;

    public string HeaderName { get; }

    public HttpHeaderType HeaderType { get; }

    public bool IsRequest => (HeaderType & HttpHeaderType.Request) == HttpHeaderType.Request;

    public bool IsResponse => (HeaderType & HttpHeaderType.Response) == HttpHeaderType.Response;

    public bool IsMultiValue(bool response)
    {
        var headerType = HeaderType & HttpHeaderType.MultiValue;

        if (headerType != HttpHeaderType.MultiValue)
            return response ? IsMultiValueInResponse : IsMultiValueInRequest;

        return response ? IsResponse : IsRequest;
    }

    public bool IsRestricted(bool response)
    {
        var headerType = HeaderType & HttpHeaderType.Restricted;

        if (headerType != HttpHeaderType.Restricted)
            return false;

        return response ? IsResponse : IsRequest;
    }
}
