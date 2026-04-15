// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>Regression battery for measurer/renderer drift across every element the
/// <see cref="ConstrainedLayoutEngine"/> reserves margin for. The original bug this battery
/// guards against: v1.1.4 shipped with a Legend-clipping bug because the engine measured
/// legends with a tick font built from <c>Size = DefaultFont.Size - 2</c> while
/// <see cref="AxesRenderer.RenderLegend"/> drew them at <c>Size = DefaultFont.Size</c> — two
/// duplicate TickFont factories drifted. The legend case was patched and the underlying
/// drift (same bug class) still affects Y-tick width, X-tick height, and ColorBar tick-label
/// reservations. v1.2.1 consolidates every themed-font factory into
/// <c>ThemedFontProvider</c> so this class of bug cannot recur — and these tests assert the
/// semantic invariant: "the engine reserves enough margin for what the renderer actually
/// draws" for every measurable element.</summary>
public class MeasurerRendererDriftTests
{
    // ── Test harness helpers ────────────────────────────────────────────────

    private static double ParsePlotAreaX(string svg)
    {
        // The FIRST rect in the SVG is the figure background (0,0,figW,figH).
        // The SECOND is the plot area (shifted in by MarginLeft).
        // For themes without a tinted plot bg we match any rect that isn't at (0,0).
        var rects = Regex.Matches(svg, @"<rect x=""(?<x>[0-9.]+)"" y=""(?<y>[0-9.]+)"" width=""[0-9.]+"" height=""[0-9.]+""");
        foreach (Match r in rects)
        {
            double x = double.Parse(r.Groups["x"].Value, CultureInfo.InvariantCulture);
            double y = double.Parse(r.Groups["y"].Value, CultureInfo.InvariantCulture);
            if (x > 0 && y > 0) return x;
        }
        throw new Xunit.Sdk.XunitException("plot area rect not found in SVG output");
    }

    private static (double X, double Y, double W, double H) ParsePlotAreaRect(string svg)
    {
        var rects = Regex.Matches(svg,
            @"<rect x=""(?<x>[0-9.]+)"" y=""(?<y>[0-9.]+)"" width=""(?<w>[0-9.]+)"" height=""(?<h>[0-9.]+)""");
        foreach (Match r in rects)
        {
            double x = double.Parse(r.Groups["x"].Value, CultureInfo.InvariantCulture);
            double y = double.Parse(r.Groups["y"].Value, CultureInfo.InvariantCulture);
            double w = double.Parse(r.Groups["w"].Value, CultureInfo.InvariantCulture);
            double h = double.Parse(r.Groups["h"].Value, CultureInfo.InvariantCulture);
            if (x > 0 && y > 0) return (x, y, w, h);
        }
        throw new Xunit.Sdk.XunitException("plot area rect not found in SVG output");
    }

    /// <summary>The font the RENDERER uses for tick labels. Before v1.2.1 this was
    /// <c>AxesRenderer.TickFont()</c>; after v1.2.1 it's <c>ThemedFontProvider.TickFont(theme)</c>.
    /// Either way the size formula is <c>DefaultFont.Size</c>.</summary>
    private static Font RendererTickFont(Theme theme) => new()
    {
        Family = theme.DefaultFont.Family,
        Size   = theme.DefaultFont.Size,
        Color  = theme.ForegroundText,
    };

    // ── 1. Legend box (existing — kept green after v1.2.1 legend fix) ──────

