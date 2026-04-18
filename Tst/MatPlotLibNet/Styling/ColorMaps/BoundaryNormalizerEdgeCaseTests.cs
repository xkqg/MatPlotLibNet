// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling.ColorMaps;

// Use a flat test namespace to avoid resolver shadowing of MatPlotLibNet.Styling.ColorMaps.Viridis etc.
namespace MatPlotLibNet.Tests.Styling.ColorMapNormalizers;

/// <summary>Edge-case coverage for <see cref="BoundaryNormalizer"/>: pushes from
/// 85.7% to 100% by exercising every branch — empty/single boundaries (degenerate
/// nBins ≤ 0), values below first boundary, between any two boundaries, exactly
/// at boundary, and above last boundary.</summary>
public class BoundaryNormalizerEdgeCaseTests
{
    [Fact]
    public void EmptyBoundaries_ReturnsHalf()
    {
        // nBins = -1 → degenerate → returns 0.5 fallback
        var n = new BoundaryNormalizer(Array.Empty<double>());
        Assert.Equal(0.5, n.Normalize(value: 5, min: 0, max: 10));
    }

    [Fact]
    public void SingleBoundary_ReturnsHalf()
    {
        // 1 boundary → 0 bins → degenerate
        var n = new BoundaryNormalizer(new[] { 5.0 });
        Assert.Equal(0.5, n.Normalize(value: 1, min: 0, max: 10));
        Assert.Equal(0.5, n.Normalize(value: 7, min: 0, max: 10));
    }

    [Theory]
    // boundaries [0, 1, 2, 3, 4]  → 4 bins
    // The implementation uses `value < boundaries[i+1]` so value falls in bin i when
    //   bin 0: value < 1 (covers everything from -∞ to just below 1) → 0/4 = 0
    //   bin 1: value < 2  → 1/4
    //   bin 2: value < 3  → 2/4
    //   bin 3: (last-bin fallback for value >= 3) → 3/4
    [InlineData(-100, 0.0)]     // below first boundary → bin 0
    [InlineData(0,    0.0)]     // at first boundary → bin 0 (value < 1 is true)
    [InlineData(0.5,  0.0)]     // mid bin 0
    [InlineData(1,    0.25)]    // at boundary 1 → bin 1 (value < 2)
    [InlineData(2.5,  0.5)]     // mid bin 2
    [InlineData(3.5,  0.75)]    // mid bin 3 → last-bin fallback
    [InlineData(100,  0.75)]    // above last → last-bin fallback
    public void NormalizesValuesToCorrectBin(double value, double expected)
    {
        var n = new BoundaryNormalizer(new[] { 0.0, 1.0, 2.0, 3.0, 4.0 });
        Assert.Equal(expected, n.Normalize(value, min: -100, max: 100), 1e-9);
    }
}
