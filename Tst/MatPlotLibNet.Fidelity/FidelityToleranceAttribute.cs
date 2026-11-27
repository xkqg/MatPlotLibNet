// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Fidelity;

/// <summary>The three limits a fidelity comparison is held to: the largest mean per-channel error, the smallest structural
/// similarity and the largest distance between the dominant colours.</summary>
/// <param name="Rms">The largest allowed RMS, on the 0 to 255 scale.</param>
/// <param name="Ssim">The smallest allowed SSIM, on the 0 to 1 scale.</param>
/// <param name="DeltaE">The largest allowed ΔE between dominant colours.</param>
public readonly record struct FidelityThresholds(double Rms, double Ssim, double DeltaE)
{
    /// <summary>The limits of a test that names none.</summary>
    public static FidelityThresholds Defaults { get; } =
        new(PerceptualDiff.DefaultRmsThreshold, PerceptualDiff.DefaultSsimThreshold, PerceptualDiff.DefaultDeltaEThreshold);
}

/// <summary>
/// Overrides the default perceptual-diff thresholds for a single fidelity test.
/// Use this when a series has irreducible AA or font-rasterization noise that
/// pushes it above the suite-wide defaults. The base limits hold the classic row; the v2 row of the same test is held to the same limits
/// unless the test names its own (<see cref="V2Rms"/>, <see cref="V2Ssim"/>, <see cref="V2DeltaE"/>), which it does wherever the
/// two styles measure differently. A limit is the worst score its row measures on Windows and in the ubuntu container plus a margin
/// (1 in RMS, 2 in the colour distance, 0.01 in SSIM), rounded to the whole number (two places for SSIM) on the side that holds
/// the row; it is made tighter when the row's picture moves closer to the reference and is never widened to a score that dropped.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class FidelityToleranceAttribute : Attribute
{
    /// <summary>Maximum allowed mean per-channel RMS (0–255 scale). Default: 8.</summary>
    public double Rms { get; init; } = PerceptualDiff.DefaultRmsThreshold;

    /// <summary>Minimum required SSIM (0–1 scale). Default: 0.92.</summary>
    public double Ssim { get; init; } = PerceptualDiff.DefaultSsimThreshold;

    /// <summary>Maximum allowed dominant-colour CIE ΔE. Default: 10.</summary>
    public double DeltaE { get; init; } = PerceptualDiff.DefaultDeltaEThreshold;

    /// <summary>The RMS limit of the v2 row; <see cref="double.NaN"/> (the default) holds it to <see cref="Rms"/>.</summary>
    public double V2Rms { get; init; } = double.NaN;

    /// <summary>The SSIM limit of the v2 row; <see cref="double.NaN"/> (the default) holds it to <see cref="Ssim"/>.</summary>
    public double V2Ssim { get; init; } = double.NaN;

    /// <summary>The ΔE limit of the v2 row; <see cref="double.NaN"/> (the default) holds it to <see cref="DeltaE"/>.</summary>
    public double V2DeltaE { get; init; } = double.NaN;

    /// <summary>The limits the row of <paramref name="subdir"/> ("classic" or "v2") is held to.</summary>
    /// <param name="subdir">The style's fixture folder.</param>
    /// <returns>The limits.</returns>
    public FidelityThresholds For(string subdir) => subdir != "v2"
        ? new FidelityThresholds(Rms, Ssim, DeltaE)
        : new FidelityThresholds(double.IsNaN(V2Rms) ? Rms : V2Rms, double.IsNaN(V2Ssim) ? Ssim : V2Ssim, double.IsNaN(V2DeltaE) ? DeltaE : V2DeltaE);
}
