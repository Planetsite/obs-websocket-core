using System;
using System.Collections.Specialized;
using System.Text;

namespace WebSocketSharp.Net;

internal abstract class AuthenticationBase
{

    private AuthenticationSchemes _scheme;

    internal NameValueCollection Parameters;

    protected AuthenticationBase(AuthenticationSchemes scheme, NameValueCollection parameters)
    {
        _scheme = scheme;
        Parameters = parameters;
    }

    public string Algorithm => Parameters["algorithm"];

    public string Nonce => Parameters["nonce"];

    public string Opaque => Parameters["opaque"];

    public string Qop => Parameters["qop"];

    public string Realm => Parameters["realm"];

    public AuthenticationSchemes Scheme => _scheme;

    internal static string CreateNonceValue()
    {
        var src = new byte[16];
        var rand = new Random();
        rand.NextBytes(src);

        var res = new StringBuilder(32);
        foreach (var b in src)
            res.Append(b.ToString("x2"));

        return res.ToString();
    }

    internal static NameValueCollection ParseParameters(string value)
    {
        var res = new NameValueCollection();
        foreach (var param in value.SplitHeaderValue(','))
        {
            var i = param.IndexOf('=');
            var name = i > 0 ? param.Substring(0, i).Trim() : null;
            var val = i < 0
                      ? param.Trim().Trim('"')
                      : i < param.Length - 1
                        ? param.Substring(i + 1).Trim().Trim('"')
                        : String.Empty;

            res.Add(name, val);
        }

        return res;
    }

    internal abstract string ToBasicString();

    internal abstract string ToDigestString();

    public override string ToString()
    {
        return _scheme == AuthenticationSchemes.Basic
               ? ToBasicString()
               : _scheme == AuthenticationSchemes.Digest
                 ? ToDigestString()
                 : String.Empty;
    }
}
