// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Tests.TestFixtures;

namespace MatPlotLibNet.Tests.Styling.ColorMapNormalizers;

/// <summary>Edge-case coverage for <see cref="LinearNormalizer"/>: pushes branch
/// coverage from 50% to 100% by hitting the degenerate-range branch (max == min)
/// and clamp-below / clamp-above paths in addition to the regular interpolation.</summary>
public class LinearNormalizerEdgeCaseTests
{
    [Fact]
    public void DegenerateRange_ReturnsHalf()
    {
        // max == min triggers the `range == 0 → 0.5` early-return branch
        Assert.Equal(0.5, LinearNormalizer.Instance.Normalize(value: 7, min: 5, max: 5));
    }

    [Fact]
    public void DegenerateRange_AnyValue_ReturnsHalf()
    {
        // Same branch — every value (even far outside the collapsed point) returns 0.5
        Assert.Equal(0.5, LinearNormalizer.Instance.Normalize(value: 1e300, min: 5, max: 5));
        Assert.Equal(0.5, LinearNormalizer.Instance.Normalize(value: -1e300, min: 5, max: 5));
    }

    [Theory]
    [InlineData(-100, 0, 100, 0.0)]   // below min → clamped to 0
    [InlineData(0,    0, 100, 0.0)]   // at min   → 0
    [InlineData(50,   0, 100, 0.5)]   // mid      → 0.5
    [InlineData(100,  0, 100, 1.0)]   // at max   → 1
    [InlineData(200,  0, 100, 1.0)]   // above max → clamped to 1
    public void Normalize_StandardCases(double value, double min, double max, double expected)
        => Assert.Equal(expected, LinearNormalizer.Instance.Normalize(value, min, max), 1e-9);

    [Theory]
    [MemberData(nameof(BoundaryDoublesData))]
    public void BoundaryDoubles_DoNotThrow_AndStayInRange(double v)
    {
        // Shouldn't throw and should clamp into [0,1] for any boundary input
        double r = LinearNormalizer.Instance.Normalize(v, min: -1000, max: 1000);
        // NaN inputs are allowed to propagate as NaN; otherwise must be in [0,1]
        Assert.True(double.IsNaN(r) || (r >= 0 && r <= 1), $"out of range: {r}");
    }

    public static IEnumerable<object[]> BoundaryDoublesData() =>
        EdgeCaseData.BoundaryDoubles.Select(d => new object[] { d });
}
