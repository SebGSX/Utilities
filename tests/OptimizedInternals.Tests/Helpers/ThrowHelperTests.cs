// Copyright © 2023 Seb Garrioch. All rights reserved.
// Published under the MIT License.

using Utilities.OptimizedInternals.Helpers;

namespace Utilities.OptimizedInternals.Tests.Helpers;

/// <summary>
///     Tests the <see cref="ThrowHelper" /> class.
/// </summary>
public class ThrowHelperTests
{
    /// <summary>
    ///     Tests that <see cref="ThrowHelper.ThrowIf{TException}(bool, string, string)" /> does not throw.
    /// </summary>
    [Fact(Timeout = GlobalTestParameters.DefaultTestTimeout)]
    [Trait("Category", "Unit")]
    public void ThrowIfArgumentException_ConditionFalse_DoesNotThrow()
    {
        // Arrange
        const string paramName = "paramName";
        const string message = "message";

        // Act
        var exception = Record.Exception(() =>
            ThrowHelper.ThrowIf<ArgumentException>(false, message, paramName));

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    ///     Tests that <see cref="ThrowHelper.ThrowIf{TException}(bool, string, string)" /> throws the correct exception.
    /// </summary>
    [Fact(Timeout = GlobalTestParameters.DefaultTestTimeout)]
    [Trait("Category", "Unit")]
    public void ThrowIfArgumentException_ConditionTrue_ThrowsArgumentException()
    {
        // Arrange
        const string paramName = "paramName";
        const string message = "message";
        const string expectedMessage = $"{message} (Parameter '{paramName}')";

        // Act
        var exception = Record.Exception(() =>
            ThrowHelper.ThrowIf<ArgumentException>(true, message, paramName));

        // Assert
        Assert.NotNull(exception);
        Assert.IsType<ArgumentException>(exception);
        Assert.Equal(expectedMessage, exception.Message);
        Assert.Equal(paramName, ((ArgumentException)exception).ParamName);
    }

    /// <summary>
    ///     Tests that <see cref="ThrowHelper.ThrowIf{TException}(bool, string)" /> does not throw.
    /// </summary>
    [Fact(Timeout = GlobalTestParameters.DefaultTestTimeout)]
    [Trait("Category", "Unit")]
    public void ThrowIfInvalidOperationException_ConditionFalse_DoesNotThrow()
    {
        // Arrange
        const string message = "message";

        // Act
        var exception = Record.Exception(() =>
            ThrowHelper.ThrowIf<InvalidOperationException>(false, message));

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    ///     Tests that <see cref="ThrowHelper.ThrowIf{TException}(bool, string)" /> throws the correct exception.
    /// </summary>
    [Fact(Timeout = GlobalTestParameters.DefaultTestTimeout)]
    [Trait("Category", "Unit")]
    public void ThrowIfInvalidOperationException_ConditionTrue_ThrowsInvalidOperationException()
    {
        // Arrange
        const string message = "message";

        // Act
        var exception = Record.Exception(() =>
            ThrowHelper.ThrowIf<InvalidOperationException>(true, message));

        // Assert
        Assert.NotNull(exception);
        Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal(message, exception.Message);
    }
}