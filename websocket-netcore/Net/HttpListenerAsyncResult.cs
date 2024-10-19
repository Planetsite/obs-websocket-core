/*
 * HttpListenerAsyncResult.cs
 *
 * This code is derived from ListenerAsyncResult.cs (System.Net) of Mono
 * (http://www.mono-project.com).
 *
 * The MIT License
 *
 * Copyright (c) 2005 Ximian, Inc. (http://www.ximian.com)
 * Copyright (c) 2012-2016 sta.blockhead
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

/*
 * Authors:
 * - Gonzalo Paniagua Javier <gonzalo@ximian.com>
 */

/*
 * Contributors:
 * - Nicholas Devenish
 */

using System;
using System.Threading;

namespace WebSocketSharp.Net;

internal class HttpListenerAsyncResult : IAsyncResult
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
