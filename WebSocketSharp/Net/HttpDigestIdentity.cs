using System.Collections.Specialized;
using System.Security.Principal;

namespace WebSocketSharp.Net;

/// <summary>
/// Holds the username and other parameters from
/// an HTTP Digest authentication attempt.
/// </summary>
public sealed class HttpDigestIdentity : GenericIdentity
{
    private NameValueCollection _parameters;

    internal HttpDigestIdentity(NameValueCollection parameters)
      : base(parameters["username"], "Digest")
    {
        _parameters = parameters;
    }

    /// <summary>
    /// Gets the algorithm parameter from a digest authentication attempt.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> that represents the algorithm parameter.
    /// </value>
    public string Algorithm => _parameters["algorithm"];

    /// <summary>
    /// Gets the cnonce parameter from a digest authentication attempt.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> that represents the cnonce parameter.
    /// </value>
    public string Cnonce => _parameters["cnonce"];

    /// <summary>
    /// Gets the nc parameter from a digest authentication attempt.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> that represents the nc parameter.
    /// </value>
    public string Nc => _parameters["nc"];

    /// <summary>
    /// Gets the nonce parameter from a digest authentication attempt.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> that represents the nonce parameter.
    /// </value>
    public string Nonce => _parameters["nonce"];

    /// <summary>
    /// Gets the opaque parameter from a digest authentication attempt.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> that represents the opaque parameter.
    /// </value>
    public string Opaque => _parameters["opaque"];

    /// <summary>
    /// Gets the qop parameter from a digest authentication attempt.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> that represents the qop parameter.
    /// </value>
    public string Qop => _parameters["qop"];

    /// <summary>
    /// Gets the realm parameter from a digest authentication attempt.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> that represents the realm parameter.
    /// </value>
    public string Realm => _parameters["realm"];

    /// <summary>
    /// Gets the response parameter from a digest authentication attempt.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> that represents the response parameter.
    /// </value>
    public string Response => _parameters["response"];

    /// <summary>
    /// Gets the uri parameter from a digest authentication attempt.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> that represents the uri parameter.
    /// </value>
    public string Uri => _parameters["uri"];

    internal bool IsValid(string password, string realm, string method, string entity)
    {
        var copied = new NameValueCollection(_parameters);
        copied["password"] = password;
        copied["realm"] = realm;
        copied["method"] = method;
        copied["entity"] = entity;

        var expected = AuthenticationResponse.CreateRequestDigest(copied);
        return _parameters["response"] == expected;
    }
}
