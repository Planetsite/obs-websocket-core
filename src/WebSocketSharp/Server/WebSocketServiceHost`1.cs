using Microsoft.Extensions.Logging;
using System;

namespace WebSocketSharp.Server;

internal sealed class WebSocketServiceHost<TBehavior> : WebSocketServiceHost
    where TBehavior : WebSocketBehavior
{
    private readonly Func<TBehavior> _creator;

    internal WebSocketServiceHost(string path, Func<TBehavior> creator, ILogger log)
        : this(path, creator, null, log)
    {
    }

    internal WebSocketServiceHost(string path, Func<TBehavior> creator, Action<TBehavior> initializer, ILogger log)
        : base(path, log)
        => _creator = CreateCreator(creator, initializer);

    public override Type BehaviorType => typeof(TBehavior);

    private Func<TBehavior> CreateCreator(Func<TBehavior> creator, Action<TBehavior> initializer)
    {
        if (initializer == null)
            return creator;

        return () =>
        {
            var ret = creator();
            initializer(ret);

            return ret;
        };
    }

    protected override WebSocketBehavior CreateSession()
        => _creator();
}
