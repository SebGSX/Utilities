// Copyright © 2023 Seb Garrioch. All rights reserved.
// Published under the MIT License.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Utilities.OptimizedInternals.Threading;

namespace Utilities.OptimizedInternals.Tests.Threading.Contract;

/// <summary>
///     Test data for <see cref="IListenerThread{T}" /> interface implementation tests.
/// </summary>
/// <remarks>
///     Instantiates <see cref="IListenerThread{T}" /> implementations with a mocked logger. All implementations must
///     preserve the expected behavior of the interface (Liskov Substitution Principle), which is tested in the
///     <see cref="ListenerThreadInterfaceImplementationTests" /> class.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Test data.")]
public class ListenerThreadInterfaceImplementationTestData : IEnumerable<object[]>
{
    /// <summary>
    ///     Returns an enumerator that iterates through the collection of test data.
    /// </summary>
    /// <returns>Returns an enumerator that iterates through the collection of test data.</returns>
    public IEnumerator<object[]> GetEnumerator()
    {
        var loggerMockListenerThread = new Mock<ILogger<ListenerThread<int>>>();
        yield return new object[]
        {
            new ListenerThread<int>(loggerMockListenerThread.Object),
            loggerMockListenerThread
        };
    }

    /// <summary>
    ///     Returns an enumerator that iterates through the collection of test data.
    /// </summary>
    /// <returns>Returns an enumerator that iterates through the collection of test data.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}