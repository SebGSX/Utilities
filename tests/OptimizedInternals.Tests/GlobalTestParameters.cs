// Copyright © 2023 Seb Garrioch. All rights reserved.
// Published under the MIT License.

using System.Diagnostics.Contracts;

namespace Utilities.OptimizedInternals.Tests;

/// <summary>
///     Provides global parameters for the test project.
/// </summary>
[Pure]
public static class GlobalTestParameters
{
    /// <summary>
    ///     The default loop wait time in milliseconds.
    /// </summary>
    public const ushort DefaultLoopWaitTime = 100;

    /// <summary>
    ///     The default thread sleep time in milliseconds.
    /// </summary>
    public const ushort DefaultThreadSleepTime = 500;

    /// <summary>
    ///     The default thread spin wait cycles.
    /// </summary>
    public const ushort DefaultThreadSpinWait = 1000;
    
    /// <summary>
    ///     The default thread spin wait timeout in milliseconds.
    /// </summary>
    public const ushort DefaultThreadSpinWaitTimeoutMilliseconds = 1000;
}