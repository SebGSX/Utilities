// Copyright © 2023 Seb Garrioch. All rights reserved.
// Published under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace Utilities.OptimizedInternals.Helpers;

/// <summary>
///     Helper methods for throwing exceptions.
/// </summary>
[Pure]
public static class ThrowHelper
{
    /// <summary>
    ///     Throws an <see cref="ArgumentException" /> with the specified message for the specified parameter.
    /// </summary>
    /// <param name="condition">The condition to check to determine whether the exception should be thrown.</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="paramName">The name of the parameter that caused the current exception.</param>
    /// <exception cref="ArgumentException">Error with a parameter.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIf<TException>(
        [DoesNotReturnIf(true)] bool condition,
        string message,
        string paramName)
        where TException : ArgumentException
    {
        if (condition) throw (TException)Activator.CreateInstance(typeof(TException), message, paramName)!;
    }

    /// <summary>
    ///     Throws an <see cref="InvalidOperationException" /> with the specified message.
    /// </summary>
    /// <param name="condition">The condition to check to determine whether the exception should be thrown.</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <exception cref="InvalidOperationException">Invalid operation.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIf<TException>([DoesNotReturnIf(true)] bool condition, string message)
        where TException : InvalidOperationException
    {
        if (condition) throw (TException)Activator.CreateInstance(typeof(TException), message)!;
    }
}