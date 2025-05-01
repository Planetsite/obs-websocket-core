using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace WebSocketSharp.Net;

internal sealed class EndPointManager
{
    private static readonly Dictionary<IPEndPoint, EndPointListener> _endpoints;

    static EndPointManager()
    {
        _endpoints = new Dictionary<IPEndPoint, EndPointListener>();
    }

    private EndPointManager()
    {
    }

    private static void addPrefix(string uriPrefix, HttpListener listener)
    {
        var pref = new HttpListenerPrefix(uriPrefix);

        var addr = convertToIPAddress(pref.Host);
        if (addr == null)
            throw new HttpListenerException(87, "Includes an invalid host.");

        if (!addr.IsLocal())
            throw new HttpListenerException(87, "Includes an invalid host.");

        int port;
        if (!Int32.TryParse(pref.Port, out port))
            throw new HttpListenerException(87, "Includes an invalid port.");

        if (!port.IsPortNumber())
            throw new HttpListenerException(87, "Includes an invalid port.");

        var path = pref.Path;
        if (path.IndexOf('%') != -1)
            throw new HttpListenerException(87, "Includes an invalid path.");

        if (path.IndexOf("//", StringComparison.Ordinal) != -1)
            throw new HttpListenerException(87, "Includes an invalid path.");

        var endpoint = new IPEndPoint(addr, port);

        EndPointListener lsnr;
        if (_endpoints.TryGetValue(endpoint, out lsnr))
        {
            if (lsnr.IsSecure ^ pref.IsSecure)
                throw new HttpListenerException(87, "Includes an invalid scheme.");
        }
        else
        {
            lsnr =
              new EndPointListener(
                endpoint,
                pref.IsSecure,
                listener.CertificateFolderPath,
                listener.SslConfiguration,
                listener.ReuseAddress
              );

            _endpoints.Add(endpoint, lsnr);
        }

        lsnr.AddPrefix(pref, listener);
    }

    private static IPAddress convertToIPAddress(string hostname)
    {
        if (hostname == "*")
            return IPAddress.Any;

        if (hostname == "+")
            return IPAddress.Any;

        return hostname.ToIPAddress();
    }

    private static async Task removePrefixAsync(string uriPrefix, HttpListener listener)
    {
        var pref = new HttpListenerPrefix(uriPrefix);

        var addr = convertToIPAddress(pref.Host);
        if (addr == null)
            return;

        if (!addr.IsLocal())
            return;

        int port;
        if (!Int32.TryParse(pref.Port, out port))
            return;

        if (!port.IsPortNumber())
            return;

        var path = pref.Path;
        if (path.IndexOf('%') != -1)
            return;

        if (path.IndexOf("//", StringComparison.Ordinal) != -1)
            return;

        var endpoint = new IPEndPoint(addr, port);

        EndPointListener lsnr;
        if (!_endpoints.TryGetValue(endpoint, out lsnr))
            return;

        if (lsnr.IsSecure ^ pref.IsSecure)
            return;

        await lsnr.RemovePrefixAsync(pref, listener);
    }

    internal static async Task<bool> RemoveEndPointAsync(IPEndPoint endpoint)
    {
        //lock (((ICollection)_endpoints).SyncRoot)
        {
            EndPointListener lsnr;
            if (!_endpoints.TryGetValue(endpoint, out lsnr))
                return false;

            _endpoints.Remove(endpoint);
            await lsnr.CloseAsync();

            return true;
        }
    }

    public static async Task AddListenerAsync(HttpListener listener)
    {
        var added = new List<string>();
        //lock (((ICollection)_endpoints).SyncRoot)
        {
            try
            {
                foreach (var pref in listener.Prefixes)
                {
                    addPrefix(pref, listener);
                    added.Add(pref);
                }
            }
            catch
            {
                foreach (var pref in added)
                    await removePrefixAsync(pref, listener);

                throw;
            }
        }
    }

    public static void AddPrefix(string uriPrefix, HttpListener listener)
    {
        //lock (((ICollection)_endpoints).SyncRoot)
        addPrefix(uriPrefix, listener);
    }

    public static async Task RemoveListenerAsync(HttpListener listener)
    {
        //lock (((ICollection)_endpoints).SyncRoot)
        {
            foreach (var pref in listener.Prefixes)
                await removePrefixAsync(pref, listener);
        }
    }

    public static async Task RemovePrefixAsync(string uriPrefix, HttpListener listener)
    {
        //lock (((ICollection)_endpoints).SyncRoot)
        await removePrefixAsync(uriPrefix, listener);
    }
}
