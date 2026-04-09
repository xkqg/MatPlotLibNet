// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Verifies <see cref="INormalizer"/> implementations.</summary>
public class NormalizerTests
{
    // --- LinearNormalizer ---

    [Fact]
    public void LinearNormalizer_MinMapsToZero()
    {
        var norm = LinearNormalizer.Instance;
        Assert.Equal(0.0, norm.Normalize(0, 0, 100), 5);
    }

    [Fact]
    public void LinearNormalizer_MaxMapsToOne()
    {
        var norm = LinearNormalizer.Instance;
        Assert.Equal(1.0, norm.Normalize(100, 0, 100), 5);
    }

    [Fact]
    public void LinearNormalizer_MidMapsToHalf()
    {
        var norm = LinearNormalizer.Instance;
        Assert.Equal(0.5, norm.Normalize(50, 0, 100), 5);
    }

    [Fact]
    public void LinearNormalizer_ClampsBelow()
    {
        var norm = LinearNormalizer.Instance;
        Assert.Equal(0.0, norm.Normalize(-10, 0, 100), 5);
    }

    [Fact]
    public void LinearNormalizer_ClampsAbove()
    {
        var norm = LinearNormalizer.Instance;
        Assert.Equal(1.0, norm.Normalize(200, 0, 100), 5);
    }

    // --- LogNormalizer ---

    [Fact]
    public void LogNormalizer_MinMapsToZero()
    {
        var norm = new LogNormalizer();
        Assert.Equal(0.0, norm.Normalize(0, 0, 100), 5);
    }

    [Fact]
    public void LogNormalizer_MaxMapsToOne()
    {
        var norm = new LogNormalizer();
        Assert.Equal(1.0, norm.Normalize(100, 0, 100), 5);
    }

    [Fact]
    public void LogNormalizer_CompressesHighValues()
    {
        var norm = new LogNormalizer();
        // In linear: 50/100 = 0.5. In log: should be > 0.5 (log compresses top)
        double result = norm.Normalize(50, 0, 100);
        Assert.True(result > 0.5, $"LogNormalizer at midpoint should be > 0.5, was {result}");
    }

    // --- TwoSlopeNormalizer ---

    [Fact]
    public void TwoSlopeNormalizer_CenterMapsToHalf()
    {
        var norm = new TwoSlopeNormalizer(center: 0);
        Assert.Equal(0.5, norm.Normalize(0, -10, 10), 5);
    }

    [Fact]
    public void TwoSlopeNormalizer_MinMapsToZero()
    {
        var norm = new TwoSlopeNormalizer(center: 0);
        Assert.Equal(0.0, norm.Normalize(-10, -10, 10), 5);
    }

    [Fact]
    public void TwoSlopeNormalizer_MaxMapsToOne()
    {
        var norm = new TwoSlopeNormalizer(center: 0);
        Assert.Equal(1.0, norm.Normalize(10, -10, 10), 5);
    }

    [Fact]
    public void TwoSlopeNormalizer_AsymmetricRange()
    {
        // Center at 0, range [-2, 10]. Value -1 should map to 0.25 (halfway in lower half)
        var norm = new TwoSlopeNormalizer(center: 0);
        Assert.Equal(0.25, norm.Normalize(-1, -2, 10), 5);
    }

    // --- BoundaryNormalizer ---

    [Fact]
    public void BoundaryNormalizer_ValueInFirstBin()
    {
        var norm = new BoundaryNormalizer([0, 10, 20, 50, 100]);
        double result = norm.Normalize(5, 0, 100);
        Assert.Equal(0.0, result, 5); // First bin → 0.0
    }

    [Fact]
    public void BoundaryNormalizer_ValueInLastBin()
    {
        var norm = new BoundaryNormalizer([0, 10, 20, 50, 100]);
        double result = norm.Normalize(75, 0, 100);
        Assert.Equal(0.75, result, 5); // 4th of 4 bins → 0.75
    }

    [Fact]
    public void BoundaryNormalizer_ExactBoundary()
    {
        var norm = new BoundaryNormalizer([0, 10, 20, 50, 100]);
        double result = norm.Normalize(10, 0, 100);
        Assert.Equal(0.25, result, 5); // On boundary 10 → second bin
    }
}
