// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Wavelet;

/// <summary>Result of a discrete wavelet <see cref="HaarDwt.Decompose"/> call:
/// <paramref name="Details"/>[k] is the length-<c>(N &gt;&gt; (k+1))</c> detail array at level
/// <c>k+1</c>, and <paramref name="Approx"/> is the final approximation of length
/// <c>N &gt;&gt; levels</c>.</summary>
internal readonly record struct DwtResult(double[][] Details, double[] Approx);
