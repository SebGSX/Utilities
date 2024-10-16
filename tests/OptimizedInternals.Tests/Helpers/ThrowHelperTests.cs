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
    [Fact]
    [Trait("Category", "Unit")]
    public void ThrowIfArgumentException_ConditionFalse_DoesNotThrow()
    {
        // Arrange
        const string expectedParamName = "paramName";
        const string expectedMessage = "message";

        // Act
        var exception = Record.Exception(() =>
            ThrowHelper.ThrowIf<ArgumentException>(false, expectedMessage, expectedParamName));

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    ///     Tests that <see cref="ThrowHelper.ThrowIf{TException}(bool, string, string)" /> throws the correct exception.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void ThrowIfArgumentException_ConditionTrue_ThrowsArgumentException()
    {
        // Arrange
        const string expectedParameterName = "paramName";
        const string expectedMessageText = "messageText";
        const string expectedMessage = $"{expectedMessageText} (Parameter '{expectedParameterName}')";

        // Act
        var exception = Record.Exception(() =>
            ThrowHelper.ThrowIf<ArgumentException>(true, expectedMessageText, expectedParameterName));

        // Assert
        Assert.NotNull(exception);
        Assert.IsType<ArgumentException>(exception);
        Assert.Equal(expectedMessage, exception.Message);
        Assert.Equal(expectedParameterName, ((ArgumentException)exception).ParamName);
    }

    /// <summary>
    ///     Tests that <see cref="ThrowHelper.ThrowIf{TException}(bool, string)" /> does not throw.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void ThrowIfInvalidOperationException_ConditionFalse_DoesNotThrow()
    {
        // Arrange
        const string expectedMessage = "message";

        // Act
        var exception = Record.Exception(() =>
            ThrowHelper.ThrowIf<InvalidOperationException>(false, expectedMessage));

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    ///     Tests that <see cref="ThrowHelper.ThrowIf{TException}(bool, string)" /> throws the correct exception.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void ThrowIfInvalidOperationException_ConditionTrue_ThrowsInvalidOperationException()
    {
        // Arrange
        const string expectedMessage = "message";

        // Act
        var exception = Record.Exception(() =>
            ThrowHelper.ThrowIf<InvalidOperationException>(true, expectedMessage));

        // Assert
        Assert.NotNull(exception);
        Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal(expectedMessage, exception.Message);
    }
}