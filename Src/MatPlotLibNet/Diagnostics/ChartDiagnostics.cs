// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Diagnostics;

/// <summary>A single diagnostic signal raised when a component degrades gracefully instead of
/// throwing or silently dropping data — e.g. a regression fit that could not be computed, or an
/// unknown series discriminator encountered while deserializing a <c>Figure</c>.</summary>
/// <param name="Source">The emitting component (e.g. <c>"RegressionSeriesRenderer"</c>,
/// <c>"ChartSerializer"</c>).</param>
/// <param name="Message">A human-readable description of what was skipped and why.</param>
/// <param name="Exception">The exception that triggered the degraded path, or
/// <see langword="null"/> when the trigger was not an exception (e.g. an unknown
/// discriminator).</param>
public readonly record struct ChartDiagnostic(string Source, string Message, Exception? Exception);

/// <summary>Process-wide diagnostic event bus for otherwise-silent degraded-mode paths in the
/// library (a swallowed regression-fit failure, a dropped unknown series discriminator on
/// deserialize, ...). A domain-named static seam — like <c>Numerics.LeastSquares</c> — rather than
/// a catch-all <c>*Helper</c>/<c>*Util</c>: this is the one diagnostics channel for the library.
/// </summary>
/// <remarks>Emitting never changes default behavior: callers still degrade gracefully (skip the
/// failing series, skip the unknown discriminator) whether or not anyone is subscribed. Subscribe
/// with <c>+=</c> to observe otherwise-invisible failure modes, and unsubscribe with <c>-=</c> when
/// done (e.g. in a test's <c>finally</c>) — this is a static, process-wide event, so a forgotten
/// subscription outlives the code that registered it. The compiler-generated field-like event
/// accessors are thread-safe for concurrent add/remove, and <see cref="Emit"/> reads the delegate
/// through the null-conditional operator, which captures a single snapshot before invoking, so a
/// concurrent unsubscribe cannot race a null check against invocation.</remarks>
public static class ChartDiagnostics
{
    /// <summary>Raised whenever a component reports a diagnostic via <see cref="Emit"/>. There is
    /// no subscriber by default — production code never depends on this event being observed.
    /// </summary>
    public static event Action<ChartDiagnostic>? Emitted;

    /// <summary>Raises <see cref="Emitted"/> with the given diagnostic. No-ops when there are no
    /// subscribers.</summary>
    public static void Emit(ChartDiagnostic diagnostic) => Emitted?.Invoke(diagnostic);
}
