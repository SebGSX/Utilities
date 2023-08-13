// Copyright © 2023 Seb Garrioch. All rights reserved.
// Published under the MIT License.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Utilities.OptimizedInternals.Helpers;

namespace Utilities.OptimizedInternals.Threading;

/// <inheritdoc />
[DebuggerDisplay(nameof(State) + " = {" + nameof(State) + "}")]
public sealed class ListenerThread<T> : IListenerThread<T>
{
    /*
     * We use a ManualResetEventSlim instead of a ManualResetEvent because the former is more efficient. The event
     * allows us to signal the worker thread that there is work to do with minimal overhead. Semaphore, SpinLock, Mutex,
     * and Monitor are all inappropriate for this use case. Monitor has too much overhead and SpinLock will waste compute
     * while waiting. Semaphore and Mutex are inappropriate because we only need to signal one thread.
     */
    private readonly ManualResetEventSlim _dataReadyEvent;

    private readonly ILogger<ListenerThread<T>> _logger;

    /*
     * While it may be tempting to use a BlockingCollection<T>, it would be inappropriate. The BlockingCollection<T>
     * uses a ConcurrentQueue<T> internally, which is exactly what we need. However, the BlockingCollection<T> is more
     * general than the implementation we need catering for bounding among other things. By contrast, using
     * ConcurrentQueue<T> with a ManualResetEventSlim is more efficient and thus faster. Use of the various spin locks
     * further improves performance.
     */
    private readonly ConcurrentQueue<T> _requestQueue;

    /*
     * Synchronizing state changes is complex and compute intensive. As such, we will depend on Monitor via lock() to
     * provide for thread safety. We will use a dedicated object for such locking to avoid deadlocks.
     */
    private readonly object _stateSyncLockObject;

    private CancellationTokenSource _cancellationTokenSource;
    private Action<T, CancellationToken>? _requestHandler;

    private volatile ListenerThreadStates _state;

