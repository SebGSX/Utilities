// Copyright © 2023 Seb Garrioch. All rights reserved.
// Published under the MIT License.

using Microsoft.Extensions.Logging;
using Utilities.OptimizedInternals.Threading;

namespace Utilities.OptimizedInternals.Tests.Threading;

/// <summary>
///     Tests the <see cref="ListenerThread{T}" /> class.
/// </summary>
public class ListenerThreadTests
{
    /// <summary>
    ///     Tests that the constructor initializes a new instance of the <see cref="ListenerThread{T}" /> class
    ///     when the parameters are valid.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_ValidParameters_InitializesNewInstance()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ListenerThread<int>>>();

        // Act
        var listenerThread = new ListenerThread<int>(loggerMock.Object);
        var state = listenerThread.State;

        // Assert
        Assert.NotNull(listenerThread);
        Assert.Equal(ListenerThreadStates.Initialized, state);
    }
}