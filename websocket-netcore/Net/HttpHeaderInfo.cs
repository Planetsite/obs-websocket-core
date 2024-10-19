namespace WebSocketSharp.Net;

internal sealed class HttpHeaderInfo
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
