// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

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

    // --- SymLogNormalizer ---

    [Fact]
    public void SymLogNormalizer_Zero_MapsToHalf()
    {
        var norm = new SymLogNormalizer(linthresh: 1.0);
        Assert.Equal(0.5, norm.Normalize(0, -100, 100), 5);
    }

    [Fact]
    public void SymLogNormalizer_Positive_MapsAboveHalf()
    {
        var norm = new SymLogNormalizer(linthresh: 1.0);
        Assert.True(norm.Normalize(50, -100, 100) > 0.5);
    }

    [Fact]
    public void SymLogNormalizer_NegativeSymmetric()
    {
        var norm = new SymLogNormalizer(linthresh: 1.0);
        double pos = norm.Normalize(50, -100, 100);
        double neg = norm.Normalize(-50, -100, 100);
        Assert.Equal(1.0 - neg, pos, 5);
    }

    [Fact]
    public void SymLogNormalizer_LinthreshRespected_LinearNearZero()
    {
        // Within linthresh (±0.5), values should map nearly linearly around center
        var norm = new SymLogNormalizer(linthresh: 10.0);
        double small = norm.Normalize(1, -100, 100);
        double large = norm.Normalize(50, -100, 100);
        // small value closer to 0.5 than large
        Assert.True(Math.Abs(small - 0.5) < Math.Abs(large - 0.5));
    }

    [Fact]
    public void SymLogNormalizer_Min_MapsToZero()
    {
        var norm = new SymLogNormalizer();
        Assert.Equal(0.0, norm.Normalize(-100, -100, 100), 5);
    }

    [Fact]
    public void SymLogNormalizer_Max_MapsToOne()
    {
        var norm = new SymLogNormalizer();
        Assert.Equal(1.0, norm.Normalize(100, -100, 100), 5);
    }

    // --- PowerNormNormalizer ---

    [Fact]
    public void PowerNormNormalizer_MinMapsToZero()
    {
        var norm = new PowerNormNormalizer(gamma: 2.0);
        Assert.Equal(0.0, norm.Normalize(0, 0, 100), 5);
    }

    [Fact]
    public void PowerNormNormalizer_MaxMapsToOne()
    {
        var norm = new PowerNormNormalizer(gamma: 2.0);
        Assert.Equal(1.0, norm.Normalize(100, 0, 100), 5);
    }

    [Fact]
    public void PowerNormNormalizer_GammaLessThanOne_CompressesLow()
    {
        // gamma=0.5: sqrt(0.5) ≈ 0.707 — midpoint 50 maps above 0.5 (low values expanded)
        var norm = new PowerNormNormalizer(gamma: 0.5);
        Assert.True(norm.Normalize(50, 0, 100) > 0.5);
    }

    [Fact]
    public void PowerNormNormalizer_GammaGreaterThanOne_CompressesHigh()
    {
        // gamma=2: 0.5^2 = 0.25 — midpoint 50 maps < 0.5
        var norm = new PowerNormNormalizer(gamma: 2.0);
        Assert.True(norm.Normalize(50, 0, 100) < 0.5);
    }

    [Fact]
    public void PowerNormNormalizer_GammaOne_IsLinear()
    {
        var norm = new PowerNormNormalizer(gamma: 1.0);
        Assert.Equal(0.5, norm.Normalize(50, 0, 100), 5);
    }

    // --- CenteredNormNormalizer ---

    [Fact]
    public void CenteredNorm_VcenterMapsToHalf()
    {
        var norm = new CenteredNormNormalizer(vcenter: 0);
        Assert.Equal(0.5, norm.Normalize(0, -10, 10), 5);
    }

    [Fact]
    public void CenteredNorm_MinMapsToZero()
    {
        var norm = new CenteredNormNormalizer(vcenter: 0);
        Assert.Equal(0.0, norm.Normalize(-10, -10, 10), 5);
    }

    [Fact]
    public void CenteredNorm_MaxMapsToOne()
    {
        var norm = new CenteredNormNormalizer(vcenter: 0);
        Assert.Equal(1.0, norm.Normalize(10, -10, 10), 5);
    }

    [Fact]
    public void CenteredNorm_AsymmetricRange_CorrectLowerHalf()
    {
        // Center at 0, range [-2, 10]. Value -1 → halfway in lower half → 0.25
        var norm = new CenteredNormNormalizer(vcenter: 0);
        Assert.Equal(0.25, norm.Normalize(-1, -2, 10), 5);
    }

    [Fact]
    public void CenteredNorm_Halfrange_Symmetric()
    {
        // vcenter=0, halfrange=5 → constrained to [-5, 5]
        // -2.5 → halfway lower → 0.25; +2.5 → halfway upper → 0.75
        var norm = new CenteredNormNormalizer(vcenter: 0, halfrange: 5);
        Assert.Equal(0.25, norm.Normalize(-2.5, -10, 10), 5);
        Assert.Equal(0.75, norm.Normalize(2.5, -10, 10), 5);
    }

    // --- NoNormNormalizer ---

    [Fact]
    public void NoNorm_PassesValueThrough()
    {
        Assert.Equal(0.7, NoNormNormalizer.Instance.Normalize(0.7, 0, 1), 5);
    }

    [Fact]
    public void NoNorm_ClampsBelowZero()
    {
        Assert.Equal(0.0, NoNormNormalizer.Instance.Normalize(-0.1, 0, 1), 5);
    }

    [Fact]
    public void NoNorm_ClampsAboveOne()
    {
        Assert.Equal(1.0, NoNormNormalizer.Instance.Normalize(1.1, 0, 1), 5);
    }

    [Fact]
    public void NoNorm_IsSingleton()
    {
        Assert.Same(NoNormNormalizer.Instance, NoNormNormalizer.Instance);
    }

    [Fact]
    public void NoNorm_IgnoresMinMax()
    {
        // min/max are irrelevant — value is taken as-is and clamped to [0,1]
        Assert.Equal(0.5, NoNormNormalizer.Instance.Normalize(0.5, -1000, 1000), 5);
    }
}
