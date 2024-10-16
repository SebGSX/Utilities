// Copyright © 2023 Seb Garrioch. All rights reserved.
// Published under the MIT License.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Utilities.OptimizedInternals.Threading;

namespace Utilities.OptimizedInternals.Tests.Threading.Contract;

/// <summary>
///     Tests implementations of the <see cref="IListenerThread{T}" /> interface.
/// </summary>
public class ListenerThreadInterfaceImplementationTests
{
    private const ushort MaxIterationSpinWait = GlobalTestParameters.DefaultThreadSpinWait * 10;

    /// <summary>
    ///     Tests that <see cref="ListenerThread{T}.Dispose()" /> when invoked disposes the implementation.
    /// </summary>
    /// <param name="implementation">The <see cref="IListenerThread{T}" /> implementation to test.</param>
    /// <param name="loggerMock">The mock of the <see cref="ILogger" /> used to log messages.</param>
    /// <typeparam name="TLogger">The type of logger used by the implementation.</typeparam>
    [Theory]
    [Trait("Category", "Unit")]
    [ClassData(typeof(ListenerThreadInterfaceImplementationTestData))]
    public void Dispose_Invoked_Disposes<TLogger>(
        IListenerThread<int> implementation,
        Mock<TLogger> loggerMock)
        where TLogger : class, ILogger
    {
        // Arrange
        loggerMock.Reset();

        // Act
        implementation.Dispose();
        SpinWait.SpinUntil(
            () => implementation.State == ListenerThreadStates.Disposed,
            GlobalTestParameters.DefaultThreadSpinWaitTimeoutMilliseconds);

        // Assert
        Assert.Equal(ListenerThreadStates.Disposed, implementation.State);
    }

    /// <summary>
    ///     Tests that <see cref="ListenerThread{T}.Dispose()" /> can be invoked concurrently without throwing any
    ///     exceptions.
    /// </summary>
    /// <param name="implementation">The <see cref="IListenerThread{T}" /> implementation to test.</param>
    /// <param name="loggerMock">The mock of the <see cref="ILogger" /> used to log messages.</param>
    /// <typeparam name="TLogger">The type of logger used by the implementation.</typeparam>
    [Theory]
    [Trait("Category", "Unit")]
    [ClassData(typeof(ListenerThreadInterfaceImplementationTestData))]
    public void Dispose_InvokedConcurrently_DoesNotThrow<TLogger>(
        IListenerThread<int> implementation,
        Mock<TLogger> loggerMock)
        where TLogger : class, ILogger
    {
        // Arrange
        loggerMock.Reset();
        var action = new Action<int, CancellationToken>(
            [ExcludeFromCodeCoverage(Justification = "Test method.")]
            (i, ct) => { Thread.SpinWait(GlobalTestParameters.DefaultThreadSpinWait); });
        implementation.Start(action);
        for (var i = 0; i < 100; i++) implementation.EnqueueRequest(i);

        // Act
        var exceptions = new ConcurrentBag<Exception?>();
        Parallel.For(0, 100, _ => { exceptions.Add(Record.Exception(implementation.Dispose)); });

        // Assert
        Assert.DoesNotContain(exceptions, e => e != null);
    }

    /// <summary>
    ///     Tests that <see cref="ListenerThread{T}.Dispose()" /> when invoked stops the listener thread and disposes of
    ///     the resources used by the implementation.
    /// </summary>
    /// <param name="implementation">The <see cref="IListenerThread{T}" /> implementation to test.</param>
    /// <param name="loggerMock">The mock of the <see cref="ILogger" /> used to log messages.</param>
    /// <typeparam name="TLogger">The type of logger used by the implementation.</typeparam>
    [Theory]
    [Trait("Category", "Unit")]
    [ClassData(typeof(ListenerThreadInterfaceImplementationTestData))]
    public void Dispose_Invoked_StopsListenerThread<TLogger>(
        IListenerThread<int> implementation,
        Mock<TLogger> loggerMock)
        where TLogger : class, ILogger
    {
        // Arrange
        loggerMock.Reset();
        var action = new Action<int, CancellationToken>(
            [ExcludeFromCodeCoverage(Justification = "Test method.")]
            (i, ct) => { Thread.SpinWait(GlobalTestParameters.DefaultThreadSpinWait); });
        implementation.Start(action);
        for (var i = 0; i < 100; i++) implementation.EnqueueRequest(i);

        // Act
        implementation.Dispose();
        SpinWait.SpinUntil(
            () => implementation.State == ListenerThreadStates.Disposed,
            GlobalTestParameters.DefaultThreadSpinWaitTimeoutMilliseconds);

        // Assert
        Assert.Equal(ListenerThreadStates.Disposed, implementation.State);
    }

