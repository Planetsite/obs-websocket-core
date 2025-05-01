using System;
using System.Threading;

namespace WebSocketSharp.Net;

internal sealed class HttpListenerAsyncResult : IAsyncResult
{
    private AsyncCallback _callback;
    private bool _completed;
    private HttpListenerContext _context;
    private Exception _exception;
    private object _sync;
    private ManualResetEvent _waitHandle;

    internal HttpListenerAsyncResult(AsyncCallback callback, object state)
    {
        _callback = callback;
        AsyncState = state;
        _sync = new object();
    }

    internal bool EndCalled { get; set; }

    internal bool InGet { get; set; }

    public object AsyncState { get; }

    public WaitHandle AsyncWaitHandle
    {
        get
        {
            lock (_sync)
                return _waitHandle ?? (_waitHandle = new ManualResetEvent(_completed));
        }
    }

    public bool CompletedSynchronously { get; private set; }

    public bool IsCompleted
    {
        get
        {
            lock (_sync)
                return _completed;
        }
    }

    private static void complete(HttpListenerAsyncResult asyncResult)
    {
        lock (asyncResult._sync)
        {
            asyncResult._completed = true;

            var waitHandle = asyncResult._waitHandle;
            if (waitHandle != null)
                waitHandle.Set();
        }

        var callback = asyncResult._callback;
        if (callback == null)
            return;

        ThreadPool.QueueUserWorkItem(
          state =>
          {
              try
              {
                  callback(asyncResult);
              }
              catch
              {
              }
          },
          null
        );
    }

    internal void Complete(Exception exception)
    {
        _exception = InGet && (exception is ObjectDisposedException)
                     ? new HttpListenerException(995, "The listener is closed.")
                     : exception;

        complete(this);
    }

    internal void Complete(HttpListenerContext context)
    {
        Complete(context, false);
    }

    internal void Complete(HttpListenerContext context, bool syncCompleted)
    {
        _context = context;
        CompletedSynchronously = syncCompleted;

        complete(this);
    }

    internal HttpListenerContext GetContext()
    {
        if (_exception != null)
            throw _exception;

        return _context;
    }
}
