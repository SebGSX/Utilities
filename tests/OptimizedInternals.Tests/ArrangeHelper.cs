// Copyright © 2023 Seb Garrioch. All rights reserved.
// Published under the MIT License.

using Microsoft.Extensions.Logging;

namespace Utilities.OptimizedInternals.Tests;

/// <summary>
///     Helps to arrange test data.
/// </summary>
public static class ArrangeHelper
{
    /// <summary>
    ///     Sets up the logger mock callback.
    /// </summary>
    /// <param name="loggerMock">The <see cref="Mock{TLogger}" /> to set up.</param>
    /// <typeparam name="TLogger">The type of logger being mocked.</typeparam>
    /// <returns>A list of tuples that will contain all sets of arguments passed to the logger mock.</returns>
    public static List<Tuple<LogLevel, object?, Exception?>> SetupLoggerMockCallback<TLogger>(
        ref Mock<TLogger> loggerMock)
        where TLogger : class, ILogger
    {
        var logInvocationArguments = new List<Tuple<LogLevel, object?, Exception?>>();
        loggerMock.Setup(
                m => m.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(new InvocationAction(invocation =>
            {
                logInvocationArguments.Add(
                    new Tuple<LogLevel, object?, Exception?>(
                        (LogLevel)invocation.Arguments[0],
                        (object?)invocation.Arguments[2],
                        (Exception?)invocation.Arguments[3]));
            }));

        return logInvocationArguments;
    }
}