    /// <summary>
    ///     Tests that <see cref="ListenerThread{T}.EnqueueRequest(T)" /> processes the request.
    /// </summary>
    /// <param name="implementation">The <see cref="IListenerThread{T}" /> implementation to test.</param>
    /// <param name="loggerMock">The mock of the <see cref="ILogger" /> used to log messages.</param>
    /// <typeparam name="TLogger">The type of logger used by the implementation.</typeparam>
    [Theory]
    [Trait("Category", "Unit")]
    [ClassData(typeof(ListenerThreadInterfaceImplementationTestData))]
    public void EnqueueRequest_Invoked_ProcessesRequest<TLogger>(
        IListenerThread<int> implementation,
        Mock<TLogger> loggerMock)
        where TLogger : class, ILogger
    {
        // Arrange
        loggerMock.Reset();
        var result = 0;

        var action = new Action<int, CancellationToken>(
            [ExcludeFromCodeCoverage(Justification = "Test method.")]
            (i, ct) =>
            {
                result = i * 2;
                Thread.SpinWait(GlobalTestParameters.DefaultLoopWaitTime);
            });

        // Act
        implementation.Start(action);
        implementation.EnqueueRequest(1);

        var stateRunningActivated = SpinWait.SpinUntil(
            () => implementation.State == ListenerThreadStates.Running,
            GlobalTestParameters.DefaultThreadSpinWaitTimeoutMilliseconds);
        
        SpinWait.SpinUntil(
            () => implementation.State == ListenerThreadStates.Ready,
            GlobalTestParameters.DefaultThreadSpinWaitTimeoutMilliseconds);
        var stateReady = implementation.State;
        
        implementation.TryStop();
        SpinWait.SpinUntil(
            () => implementation.State == ListenerThreadStates.Stopped,
            GlobalTestParameters.DefaultThreadSpinWaitTimeoutMilliseconds);
        var stateStopped = implementation.State;

        // Assert
        Assert.Equal(2, result);
        Assert.Equal(ListenerThreadStates.Ready, stateReady);
        Assert.Equal(ListenerThreadStates.Stopped, stateStopped);
        Assert.True(stateRunningActivated);
    }

    /// <summary>
    ///     Tests that <see cref="ListenerThread{T}.EnqueueRequest(T)" /> processes concurrent requests.
    /// </summary>
    /// <param name="implementation">The <see cref="IListenerThread{T}" /> implementation to test.</param>
    /// <param name="loggerMock">The mock of the <see cref="ILogger" /> used to log messages.</param>
    /// <typeparam name="TLogger">The type of logger used by the implementation.</typeparam>
    [Theory]
    [Trait("Category", "Unit")]
    [ClassData(typeof(ListenerThreadInterfaceImplementationTestData))]
    public void EnqueueRequest_InvokedConcurrently_ProcessesRequests<TLogger>(
        IListenerThread<int> implementation,
        Mock<TLogger> loggerMock)
        where TLogger : class, ILogger
    {
        // Arrange
        loggerMock.Reset();

        var action = new Action<int, CancellationToken>(
            [ExcludeFromCodeCoverage(Justification = "Test method.")]
            (i, ct) => { Thread.SpinWait(GlobalTestParameters.DefaultThreadSpinWait); });

        // Act
        implementation.Start(action);
        var states = new ConcurrentBag<ListenerThreadStates>();
        Parallel.For(0, 127, i =>
        {
            implementation.EnqueueRequest(i);
            states.Add(implementation.State);
        });
        
        SpinWait.SpinUntil(
            () => implementation.State == ListenerThreadStates.Ready,
            GlobalTestParameters.DefaultThreadSpinWaitTimeoutMilliseconds);
        var stateReady = implementation.State;
        
        implementation.TryStop();
        SpinWait.SpinUntil(
            () => implementation.State == ListenerThreadStates.Stopped,
            GlobalTestParameters.DefaultThreadSpinWaitTimeoutMilliseconds);
        var stateStopped = implementation.State;

        // Assert
        Assert.All(states, s => Assert.Equal(ListenerThreadStates.Running, s));
        Assert.Equal(ListenerThreadStates.Ready, stateReady);
        Assert.Equal(ListenerThreadStates.Stopped, stateStopped);
    }

