// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests;

/// <summary>A normalizer an application wrote, as a test hands one to a colour scale: it has no JSON form and answers what
/// <paramref name="normalize"/> says for a value and its range.</summary>
/// <param name="normalize">The fraction of a value between a low and a high end.</param>
internal sealed class TestNormalizer(Func<double, double, double, double> normalize) : INormalizer
{
    public double Normalize(double value, double min, double max) => normalize(value, min, max);
}