    [Fact]
    public void LegendBox_MeasurerMatchesRenderedRect_NoClipping()
    {
        double[] xo = Enumerable.Range(0, 100).Select(i => i * 10.0 / 99).ToArray();

        var figure = Plt.Create()
            .WithSize(900, 500)
            .TightLayout()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot(xo, xo.Select(v => Math.Sin(v)).ToArray(), s => s.Label = "sin(x)");
                ax.Plot(xo, xo.Select(v => Math.Cos(v)).ToArray(), s => s.Label = "cos(x)");
                ax.Plot(xo, xo.Select(v => 0.5 * Math.Sin(2 * v)).ToArray(),
                    s => s.Label = "½ sin(2x)");
                ax.Plot(xo, xo.Select(v => Math.Exp(-v / 5) * Math.Cos(v)).ToArray(),
                    s => s.Label = "exp(-x/5)·cos(x)");
                ax.WithLegend(l => l with { Position = LegendPosition.OutsideRight, Title = "Series" });
            })
            .Build();

        var measured = LegendMeasurer.MeasureBox(figure.SubPlots[0], new SvgRenderContext(), figure.Theme);

        string svg = new Transforms.SvgTransform().Render(figure);
        var m = Regex.Match(svg,
            @"<rect x=""(?<x>[0-9.]+)"" y=""[0-9.]+"" width=""(?<w>[0-9.]+)"" height=""[0-9.]+""[^>]*stroke=""#CCCCCC""");
        Assert.True(m.Success, "legend frame rect not found");

        double drawnX = double.Parse(m.Groups["x"].Value, CultureInfo.InvariantCulture);
        double drawnW = double.Parse(m.Groups["w"].Value, CultureInfo.InvariantCulture);

        Assert.True(Math.Abs(measured.Width - drawnW) < 0.5,
            $"LegendMeasurer width ({measured.Width:F2}) disagrees with drawn width ({drawnW:F2}).");
        Assert.True(drawnX + drawnW <= 900,
            $"Legend rect right edge ({drawnX + drawnW:F2}) exceeds figure width 900.");
    }

    // ── 2. Y-tick label width ─────────────────────────────────────────────
    //   Engine line 204-206 reserves maxYTickWidth + tickLength + tickPad + PadLeft.
    //   Pre-fix it measured with DefaultFont.Size - 2 (10 pt), under-reserving by ~2 pt × char-width.

    [Fact]
    public void YTickLabelWidth_LayoutEngineReserves_EnoughLeftMargin()
    {
        // Wide Y-tick labels: force integer ticks from -9999 to 9999 so the widest label
        // is e.g. "-9999" (or maybe scientific). 5+ chars * 12pt font = real width.
        var figure = Plt.Create()
            .WithSize(600, 400)
            .TightLayout()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot(
                    [0.0, 1.0, 2.0],
                    [-9999.0, 0.0, 9999.0]);
                ax.SetYLim(-9999, 9999);
            })
            .Build();

        string svg = new Transforms.SvgTransform().Render(figure);
        double plotX = ParsePlotAreaX(svg);

        // What the renderer would actually measure
        var tickFont = RendererTickFont(figure.Theme);
        double maxTickLabelWidth = ChartServices.FontMetrics.Measure("-9999", tickFont).Width;

        // Renderer draws the Y-tick label right-aligned at x = PlotArea.X - tickLength - tickPad.
        // For the label's LEFT edge to be ≥ 0 (fit in figure), we need:
        //   plotX >= tickLength + tickPad + maxTickLabelWidth
        var major = figure.SubPlots[0].YAxis.MajorTicks;
        double minRequired = major.Length + major.Pad + maxTickLabelWidth;

        Assert.True(plotX >= minRequired,
            $"Y-tick label clip: MarginLeft={plotX:F2} < required {minRequired:F2} " +
            $"(tickLength={major.Length} tickPad={major.Pad} maxLabel={maxTickLabelWidth:F2})");
    }

    // ── 3. X-tick label height ────────────────────────────────────────────
    //   Engine line 224: tickCellBottom = xMajor.Length + xMajor.Pad + tickH
    //     where tickH = ctx.MeasureText("0", tickFont).Height  ← tickFont drifted

    [Fact]
    public void XTickLabelHeight_LayoutEngineReserves_EnoughBottomMargin()
    {
        var figure = Plt.Create()
            .WithSize(600, 400)
            .TightLayout()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([0.0, 1.0, 2.0], [0.0, 1.0, 2.0]);
            })
            .Build();

        string svg = new Transforms.SvgTransform().Render(figure);
        var (plotX, plotY, plotW, plotH) = ParsePlotAreaRect(svg);
        double plotBottom = plotY + plotH;
        double marginBottom = 400 - plotBottom;

        // Renderer draws X-tick label at y = PlotArea.Y + PlotArea.Height + tickLength + tickPad + labelSize
        // For the label's BOTTOM to be ≤ figureHeight, we need:
        //   marginBottom >= tickLength + tickPad + labelHeight
        var major = figure.SubPlots[0].XAxis.MajorTicks;
        var tickFont = RendererTickFont(figure.Theme);
        double labelHeight = ChartServices.FontMetrics.Measure("0", tickFont).Height;
        double minRequired = major.Length + major.Pad + labelHeight;

        Assert.True(marginBottom >= minRequired,
            $"X-tick label clip: MarginBottom={marginBottom:F2} < required {minRequired:F2}");
    }

    // ── 4. ColorBar tick label width ──────────────────────────────────────
    //   Engine line 260: tickLabelW = ctx.MeasureText("0.0000", tickFont).Width
    //   Renderer uses the actual colorbar tick labels with the DefaultFont.Size font.

    [Fact]
    public void ColorBarTickLabelWidth_LayoutEngineReserves_EnoughRightMargin()
    {
        double[,] z = new double[3, 3] { { 0, 1, 2 }, { 1, 2, 3 }, { 2, 3, 4 } };

        var figure = Plt.Create()
            .WithSize(600, 400)
            .TightLayout()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Heatmap(z);
                ax.WithColorBar();
            })
            .Build();

        string svg = new Transforms.SvgTransform().Render(figure);
        var (plotX, plotY, plotW, plotH) = ParsePlotAreaRect(svg);
        double plotRight = plotX + plotW;
        double marginRight = 600 - plotRight;

        // The renderer measures colorbar tick labels at DefaultFont.Size.
        // Engine line 260 reserves: padding + barWidth + 4 + tickLabelW + 8 + labelH + PadRight.
        // We can't easily parse the colorbar position out of the SVG but the invariant is:
        //   marginRight must fit barWidth + tick-label-width at the renderer's actual font size.
        var tickFont = RendererTickFont(figure.Theme);
        double tickLabelW = ChartServices.FontMetrics.Measure("0.0000", tickFont).Width;
        // Conservative lower bound: 20 (bar) + 4 + tickLabelW
        double minRequired = 20 + 4 + tickLabelW;

        Assert.True(marginRight >= minRequired,
            $"ColorBar under-reserved: MarginRight={marginRight:F2} < required {minRequired:F2}");
    }

    // ── 5. Axes title height (guard — currently MATCH, keep as regression guard) ──

    [Fact]
    public void AxesTitleHeight_LayoutEngineReserves_EnoughTopMargin()
    {
        var figure = Plt.Create()
            .WithSize(600, 400)
            .TightLayout()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([0.0, 1.0], [0.0, 1.0]);
                ax.WithTitle("A Title");
            })
            .Build();

        string svg = new Transforms.SvgTransform().Render(figure);
        var (plotX, plotY, plotW, plotH) = ParsePlotAreaRect(svg);

        // The renderer draws the axes title at PlotArea.Y - 8 (TitleFont(2) @ DefaultFont.Size + 2).
        // For the title's TOP to be ≥ 0, we need: plotY >= labelHeight + 8
        var titleFont = new Font
        {
            Family = figure.Theme.DefaultFont.Family,
            Size   = figure.Theme.DefaultFont.Size + 2,
            Weight = FontWeight.Bold,
        };
        double titleHeight = ChartServices.FontMetrics.Measure("A Title", titleFont).Height;
        double minRequired = titleHeight + 8;

        Assert.True(plotY >= minRequired,
            $"Axes title clip: MarginTop={plotY:F2} < required {minRequired:F2}");
    }

    // ── 6. SupTitle height (guard — currently MATCH) ──

    [Fact]
    public void SupTitleHeight_LayoutEngineReserves_EnoughTopMargin()
    {
        var figure = Plt.Create()
            .WithSize(600, 400)
            .WithTitle("Figure Suptitle")
            .TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1.0], [0.0, 1.0]))
            .Build();

        string svg = new Transforms.SvgTransform().Render(figure);
        var (plotX, plotY, plotW, plotH) = ParsePlotAreaRect(svg);

        var supTitleFont = new Font
        {
            Family = figure.Theme.DefaultFont.Family,
            Size   = figure.Theme.DefaultFont.Size + 4,
            Weight = FontWeight.Bold,
        };
        double supH = ChartServices.FontMetrics.Measure("Figure Suptitle", supTitleFont).Height;
        double minRequired = supH + 8;

        Assert.True(plotY >= minRequired,
            $"Suptitle clip: MarginTop={plotY:F2} < required {minRequired:F2}");
    }

    // ── 7. Secondary X-axis label top margin (audit flag — verify first) ──

    [Fact]
    public void SecondaryXAxisLabel_LayoutEngineReserves_EnoughTopMargin()
    {
        var figure = Plt.Create()
            .WithSize(600, 400)
            .TightLayout()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([0.0, 1.0], [0.0, 1.0]);
                ax.WithSecondaryXAxis(secondary =>
                {
                    secondary.SetXLabel("Secondary X");
                });
            })
            .Build();

        string svg = new Transforms.SvgTransform().Render(figure);
        var (plotX, plotY, plotW, plotH) = ParsePlotAreaRect(svg);

        var labelFont = new Font
        {
            Family = figure.Theme.DefaultFont.Family,
            Size   = figure.Theme.DefaultFont.Size,
        };
        double labelHeight = ChartServices.FontMetrics.Measure("Secondary X", labelFont).Height;
        // The renderer draws the secondary X label with its baseline at PlotArea.Y - 28.
        // Its actual top is (PlotArea.Y - 28 - 0.8 * labelHeight). For fit: plotY >= 28 + 0.8*h + 0.2*h = 28 + h.
        double minRequired = 28 + labelHeight;

        Assert.True(plotY >= minRequired,
            $"Secondary X-axis label clip: MarginTop={plotY:F2} < required {minRequired:F2}");
    }
}
