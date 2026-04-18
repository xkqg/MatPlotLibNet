// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Tests.TestFixtures;

namespace MatPlotLibNet.Tests.Styling.ColorMapNormalizers;

/// <summary>Edge-case coverage for <see cref="PowerNormNormalizer"/>: pushes branch
/// coverage from 50% to 100% by hitting the <c>range == 0</c> and clamp paths
/// alongside the standard gamma curve cases already in <c>NormalizerTests</c>.</summary>
public class PowerNormNormalizerEdgeCaseTests
{
    [Fact]
    public void DegenerateRange_ReturnsHalf()
        => Assert.Equal(0.5, new PowerNormNormalizer(gamma: 2.0).Normalize(value: 9, min: 5, max: 5));

    [Theory]
    [InlineData(-1000, 0, 100, 2.0, 0.0)]   // below min → 0^gamma = 0
    [InlineData(2000,  0, 100, 2.0, 1.0)]   // above max → 1^gamma = 1
    [InlineData(0,     0, 100, 0.5, 0.0)]   // at min → 0
    [InlineData(100,   0, 100, 0.5, 1.0)]   // at max → 1
    public void Normalize_ClampedEndpoints(double value, double min, double max, double gamma, double expected)
        => Assert.Equal(expected, new PowerNormNormalizer(gamma).Normalize(value, min, max), 1e-9);

    [Fact]
    public void GammaProperty_IsExposed()
        => Assert.Equal(2.5, new PowerNormNormalizer(gamma: 2.5).Gamma);

    [Theory]
    [MemberData(nameof(BoundaryDoublesData))]
    public void BoundaryDoubles_DoNotThrow(double v)
    {
        var unused = new PowerNormNormalizer(gamma: 1.0).Normalize(v, min: 0, max: 100);
        Assert.True(true);
    }

    public static IEnumerable<object[]> BoundaryDoublesData() =>
        EdgeCaseData.BoundaryDoubles.Select(d => new object[] { d });
}
