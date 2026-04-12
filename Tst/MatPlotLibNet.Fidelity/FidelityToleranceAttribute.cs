// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Fidelity;

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
