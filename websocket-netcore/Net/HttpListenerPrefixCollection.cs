using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebSocketSharp.Net;

/// <summary>
/// Provides a collection used to store the URI prefixes for a instance of
/// the <see cref="HttpListener"/> class.
/// </summary>
/// <remarks>
/// The <see cref="HttpListener"/> instance responds to the request which has
/// a requested URI that the prefixes most closely match.
/// </remarks>
public sealed class HttpListenerPrefixCollection : ICollection<string>
{
    private HttpListener _listener;
    private List<string> _prefixes;

    internal HttpListenerPrefixCollection(HttpListener listener)
    {
        _listener = listener;
        _prefixes = new List<string>();
    }

    /// <summary>
    /// Gets the number of prefixes in the collection.
    /// </summary>
    /// <value>
    /// An <see cref="int"/> that represents the number of prefixes.
    /// </value>
    public int Count => _prefixes.Count;

    /// <summary>
    /// Gets a value indicating whether the access to the collection is
    /// read-only.
    /// </summary>
    /// <value>
    /// Always returns <c>false</c>.
    /// </value>
    public bool IsReadOnly => false;

    /// <summary>
    /// Gets a value indicating whether the access to the collection is
    /// synchronized.
    /// </summary>
    /// <value>
    /// Always returns <c>false</c>.
    /// </value>
    public bool IsSynchronized => false;

    /// <summary>
    /// Adds the specified URI prefix to the collection.
    /// </summary>
    /// <param name="uriPrefix">
    ///   <para>
    ///   A <see cref="string"/> that specifies the URI prefix to add.
    ///   </para>
    ///   <para>
    ///   It must be a well-formed URI prefix with http or https scheme,
    ///   and must end with a '/'.
    ///   </para>
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="uriPrefix"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="uriPrefix"/> is invalid.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// The <see cref="HttpListener"/> instance associated with this
    /// collection is closed.
    /// </exception>
    public void Add(string uriPrefix)
    {
        if (_listener.IsDisposed)
            throw new ObjectDisposedException(_listener.GetType().ToString());

        HttpListenerPrefix.CheckPrefix(uriPrefix);

        if (_prefixes.Contains(uriPrefix))
            return;

        _prefixes.Add(uriPrefix);

        if (!_listener.IsListening)
            return;

        EndPointManager.AddPrefix(uriPrefix, _listener);
    }

    /// <summary>
    /// Removes all URI prefixes from the collection.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// The <see cref="HttpListener"/> instance associated with this
    /// collection is closed.
    /// </exception>
    public void Clear() =>
        ClearAsync().Wait();

    public async Task ClearAsync()
    {
        if (_listener.IsDisposed)
            throw new ObjectDisposedException(_listener.GetType().ToString());

        _prefixes.Clear();

        if (!_listener.IsListening)
            return;

        await EndPointManager.RemoveListenerAsync(_listener);
    }

    /// <summary>
    /// Returns a value indicating whether the collection contains the
    /// specified URI prefix.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the collection contains the URI prefix; otherwise,
    /// <c>false</c>.
    /// </returns>
    /// <param name="uriPrefix">
    /// A <see cref="string"/> that specifies the URI prefix to test.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="uriPrefix"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// The <see cref="HttpListener"/> instance associated with this
    /// collection is closed.
    /// </exception>
    public bool Contains(string uriPrefix)
    {
        if (_listener.IsDisposed)
            throw new ObjectDisposedException(_listener.GetType().ToString());

        if (uriPrefix == null)
            throw new ArgumentNullException("uriPrefix");

        return _prefixes.Contains(uriPrefix);
    }

    /// <summary>
    /// Copies the contents of the collection to the specified array of string.
    /// </summary>
    /// <param name="array">
    /// An array of <see cref="string"/> that specifies the destination of
    /// the URI prefix strings copied from the collection.
    /// </param>
    /// <param name="offset">
    /// An <see cref="int"/> that specifies the zero-based index in
    /// the array at which copying begins.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="array"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="offset"/> is less than zero.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The space from <paramref name="offset"/> to the end of
    /// <paramref name="array"/> is not enough to copy to.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// The <see cref="HttpListener"/> instance associated with this
    /// collection is closed.
    /// </exception>
    public void CopyTo(string[] array, int offset)
    {
        if (_listener.IsDisposed)
            throw new ObjectDisposedException(_listener.GetType().ToString());

        _prefixes.CopyTo(array, offset);
    }

    /// <summary>
    /// Gets the enumerator that iterates through the collection.
    /// </summary>
    /// <returns>
    /// An <see cref="T:System.Collections.Generic.IEnumerator{string}"/>
    /// instance that can be used to iterate through the collection.
    /// </returns>
    public IEnumerator<string> GetEnumerator()
    {
        return _prefixes.GetEnumerator();
    }

    /// <summary>
    /// Removes the specified URI prefix from the collection.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the URI prefix is successfully removed; otherwise,
    /// <c>false</c>.
    /// </returns>
    /// <param name="uriPrefix">
    /// A <see cref="string"/> that specifies the URI prefix to remove.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="uriPrefix"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// The <see cref="HttpListener"/> instance associated with this
    /// collection is closed.
    /// </exception>
    public bool Remove(string uriPrefix) =>
        RemoveAsync(uriPrefix).Result;

    public async Task<bool> RemoveAsync(string uriPrefix)
    {
        if (_listener.IsDisposed)
            throw new ObjectDisposedException(_listener.GetType().ToString());

        if (uriPrefix == null)
            throw new ArgumentNullException("uriPrefix");

        var ret = _prefixes.Remove(uriPrefix);

        if (!ret)
            return ret;

        if (!_listener.IsListening)
            return ret;

        await EndPointManager.RemovePrefixAsync(uriPrefix, _listener);

        return ret;
    }

    /// <summary>
    /// Gets the enumerator that iterates through the collection.
    /// </summary>
    /// <returns>
    /// An <see cref="IEnumerator"/> instance that can be used to iterate
    /// through the collection.
    /// </returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return _prefixes.GetEnumerator();
    }
}
