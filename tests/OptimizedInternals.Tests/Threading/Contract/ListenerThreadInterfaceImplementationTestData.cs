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
[ExcludeFromCodeCoverage(Justification = "Test data.")]
public class ListenerThreadInterfaceImplementationTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        var loggerMockListenerThread = new Mock<ILogger<ListenerThread<int>>>();
        yield return new object[]
        {
            new ListenerThread<int>(loggerMockListenerThread.Object),
            loggerMockListenerThread
        };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}