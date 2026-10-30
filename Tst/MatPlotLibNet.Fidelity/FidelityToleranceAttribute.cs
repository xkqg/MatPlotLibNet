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
/// pushes it above the suite-wide defaults.
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
}
