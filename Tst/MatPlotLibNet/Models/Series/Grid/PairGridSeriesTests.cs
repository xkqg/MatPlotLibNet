// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="PairGridSeries"/> construction, validation, clamping and its diagonal densities. What it
/// starts from and the settings it stores are held by the resting-value corpus and the drawn-property round trips; render
/// behaviour lives in <c>PairGridRenderTests</c>.</summary>
public class PairGridSeriesTests
{
    private static double[][] SampleVars => new[]
    {
        new[] { 1.0, 2.0, 3.0, 4.0 },
        new[] { 0.5, 1.5, 2.5, 3.5 },
        new[] { 9.0, 8.0, 7.0, 6.0 },
    };

    // ── Construction ─────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_EmptyVariables_Throws()
    {
        Assert.Throws<ArgumentException>(() => new PairGridSeries(Array.Empty<double[]>()));
    }

    [Fact]
    public void Constructor_JaggedVariables_Throws()
    {
        var v = new[]
        {
            new[] { 1.0, 2.0, 3.0 },
            new[] { 1.0, 2.0 }, // shorter
        };
        Assert.Throws<ArgumentException>(() => new PairGridSeries(v));
    }

    [Fact]
    public void Constructor_SingleVariable_Accepted()
    {
        var v = new[] { new[] { 1.0, 2.0, 3.0 } };
        var s = new PairGridSeries(v);
        Assert.Single(s.Variables);
    }

    // ── CellSpacing clamping [0, 0.2] ─────────────────────────────────────────

    /// <summary>The space between cells is held between 0 and 0.2 of a cell: a value outside is clamped to the nearer end, one inside
    /// (both ends included) is kept as given.</summary>
    [Theory]
    [InlineData(-0.5, 0.0)]
    [InlineData(0.9, 0.2)]
    [InlineData(0.07, 0.07)]
    [InlineData(0.0, 0.0)]
    [InlineData(0.2, 0.2)]
    public void CellSpacing_IsHeldBetweenZeroAndTwoTenths(double given, double kept) =>
        Assert.Equal(kept, new PairGridSeries(SampleVars) { CellSpacing = given }.CellSpacing, 10);

    // ── ComputeDataRange ──────────────────────────────────────────────────────

    [Fact]
    public void ComputeDataRange_ReturnsZeroToN()
    {
        var s = new PairGridSeries(SampleVars);
        var range = s.ComputeDataRange(null!);
        // 3 variables → outer axes span [0, 3] in both dimensions
        Assert.Equal(0.0, range.XMin);
        Assert.Equal(3.0, range.XMax);
        Assert.Equal(0.0, range.YMin);
        Assert.Equal(3.0, range.YMax);
    }

    // ── Diagonal densities ───────────────────────────────────────────────────

    /// <summary>The density on the diagonal of a variable is the density curve a <see cref="KdeSeries"/> of the same
    /// observations estimates: one rule for the density of a sample.</summary>
    [Fact]
    public void TheDiagonalDensities_AreOneCurvePerVariable_TheCurveADensitySeriesEstimates()
    {
        var series = new PairGridSeries(SampleVars) { DiagonalKind = PairGridDiagonalKind.Kde };

        var densities = series.DiagonalDensitiesUnder(ThemeLooks.Library);

        Assert.Equal(3, densities.Length);
        for (int v = 0; v < densities.Length; v++)
        {
            var only = Assert.Single(densities[v]);
            var expected = new KdeSeries(SampleVars[v]).CurveUnder(ThemeLooks.Library)!.Value;
            Assert.Equal(expected.X, only.Curve!.Value.X);
            Assert.Equal(expected.Y, only.Curve!.Value.Y);
        }
    }

