// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.FluentApi;

/// <summary>Covers the remaining uncovered branch arms in <see cref="FacetedFigure"/> after
/// JointPlotFigureTests and PairPlotFigureTests provided baseline coverage:
///   - L51 TRUE: both Width and Height set → fb.WithSize called
///   - L99 AddLines TRUE: hueLabels null → single Plot series
///   - L99 AddLines FALSE: hueLabels set → one Plot series per hue group
///   - L127 TRUE: Palette is { Length: > 0 } → custom colours used in AddHistograms hue path</summary>
public class FacetedFigureTests
{
    private static readonly double[] X   = [1.0, 2.0, 3.0, 4.0];
    private static readonly double[] Y   = [1.1, 2.2, 3.3, 4.4];
    private static readonly string[] Hue = ["A", "A", "B", "B"];

    // ── Minimal test subclass ────────────────────────────────────────────────

    private sealed class LineFigure : FacetedFigure
    {
        private readonly double[] _x, _y;
        private readonly string[]? _hue;

        public LineFigure(double[] x, double[] y, string[]? hue = null)
        { _x = x; _y = y; _hue = hue; }

        protected override void BuildCore(FigureBuilder fb) =>
            fb.AddSubPlot(1, 1, 1, ax => AddLines(ax, _x, _y, _hue));
    }

    // ── L51 TRUE arm ────────────────────────────────────────────────────────

    /// <summary>L51 TRUE — both Width and Height set on a FacetedFigure subclass →
    /// FigureBuilder.WithSize is called before BuildCore.</summary>
    [Fact]
    public void Build_WithExplicitSize_CallsWithSize()
    {
        var fig = new JointPlotFigure(X, Y) { Width = 400, Height = 300 }.Build().Build();
        Assert.Equal(400, fig.Width);
        Assert.Equal(300, fig.Height);
    }

    // ── L99 AddLines arms ───────────────────────────────────────────────────

    /// <summary>L99 TRUE — hueLabels null → AddLines adds a single Plot (LineSeries).</summary>
    [Fact]
    public void AddLines_NoHue_AddsSingleLineSeries()
    {
        var fig = new LineFigure(X, Y).Build().Build();
        Assert.Single(fig.SubPlots[0].Series);
        Assert.IsType<LineSeries>(fig.SubPlots[0].Series[0]);
    }

    /// <summary>L99 FALSE — hueLabels set → AddLines adds one Plot per unique hue label.</summary>
    [Fact]
    public void AddLines_WithHue_AddsOneSeriesPerHueGroup()
    {
        int expected = Hue.Distinct().Count();
        var fig = new LineFigure(X, Y, Hue).Build().Build();
        Assert.Equal(expected, fig.SubPlots[0].Series.Count);
        Assert.All(fig.SubPlots[0].Series, s => Assert.IsType<LineSeries>(s));
    }

    // ── L127 Palette TRUE arm ────────────────────────────────────────────────

    /// <summary>L127 TRUE — Palette is { Length: &gt; 0 } → AddHistograms hue path uses the
    /// custom palette instead of HueGrouper.DefaultPalette.</summary>
    [Fact]
    public void AddHistograms_WithHue_WithCustomPalette_UsesCustomColors()
    {
        var palette = new[] { Colors.Red, Colors.Blue };
        var fig = new JointPlotFigure(X, Y)
        {
            Hue     = Hue,
            Palette = palette,
        }.Build().Build();
        // Two hue groups → two histogram series in the top marginal panel
        Assert.Equal(2, fig.SubPlots[0].Series.Count);
        // Both are histograms
        Assert.All(fig.SubPlots[0].Series, s => Assert.IsType<HistogramSeries>(s));
    }
}
