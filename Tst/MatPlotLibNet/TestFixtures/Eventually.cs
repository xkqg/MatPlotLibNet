// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests;

/// <summary>The wait a test uses for something another thread does: a publish, an event reaching a handler, a message arriving. It
/// is the only deadline loop of the tests, and it fails when the condition never holds, so a test does not go on to assert on a state
/// it never reached.</summary>
internal static class Eventually
{
    /// <summary>How long a wait lasts when the test does not say.</summary>
    private static readonly TimeSpan DefaultWithin = TimeSpan.FromSeconds(5);

    /// <summary>Returns once <paramref name="condition"/> holds, looking again every few milliseconds.</summary>
    /// <param name="condition">What is waited for; it may be read from any thread, so it reads shared state safely.</param>
    /// <param name="within">The longest the wait lasts; left out, five seconds.</param>
    /// <returns>A task that ends when the condition holds.</returns>
    /// <exception cref="TimeoutException">The condition did not hold within <paramref name="within"/>.</exception>
    public static async Task HoldsAsync(Func<bool> condition, TimeSpan? within = null)
    {
        var limit = within ?? DefaultWithin;
        var deadline = DateTime.UtcNow + limit;
        while (!condition())
        {
            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException($"The condition did not hold within {limit.TotalMilliseconds:0} ms.");
            }

            await Task.Delay(10);
        }
    }
}