    /// <summary>Split by hue, a variable has one density per group, smallest group first, each estimated from the group's
    /// drawable observations; a group with nothing to draw has no curve.</summary>
    [Fact]
    public void TheDiagonalDensities_SplitByHue_AreOnePerGroup_SmallestGroupFirst()
    {
        var series = new PairGridSeries([[1.0, 5, 2, 6, double.NaN, 3], [0.0, 1, 2, 3, 4, 5]])
        {
            DiagonalKind = PairGridDiagonalKind.Kde,
            HueGroups = [2, -1, 2, -1, 7, 2],
        };

        var first = series.DiagonalDensitiesUnder(ThemeLooks.Library)[0];

        Assert.Equal(new[] { -1, 2, 7 }, first.Select(d => d.Group));
        Assert.Equal(new KdeSeries([5.0, 6]).CurveUnder(ThemeLooks.Library)!.Value.Y, first[0].Curve!.Value.Y);
        Assert.Equal(new KdeSeries([1.0, 2, 3]).CurveUnder(ThemeLooks.Library)!.Value.Y, first[1].Curve!.Value.Y);
        Assert.Null(first[2].Curve);
    }

    /// <summary>A grid whose groups do not match its samples is not split: one density per variable.</summary>
    [Fact]
    public void TheDiagonalDensities_WithGroupsThatDoNotMatchTheSamples_AreOnePerVariable()
    {
        var series = new PairGridSeries(SampleVars) { DiagonalKind = PairGridDiagonalKind.Kde, HueGroups = [0, 1] };

        Assert.All(series.DiagonalDensitiesUnder(ThemeLooks.Library), variable => Assert.Single(variable));
    }

    /// <summary>The densities are estimated once and kept: asked twice, they are the same estimate.</summary>
    [Fact]
    public void TheDiagonalDensities_AskedTwice_AreEstimatedOnce()
    {
        var series = new PairGridSeries(SampleVars) { DiagonalKind = PairGridDiagonalKind.Kde };

        Assert.Same(series.DiagonalDensitiesUnder(ThemeLooks.Library), series.DiagonalDensitiesUnder(ThemeLooks.Library));
    }

    /// <summary>A figure drawn again with its grid unchanged (here its title changed, so the subplot is drawn) draws the
    /// densities the series kept and estimates none: a kept curve altered behind the series' back shows in the second
    /// picture, which a fresh estimate would not.</summary>
    [Fact]
    public void TheDiagonal_OfAGridDrawnAgainUnchanged_IsDrawnFromTheKeptDensities()
    {
        PairGridSeries series = null!;
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.PairGrid(SkewedVars, s =>
        {
            s.DiagonalKind = PairGridDiagonalKind.Kde;
            s.HueGroups = [0, 1, 0, 1, 0, 1, 0, 1];
            series = s;
        })).Build();
        _ = figure.ToSvg();
        var kept = series.DiagonalDensitiesUnder(figure.Theme.Looks);

        Array.Reverse(kept[0][0].Curve!.Value.Y);
        figure.SubPlots[0].Title = "drawn again";
        string again = figure.ToSvg();

        Assert.Same(kept, series.DiagonalDensitiesUnder(figure.Theme.Looks));
        Assert.NotEqual(figure.RoundTripped().ToSvg(), again);
    }

    /// <summary>A value changed in place in a variable or in the hue groups passes no setter; the grid is drawn as a fresh
    /// figure of the same data draws it all the same.</summary>
    [Fact]
    public void AValueChangedInPlace_IsDrawnAsAFreshGridDrawsIt()
    {
        var variables = SkewedVars;
        int[] hue = [0, 1, 0, 1, 0, 1, 0, 1];
        PairGridSeries series = null!;
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.PairGrid(variables, s =>
        {
            s.DiagonalKind = PairGridDiagonalKind.Kde;
            s.HueGroups = hue;
            series = s;
        })).Build();
        _ = figure.ToSvg();
        var before = series.DiagonalDensitiesUnder(ThemeLooks.Library);

        variables[1][2] = 40;
        string valueChanged = figure.ToSvg();
        Assert.NotSame(before, series.DiagonalDensitiesUnder(ThemeLooks.Library));
        Assert.Equal(figure.RoundTripped().ToSvg(), valueChanged);

        hue[3] = 2;
        Assert.Equal(figure.RoundTripped().ToSvg(), figure.ToSvg());
    }

    private static double[][] SkewedVars =>
    [
        [1.0, 1.2, 1.3, 2.0, 4.5, 9.0, 1.1, 2.2],
        [3.0, 0.5, 0.25, 7.0, 6.5, 6.0, 1.0, 2.0],
    ];
}
