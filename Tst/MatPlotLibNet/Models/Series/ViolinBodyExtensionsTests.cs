// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>The outline of a violin's body: how wide it is at each height. The renderer draws it and the series ranges over
/// it, so the width is decided here once. The outline is the sample's Gaussian kernel density, the same estimator the density
/// curve and the ridgeline are drawn with (Silverman's bandwidth), read at 100 heights from the lowest value to the highest as
/// matplotlib's <c>violinplot</c> reads its estimate (<c>points=100</c>), and scaled so that the violin's widest point is the
/// width: a violin of several samples has the same widest point whatever the sizes of the samples.
/// <para>Measured before the change: the width was the share of the sample within a twentieth of the extent of a height, a count
/// in a window and not a density, so on 12 observations the outline had five bumps and pinched to no width inside the body.</para></summary>
public class ViolinBodyExtensionsTests
{
    private static readonly double[] Twelve = [4.1, 4.8, 5.0, 5.3, 5.5, 5.9, 6.2, 6.4, 6.8, 7.1, 7.7, 8.4];

    private static int LocalMaxima(IReadOnlyList<ViolinSlice> outline) =>
        Enumerable.Range(1, outline.Count - 2).Count(i => outline[i].HalfWidth > outline[i - 1].HalfWidth && outline[i].HalfWidth >= outline[i + 1].HalfWidth);

    [Fact]
    public void TheOutline_StandsAtOneHundredHeights_FromTheLowestValueToTheHighest()
    {
        var outline = new[] { 1.0, 2, 3, 4, 5 }.Outline(0.5);

        Assert.Equal(ViolinBodyExtensions.Heights, outline.Length);
        Assert.Equal(100, outline.Length);
        Assert.Equal(1, outline[0].Y);
        Assert.Equal(5, outline[^1].Y);
        Assert.Equal(1 + 4.0 / 99, outline[1].Y, 12);
    }

    /// <summary>Twelve observations, the sample of the raincloud example: one smooth hump, where the window count had five bumps
    /// with the body pinched to no width between them.</summary>
    [Fact]
    public void TheOutline_OfTwelveObservations_IsOneHump()
    {
        var outline = Twelve.Outline(0.5);

        Assert.Equal(1, LocalMaxima(outline));
        Assert.All(outline, slice => Assert.True(slice.HalfWidth > 0, $"the body is pinched to no width at {slice.Y}"));
    }

    /// <summary>The violin is scaled by its own peak: its widest half-width is the width the series says, whatever the sample.</summary>
    [Theory]
    [InlineData(0.5)]
    [InlineData(2.0)]
    public void TheWidestHalfWidth_IsTheWidth(double widths)
    {
        Assert.Equal(widths, Twelve.Outline(widths).Max(slice => slice.HalfWidth), 12);
        Assert.Equal(widths, new[] { 1.0, 2, 3, 4, 12 }.Outline(widths).Max(slice => slice.HalfWidth), 12);
    }

    /// <summary>The half-width at a height is the Gaussian density of the sample there over the density at the highest height,
    /// times the width, with Silverman's bandwidth: checked here against the formula written out once more, to 1e-12.</summary>
    [Fact]
    public void TheHalfWidth_IsTheKernelDensityOverItsPeak_TimesTheWidth()
    {
        double[] sample = [1, 2, 3, 4, 12];
        double n = sample.Length;
        double mean = sample.Average();
        double sd = Math.Sqrt(sample.Sum(v => (v - mean) * (v - mean)) / (n - 1));
        double h = 1.06 * sd * Math.Pow(n, -0.2);
        Assert.Equal(3.3751301062875134, h, 12);

        double Density(double y) => sample.Sum(v => Math.Exp(-0.5 * Math.Pow((y - v) / h, 2))) / (n * h * Math.Sqrt(2 * Math.PI));
        double[] ys = [.. Enumerable.Range(0, 100).Select(i => 1 + 11.0 * i / 99)];
        double peak = ys.Max(Density);

        var outline = sample.Outline(0.5);

        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(ys[i], outline[i].Y, 12);
            Assert.Equal(0.5 * Density(ys[i]) / peak, outline[i].HalfWidth, 12);
        }
    }

    [Fact]
    public void TheHalfWidth_ScalesWithTheWidth()
    {
        var narrow = new[] { 1.0, 2, 3, 4, 5 }.Outline(0.5);
        var wide = new[] { 1.0, 2, 3, 4, 5 }.Outline(2);

        for (int i = 0; i < narrow.Length; i++) Assert.Equal(4 * narrow[i].HalfWidth, wide[i].HalfWidth, 12);
    }

    /// <summary>Two sizes of one shape are the same violin: the density is divided by its peak, not by the number of observations.</summary>
    [Fact]
    public void TheSampleSize_DoesNotChangeTheWidestPoint()
    {
        double[] small = [1, 2, 3];
        double[] large = [.. small.SelectMany(v => Enumerable.Repeat(v, 50))];

        Assert.Equal(small.Outline(0.5).Max(s => s.HalfWidth), large.Outline(0.5).Max(s => s.HalfWidth), 9);
    }

    /// <summary>A sample without a spread (one observation, or every value the same) has no density to draw: its body is the
    /// spike matplotlib draws, the whole width at its value, at every height. The window count ran it from the value up to the
    /// value plus 1.</summary>
    [Theory]
    [InlineData(5.0)]
    [InlineData(5.0, 5.0, 5.0, 5.0)]
    public void ASampleWithoutASpread_IsASpikeAtItsValue(params double[] sample)
    {
        var outline = sample.Outline(0.5);

        Assert.Equal(100, outline.Length);
        Assert.All(outline, slice => Assert.Equal(new ViolinSlice(5, 0.5), slice));
    }

    /// <summary>Two clumps are two humps with a dip between, a density and not a pair of separate bars: the bandwidth (4.05 for
    /// ten units apart) smooths them into each other.</summary>
    [Fact]
    public void TwoClumps_MakeTwoHumps_AndADipBetween()
    {
        var outline = new[] { 0.0, 0, 0, 10, 10, 10 }.Outline(1);

        Assert.Equal(2, LocalMaxima(outline));
        double dip = outline.Min(slice => slice.HalfWidth);
        Assert.True(dip > 0 && dip < 1, $"the dip is {dip}");
    }

    /// <summary>The violin has no kernel or bandwidth to choose: it reads the library's one Gaussian estimator at its one
    /// bandwidth rule, as the density curve does. A setting for either would be a second way to draw the same density.</summary>
    [Fact]
    public void TheViolin_OffersNoKernelOrBandwidth()
    {
        var members = typeof(ViolinSeries).GetProperties().Select(p => p.Name).Concat(typeof(ViolinSeries).GetMethods().Select(m => m.Name));

        Assert.DoesNotContain(members, name => name.Contains("Kernel", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Bandwidth", StringComparison.OrdinalIgnoreCase));
    }
}
