// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>The width of the kernel a density is smoothed with where its author names none is a value of the theme's density look
/// (<see cref="DensityLook"/>): under the matplotlib themes scipy's Scott factor, which matplotlib's <c>violinplot</c>
/// (<c>mlab.GaussianKDE(bw_method='scott')</c>) and seaborn's <c>kdeplot</c> and <c>pairplot</c> (<c>gaussian_kde</c>) smooth with:
/// <c>std(ddof=1) * n ** -0.2</c>; under the library's own themes Silverman's rule of thumb, 1.06 times that. seaborn's curve stands on
/// 200 points (<c>gridsize=200</c>), the library's on 100. Printed with python for the sample 1, 2, 2, 3, 5: bandwidth
/// 1.0991827828099079; <c>kdeplot</c> x[0] -2.2975483484297237, x[100] 3.026620845972008, y[100] 0.19417805223218848, x[150]
/// 5.688705443172873, y[150] 0.06382445764904433; <c>violinplot</c>'s body over <c>linspace(1, 5, 100)</c>, as a share of its widest:
/// 0.7512502721609831 at 1, 0.8013011222204797 at the 51st height, 0.3706413828942848 at 5.</summary>
public class DensityLookTests
{
    private static readonly double[] Sample = [1.0, 2, 2, 3, 5];
    private const double Scott = 1.0991827828099079;

    [Fact]
    public void TheRestingBandwidth_IsTheFactorTimesTheSpreadOverTheFifthRootOfTheCount()
    {
        Assert.Equal(Scott, GaussianKde.Bandwidth(Sample, GaussianKde.ScottFactor), 14);
        Assert.Equal(1.06 * Scott, GaussianKde.Bandwidth(Sample, GaussianKde.SilvermanFactor), 14);
        Assert.Equal(GaussianKde.SilvermanFactor, DensityLook.Library.BandwidthFactor);
        Assert.Equal(GaussianKde.ScottFactor, DensityLook.Scipy.BandwidthFactor);
    }

    [Fact]
    public void ADensityCurve_IsSeabornsUnderTheMatplotlibThemes()
    {
        var curve = new KdeSeries(Sample).CurveUnder(Theme.MatplotlibV2.Looks)!.Value;

        Assert.Equal(200, curve.X.Length);
        Assert.Equal(-2.2975483484297237, curve.X[0], 12);
        Assert.Equal(3.026620845972008, curve.X[100], 12);
        Assert.Equal(0.19417805223218848, curve.Y[100], 12);
        Assert.Equal(5.688705443172873, curve.X[150], 12);
        Assert.Equal(0.06382445764904433, curve.Y[150], 12);
    }

    [Fact]
    public void ADensityCurve_KeepsTheLibrarysRuleUnderItsThemes()
    {
        var curve = new KdeSeries(Sample).CurveUnder(Theme.Default.Looks)!.Value;

        Assert.Equal(100, curve.X.Length);
        Assert.Equal(1 - 3 * 1.06 * Scott, curve.X[0], 12);
    }

    [Fact]
    public void AGivenBandwidth_IsDrawnAsGiven_OnTheThemesGrid()
    {
        var curve = new KdeSeries(Sample) { Bandwidth = 0.5 }.CurveUnder(Theme.MatplotlibV2.Looks)!.Value;

        Assert.Equal(200, curve.X.Length);
        Assert.Equal(1 - 3 * 0.5, curve.X[0], 12);
    }

    /// <summary>The curve is kept for the theme it was estimated under: asked again under that theme it is the same, asked under another
    /// it is estimated again.</summary>
    [Fact]
    public void AKeptCurve_IsKeptPerRule()
    {
        var kde = new KdeSeries(Sample);
        var seaborn = kde.CurveUnder(Theme.MatplotlibV2.Looks)!.Value;

        Assert.Same(seaborn.Y, kde.CurveUnder(Theme.MatplotlibClassic.Looks)!.Value.Y);
        Assert.NotEqual(seaborn.X.Length, kde.CurveUnder(Theme.Default.Looks)!.Value.X.Length);
        Assert.NotSame(seaborn.Y, kde.CurveUnder(Theme.MatplotlibV2.Looks)!.Value.Y);
    }

    [Fact]
    public void AViolinsBody_IsMatplotlibsUnderTheMatplotlibThemes()
    {
        var violin = new ViolinSeries([Sample]);
        var outline = violin.BodiesUnder(Theme.MatplotlibV2.Looks)[0].Outline;
        double widest = outline.Max(slice => slice.HalfWidth);

        Assert.Equal(100, outline.Length);
        Assert.Equal(0.7512502721609831, outline[0].HalfWidth / widest, 12);
        Assert.Equal(0.8013011222204797, outline[50].HalfWidth / widest, 12);
        Assert.Equal(0.3706413828942848, outline[99].HalfWidth / widest, 12);
        Assert.NotEqual(outline[50].HalfWidth, violin.BodiesUnder(Theme.Default.Looks)[0].Outline[50].HalfWidth);
    }

    [Fact]
    public void APairGridsDiagonal_IsTheDensityCurveOfItsTheme()
    {
        var grid = new PairGridSeries([Sample, [2.0, 3, 4, 4, 6]]) { DiagonalKind = PairGridDiagonalKind.Kde };

        foreach (var theme in new[] { Theme.MatplotlibV2, Theme.Default })
        {
            var diagonal = grid.DiagonalDensitiesUnder(theme.Looks)[0][0].Curve!.Value;
            var curve = new KdeSeries(Sample).CurveUnder(theme.Looks)!.Value;
            Assert.Equal(curve.X, diagonal.X);
            Assert.Equal(curve.Y, diagonal.Y);
        }
    }

    [Theory]
    [InlineData("MatplotlibV2")]
    [InlineData("MatplotlibClassic")]
    [InlineData("Default")]
    public void TheDensityLook_TravelsThroughTheThemeJson(string themeName)
    {
        var theme = themeName.AsTheme();

        Assert.Same(theme.Looks.Density, theme.ThroughDto().Looks.Density);
        Assert.Same(themeName == "Default" ? DensityLook.Library : DensityLook.Scipy, theme.Looks.Density);
    }
}
