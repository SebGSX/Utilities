// Copyright © 2023 Seb Garrioch. All rights reserved.
// Published under the MIT License.

namespace Utilities.OptimizedInternals.Threading;

/// <summary>
///     Encapsulates a thread that listens for incoming requests, queues them, and then dispatches them for processing.
/// </summary>
/// <typeparam name="T">The type of the request that the listener listens for and then processes.</typeparam>
/// <remarks>
///     Internally, the <see cref="IListenerThread{T}" /> interface implementation uses a <see cref="Thread" />.
/// </remarks>
public interface IListenerThread<T> : IDisposable
{
    /// <summary>
    ///     The thread join timeout in milliseconds.
    /// </summary>
    public const ushort ThreadJoinTimeout = 2500;

    /// <summary>
    ///     Gets the listener thread's state.
    /// </summary>
    public ListenerThreadStates State { get; }

    /// <summary>
    ///     Enqueues a request for processing.
    /// </summary>
    /// <param name="request">The request to enqueue.</param>
    public void EnqueueRequest(T request);

    /// <summary>
    ///     Starts the listener thread.
    /// </summary>
    /// <param name="requestProcessorAction">
    ///     The <see cref="Action{T, CancellationToken}" /> to invoke when a request is ready to be processed.
    /// </param>
    public void Start(Action<T, CancellationToken> requestProcessorAction);

    /// <summary>
    ///     Tries to reset the listener thread to its initial state, which recycles the thread and clears the request
    ///     queue.
    /// </summary>
    /// <returns><c>true</c> if the listener thread was reset; otherwise, <c>false</c>.</returns>
    /// <remarks>
    ///     Unless the listener thread is in the <see cref="ListenerThreadStates.Stopping" />,
    ///     <see cref="ListenerThreadStates.Stopped" />, <see cref="ListenerThreadStates.Resetting" />, or
    ///     <see cref="ListenerThreadStates.Initialized" /> states, the listener thread will be reset successfully.
    /// </remarks>
    public bool TryReset();

    /// <summary>
    ///     Tries to stop the listener thread.
    /// </summary>
    /// <returns><c>true</c> if the listener thread was stopped; otherwise, <c>false</c>.</returns>
    /// <remarks>
    ///     To restart the listener thread, call <see cref="TryReset()" /> and then
    ///     <see cref="Start(Action{T, CancellationToken})" />. Please note that that <see cref="TryStop()" />
    ///     attempts a graceful shutdown of the thread, failing which it interrupts the thread to force it to stop.
    /// </remarks>
    public bool TryStop();
}