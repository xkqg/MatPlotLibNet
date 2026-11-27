// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace MatPlotLibNet.Benchmarks.Drag;

/// <summary>What one drag run cost per frame.</summary>
/// <param name="Name">What was drawn.</param>
/// <param name="Frames">How many frames were measured: the frames asked for, or more when that was too little processor time for
/// the clock's step to be small beside it.</param>
/// <param name="WallMs">Wall-clock milliseconds per frame.</param>
/// <param name="MedianMs">The median frame, in milliseconds.</param>
/// <param name="P95Ms">The 95th-percentile frame, in milliseconds.</param>
/// <param name="CpuMs">Processor milliseconds per frame, the whole process.</param>
/// <param name="CpuError">How far <paramref name="CpuMs"/> can be off, as a share of itself: one step of the processor clock over the
/// time read.</param>
/// <param name="AllocKiB">Kibibytes allocated per frame.</param>
public readonly record struct DragResult(string Name, int Frames, double WallMs, double MedianMs, double P95Ms, double CpuMs, double CpuError, double AllocKiB)
{
    /// <inheritdoc />
    public override string ToString() => string.Create(CultureInfo.InvariantCulture,
        $"{Name,-26} {Frames} frames  wall {WallMs,7:F2} ms/frame  p50 {MedianMs,7:F2}  p95 {P95Ms,7:F2}  cpu {CpuMs,7:F2} ms/frame (+/-{CpuError * 100,4:F1} %)  alloc {AllocKiB,8:F0} KiB/frame");
}
