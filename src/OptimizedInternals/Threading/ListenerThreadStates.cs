// Copyright © 2023 Seb Garrioch. All rights reserved.
// Published under the MIT License.

namespace Utilities.OptimizedInternals.Threading;

/// <summary>
///     Represents the different states of a listener thread.
/// </summary>
/// <remarks>
///     The listener thread alternates between <see cref="Ready" /> and <see cref="Running" /> until stopped or reset.
/// </remarks>
public enum ListenerThreadStates
{
    /// <summary>
    ///     The listener thread is disposed.
    /// </summary>
    Disposed = -2,

    /// <summary>
    ///     The listener thread is disposing.
    /// </summary>
    Disposing = -1,

    /// <summary>
    ///     The listener thread is initialized and ready to start.
    /// </summary>
    /// <remarks>The listener thread will not accept requests until started.</remarks>
    Initialized = 0,

    /// <summary>
    ///     The listener thread is started and ready to accept requests.
    /// </summary>
    /// <remarks>The listener thread's internal queue is empty in this state.</remarks>
    Ready = 1,

    /// <summary>
    ///     The listener thread is running and processing requests.
    /// </summary>
    /// <remarks>The listener thread will continue to accept requests while running.</remarks>
    Running = 2,

    /// <summary>
    ///     The listener thread is stopping and is no longer able to accept or process requests.
    /// </summary>
    /// <remarks>The listener thread is cancelling all pending and active requests.</remarks>
    Stopping = 3,

    /// <summary>
    ///     The listener thread is stopped and is no longer able to accept or process requests.
    /// </summary>
    /// <remarks>
    ///     The listener thread must be reset to <see cref="Initialized" /> before it can be started again.
    /// </remarks>
    Stopped = 4,

    /// <summary>
    ///     The listener thread is resetting and is no longer able to accept or process requests.
    /// </summary>
    Resetting = 5
}