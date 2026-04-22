// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Ehlers;

/// <summary>Per-bar output of <see cref="HilbertDiscriminator"/>: dominant-cycle
/// <paramref name="Period"/>, current <paramref name="Phase"/> (degrees), and the
/// in-phase / quadrature components (<paramref name="I1"/> / <paramref name="Q1"/>)
/// used by MAMA/FAMA, Sinewave, and Adaptive Stochastic.</summary>
internal readonly record struct HilbertResult(
    double[] Period,
    double[] Phase,
    double[] I1,
    double[] Q1);