    // SpinLock is the most efficient lock for setting state in that setting state is very quick.
    private SpinLock _stateLock;
    private Thread _workerThread;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ListenerThread{T}" /> class.
    /// </summary>
    /// <param name="logger">
    ///     The <see cref="ILogger{TCategoryName}" /> to use for logging.
    /// </param>
    /// <remarks>
    ///     Internally, the <see cref="ListenerThread{T}" /> class uses a <see cref="ConcurrentQueue{T}" /> to normalize
    ///     the flow of requests. While it may be tempting to simply hand requests to the listener thread piecemeal,
    ///     doing so would result in the listener thread suspending and resuming in between each request submission. As
    ///     such, the internal queue reduces significant overhead at the cost of a small amount of memory.
    /// </remarks>
    public ListenerThread(ILogger<ListenerThread<T>> logger)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _dataReadyEvent = new ManualResetEventSlim(false);
        _logger = logger;
        _requestQueue = new ConcurrentQueue<T>();
        _state = ListenerThreadStates.Initialized;
        _stateLock = new SpinLock();
        _stateSyncLockObject = new object();
        _workerThread = new Thread(() => WorkerThreadDelegate(_cancellationTokenSource.Token));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_stateSyncLockObject)
        {
            if (!SetState(ListenerThreadStates.Disposing)) return;

            StopInternal();
            // Stryker disable once all
            _dataReadyEvent.Dispose();
            SetState(ListenerThreadStates.Disposed);
            // Stryker disable once all
            GC.SuppressFinalize(this);
        }
    }

    /// <inheritdoc />
    public ListenerThreadStates State => _state;

    /// <inheritdoc />
    public void EnqueueRequest(T request)
    {
        ObjectDisposedException.ThrowIf(
            _state is ListenerThreadStates.Disposing or ListenerThreadStates.Disposed,
            typeof(ListenerThread<T>));

        ThrowHelper.ThrowIf<InvalidOperationException>(
            _state != ListenerThreadStates.Ready && _state != ListenerThreadStates.Running,
            "Cannot enqueue a request while the thread is not ready or running.");

        _requestQueue.Enqueue(request);
        // Set the state to running after enqueueing the request to ensure that the request is processed.
        SetState(ListenerThreadStates.Running);
    }

    /// <inheritdoc />
    public void Start(Action<T, CancellationToken> requestProcessorAction)
    {
        lock (_stateSyncLockObject)
        {
            ObjectDisposedException.ThrowIf(
                _state is ListenerThreadStates.Disposing or ListenerThreadStates.Disposed,
                typeof(ListenerThread<T>));

            ThrowHelper.ThrowIf<InvalidOperationException>(
                _state != ListenerThreadStates.Initialized,
                "Cannot start the listener thread unless it is the initialized state.");

            _requestHandler = requestProcessorAction;
            _workerThread.Start();
            SetState(ListenerThreadStates.Ready);
        }
    }

    /// <inheritdoc />
    public bool TryReset()
    {
        lock (_stateSyncLockObject)
        {
            ObjectDisposedException.ThrowIf(
                _state is ListenerThreadStates.Disposing or ListenerThreadStates.Disposed,
                typeof(ListenerThread<T>));

            if (!SetState(ListenerThreadStates.Resetting)) return false;

            /*
             * Stop the listener thread, which will cancel the worker thread's CancellationToken and clear the request
             * queue. Do not call Stop() because it will cause a deadlock.
             */
            StopInternal();

            Debug.Assert(!_workerThread.IsAlive,
                nameof(ListenerThread<T>) + "." + nameof(WorkerThreadDelegate) +
                ": _workerThread.IsAlive == true");

            /*
             * Calling TryReset() will always return false because Stop() calls Cancel() on _cancellationTokenSource. As
             * such, we create a new instance of CancellationTokenSource to reset _cancellationTokenSource.
             */
            _cancellationTokenSource = new CancellationTokenSource();
            // Once stopped, a thread cannot be restarted. As such, we create a new instance of Thread.
            _workerThread = new Thread(() => WorkerThreadDelegate(_cancellationTokenSource.Token));
            _logger.LogWarning("Listener thread reset.");
            SetState(ListenerThreadStates.Initialized);
            // By this stage, the listener thread is in the same state as it was when it was first created.
            return true;
        }
    }

    /// <inheritdoc />
    public bool TryStop()
    {
        lock (_stateSyncLockObject)
        {
            ObjectDisposedException.ThrowIf(
                _state is ListenerThreadStates.Disposing or ListenerThreadStates.Disposed,
                typeof(ListenerThread<T>));

            if (!SetState(ListenerThreadStates.Stopping)) return false;

            StopInternal();

            Debug.Assert(!_workerThread.IsAlive,
                nameof(ListenerThread<T>) + "." + nameof(WorkerThreadDelegate) +
                ": _workerThread.IsAlive == true");

            SetState(ListenerThreadStates.Stopped);

            return true;
        }
    }

    private void ProcessQueue(CancellationToken cancellationToken)
    {
        Debug.Assert(_requestHandler is not null,
            nameof(ListenerThread<T>) + "." + nameof(WorkerThreadDelegate) +
            ": _requestProcessorAction is null");

        try
        {
            while (_requestQueue.TryDequeue(out var request))
                try
                {
                    // It is expected that the request handler action will handle any exceptions.
                    _requestHandler!(request, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // The operation cancellation exception is handled by this method's caller and must not be swallowed.
                    throw;
                }
                catch (ThreadInterruptedException e)
                {
                    // Special handling for thread interruption.
                    _logger.LogError(e, "Thread interrupted while processing a request.");
                }
                catch (Exception e)
                {
                    // The worker thread must not throw exceptions.
                    _logger.LogError(e, "An exception occurred while processing a request.");
                }
        }
        finally
        {
            SetState(ListenerThreadStates.Ready);
        }
    }

    private bool SetState(ListenerThreadStates state)
    {
        // Stryker disable once all
        var lockTaken = false;
        try
        {
            _stateLock.Enter(ref lockTaken);
            if (_state == state) return false;

            switch (state)
            {
                case ListenerThreadStates.Disposing
                    /*
                     * Calling Dispose() repeatedly must not throw an exception despite the fact that it will try to set
                     * the state to Disposing. As such, we return false to indicate that the state was not changed.
                     * Doing so will prevent Dispose() from executing fully more than once.
                     */
                    when _state is ListenerThreadStates.Disposed:
                    return false;
                case ListenerThreadStates.Ready
                    // Stryker disable once all
                    when _dataReadyEvent.IsSet && _requestQueue.IsEmpty:
                    _dataReadyEvent.Reset();
                    break;
                case ListenerThreadStates.Running
                    // Stryker disable once all
                    when !_dataReadyEvent.IsSet && !_requestQueue.IsEmpty:
                    _dataReadyEvent.Set();
                    break;
                case ListenerThreadStates.Stopping
                    when _state is ListenerThreadStates.Stopped:
                    return false;
                case ListenerThreadStates.Resetting
                    when _state is ListenerThreadStates.Initialized:
                    return false;
                case ListenerThreadStates.Disposed:
                case ListenerThreadStates.Initialized:
                case ListenerThreadStates.Stopped:
                default:
                    // These states do not require any special handling.
                    break;
            }

            _state = state;
            return true;
        }
        finally
        {
            if (lockTaken) _stateLock.Exit(false);
        }
    }

    private void StopInternal()
    {
        _requestQueue.Clear();
        _cancellationTokenSource.Cancel();
        _dataReadyEvent.Reset();

        if (!_workerThread.IsAlive) return;

        /*
         * Given that the worker thread will alternate between waiting on the data ready event and processing
         * requests, we should only stop the worker thread when the listener thread is no longer needed or it is
         * being reset. Under such circumstances, it is appropriate to wait for the worker thread to stop.
         */

        // Stryker disable once all
        if (_workerThread.Join(IListenerThread<T>.ThreadJoinTimeout)) return;

        _logger.LogWarning("Worker thread did not stop in a timely manner. Interrupting the thread...");

        /*
         * With reference to the above comment, when we call Stop() we expect the worker thread to stop in a timely
         * manner. Accordingly, we prevent the listener from receiving new requests and clear the request queue then
         * we cancel the token and reset the data ready event. We then wait for the worker thread to stop within a
         * reasonable amount of time. If the worker thread does not stop within the allotted time, we interrupt the
         * thread. Doing so is a last resort, which should only be needed when the worker thread has hung perhaps
         * due to a deadlock or an infinite loop.
         */
        // Stryker disable once all
        _workerThread.Interrupt();
        _workerThread.Join();

        _logger.LogWarning("Worker thread interrupted.");
    }

    private void WorkerThreadDelegate(CancellationToken cancellationToken)
    {
        Debug.Assert(_requestHandler is not null,
            nameof(ListenerThread<T>) + "." + nameof(WorkerThreadDelegate) +
            ": _requestProcessorAction is null");

        try
        {
            /*
             * The worker thread will alternate between waiting on the data ready event and processing requests until
             * cancellation is requested.
             */
            while (!cancellationToken.IsCancellationRequested)
            {
                _dataReadyEvent.Wait(cancellationToken);
                ProcessQueue(cancellationToken);
            }
        }
        /*
         * Other than the cancellation token being cancelled, ProcessQueue() exceptions should not bubble up because
         * the method swallows exceptions and logs them. Any exception that occurs at this level is unexpected and is
         * allowed to propagate.
         */
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Worker thread cancelled.");
        }
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage(Justification = "Finalizers cannot be tested.")]
    ~ListenerThread()
    {
        if (_state == ListenerThreadStates.Disposed) return;
        Dispose();
    }
}