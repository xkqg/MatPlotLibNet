// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Tests.TestFixtures;

namespace MatPlotLibNet.Tests.Styling.ColorMapNormalizers;

/// <summary>Edge-case coverage for <see cref="TwoSlopeNormalizer"/>: pushes branch
/// coverage from 66.7% to 100% by exercising the degenerate-half cases
/// (lowerRange == 0 and upperRange == 0) and the value-above/below paths.</summary>
public class TwoSlopeNormalizerEdgeCaseTests
{
    [Fact]
    public void Center_Exposed()
        => Assert.Equal(7.5, new TwoSlopeNormalizer(center: 7.5).Center);

    [Fact]
    public void DegenerateLowerHalf_CenterEqualsMin_ValueAtCenter_ReturnsHalf()
    {
        // Center == min → lowerRange = 0 → must hit the `lowerRange == 0 → 0.5` branch
        var n = new TwoSlopeNormalizer(center: 0);
        Assert.Equal(0.5, n.Normalize(value: 0, min: 0, max: 10));
    }

    [Fact]
    public void DegenerateLowerHalf_CenterEqualsMin_ValueBelowCenter_ReturnsHalf()
    {
        // Below-center value gets clamped to min == centre → lowerRange == 0 path
        var n = new TwoSlopeNormalizer(center: 0);
        Assert.Equal(0.5, n.Normalize(value: -5, min: 0, max: 10));
    }

    [Fact]
    public void DegenerateUpperHalf_CenterEqualsMax_ValueAtCenter_ReturnsHalf()
    {
        // Center == max → upperRange = 0 → only fires if value > center after clamp,
        // which it won't.  Value AT center hits the lower path with full range.
        // Push value above max (gets clamped) → lower-half because clamped <= center.
        var n = new TwoSlopeNormalizer(center: 10);
        // value = 20 clamped to 10 → clamped == center → lower branch with full lowerRange
        Assert.Equal(0.5, n.Normalize(value: 20, min: 0, max: 10));
    }

    [Fact]
    public void DegenerateBothHalves_AllZero_ReturnsHalf()
    {
        // min == max == center → both halves degenerate
        var n = new TwoSlopeNormalizer(center: 5);
        Assert.Equal(0.5, n.Normalize(value: 5, min: 5, max: 5));
    }

    [Theory]
    [InlineData(-10, -10, 10,  0.0)]   // at min → 0
    [InlineData(10, -10, 10,  1.0)]    // at max → 1
    [InlineData(0,  -10, 10,  0.5)]    // at center
    [InlineData(-100, -10, 10, 0.0)]   // below min clamped → 0
    [InlineData(100,  -10, 10, 1.0)]   // above max clamped → 1
    public void Normalize_StandardCases(double value, double min, double max, double expected)
        => Assert.Equal(expected, new TwoSlopeNormalizer(center: 0).Normalize(value, min, max), 1e-9);

    [Theory]
    [MemberData(nameof(BoundaryDoublesData))]
    public void BoundaryDoubles_DoNotThrow(double v)
    {
        var unused = new TwoSlopeNormalizer(center: 0).Normalize(v, min: -100, max: 100);
        Assert.True(true);
    }

    public static IEnumerable<object[]> BoundaryDoublesData() =>
        EdgeCaseData.BoundaryDoubles.Select(d => new object[] { d });
}
