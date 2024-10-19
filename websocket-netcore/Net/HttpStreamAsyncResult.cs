/*
 * Authors:
 * - Gonzalo Paniagua Javier <gonzalo@novell.com>
 */

using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketSharp.Net;

internal sealed class HttpStreamAsyncResult : IAsyncResult
{
    private Func<Task> _callbackAsync;
    private bool _completed;
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

    internal byte[] Buffer { get; set; }

    internal int Count { get; set; }

    internal Exception Exception { get; private set; }

    internal bool HasException => Exception != null;

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

    public bool CompletedSynchronously => _syncRead == Count;

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
        Exception = exception;
        await CompleteAsync();
    }
}
