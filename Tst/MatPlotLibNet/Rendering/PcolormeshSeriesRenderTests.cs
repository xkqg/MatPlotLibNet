// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="PcolormeshSeries"/> rendering.</summary>
public class PcolormeshSeriesRenderTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0, 3.0];
    private static readonly double[] Y = [0.0, 1.0, 2.0];
    private static readonly double[,] C = { { 1.0, 2.0, 3.0 }, { 4.0, 5.0, 6.0 } };

    [Fact]
    public void Pcolormesh_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pcolormesh(X, Y, C))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Pcolormesh_SvgContainsRectangles()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pcolormesh(X, Y, C))
            .ToSvg();
        Assert.Contains("<rect", svg);
    }

    [Fact]
    public void Pcolormesh_SingleCell_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pcolormesh([0.0, 1.0], [0.0, 1.0], new double[,] { { 5.0 } }))
            .ToSvg();
        Assert.Contains("<rect", svg);
    }

    [Fact]
    public void Pcolormesh_FluentShortcut_ProducesSvg()
    {
        string svg = Plt.Create()
            .Pcolormesh(X, Y, C)
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Pcolormesh_EmptyC_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pcolormesh([0.0, 1.0], [0.0, 1.0], new double[0, 0]))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    // Branch coverage — exercise each early-return guard in PcolormeshSeriesRenderer.

    [Fact]
    public void Pcolormesh_ZeroCols_EarlyReturn_NoCellsEmitted()
    {
        // rows>0 but cols==0 — the second half of `rows == 0 || cols == 0`.
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pcolormesh([0.0, 1.0], [0.0, 1.0, 2.0], new double[2, 0]))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Pcolormesh_XTooShort_EarlyReturn()
    {
        // X.Length < 2 — first half of edges-too-short guard.
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pcolormesh([0.0], [0.0, 1.0, 2.0], new double[,] { { 1.0 } }))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Pcolormesh_YTooShort_EarlyReturn()
    {
        // Y.Length < 2 — second half of edges-too-short guard.
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pcolormesh([0.0, 1.0, 2.0], [0.0], new double[,] { { 1.0 } }))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Pcolormesh_AllEqualValues_DegenerateGuard()
    {
        // Hit the (min == max) branch of ResolveColormapping via the renderer.
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pcolormesh([0.0, 1.0, 2.0], [0.0, 1.0, 2.0],
                new double[,] { { 5.0, 5.0 }, { 5.0, 5.0 } }))
            .ToSvg();
        Assert.Contains("<rect", svg);
    }

    // ── the cells reach the spines ─────────────────────────────────────────────
    //
    // A heatmap fills its plot rectangle exactly; a pcolormesh, which is the same picture over coordinates the
    // caller chooses, did not. The axes added their usual 5 % breathing room around the data, so every mesh was
    // drawn inside a gutter with the spines standing off the cells. Measured against matplotlib's own output on
    // the sin/cos fixture before this changed: RMS 57.7 of 255 and SSIM 0.51 on the classic theme.

    /// <summary>The plot rectangle the axes reserved: the widest rectangle that is not the whole figure.</summary>
    private static (double X0, double X1, double Y0, double Y1) PlotArea(string svg)
    {
        double x0 = 0, y0 = 0, w = 0, h = 0;
        foreach (Match m in Regex.Matches(svg,
            @"<rect x=""([0-9.eE+-]+)"" y=""([0-9.eE+-]+)"" width=""([0-9.eE+-]+)"" height=""([0-9.eE+-]+)"""))
        {
            double mw = double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
            double mh = double.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture);
            if (mw < 639 && mw * mh > w * h)
            {
                x0 = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                y0 = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                w = mw; h = mh;
            }
        }

        return (x0, x0 + w, y0, y0 + h);
    }

    [Fact]
    public void TheCells_FillThePlotAreaFromSpineToSpine()
    {
        string svg = Plt.Create().WithSize(640, 480)
            .AddSubPlot(1, 1, 1, ax => ax.Pcolormesh(X, Y, C))
            .ToSvg();

        var area = PlotArea(svg);
        var cells = Regex.Matches(svg,
                @"<rect x=""([0-9.eE+-]+)"" y=""([0-9.eE+-]+)"" width=""([0-9.eE+-]+)"" height=""([0-9.eE+-]+)""")
            .Select(m => (
                X: double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                Y: double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture),
                W: double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture),
                H: double.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture)))
            .Where(r => r.W < area.X1 - area.X0 - 0.5)
            .ToArray();

        Assert.Equal(6, cells.Length);
        Assert.Equal(area.X0, cells.Min(c => c.X), 1);
        Assert.Equal(area.X1, cells.Max(c => c.X + c.W), 1);
        Assert.Equal(area.Y0, cells.Min(c => c.Y), 1);
        Assert.Equal(area.Y1, cells.Max(c => c.Y + c.H), 1);
    }

    [Fact]
    public void TheAxisRange_IsTheEdgesTheCallerGave()
    {
        var series = new PcolormeshSeries(X, Y, C);

        var range = series.ComputeDataRange(new NoAxisLimits());

        Assert.Equal(0.0, range.StickyXMin);
        Assert.Equal(3.0, range.StickyXMax);
        Assert.Equal(0.0, range.StickyYMin);
        Assert.Equal(2.0, range.StickyYMax);
    }

    [Fact]
    public void AMeshWithNoEdges_SticksToNothing()
    {
        var series = new PcolormeshSeries(Array.Empty<double>(), Array.Empty<double>(), new double[0, 0]);

        var range = series.ComputeDataRange(new NoAxisLimits());

        Assert.Null(range.StickyXMin);
        Assert.Null(range.StickyYMax);
    }

    /// <summary>An axes that has not been given limits — what a series sees before the axes decide anything.</summary>
    private sealed class NoAxisLimits : IAxesContext
    {
        public double? XAxisMin => null;
        public double? XAxisMax => null;
        public double? YAxisMin => null;
        public double? YAxisMax => null;
        public BarMode BarMode => BarMode.Grouped;
        public IReadOnlyList<ISeries> AllSeries => [];
    }
}
