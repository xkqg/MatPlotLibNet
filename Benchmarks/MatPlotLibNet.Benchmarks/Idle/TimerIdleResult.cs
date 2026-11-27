// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace MatPlotLibNet.Benchmarks.Idle;

/// <summary>What a set of idle streaming figures cost the process while no data changed.</summary>
/// <param name="Figures">How many streaming figures (so timers) the process held.</param>
/// <param name="Seconds">How long the process was measured, in wall-clock seconds.</param>
/// <param name="CpuMs">Processor milliseconds the process used over the whole span.</param>
/// <param name="MegaCycles">Millions of processor cycles the process used over the whole span, which resolves what the processor
/// time's clock tick cannot; zero where the platform has no such counter.</param>
/// <param name="WorkItems">Work items the thread pool completed over the span: every timer callback is one.</param>
public readonly record struct TimerIdleResult(int Figures, double Seconds, double CpuMs, double MegaCycles, long WorkItems)
{
    /// <inheritdoc />
    public override string ToString() => string.Create(CultureInfo.InvariantCulture,
        $"{Figures,3} figures over {Seconds:F1} s: cpu {CpuMs,8:F1} ms  cycles {MegaCycles,9:F2} M  pool work items {WorkItems,6} ({WorkItems / Seconds,6:F1} /s)");
}