    /// <summary>
    ///     Tests that <see cref="ListenerThread{T}.EnqueueRequest(T)" /> throws an <see cref="ObjectDisposedException" />
    ///     when invoked after <see cref="ListenerThread{T}.Dispose()" />.
    /// </summary>
    /// <param name="implementation">The <see cref="IListenerThread{T}" /> implementation to test.</param>
    /// <param name="loggerMock">The mock of the <see cref="ILogger" /> used to log messages.</param>
    /// <typeparam name="TLogger">The type of logger used by the implementation.</typeparam>
    [Theory]
    [Trait("Category", "Unit")]
    [ClassData(typeof(ListenerThreadInterfaceImplementationTestData))]
    public void EnqueueRequest_IsDisposed_ThrowsObjectDisposedException<TLogger>(
        IListenerThread<int> implementation,
        Mock<TLogger> loggerMock)
        where TLogger : class, ILogger
    {
        // Arrange
        loggerMock.Reset();
        implementation.Dispose();
        var state = implementation.State;

        // Act
        var exception = (ObjectDisposedException)Record.Exception(() => implementation.EnqueueRequest(1))!;

        // Assert
        Assert.NotNull(exception);
        Assert.Equal(implementation.GetType().FullName, exception.ObjectName);
        Assert.Equal(ListenerThreadStates.Disposed, state);
        Assert.IsType<ObjectDisposedException>(exception);
    }

    /// <summary>
    ///     Tests that <see cref="ListenerThread{T}.EnqueueRequest(T)" /> throws an <see cref="InvalidOperationException" />
    ///     when invoked while its state is neither <see cref="ListenerThreadStates.Ready" /> nor
    ///     <see cref="ListenerThreadStates.Running" />.
    /// </summary>
    /// <param name="implementation">The <see cref="IListenerThread{T}" /> implementation to test.</param>
    /// <param name="loggerMock">The mock of the <see cref="ILogger" /> used to log messages.</param>
    /// <typeparam name="TLogger">The type of logger used by the implementation.</typeparam>
    [Theory]
    [Trait("Category", "Unit")]
    [ClassData(typeof(ListenerThreadInterfaceImplementationTestData))]
    public void EnqueueRequest_NotReadyOrRunning_ThrowsInvalidOperationException<TLogger>(
        IListenerThread<int> implementation,
        Mock<TLogger> loggerMock)
        where TLogger : class, ILogger
    {
        // Arrange
        loggerMock.Reset();
        const string expectedExceptionMessage = "Cannot enqueue a request while the thread is not ready or running.";

        var action = new Action<int, CancellationToken>(
            [ExcludeFromCodeCoverage(Justification = "Test method.")]
            (i, ct) => { });

        // Act
        var stateInitialized = implementation.State;
        var exceptionInitialized = Record.Exception(() => implementation.EnqueueRequest(1));
        implementation.Start(action);
        implementation.TryStop();
        var stateStopped = implementation.State;
        var exceptionStopped = Record.Exception(() => implementation.EnqueueRequest(1));

        // Assert
        Assert.Equal(ListenerThreadStates.Initialized, stateInitialized);
        Assert.NotNull(exceptionInitialized);
        Assert.Equal(expectedExceptionMessage, exceptionInitialized.Message);
        Assert.IsType<InvalidOperationException>(exceptionInitialized);
        Assert.Equal(ListenerThreadStates.Stopped, stateStopped);
        Assert.NotNull(exceptionStopped);
        Assert.Equal(expectedExceptionMessage, exceptionStopped.Message);
        Assert.IsType<InvalidOperationException>(exceptionStopped);
    }

