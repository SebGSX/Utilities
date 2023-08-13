// Copyright © 2023 Seb Garrioch. All rights reserved.
// Published under the MIT License.

using Utilities.OptimizedInternals.Threading;

namespace Utilities.OptimizedInternals.Tests.Threading;

/// <summary>
///     Tests the <see cref="ListenerThreadStates" /> class.
/// </summary>
public class ListenerThreadStatesTests
{
    /// <summary>
    ///     Tests that the <see cref="ListenerThreadStates" /> members return the expected values.
    /// </summary>
    [Fact(Timeout = GlobalTestParameters.DefaultTestTimeout)]
    [Trait("Category", "Unit")]
    public void Members_ReturnExpectedValues()
    {
        // Arrange
        const int expectedDisposed = -2;
        const int expectedDisposing = -1;
        const int expectedInitialized = 0;
        const int expectedReady = 1;
        const int expectedRunning = 2;
        const int expectedStopping = 3;
        const int expectedStopped = 4;
        const int expectedResetting = 5;

        // Act
        const int actualDisposed = (int)ListenerThreadStates.Disposed;
        const int actualDisposing = (int)ListenerThreadStates.Disposing;
        const int actualInitialized = (int)ListenerThreadStates.Initialized;
        const int actualReady = (int)ListenerThreadStates.Ready;
        const int actualRunning = (int)ListenerThreadStates.Running;
        const int actualStopping = (int)ListenerThreadStates.Stopping;
        const int actualStopped = (int)ListenerThreadStates.Stopped;
        const int actualResetting = (int)ListenerThreadStates.Resetting;

        // Assert
        Assert.Equal(expectedDisposed, actualDisposed);
        Assert.Equal(expectedDisposing, actualDisposing);
        Assert.Equal(expectedInitialized, actualInitialized);
        Assert.Equal(expectedReady, actualReady);
        Assert.Equal(expectedRunning, actualRunning);
        Assert.Equal(expectedStopping, actualStopping);
        Assert.Equal(expectedStopped, actualStopped);
        Assert.Equal(expectedResetting, actualResetting);
    }
}