/*
 * HttpStreamAsyncResult.cs
 *
 * This code is derived from HttpStreamAsyncResult.cs (System.Net) of Mono
 * (http://www.mono-project.com).
 *
 * The MIT License
 *
 * Copyright (c) 2005 Novell, Inc. (http://www.novell.com)
 * Copyright (c) 2012-2015 sta.blockhead
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
 * - Gonzalo Paniagua Javier <gonzalo@novell.com>
 */

using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketSharp.Net;

internal class HttpStreamAsyncResult : IAsyncResult
{
    private byte[] _buffer;
    private Func<Task> _callbackAsync;
    private bool _completed;
    private int _count;
    private Exception _exception;
    private int _offset;
    private object _state;
    //private object _sync;
    private int _syncRead;
    private ManualResetEvent _waitHandle;




    internal HttpStreamAsyncResult(Func<Task> callback, object state)
    {
        _callbackAsync = callback;
        _state = state;
        //_sync = new object();
    }




    internal byte[] Buffer
    {
        get
        {
            return _buffer;
        }

        set
        {
            _buffer = value;
        }
    }

    internal int Count
    {
        get
        {
            return _count;
        }

        set
        {
            _count = value;
        }
    }

    internal Exception Exception
    {
        get
        {
            return _exception;
        }
    }

    internal bool HasException => _exception != null;

    internal int Offset
    {
        get => _offset;

        set => _offset = value;
    }

    internal int SyncRead
    {
        get => _syncRead;

        set => _syncRead = value;
    }

    public object AsyncState => _state;

    //lock (_sync)
    public WaitHandle AsyncWaitHandle => _waitHandle ??= new ManualResetEvent(_completed);

    public bool CompletedSynchronously => _syncRead == _count;

    //lock (_sync)
    public bool IsCompleted => _completed;

    internal async Task CompleteAsync()
    {
        //lock (_sync)
        {
            if (_completed)
                return;

            _completed = true;
            if (_waitHandle != null)
                _waitHandle.Set();

            if (_callbackAsync != null)
                await _callbackAsync();
        }
    }

    internal async Task CompleteAsync(Exception exception)
    {
        _exception = exception;
        await CompleteAsync();
    }
}
