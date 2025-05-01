using System.Security.Principal;

namespace WebSocketSharp.Net;

/// <summary>
/// Holds the username and password from an HTTP Basic authentication attempt.
/// </summary>
public class HttpBasicIdentity : GenericIdentity
{
    private string _password;

    internal HttpBasicIdentity(string username, string password)
      : base(username, "Basic")
    {
        _password = password;
    }

    /// <summary>
    /// Gets the password from a basic authentication attempt.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> that represents the password.
    /// </value>
    public virtual string Password => _password;
}
