using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

static class SocketOperationsEx
{
    private delegate bool SocketOperationDelegate(Socket socket, SocketAsyncEventArgs args);

    private static readonly SocketOperationDelegate AcceptAsyncHandler = (s, e) => s.AcceptAsync(e);
    private static readonly SocketOperationDelegate ConnectAsyncHandler = (s, e) => s.ConnectAsync(e);
    private static readonly SocketOperationDelegate ReceiveAsyncHandler = (s, e) => s.ReceiveAsync(e);
    private static readonly SocketOperationDelegate SendAsyncHandler = (s, e) => s.SendAsync(e);

    //TODO: Fix alloction per socket accept and remo task.
    public static async Task<Socket> AcceptSocketAsync(this Socket listenSocket, SocketAwaitableEventArgs awaitable)
    {
        await AcceptAsync(listenSocket, awaitable);
        var acceptSocket = awaitable.AcceptSocket;
        awaitable.AcceptSocket = null;
        return acceptSocket;
    }

    public static SocketAwaitableEventArgs AcceptAsync(this Socket socket, SocketAwaitableEventArgs awaitable)
    {
        return OperationAsync(socket, awaitable, AcceptAsyncHandler);
    }

    public static SocketAwaitableEventArgs ConnectSocketAsync(this Socket socket, SocketAwaitableEventArgs awaitable)
    {
        return OperationAsync(socket, awaitable, ConnectAsyncHandler);
    }

    public static SocketAwaitableEventArgs ReceiveSocketAsync(this Socket socket, SocketAwaitableEventArgs awaitable)
    {
        return OperationAsync(socket, awaitable, ReceiveAsyncHandler);
    }

    public static SocketAwaitableEventArgs SendSocketAsync(this Socket socket, SocketAwaitableEventArgs awaitable)
    {
        return OperationAsync(socket, awaitable, SendAsyncHandler);
    }

    static SocketAwaitableEventArgs OperationAsync(this Socket socket, SocketAwaitableEventArgs awaitable, SocketOperationDelegate socketFunc)
    {
        awaitable.StartOperation();

        if (!socketFunc(socket, awaitable))
        {
            awaitable.CompleteOperation();
        }

        return awaitable;
    }
}

public class SocketAwaitableEventArgs : SocketAsyncEventArgs, IAwaiter
{
    private readonly static Action Sentinel = () => { };

    private bool _wasCompleted;
    private Action _continuation;

    internal SocketAwaitableEventArgs()
    {
        _wasCompleted = true;
    }

    protected override void OnCompleted(SocketAsyncEventArgs e)
    {
        base.OnCompleted(e);
        this.CompleteOperation();
        var prev = _continuation ?? Interlocked.CompareExchange(ref _continuation, Sentinel, null);
        if (prev != null)
        {
            prev();
        }
    }

    internal void StartOperation()
    {
        if (!_wasCompleted)
        {
            throw new InvalidOperationException("Cannot start operation on a pending awaiter.");
        }
        _wasCompleted = false;
        _continuation = null;
    }

    internal void CompleteOperation()
    {
        _wasCompleted = true;
    }

    #region Awaitable

    public SocketAwaitableEventArgs GetAwaiter() { return this; }

    public bool IsCompleted { get { return _wasCompleted; } }

    public void OnCompleted(Action continuation)
    {
        if (_continuation == Sentinel || Interlocked.CompareExchange(ref _continuation, continuation, null) == Sentinel)
        {
            // Run the continuation synchronously
            // TODO: Ensure no stack dives and if so do Task.Run()                
            continuation();
        }
    }

    public void GetResult()
    {
        if (this.SocketError != SocketError.Success)
            throw new SocketException((int)this.SocketError);
    }

    #endregion

}

internal interface IAwaiter : INotifyCompletion
{
    void GetResult();

    bool IsCompleted { get; }
}

internal interface IAwaiter<out TResult> : INotifyCompletion
{
    TResult GetResult();

    bool IsCompleted { get; }
}