    /// <summary>
    ///     Tests that <see cref="ListenerThread{T}.EnqueueRequest(T)" /> adds a request to the queue.
    /// </summary>
    /// <param name="implementation">The <see cref="IListenerThread{T}" /> implementation to test.</param>
    /// <param name="loggerMock">The mock of the <see cref="ILogger" /> used to log messages.</param>
    /// <typeparam name="TLogger">The type of logger used by the implementation.</typeparam>
    [Theory]
    [Trait("Category", "Unit")]
    [ClassData(typeof(ListenerThreadInterfaceImplementationTestData))]
    public void EnqueueRequest_ExceptionThrown_LogsException<TLogger>(
        IListenerThread<int> implementation,
        Mock<TLogger> loggerMock)
        where TLogger : class, ILogger
    {
        // Arrange
        loggerMock.Reset();
        var logInvocationArguments = ArrangeHelper.SetupLoggerMockCallback(ref loggerMock);
        const string expectedCancellationLogMessage = "Error: Worker thread cancelled.";
        const string expectedErrorLogMessage = "An exception occurred while processing a request.";
        const string expectedExceptionMessage = "Testing exception logging.";

        var action = new Action<int, CancellationToken>(
            [ExcludeFromCodeCoverage(Justification = "Test method.")]
            (i, ct) => throw new InvalidOperationException(expectedExceptionMessage));

        // Act
        implementation.Start(action);
        implementation.EnqueueRequest(1);
        
        SpinWait.SpinUntil(
            () => implementation.State == ListenerThreadStates.Ready,
            GlobalTestParameters.DefaultThreadSpinWaitTimeoutMilliseconds);
        var stateReady = implementation.State;
        
        implementation.TryStop();
        SpinWait.SpinUntil(
            () => implementation.State == ListenerThreadStates.Stopped,
            GlobalTestParameters.DefaultThreadSpinWaitTimeoutMilliseconds);

        // Assert
        Assert.Equal(LogLevel.Error, logInvocationArguments[0].Item1);
        Assert.Equal(expectedErrorLogMessage, logInvocationArguments[0].Item2?.ToString());
        Assert.IsType<InvalidOperationException>(logInvocationArguments[0].Item3);
        Assert.Equal(expectedExceptionMessage, logInvocationArguments[0].Item3?.Message);
        Assert.Equal(LogLevel.Error, logInvocationArguments[1].Item1);
        Assert.Equal(expectedCancellationLogMessage, logInvocationArguments[1].Item2?.ToString());
        Assert.Equal(ListenerThreadStates.Ready, stateReady);
        loggerMock.Verify(
            m => m.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2));
    }

    /// <summary>
    ///     Tests that <see cref="ListenerThread{T}.Start(Action{T, CancellationToken})" /> throws an
    ///     <see cref="ObjectDisposedException" /> when invoked after <see cref="ListenerThread{T}.Dispose()" />.
    /// </summary>
    /// <param name="implementation">The <see cref="IListenerThread{T}" /> implementation to test.</param>
    /// <param name="loggerMock">The mock of the <see cref="ILogger" /> used to log messages.</param>
    /// <typeparam name="TLogger">The type of logger used by the implementation.</typeparam>
    [Theory]
    [Trait("Category", "Unit")]
    [ClassData(typeof(ListenerThreadInterfaceImplementationTestData))]
    public void Start_IsDisposed_ThrowsObjectDisposedException<TLogger>(
        IListenerThread<int> implementation,
        Mock<TLogger> loggerMock)
        where TLogger : class, ILogger
    {
        // Arrange
        loggerMock.Reset();
        implementation.Dispose();
        var state = implementation.State;
        var action = new Action<int, CancellationToken>(
            [ExcludeFromCodeCoverage(Justification = "Test method.")]
            (i, ct) => { });

        // Act
        var exception = (ObjectDisposedException)Record.Exception(() => implementation.Start(action))!;

        // Assert
        Assert.NotNull(exception);
        Assert.Equal(implementation.GetType().FullName, exception.ObjectName);
        Assert.Equal(ListenerThreadStates.Disposed, state);
        Assert.IsType<ObjectDisposedException>(exception);
    }

    /// <summary>
    ///     Tests that <see cref="ListenerThread{T}.Start(Action{T, CancellationToken})" /> starts the listener thread
    ///     when a valid action is set.
    /// </summary>
    /// <param name="implementation">The <see cref="IListenerThread{T}" /> implementation to test.</param>
    /// <param name="loggerMock">The mock of the <see cref="ILogger" /> used to log messages.</param>
    /// <typeparam name="TLogger">The type of logger used by the implementation.</typeparam>
    [Theory]
    [Trait("Category", "Unit")]
    [ClassData(typeof(ListenerThreadInterfaceImplementationTestData))]
    public void Start_ValidAction_StartsListenerThread<TLogger>(
        IListenerThread<int> implementation,
        Mock<TLogger> loggerMock)
        where TLogger : class, ILogger
    {
        // Arrange
        loggerMock.Reset();
        var action = new Action<int, CancellationToken>(
            [ExcludeFromCodeCoverage(Justification = "Test method.")]
            (i, ct) => { });

        // Act
        var exception = Record.Exception(() => implementation.Start(action));
        var state = implementation.State;

        // Assert
        Assert.Null(exception);
        Assert.Equal(ListenerThreadStates.Ready, state);
        implementation.TryStop();
    }

    /// <summary>
    ///     Tests that <see cref="ListenerThread{T}.Start(Action{T, CancellationToken})" /> does not execute the action
    ///     when called.
    /// </summary>
    /// <param name="implementation">The <see cref="IListenerThread{T}" /> implementation to test.</param>
    /// <param name="loggerMock">The mock of the <see cref="ILogger" /> used to log messages.</param>
    /// <typeparam name="TLogger">The type of logger used by the implementation.</typeparam>
    [Theory]
    [Trait("Category", "Unit")]
    [ClassData(typeof(ListenerThreadInterfaceImplementationTestData))]
    public void Start_ValidAction_DoesNotExecuteAction<TLogger>(
        IListenerThread<int> implementation,
        Mock<TLogger> loggerMock)
        where TLogger : class, ILogger
    {
        // Arrange
        loggerMock.Reset();
        var actionExecuted = false;

        // Act
        implementation.Start([ExcludeFromCodeCoverage(Justification = "Test method.")](_, _) =>
        {
            actionExecuted = true;
        });

        // Assert
        Assert.False(actionExecuted);
        implementation.TryStop();
    }

    /// <summary>
    ///     Tests that <see cref="ListenerThread{T}.Start(Action{T, CancellationToken})" /> throws an
    ///     <see cref="InvalidOperationException" /> when invoked repeatedly.
    /// </summary>
    /// <param name="implementation">The <see cref="IListenerThread{T}" /> implementation to test.</param>
    /// <param name="loggerMock">The mock of the <see cref="ILogger" /> used to log messages.</param>
    /// <typeparam name="TLogger">The type of logger used by the implementation.</typeparam>
    [Theory]
    [Trait("Category", "Unit")]
    [ClassData(typeof(ListenerThreadInterfaceImplementationTestData))]
    public void Start_InvokedRepeatedly_ThrowsInvalidOperationException<TLogger>(
        IListenerThread<int> implementation,
        Mock<TLogger> loggerMock)
        where TLogger : class, ILogger
    {
        // Arrange
        loggerMock.Reset();

        const string expectedExceptionMessage = "Cannot start the listener thread unless it is the initialized state.";

        var action = new Action<int, CancellationToken>(
            [ExcludeFromCodeCoverage(Justification = "Test method.")]
            (i, ct) => { });

        // Act
        implementation.Start(action);
        var exception = Record.Exception(() => implementation.Start(action));
        var state = implementation.State;

        // Assert
        Assert.NotNull(exception);
        Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal(expectedExceptionMessage, exception.Message);
        Assert.Equal(ListenerThreadStates.Ready, state);
    }

    /// <summary>
    ///     Tests that <see cref="ListenerThread{T}.Start(Action{T, CancellationToken})" /> throws an
    ///     <see cref="InvalidOperationException" /> when invoked after <see cref="ListenerThread{T}.TryStop" />.
    /// </summary>
    /// <param name="implementation">The <see cref="IListenerThread{T}" /> implementation to test.</param>
    /// <param name="loggerMock">The mock of the <see cref="ILogger" /> used to log messages.</param>
    /// <typeparam name="TLogger">The type of logger used by the implementation.</typeparam>
    [Theory]
    [Trait("Category", "Unit")]
    [ClassData(typeof(ListenerThreadInterfaceImplementationTestData))]
    public void Start_ValidActionAfterStopInvoked_ThrowsInvalidOperationException<TLogger>(
        IListenerThread<int> implementation,
        Mock<TLogger> loggerMock)
        where TLogger : class, ILogger
    {
        // Arrange
        loggerMock.Reset();

        var action = new Action<int, CancellationToken>(
            [ExcludeFromCodeCoverage(Justification = "Test method.")]
            (i, ct) => { });

        // Act
        implementation.Start(action);
        implementation.TryStop();
        var exception = Record.Exception(() => implementation.Start(action));

        // Assert
        Assert.NotNull(exception);
        Assert.IsType<InvalidOperationException>(exception);
    }

    /// <summary>
    ///     Tests that <see cref="ListenerThread{T}.TryReset()" /> when the listener thread is initialized returns false.
    /// </summary>
    /// <param name="implementation">The <see cref="IListenerThread{T}" /> implementation to test.</param>
    /// <param name="loggerMock">The mock of the <see cref="ILogger" /> used to log messages.</param>
    /// <typeparam name="TLogger">The type of logger used by the implementation.</typeparam>
    [Theory]
    [Trait("Category", "Unit")]
    [ClassData(typeof(ListenerThreadInterfaceImplementationTestData))]
    public void TryReset_InitializedInvoked_ReturnsFalse<TLogger>(
        IListenerThread<int> implementation,
        Mock<TLogger> loggerMock)
        where TLogger : class, ILogger
    {
        // Arrange
        loggerMock.Reset();

        // Act
        var result = implementation.TryReset();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    ///     Tests that <see cref="ListenerThread{T}.TryReset()" /> successfully resets the listener thread and prepares it
    ///     for reuse.
    /// </summary>
    /// <param name="implementation">The <see cref="IListenerThread{T}" /> implementation to test.</param>
    /// <param name="loggerMock">The mock of the <see cref="ILogger" /> used to log messages.</param>
    /// <typeparam name="TLogger">The type of logger used by the implementation.</typeparam>
    [Theory]
    [Trait("Category", "Unit")]
    [ClassData(typeof(ListenerThreadInterfaceImplementationTestData))]
    public void TryReset_Invoked_ResetsListenerThread<TLogger>(
        IListenerThread<int> implementation,
        Mock<TLogger> loggerMock)
        where TLogger : class, ILogger
    {
        // Arrange
        loggerMock.Reset();
        var action = new Action<int, CancellationToken>(
            [ExcludeFromCodeCoverage(Justification = "Test method.")]
            (i, ct) => { });
        implementation.Start(action);

        // Act
        var exceptionReset = Record.Exception(() => implementation.TryReset());
        var stateInitialized = implementation.State;
        var exceptionStart = Record.Exception(() => implementation.Start(action));
        var stateReady = implementation.State;
        var exceptionEnqueueRequest = Record.Exception(() => implementation.EnqueueRequest(1));

        // Assert
        Assert.Null(exceptionReset);
        Assert.Null(exceptionStart);
        Assert.Null(exceptionEnqueueRequest);
        Assert.Equal(ListenerThreadStates.Initialized, stateInitialized);
        Assert.Equal(ListenerThreadStates.Ready, stateReady);
        implementation.TryStop();
    }

    /// <summary>
    ///     Tests that <see cref="ListenerThread{T}.TryReset()" /> successfully resets the listener thread and prepares
    ///     it for reuse when invoked concurrently. Only the first successful invocation resets the listener thread
    ///     while the rest return false.
    /// </summary>
    /// <param name="implementation">The <see cref="IListenerThread{T}" /> implementation to test.</param>
    /// <param name="loggerMock">The mock of the <see cref="ILogger" /> used to log messages.</param>
    /// <typeparam name="TLogger">The type of logger used by the implementation.</typeparam>
    [Theory]
    [Trait("Category", "Unit")]
    [ClassData(typeof(ListenerThreadInterfaceImplementationTestData))]
    public void TryReset_InvokedConcurrently_ResetsListenerThread<TLogger>(
        IListenerThread<int> implementation,
        Mock<TLogger> loggerMock)
        where TLogger : class, ILogger
    {
        // Arrange
        loggerMock.Reset();
        var action = new Action<int, CancellationToken>(
            [ExcludeFromCodeCoverage(Justification = "Test method.")]
            (i, ct) => { });
        implementation.Start(action);
        const uint expectedIterations = 100;

        // Act
        var readyState = implementation.State;
        var results = new ConcurrentBag<bool>();
        Parallel.For(0, 100, _ => results.Add(implementation.TryReset()));
        var initializedState = implementation.State;

        // Assert
        Assert.Equal(ListenerThreadStates.Initialized, initializedState);
        Assert.Equal(ListenerThreadStates.Ready, readyState);
        Assert.Equal(expectedIterations - 1, (uint)results.Count(r => !r));
        Assert.Single(results, true);
    }

    /// <summary>
    ///     Tests that <see cref="ListenerThread{T}.TryReset()" /> throws an <see cref="ObjectDisposedException" /> when
    ///     invoked after <see cref="ListenerThread{T}.Dispose()" />.
    /// </summary>
    /// <param name="implementation">The <see cref="IListenerThread{T}" /> implementation to test.</param>
    /// <param name="loggerMock">The mock of the <see cref="ILogger" /> used to log messages.</param>
    /// <typeparam name="TLogger">The type of logger used by the implementation.</typeparam>
    [Theory]
    [Trait("Category", "Unit")]
    [ClassData(typeof(ListenerThreadInterfaceImplementationTestData))]
    public void TryReset_IsDisposed_ThrowsObjectDisposedException<TLogger>(
        IListenerThread<int> implementation,
        Mock<TLogger> loggerMock)
        where TLogger : class, ILogger
    {
        // Arrange
        loggerMock.Reset();
        implementation.Dispose();
        var state = implementation.State;

        // Act
        var exception = (ObjectDisposedException)Record.Exception(() => implementation.TryReset())!;

        // Assert
        Assert.NotNull(exception);
        Assert.Equal(implementation.GetType().FullName, exception.ObjectName);
        Assert.Equal(ListenerThreadStates.Disposed, state);
        Assert.IsType<ObjectDisposedException>(exception);
    }

    /// <summary>
    ///     Tests that <see cref="ListenerThread{T}.TryStop()" /> stops the listener thread.
    /// </summary>
    /// <param name="implementation">The <see cref="IListenerThread{T}" /> implementation to test.</param>
    /// <param name="loggerMock">The mock of the <see cref="ILogger" /> used to log messages.</param>
    /// <typeparam name="TLogger">The type of logger used by the implementation.</typeparam>
    [Theory]
    [Trait("Category", "Unit")]
    [ClassData(typeof(ListenerThreadInterfaceImplementationTestData))]
    public void TryStop_Invoked_StopsListenerThread<TLogger>(
        IListenerThread<int> implementation,
        Mock<TLogger> loggerMock)
        where TLogger : class, ILogger
    {
        // Arrange
        loggerMock.Reset();
        var logInvocationArguments = ArrangeHelper.SetupLoggerMockCallback(ref loggerMock);

        const string expectedCancellationLogMessage = "Error: Worker thread cancelled.";

        var action = new Action<int, CancellationToken>(
            [ExcludeFromCodeCoverage(Justification = "Test method.")]
            (i, ct) =>
            {
                uint spinCount = 0;

                /*
                 * Simulate a long running thread; however, make sure it eventually stops so that the test can complete.
                 * Without some way to terminate the loop, the test will waste compute resources until it times out or
                 * crashes. If the code being tested is incorrect, the OperationCancelledException will never be thrown
                 * meaning that we need the test to end the loop after a certain number of iterations.
                 */
                while (spinCount < MaxIterationSpinWait * 10)
                {
                    ct.ThrowIfCancellationRequested();
                    Thread.SpinWait(GlobalTestParameters.DefaultThreadSpinWait);
                    spinCount++;
                }
            });
        implementation.Start(action);
        implementation.EnqueueRequest(1);
        SpinWait.SpinUntil(
            () => implementation.State == ListenerThreadStates.Running,
            GlobalTestParameters.DefaultThreadSpinWaitTimeoutMilliseconds);

        // Act
        implementation.TryStop();
        var state = implementation.State;

        // Assert
        Assert.Equal(LogLevel.Error, logInvocationArguments[0].Item1);
        Assert.Equal(expectedCancellationLogMessage, logInvocationArguments[0].Item2?.ToString());
        Assert.Throws<InvalidOperationException>(() => implementation.Start(action));
        Assert.Equal(ListenerThreadStates.Stopped, state);
        loggerMock.Verify(
            m => m.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    ///     Tests that <see cref="ListenerThread{T}.TryStop()" /> successfully stops the listener thread when invoked
    ///     concurrently. Only the first successful invocation stops the listener thread while the rest return false.
    /// </summary>
    /// <param name="implementation">The <see cref="IListenerThread{T}" /> implementation to test.</param>
    /// <param name="loggerMock">The mock of the <see cref="ILogger" /> used to log messages.</param>
    /// <typeparam name="TLogger">The type of logger used by the implementation.</typeparam>
    [Theory]
    [Trait("Category", "Unit")]
    [ClassData(typeof(ListenerThreadInterfaceImplementationTestData))]
    public void TryStop_InvokedConcurrently_StopsListenerThread<TLogger>(
        IListenerThread<int> implementation,
        Mock<TLogger> loggerMock)
        where TLogger : class, ILogger
    {
        // Arrange
        loggerMock.Reset();
        var action = new Action<int, CancellationToken>(
            [ExcludeFromCodeCoverage(Justification = "Test method.")]
            (i, ct) => { });
        implementation.Start(action);
        const uint expectedIterations = 100;

        // Act
        var readyState = implementation.State;
        var results = new ConcurrentBag<bool>();
        Parallel.For(0, expectedIterations, _ => results.Add(implementation.TryStop()));
        var stoppedState = implementation.State;

        // Assert
        Assert.Equal(ListenerThreadStates.Ready, readyState);
        Assert.Equal(ListenerThreadStates.Stopped, stoppedState);
        Assert.Equal(expectedIterations - 1, (uint)results.Count(r => !r));
        Assert.Single(results, true);
    }

    /// <summary>
    ///     Tests that <see cref="ListenerThread{T}.TryStop()" /> when called repeatedly does not throw any exceptions.
    /// </summary>
    /// <param name="implementation">The <see cref="IListenerThread{T}" /> implementation to test.</param>
    /// <param name="loggerMock">The mock of the <see cref="ILogger" /> used to log messages.</param>
    /// <typeparam name="TLogger">The type of logger used by the implementation.</typeparam>
    [Theory]
    [Trait("Category", "Unit")]
    [ClassData(typeof(ListenerThreadInterfaceImplementationTestData))]
    public void TryStop_InvokedRepeatedly_DoesNotThrow<TLogger>(
        IListenerThread<int> implementation,
        Mock<TLogger> loggerMock)
        where TLogger : class, ILogger
    {
        // Arrange
        loggerMock.Reset();
        var action = new Action<int, CancellationToken>(
            [ExcludeFromCodeCoverage(Justification = "Test method.")]
            (i, ct) => { });
        implementation.Start(action);

        // Act
        implementation.TryStop();
        var exception1 = Record.Exception(() => implementation.TryStop());
        var exception2 = Record.Exception(() => implementation.TryStop());
        var state = implementation.State;

        // Assert
        Assert.Null(exception1);
        Assert.Null(exception2);
        Assert.Throws<InvalidOperationException>(() => implementation.Start(action));
        Assert.Equal(ListenerThreadStates.Stopped, state);
    }

    /// <summary>
    ///     Tests that <see cref="ListenerThread{T}.TryStop()" /> throws an <see cref="ObjectDisposedException" />
    ///     when invoked after <see cref="ListenerThread{T}.Dispose()" />.
    /// </summary>
    /// <param name="implementation">The <see cref="IListenerThread{T}" /> implementation to test.</param>
    /// <param name="loggerMock">The mock of the <see cref="ILogger" /> used to log messages.</param>
    /// <typeparam name="TLogger">The type of logger used by the implementation.</typeparam>
    [Theory]
    [Trait("Category", "Unit")]
    [ClassData(typeof(ListenerThreadInterfaceImplementationTestData))]
    public void TryStop_IsDisposed_ThrowsObjectDisposedException<TLogger>(
        IListenerThread<int> implementation,
        Mock<TLogger> loggerMock)
        where TLogger : class, ILogger
    {
        // Arrange
        loggerMock.Reset();
        implementation.Dispose();
        var state = implementation.State;

        // Act
        var exception = (ObjectDisposedException)Record.Exception(() => implementation.TryStop())!;

        // Assert
        Assert.NotNull(exception);
        Assert.Equal(implementation.GetType().FullName, exception.ObjectName);
        Assert.Equal(ListenerThreadStates.Disposed, state);
        Assert.IsType<ObjectDisposedException>(exception);
    }

    /// <summary>
    ///     Tests that <see cref="ListenerThread{T}.TryStop" /> will forcibly stop the listener thread if it has hung.
    /// </summary>
    /// <param name="implementation">The <see cref="IListenerThread{T}" /> implementation to test.</param>
    /// <param name="loggerMock">The mock of the <see cref="ILogger" /> used to log messages.</param>
    /// <typeparam name="TLogger">The type of logger used by the implementation.</typeparam>
    [Theory]
    [Trait("Category", "Unit")]
    [ClassData(typeof(ListenerThreadInterfaceImplementationTestData))]
    public void TryStop_WorkerThreadHung_StopsWorkerThread<TLogger>(
        IListenerThread<int> implementation,
        Mock<TLogger> loggerMock)
        where TLogger : class, ILogger
    {
        // Arrange
        loggerMock.Reset();
        var logInvocationArguments = ArrangeHelper.SetupLoggerMockCallback(ref loggerMock);

        const string expectedWarningLogMessage1 =
            "Worker thread did not stop in a timely manner. Interrupting the thread...";
        const string expectedWarningLogMessage2 = "Worker thread interrupted.";
        const string expectedErrorLogMessage = "Thread interrupted while processing a request.";

        var actionInvoked = false;
        var action = new Action<int, CancellationToken>(
            [ExcludeFromCodeCoverage(Justification = "Test method.")]
            (i, ct) =>
            {
                actionInvoked = true;
                /* Simulate a hung thread:
                 * The parameter i will never equal 5, which is why the timeout is vital to avoid an infinite loop.
                 */
                SpinWait.SpinUntil(
                    () => i.Equals(5),
                    GlobalTestParameters.DefaultThreadSpinWaitTimeoutMilliseconds * 10);
            });

        // Act
        implementation.Start(action);
        implementation.EnqueueRequest(1);
        implementation.EnqueueRequest(2);
        implementation.EnqueueRequest(3);
        implementation.EnqueueRequest(4);
        SpinWait.SpinUntil(
            () => actionInvoked,
            GlobalTestParameters.DefaultThreadSpinWaitTimeoutMilliseconds);
        
        var result = implementation.TryStop();
        var stateStopped = implementation.State;

        // Assert
        Assert.Equal(3, logInvocationArguments.Count);
        Assert.Equal(LogLevel.Warning, logInvocationArguments[0].Item1);
        Assert.Equal(expectedWarningLogMessage1, logInvocationArguments[0].Item2?.ToString());
        Assert.Equal(LogLevel.Error, logInvocationArguments[1].Item1);
        Assert.Equal(expectedErrorLogMessage, logInvocationArguments[1].Item2?.ToString());
        Assert.IsType<ThreadInterruptedException>(logInvocationArguments[1].Item3);
        Assert.Equal(LogLevel.Error, logInvocationArguments[2].Item1);
        Assert.Equal(expectedWarningLogMessage2, logInvocationArguments[2].Item2?.ToString());
        Assert.Equal(ListenerThreadStates.Stopped, stateStopped);
        Assert.True(result);
        loggerMock.Verify(
            m => m.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(3));
    }
}