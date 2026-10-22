// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Builders;
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
                // The secondary X axis is drawn only when it has series; without one there is no label to reserve.
                ax.WithSecondaryXAxis(secondary => secondary.PlotXSecondary([0.0, 100.0], [0.0, 1.0]).SetXLabel("Secondary X"));
            })
            .Build();

        string svg = new Transforms.SvgTransform().Render(figure);
        var label = Texts(svg).Single(t => t.Text == "Secondary X");
        double labelTop = label.Y - ChartServices.FontMetrics.Measure("Secondary X", new Font { Size = label.FontSize }).Height;

        Assert.True(labelTop >= 0, $"Secondary X-axis label clip: its top is at {labelTop:F2} < 0");
    }

    // ── 8. End-of-line label (1.19.0) ──
    //   The label sits a gap past the end of its line, in the legend's font; the engine reserves that gap + width.

    [Fact]
    public void EndLabel_LayoutEngineReserves_EnoughRightMargin()
    {
        var figure = Plt.Create()
            .WithSize(600, 400)
            .TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1.0], [0.0, 1.0], s => s.Label = "Revenue").WithDirectLabel())
            .Build();

        string svg = new Transforms.SvgTransform().Render(figure);
        var (plotX, plotY, plotW, plotH) = ParsePlotAreaRect(svg);

        var font = RendererTickFont(figure.Theme);
        double labelWidth = ChartServices.FontMetrics.Measure("Revenue", font).Width;
        double minRequired = DirectLabelMetrics.Gap + labelWidth;
        double marginRight = 600 - (plotX + plotW);

        Assert.True(marginRight >= minRequired,
            $"End label clip: MarginRight={marginRight:F2} < required {minRequired:F2}");
    }

    // ── 9. Secondary Y tick labels (1.19.0) ──
    //   Drawn left-aligned at Length + Pad past the right spine. Before 1.19.0 the engine charged only the secondary
    //   TITLE, so without one the tick labels ran past the figure edge.

    [Fact]
    public void SecondaryYTickLabels_LayoutEngineReserves_EnoughRightMargin()
    {
        var figure = Plt.Create()
            .WithSize(600, 400)
            .TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([0.0, 1.0], [0.0, 1.0])
                .WithSecondaryYAxis(y2 => y2.Plot([0.0, 1.0], [100_000.0, 900_000.0])))
            .Build();

        string svg = new Transforms.SvgTransform().Render(figure);
        var (plotX, plotY, plotW, plotH) = ParsePlotAreaRect(svg);
        double plotRight = plotX + plotW;

        var font = RendererTickFont(figure.Theme);
        var labels = Regex.Matches(svg, @"<text x=""(?<x>[0-9.]+)""[^>]*text-anchor=""start""[^>]*>(?<t>[^<]+)</text>")
            .Select(m => (X: double.Parse(m.Groups["x"].Value, CultureInfo.InvariantCulture), Text: m.Groups["t"].Value))
            .Where(l => l.X > plotRight)
            .ToList();
        Assert.NotEmpty(labels);
        foreach (var (x, text) in labels)
        {
            double right = x + ChartServices.FontMetrics.Measure(text, font).Width;
            Assert.True(right <= 600, $"Secondary Y tick label '{text}' clip: ends at {right:F2} > 600");
        }
    }

    // ── shared parsing for the drawn text of 10-12 ──

    private readonly record struct DrawnText(double X, double Y, double FontSize, string Anchor, string Text);

    private static List<DrawnText> Texts(string svg) =>
        Regex.Matches(svg, @"<text x=""(?<x>-?[0-9.]+)"" y=""(?<y>-?[0-9.]+)"" font-family=""[^""]*"" font-size=""(?<fs>[0-9.]+)"" text-anchor=""(?<a>[a-z]+)""[^>]*>(?<t>[^<]*)</text>")
            .Select(m => new DrawnText(
                double.Parse(m.Groups["x"].Value, CultureInfo.InvariantCulture),
                double.Parse(m.Groups["y"].Value, CultureInfo.InvariantCulture),
                double.Parse(m.Groups["fs"].Value, CultureInfo.InvariantCulture),
                m.Groups["a"].Value,
                m.Groups["t"].Value))
            .ToList();

    private static FigureBuilder Layout(bool tight) =>
        tight ? Plt.Create().WithSize(600, 400).TightLayout() : Plt.Create().WithSize(600, 400);

    // ── 10. Secondary Y title (1.19.0) ──
    //   The part centres the rotated title at TickConfig.AxisLabelOffset(widest tick label) + LabelStyle.Pad past the
    //   right spine; the engine reserves the same offset plus the title height. The two read one owner, so the title
    //   fits whatever the tick label width or the tick mark length and pad.

    [Theory]
    [InlineData(true, 1.0, 3.5, 3.5)]
    [InlineData(true, 100_000.0, 3.5, 3.5)]
    [InlineData(true, 100_000.0, 14.0, 11.0)]
    [InlineData(true, 0.001, 9.0, 2.0)]
    [InlineData(false, 1.0, 3.5, 3.5)]
    [InlineData(false, 100_000.0, 14.0, 11.0)]
    public void SecondaryYTitle_LayoutReservesItsRightEdge(bool tight, double scale, double length, double pad)
    {
        const string Title = "Secondary title";
        var figure = Layout(tight)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([0.0, 1.0], [0.0, 1.0])
                .WithSecondaryYAxis(y2 => y2.Plot([0.0, 1.0], [1 * scale, 9 * scale]).SetYLabel(Title)))
            .Build();
        var axis = figure.SubPlots[0].SecondaryYAxis!;
        axis.MajorTicks = axis.MajorTicks with { Length = length, Pad = pad };

        string svg = new Transforms.SvgTransform().Render(figure);
        var title = Texts(svg).Single(t => t.Text == Title);
        double height = ChartServices.FontMetrics.Measure(Title, new Font { Size = title.FontSize }).Height;

        Assert.True(title.X + height <= 600,
            $"Secondary Y title clip: its far edge is at {title.X + height:F2} > 600 (tight={tight}, scale={scale}, length={length}, pad={pad})");
    }

    // ── 11. Mirrored X tick labels under the axes title (1.19.0) ──
    //   WithXTicksMirrored draws the X tick labels above the top spine, where the axes title used to sit. The title
    //   rises above them and the top margin holds both.

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MirroredXTicks_TheAxesTitleSitsAboveTheTopTickLabels(bool tight)
    {
        const string Title = "A Title";
        var figure = Layout(tight)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1.0, 2.0], [0.0, 1.0, 2.0]).WithXTicksMirrored().WithTitle(Title))
            .Build();

        string svg = new Transforms.SvgTransform().Render(figure);
        var (_, plotY, _, _) = ParsePlotAreaRect(svg);
        var texts = Texts(svg);
        var title = texts.Single(t => t.Text == Title);
        var topLabels = texts.Where(t => t.Text != Title && t.Y < plotY).ToList();
        Assert.NotEmpty(topLabels);

        double labelTop = topLabels.Min(t => t.Y - ChartServices.FontMetrics.Measure(t.Text, new Font { Size = t.FontSize }).Height);
        double titleBottom = title.Y + 0.2 * title.FontSize;
        double titleTop = title.Y - ChartServices.FontMetrics.Measure(Title, new Font { Size = title.FontSize }).Height;
        Assert.True(titleBottom <= labelTop,
            $"Axes title overlaps the mirrored X tick labels: title bottom {titleBottom:F2} > tick label top {labelTop:F2} (tight={tight})");
        Assert.True(titleTop >= 0, $"Axes title clip: its top is at {titleTop:F2} < 0 (tight={tight})");
    }

    // ── 12. Mirrored Y tick labels (1.19.0) ──
    //   WithYTicksMirrored draws the Y tick labels left-aligned at Length + Pad past the right spine; the right margin
    //   holds the mark, the pad and the widest of them.

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MirroredYTicks_LayoutReservesTheRightTickLabels(bool tight)
    {
        var figure = Layout(tight)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1.0], [100_000.0, 900_000.0]).WithYTicksMirrored())
            .Build();

        string svg = new Transforms.SvgTransform().Render(figure);
        var (plotX, _, plotW, _) = ParsePlotAreaRect(svg);
        double plotRight = plotX + plotW;
        var labels = Texts(svg).Where(t => t.Anchor == "start" && t.X > plotRight).ToList();
        Assert.NotEmpty(labels);

        var major = figure.SubPlots[0].YAxis.MajorTicks;
        double widest = labels.Max(t => ChartServices.FontMetrics.Measure(t.Text, new Font { Size = t.FontSize }).Width);
        double required = major.Length + major.Pad + widest;
        double marginRight = 600 - plotRight;
        Assert.True(marginRight >= required,
            $"Mirrored Y tick label clip: MarginRight={marginRight:F2} < mark + pad + widest label {required:F2} (tight={tight})");
    }

    // ── 13. Secondary X axis under the axes title (1.19.0) ──
    //   The secondary X axis draws its tick labels above the top spine and its own title above them; the axes title
    //   rises above both, the secondary title stands clear of the tick labels, and the top margin holds all of it,
    //   whatever the tick label size, the mark length and pad, or the secondary title's font.

    [Theory]
    [InlineData(true, null, 3.5, 3.5, null, true)]
    [InlineData(true, 20.0, 3.5, 3.5, null, true)]
    [InlineData(true, 9.0, 12.0, 9.0, 18.0, true)]
    [InlineData(true, 28.0, 14.0, 11.0, 30.0, true)]
    [InlineData(true, null, 3.5, 3.5, null, false)]
    [InlineData(false, null, 3.5, 3.5, null, true)]
    [InlineData(false, 20.0, 12.0, 9.0, 18.0, true)]
    [InlineData(false, 28.0, 14.0, 11.0, 30.0, true)]
    [InlineData(false, null, 3.5, 3.5, null, false)]
    public void SecondaryXAxis_TheTitlesClearTheTopTickLabels(bool tight, double? labelSize, double length, double pad,
                                                              double? axisTitleSize, bool withAxisTitle)
    {
        const string Title = "A Title", AxisTitle = "Secondary X";
        var figure = Layout(tight)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([0.0, 1.0, 2.0], [0.0, 1.0, 2.0]).WithTitle(Title)
                .WithSecondaryXAxis(x2 =>
                {
                    x2.PlotXSecondary([1_000.0, 3_000.0, 5_000.0], [0.0, 1.0, 2.0]);
                    if (withAxisTitle) x2.SetXLabel(AxisTitle);
                }))
            .Build();
        var axis = figure.SubPlots[0].SecondaryXAxis!;
        axis.MajorTicks = axis.MajorTicks with { Length = length, Pad = pad, LabelSize = labelSize };
        if (axisTitleSize is { } size) axis.LabelStyle = new TextStyle { FontSize = size };
        string context = $"tight={tight}, labelSize={labelSize}, length={length}, pad={pad}, axisTitleSize={axisTitleSize}";

        string svg = new Transforms.SvgTransform().Render(figure);
        var (_, plotY, _, _) = ParsePlotAreaRect(svg);
        var texts = Texts(svg);
        static double Top(DrawnText t) => t.Y - ChartServices.FontMetrics.Measure(t.Text, new Font { Size = t.FontSize }).Height;
        static double Bottom(DrawnText t) => t.Y + 0.2 * t.FontSize;
        var title = texts.Single(t => t.Text == Title);
        var tickLabels = texts.Where(t => t.Text != Title && t.Text != AxisTitle && t.Y < plotY).ToList();
        Assert.NotEmpty(tickLabels);
        double tickTop = tickLabels.Min(Top);

        Assert.True(Bottom(title) <= tickTop,
            $"Axes title overlaps the secondary X tick labels: title bottom {Bottom(title):F2} > tick label top {tickTop:F2} ({context})");
        Assert.True(Top(title) >= 0, $"Axes title clip: its top is at {Top(title):F2} < 0 ({context})");
        if (!withAxisTitle) return;
        var axisTitle = texts.Single(t => t.Text == AxisTitle);
        Assert.True(Bottom(axisTitle) <= tickTop,
            $"Secondary X title overlaps its tick labels: title bottom {Bottom(axisTitle):F2} > tick label top {tickTop:F2} ({context})");
        Assert.True(Bottom(title) <= Top(axisTitle),
            $"Axes title overlaps the secondary X title: {Bottom(title):F2} > {Top(axisTitle):F2} ({context})");
        Assert.True(Top(axisTitle) >= 0, $"Secondary X title clip: its top is at {Top(axisTitle):F2} < 0 ({context})");
    }

    // ── 14. Category names on the Y axis ──
    //   A horizontal bar chart names its categories on the Y axis, right-aligned at Length + Pad left of the spine. The
    //   left margin (the gutter, for an axes right of another) holds the mark, the pad and the longest name, then the
    //   Y axis title past it, in both layout paths; a name longer than the fixed 120 px cap lifts the cap, as measured
    //   content does on the right edge.

    private static readonly string[] Regions = ["North", "South-east coastal region", "West"];

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    public void CategoryNamesOnTheYAxis_TheLeftMarginHoldsTheLongestName(bool tight, bool withAxisTitle, bool veryLong)
    {
        const string AxisTitle = "Region";
        string[] names = veryLong ? [.. Regions.Append("An exceptionally long category name, wider than any cap")] : Regions;
        var figure = Layout(tight)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Bar(names, [.. names.Select((_, i) => i + 1.0)], s => s.Orientation = MatPlotLibNet.Models.Series.BarOrientation.Horizontal);
                if (withAxisTitle) ax.SetYLabel(AxisTitle);
            })
            .Build();
        string context = $"tight={tight}, withAxisTitle={withAxisTitle}, veryLong={veryLong}";

        string svg = new Transforms.SvgTransform().Render(figure);
        var (plotX, _, _, _) = ParsePlotAreaRect(svg);
        var texts = Texts(svg);
        var drawn = texts.Where(t => names.Contains(t.Text)).ToList();

        Assert.Equal(names.Length, drawn.Count);
        double longest = drawn.Max(t => ChartServices.FontMetrics.Measure(t.Text, new Font { Size = t.FontSize }).Width);
        var major = figure.SubPlots[0].YAxis.MajorTicks;
        Assert.True(plotX >= major.Length + major.Pad + longest,
            $"Category name clip: MarginLeft={plotX:F2} < mark + pad + longest name {major.Length + major.Pad + longest:F2} ({context})");
        Assert.All(drawn, t => Assert.True(t.X - ChartServices.FontMetrics.Measure(t.Text, new Font { Size = t.FontSize }).Width >= 0,
            $"'{t.Text}' starts left of the figure ({context})"));
        if (!withAxisTitle) return;
        var title = texts.Single(t => t.Text == AxisTitle);
        double half = ChartServices.FontMetrics.Measure(AxisTitle, new Font { Size = title.FontSize }).Height / 2;
        Assert.True(title.X - half >= 0, $"Y axis title clip: its left edge is at {title.X - half:F2} < 0 ({context})");
        double nameLeft = drawn.Min(t => t.X - ChartServices.FontMetrics.Measure(t.Text, new Font { Size = t.FontSize }).Width);
        Assert.True(title.X + half <= nameLeft, $"Y axis title overlaps the names: {title.X + half:F2} > {nameLeft:F2} ({context})");
    }

    // ── 14b. Names set as the ticks of the Y axis ──
    //   A verb that names categories on the Y axis (a dot plot, a ridgeline) sets the pair a bar chart's names are
    //   drawn with, a FixedLocator and a CategoryFormatter, as the axis' own ticks. Their width is the names', which no
    //   estimate knows, so the layout measures them the way it measures the names of bars that lie down.

    private static readonly string[] Countries = ["Netherlands", "United Kingdom", "Czech Republic"];

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public void NamesSetAsYTicks_TheLeftMarginHoldsTheLongestName(bool tight, bool withAxisTitle)
    {
        const string AxisTitle = "Country";
        var figure = Layout(tight)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Scatter([3.0, 5.0, 4.0], [0.5, 1.5, 2.5])
                    .SetYTickLocator(new MatPlotLibNet.Rendering.TickLocators.FixedLocator([0.5, 1.5, 2.5]))
                    .SetYTickFormatter(new MatPlotLibNet.Rendering.TickFormatters.CategoryFormatter(Countries));
                if (withAxisTitle) ax.SetYLabel(AxisTitle);
            })
            .Build();
        string context = $"tight={tight}, withAxisTitle={withAxisTitle}";

        string svg = new Transforms.SvgTransform().Render(figure);
        double plotX = ParsePlotAreaX(svg);
        var texts = Texts(svg);
        var drawn = texts.Where(t => Countries.Contains(t.Text)).ToList();

        Assert.Equal(Countries.Length, drawn.Count);
        double longest = drawn.Max(t => ChartServices.FontMetrics.Measure(t.Text, new Font { Size = t.FontSize }).Width);
        var major = figure.SubPlots[0].YAxis.MajorTicks;
        Assert.True(plotX >= major.Length + major.Pad + longest,
            $"Name clip: MarginLeft={plotX:F2} < mark + pad + longest name {major.Length + major.Pad + longest:F2} ({context})");
        Assert.All(drawn, t => Assert.True(t.X - ChartServices.FontMetrics.Measure(t.Text, new Font { Size = t.FontSize }).Width >= 0,
            $"'{t.Text}' starts left of the figure ({context})"));
        if (!withAxisTitle) return;
        var title = texts.Single(t => t.Text == AxisTitle);
        double half = ChartServices.FontMetrics.Measure(AxisTitle, new Font { Size = title.FontSize }).Height / 2;
        Assert.True(title.X - half >= 0, $"Y axis title clip: its left edge is at {title.X - half:F2} < 0 ({context})");
        double nameLeft = drawn.Min(t => t.X - ChartServices.FontMetrics.Measure(t.Text, new Font { Size = t.FontSize }).Width);
        Assert.True(title.X + half <= nameLeft, $"Y axis title overlaps the names: {title.X + half:F2} > {nameLeft:F2} ({context})");
    }

    /// <summary>An axes right of another holds names set as ticks inside the gutter: they end left of its spine and start right
    /// of the neighbour's plot.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NamesSetAsYTicksOfAnInteriorAxes_TheGutterHoldsTheLongestName(bool tight)
    {
        var builder = tight ? Plt.Create().WithSize(900, 400).TightLayout() : Plt.Create().WithSize(900, 400);
        var figure = builder
            .AddSubPlot(1, 2, 1, ax => ax.Plot([0.0, 1.0], [0.0, 1.0]))
            .AddSubPlot(1, 2, 2, ax => ax
                .Scatter([3.0, 5.0, 4.0], [0.5, 1.5, 2.5])
                .SetYTickLocator(new MatPlotLibNet.Rendering.TickLocators.FixedLocator([0.5, 1.5, 2.5]))
                .SetYTickFormatter(new MatPlotLibNet.Rendering.TickFormatters.CategoryFormatter(Countries)))
            .Build();

        var areas = new MatPlotLibNet.Rendering.ChartRenderer().ComputeLayout(figure, new SvgRenderContext()).PlotAreas;
        string svg = new Transforms.SvgTransform().Render(figure);
        var drawn = Texts(svg).Where(t => Countries.Contains(t.Text)).ToList();

        Assert.Equal(Countries.Length, drawn.Count);
        double leftPlotRight = areas[0].X + areas[0].Width;
        Assert.All(drawn, t =>
        {
            double left = t.X - ChartServices.FontMetrics.Measure(t.Text, new Font { Size = t.FontSize }).Width;
            Assert.True(left >= leftPlotRight, $"'{t.Text}' starts at {left:F2}, inside the left plot (ends {leftPlotRight:F2}; tight={tight})");
            Assert.True(t.X <= areas[1].X, $"'{t.Text}' ends past its own spine (tight={tight})");
        });
    }

    // ── 15. Category names on the X axis ──
    //   Upright bars name their categories on the X axis as its tick labels: the mark, the pad and the name below the
    //   spine, the X axis title a 6 px gap past them. The bottom margin holds the names and the title in both layout
    //   paths, whatever the label size or the mark length and pad.

    [Theory]
    [InlineData(true, null, 3.5, 3.5, true)]
    [InlineData(true, 26.0, 12.0, 9.0, true)]
    [InlineData(true, 26.0, 12.0, 9.0, false)]
    [InlineData(false, null, 3.5, 3.5, true)]
    [InlineData(false, 26.0, 12.0, 9.0, true)]
    [InlineData(false, 26.0, 12.0, 9.0, false)]
    public void CategoryNamesOnTheXAxis_TheBottomMarginHoldsThemAndTheTitleClearsThem(bool tight, double? labelSize,
        double length, double pad, bool withAxisTitle)
    {
        const string AxisTitle = "Quarter";
        string[] names = ["Q1", "Q2", "Q3"];
        var figure = Layout(tight)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Bar(names, [1.0, 2.0, 3.0]);
                if (withAxisTitle) ax.SetXLabel(AxisTitle);
            })
            .Build();
        var axis = figure.SubPlots[0].XAxis;
        axis.MajorTicks = axis.MajorTicks with { LabelSize = labelSize, Length = length, Pad = pad };
        string context = $"tight={tight}, labelSize={labelSize}, length={length}, pad={pad}, withAxisTitle={withAxisTitle}";

        string svg = new Transforms.SvgTransform().Render(figure);
        var (_, plotY, _, plotH) = ParsePlotAreaRect(svg);
        var texts = Texts(svg);
        var drawn = texts.Where(t => names.Contains(t.Text)).ToList();
        static double Bottom(DrawnText t) => t.Y + 0.2 * t.FontSize;
        static double Top(DrawnText t) => t.Y - ChartServices.FontMetrics.Measure(t.Text, new Font { Size = t.FontSize }).Height;

        Assert.Equal(names.Length, drawn.Count);
        Assert.All(drawn, t => Assert.True(Top(t) >= plotY + plotH, $"'{t.Text}' reaches above the bottom spine ({context})"));
        Assert.All(drawn, t => Assert.True(Bottom(t) <= 400, $"'{t.Text}' clip: its bottom is at {Bottom(t):F2} > 400 ({context})"));
        if (!withAxisTitle) return;
        var title = texts.Single(t => t.Text == AxisTitle);
        Assert.True(Top(title) >= drawn.Max(Bottom),
            $"X axis title overlaps the names: title top {Top(title):F2} < name bottom {drawn.Max(Bottom):F2} ({context})");
        Assert.True(Bottom(title) <= 400, $"X axis title clip: its bottom is at {Bottom(title):F2} > 400 ({context})");
    }

    /// <summary>An X axis title in a style of its own (a larger font, a pad) stands clear of the names and inside the
    /// figure: the layout charges the title in the style the renderer draws it in.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CategoryNamesOnTheXAxis_AStyledTitleFitsBelowThem(bool tight)
    {
        const string AxisTitle = "Quarter";
        string[] names = ["Q1", "Q2", "Q3"];
        var figure = Layout(tight).AddSubPlot(1, 1, 1, ax => ax.Bar(names, [1.0, 2.0, 3.0]).SetXLabel(AxisTitle)).Build();
        figure.SubPlots[0].XAxis.LabelStyle = new TextStyle { FontSize = 30, Pad = 8 };

        string svg = new Transforms.SvgTransform().Render(figure);
        var texts = Texts(svg);
        var title = texts.Single(t => t.Text == AxisTitle);
        double nameBottom = texts.Where(t => names.Contains(t.Text)).Max(t => t.Y + 0.2 * t.FontSize);
        double titleTop = title.Y - ChartServices.FontMetrics.Measure(AxisTitle, new Font { Size = title.FontSize }).Height;

        Assert.Equal(30, title.FontSize);
        Assert.True(titleTop >= nameBottom, $"the title (top {titleTop:F2}) overlaps the names (bottom {nameBottom:F2}; tight={tight})");
        Assert.True(title.Y + 0.2 * title.FontSize <= 400, $"X axis title clip: its bottom is at {title.Y + 0.2 * title.FontSize:F2} > 400 (tight={tight})");
    }

    // ── 16. The X axis title under a numeric X axis ──
    //   The X tick labels stand the mark, the pad and their height below the bottom spine and the X axis title a 6 px gap
    //   past them (35 px below the spine when no tick label is drawn). The bottom margin holds both in both layout paths,
    //   whatever the figure's height, the theme's margins, the title's style, the tick label size or the axis' scale.

    private const string XTitle = "Time since start (s)";

    public static TheoryData<bool, int, string> XTitleCases()
    {
        var data = new TheoryData<bool, int, string>();
        foreach (bool tight in new[] { false, true })
        foreach (int height in new[] { 300, 350, 400, 500 })
        foreach (string variant in new[] { "plain", "styled", "ticks", "hidden", "dates", "log", "ggplot" })
            data.Add(tight, height, variant);
        return data;
    }

    private static Figure XTitled(bool tight, int height, string variant)
    {
        var builder = Plt.Create().WithSize(500, height);
        if (tight) builder = builder.TightLayout();
        if (variant == "ggplot") builder = builder.WithTheme(Theme.Ggplot);
        var figure = builder.AddSubPlot(1, 1, 1, ax =>
        {
            if (variant == "dates")
                ax.Plot([.. Enumerable.Range(0, 4).Select(i => new DateTime(2024, 1, 1).AddDays(i * 60).ToOADate())], [1.0, 3.0, 2.0, 4.0])
                  .SetXDateFormat("yyyy-MM");
            else if (variant == "log") ax.Plot([1.0, 10.0, 100.0, 1000.0], [1.0, 3.0, 2.0, 4.0]).SetXScale(AxisScale.Log);
            else ax.Plot([0.0, 1.0, 2.0, 3.0], [1.0, 3.0, 2.0, 4.0]);
            ax.SetXLabel(XTitle);
        }).Build();
        var axis = figure.SubPlots[0].XAxis;
        if (variant == "styled") axis.LabelStyle = new TextStyle { FontSize = 24, Pad = 8 };
        if (variant == "ticks") axis.MajorTicks = axis.MajorTicks with { LabelSize = 20, Length = 9, Pad = 6 };
        if (variant == "hidden") axis.MajorTicks = axis.MajorTicks with { Visible = false };
        return figure;
    }

    [Theory]
    [MemberData(nameof(XTitleCases))]
    public void NumericXAxis_TheXTitleStaysInsideTheFigure_AndClearsTheTickLabels(bool tight, int height, string variant)
    {
        var figure = XTitled(tight, height, variant);
        string context = $"tight={tight}, height={height}, variant={variant}";

        string svg = new Transforms.SvgTransform().Render(figure);
        var (_, plotY, _, plotH) = ParsePlotAreaRect(svg);
        var texts = Texts(svg);
        var title = texts.Single(t => t.Text == XTitle);
        double titleBottom = title.Y + 0.2 * title.FontSize;
        double titleTop = title.Y - ChartServices.FontMetrics.Measure(XTitle, new Font { Size = title.FontSize }).Height;
        var tickLabels = texts.Where(t => t.Text != XTitle && t.Y > plotY + plotH && t.Y < title.Y).ToList();

        Assert.True(titleBottom <= height, $"X axis title clip: its bottom is at {titleBottom:F2} > {height} ({context})");
        if (variant == "hidden") { Assert.Empty(tickLabels); return; }
        Assert.NotEmpty(tickLabels);
        double tickBottom = tickLabels.Max(t => t.Y + 0.2 * t.FontSize);
        Assert.True(titleTop >= tickBottom, $"X axis title overlaps the tick labels: title top {titleTop:F2} < label bottom {tickBottom:F2} ({context})");
    }

    /// <summary>An axes in an upper row keeps its X tick labels and its X axis title clear of the title of the axes below
    /// it: the row gap holds the upper row's bottom need and the lower row's title, in both layout paths.</summary>
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void NumericXAxisInAnUpperRow_TheLowerTitleClearsItsTickLabelsAndItsTitle(bool tight, bool withXTitle)
    {
        const string Lower = "Lower panel";
        var builder = tight ? Plt.Create().WithSize(500, 600).TightLayout() : Plt.Create().WithSize(500, 600);
        var figure = builder
            .AddSubPlot(2, 1, 1, ax =>
            {
                ax.Plot([0.0, 1.0, 2.0], [1.0, 3.0, 2.0]);
                if (withXTitle) ax.SetXLabel(XTitle);
            })
            .AddSubPlot(2, 1, 2, ax => ax.Plot([0.0, 1.0, 2.0], [1.0, 3.0, 2.0]).WithTitle(Lower))
            .Build();
        string context = $"tight={tight}, withXTitle={withXTitle}";

        var areas = new MatPlotLibNet.Rendering.ChartRenderer().ComputeLayout(figure, new SvgRenderContext()).PlotAreas;
        string svg = new Transforms.SvgTransform().Render(figure);
        var texts = Texts(svg);
        double upperBottom = areas[0].Y + areas[0].Height;
        var lowerTitle = texts.Single(t => t.Text == Lower);
        double lowerTitleTop = lowerTitle.Y - ChartServices.FontMetrics.Measure(Lower, new Font { Size = lowerTitle.FontSize }).Height;
        var upperBelow = texts.Where(t => t.Y > upperBottom && t.Y < areas[1].Y).Where(t => t.Text != Lower).ToList();

        Assert.NotEmpty(upperBelow);
        double lowest = upperBelow.Max(t => t.Y + 0.2 * t.FontSize);
        Assert.True(lowest <= lowerTitleTop,
            $"the lower title (top {lowerTitleTop:F2}) overlaps what the upper row draws below its spine (bottom {lowest:F2}; {context})");
    }

    // ── 17. Turned X tick labels ──
    //   X tick labels that would run into each other turn 30 degrees (numbers, dates and category names alike), and a set
    //   rotation turns them by its angle. A turned label hangs from its end below the axis (from its start above it,
    //   mirrored), so it reaches its turned width and its turned descent below its baseline. The bottom margin holds the
    //   turned labels and the X axis title stands clear below them, in both layout paths; turned mirrored labels stand
    //   under the axes title. The cases are the ones measured when the labels were still measured upright: 24 names in
    //   600 x 400, 5 candlestick dates in 400 x 300, 40 numeric dates in 500 x 350, and a set 90-degree turn.

    private const string TurnedTitle = "Turned axis title";

    /// <summary>A drawn text with its turn: <see cref="Rotation"/> in degrees counter-clockwise, as the axis was given it.</summary>
    private readonly record struct TurnedText(double X, double Y, double FontSize, string Anchor, double Rotation, string Text);

    private static List<TurnedText> TurnedTexts(string svg) =>
        Regex.Matches(svg, @"<text x=""(?<x>-?[0-9.]+)"" y=""(?<y>-?[0-9.]+)"" font-family=""[^""]*"" font-size=""(?<fs>[0-9.]+)"" text-anchor=""(?<a>[a-z]+)""(?: transform=""rotate\((?<r>-?[0-9.]+),[^)]*\)"")?[^>]*>(?<t>[^<]*)</text>")
            .Select(m => new TurnedText(
                double.Parse(m.Groups["x"].Value, CultureInfo.InvariantCulture),
                double.Parse(m.Groups["y"].Value, CultureInfo.InvariantCulture),
                double.Parse(m.Groups["fs"].Value, CultureInfo.InvariantCulture),
                m.Groups["a"].Value,
                m.Groups["r"].Success ? -double.Parse(m.Groups["r"].Value, CultureInfo.InvariantCulture) : 0,
                m.Groups["t"].Value))
            .ToList();

    /// <summary>The lowest (<paramref name="lowest"/>) or highest point of a drawn text: its box runs along the baseline
    /// from the anchor by its width (to the left of an end anchor, both ways from a middle one), from its measured height
    /// above the baseline to a fifth of its size below it (the drift tests' top and bottom), turned counter-clockwise
    /// about the anchor by its rotation.</summary>
    private static double Extreme(TurnedText t, bool lowest)
    {
        double width = ChartServices.FontMetrics.Measure(t.Text, new Font { Size = t.FontSize }).Width;
        double height = ChartServices.FontMetrics.Measure(t.Text, new Font { Size = t.FontSize }).Height;
        double start = t.Anchor switch { "end" => -width, "middle" => -width / 2, _ => 0 };
        double radians = t.Rotation * Math.PI / 180, sin = Math.Sin(radians), cos = Math.Cos(radians);
        var downs = from x in new[] { start, start + width }
                    from y in new[] { -height, 0.2 * t.FontSize }
                    select -x * sin + y * cos;
        return t.Y + (lowest ? downs.Max() : downs.Min());
    }

    public static TheoryData<string, bool> TurnedCases()
    {
        var data = new TheoryData<string, bool>();
        foreach (bool tight in new[] { false, true })
        foreach (string variant in new[] { "names", "candles", "dates", "rotation90" })
            data.Add(variant, tight);
        return data;
    }

    private static readonly string[] ManyNames = [.. Enumerable.Range(0, 24).Select(i => "Category " + i)];

    private static Figure Turned(string variant, bool tight, bool withTitle = true, bool mirrored = false)
    {
        var (width, height) = variant switch { "names" => (600, 400), "candles" => (400, 300), _ => (500, 350) };
        var builder = Plt.Create().WithSize(width, height);
        if (tight) builder = builder.TightLayout();
        return builder.AddSubPlot(1, 1, 1, ax =>
        {
            switch (variant)
            {
                case "names": ax.Bar(ManyNames, [.. ManyNames.Select((_, i) => (double)i)]); break;
                case "candles":
                    ax.Candlestick([10.0, 11, 12, 11, 13], [15.0, 14, 16, 15, 16], [8.0, 9, 10, 9, 10], [12.0, 10, 15, 12, 14],
                        ["2024-01-01", "2024-01-02", "2024-01-03", "2024-01-04", "2024-01-05"]);
                    break;
                case "dates":
                    ax.Plot([.. Enumerable.Range(0, 40).Select(i => new DateTime(2024, 1, 1).AddDays(i * 9).ToOADate())],
                        [.. Enumerable.Range(0, 40).Select(i => (double)(i % 7))]).SetXDateFormat("yyyy-MM-dd");
                    break;
                default: ax.Plot([0.0, 10000, 20000, 30000], [1.0, 2, 3, 4]).WithXTickLabelRotation(90); break;
            }
            if (withTitle) ax.SetXLabel(TurnedTitle);
            if (mirrored) ax.WithXTicksMirrored().WithTitle("A Title");
        }).Build();
    }

    [Theory]
    [MemberData(nameof(TurnedCases))]
    public void TurnedXTickLabels_StayInsideTheFigure_AndTheXTitleClearsThem(string variant, bool tight)
    {
        var figure = Turned(variant, tight);
        string context = $"variant={variant}, tight={tight}";

        string svg = new Transforms.SvgTransform().Render(figure);
        var (plotX, plotY, plotW, plotH) = ParsePlotAreaRect(svg);
        var texts = TurnedTexts(svg);
        var title = texts.Single(t => t.Text == TurnedTitle);
        var labels = texts.Where(t => t.Text != TurnedTitle && t.Y > plotY + plotH && t.X >= plotX - 1e-9 && t.X <= plotX + plotW + 1e-9).ToList();

        Assert.Contains(labels, t => t.Rotation != 0);
        double lowest = labels.Max(t => Extreme(t, lowest: true));
        Assert.True(lowest <= figure.Height, $"turned X tick labels clip: they reach {lowest:F2} > {figure.Height} ({context})");
        double titleTop = Extreme(title, lowest: false);
        Assert.True(titleTop >= lowest, $"X axis title overlaps the turned tick labels: title top {titleTop:F2} < label bottom {lowest:F2} ({context})");
        Assert.True(Extreme(title, lowest: true) <= figure.Height,
            $"X axis title clip: its bottom is at {Extreme(title, lowest: true):F2} > {figure.Height} ({context})");
    }

    [Theory]
    [InlineData("dates", false)]
    [InlineData("dates", true)]
    [InlineData("rotation90", false)]
    [InlineData("rotation90", true)]
    public void TurnedMirroredXTickLabels_TheAxesTitleSitsAboveThem_AndBothStayInsideTheFigure(string variant, bool tight)
    {
        var figure = Turned(variant, tight, withTitle: false, mirrored: true);
        string context = $"variant={variant}, tight={tight}";

        string svg = new Transforms.SvgTransform().Render(figure);
        var (plotX, plotY, plotW, plotH) = ParsePlotAreaRect(svg);
        var texts = TurnedTexts(svg);
        var title = texts.Single(t => t.Text == "A Title");
        var top = texts.Where(t => t.Text != "A Title" && t.Y < plotY && t.X >= plotX - 1e-9 && t.X <= plotX + plotW + 1e-9).ToList();
        var bottom = texts.Where(t => t.Y > plotY + plotH && t.X >= plotX - 1e-9 && t.X <= plotX + plotW + 1e-9).ToList();

        Assert.Contains(top, t => t.Rotation != 0);
        double highest = top.Min(t => Extreme(t, lowest: false));
        Assert.True(highest >= 0, $"turned mirrored labels clip: they reach {highest:F2} < 0 ({context})");
        Assert.True(Extreme(title, lowest: true) <= highest,
            $"axes title overlaps the turned mirrored labels: title bottom {Extreme(title, lowest: true):F2} > label top {highest:F2} ({context})");
        Assert.True(Extreme(title, lowest: false) >= 0, $"axes title clip: its top is at {Extreme(title, lowest: false):F2} < 0 ({context})");
        double lowest = bottom.Max(t => Extreme(t, lowest: true));
        Assert.True(lowest <= figure.Height, $"turned X tick labels clip: they reach {lowest:F2} > {figure.Height} ({context})");
    }

    /// <summary>Labels turn by the width of their own plot, and the layout reserves them by it: six names stand upright
    /// under a plot as wide as an 800 px figure allows, but turn under one of its two columns, and both layout paths hold
    /// them turned, with the X title clear below them.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TurnedXTickLabelsInOneOfTwoColumns_AreReservedAtTheWidthOfTheirOwnPlot(bool tight)
    {
        string[] names = [.. Enumerable.Range(0, 6).Select(i => "Category " + i)];
        double[] values = [.. names.Select((_, i) => i + 1.0)];
        FigureBuilder Sized() => tight ? Plt.Create().WithSize(800, 350).TightLayout() : Plt.Create().WithSize(800, 350);
        var alone = Sized().AddSubPlot(1, 1, 1, ax => ax.Bar(names, values).SetXLabel(TurnedTitle)).Build();
        var figure = Sized()
            .AddSubPlot(1, 2, 1, ax => ax.Bar(names, values).SetXLabel(TurnedTitle))
            .AddSubPlot(1, 2, 2, ax => ax.Plot([0.0, 1.0, 2.0], [1.0, 3.0, 2.0]))
            .Build();

        Assert.All(TurnedTexts(new Transforms.SvgTransform().Render(alone)).Where(t => names.Contains(t.Text)),
            t => Assert.Equal(0.0, t.Rotation));
        var texts = TurnedTexts(new Transforms.SvgTransform().Render(figure));
        var turned = texts.Where(t => names.Contains(t.Text)).ToList();
        var title = texts.Single(t => t.Text == TurnedTitle);

        Assert.All(turned, t => Assert.Equal(30.0, t.Rotation));
        double lowest = turned.Max(t => Extreme(t, lowest: true));
        Assert.True(lowest <= figure.Height, $"turned X tick labels clip: they reach {lowest:F2} > {figure.Height} (tight={tight})");
        Assert.True(Extreme(title, lowest: false) >= lowest,
            $"X axis title overlaps the turned tick labels: title top {Extreme(title, lowest: false):F2} < label bottom {lowest:F2} (tight={tight})");
        Assert.True(Extreme(title, lowest: true) <= figure.Height,
            $"X axis title clip: its bottom is at {Extreme(title, lowest: true):F2} > {figure.Height} (tight={tight})");
    }

    /// <summary>Turned X tick labels in an upper row stand clear of the title of the axes below: the row gap holds them,
    /// turned, and the lower title.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TurnedXTickLabelsInAnUpperRow_TheLowerTitleClearsThem(bool tight)
    {
        const string Lower = "Lower panel";
        var builder = tight ? Plt.Create().WithSize(600, 600).TightLayout() : Plt.Create().WithSize(600, 600);
        var figure = builder
            .AddSubPlot(2, 1, 1, ax => ax.Bar(ManyNames, [.. ManyNames.Select((_, i) => (double)i)]))
            .AddSubPlot(2, 1, 2, ax => ax.Plot([0.0, 1.0, 2.0], [1.0, 3.0, 2.0]).WithTitle(Lower))
            .Build();

        var areas = new MatPlotLibNet.Rendering.ChartRenderer().ComputeLayout(figure, new SvgRenderContext()).PlotAreas;
        string svg = new Transforms.SvgTransform().Render(figure);
        var texts = TurnedTexts(svg);
        double upperBottom = areas[0].Y + areas[0].Height;
        var lowerTitle = texts.Single(t => t.Text == Lower);
        var turned = texts.Where(t => ManyNames.Contains(t.Text)).ToList();

        Assert.Contains(turned, t => t.Rotation != 0);
        Assert.All(turned, t => Assert.True(t.Y > upperBottom, $"'{t.Text}' is not below the upper plot (tight={tight})"));
        double lowest = turned.Max(t => Extreme(t, lowest: true));
        Assert.True(lowest <= Extreme(lowerTitle, lowest: false),
            $"the lower title (top {Extreme(lowerTitle, lowest: false):F2}) overlaps the upper row's turned labels (bottom {lowest:F2}; tight={tight})");
    }

    /// <summary>An axes right of another names its categories inside the gutter: the names end left of its spine and start
    /// right of the neighbour's plot.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CategoryNamesOnTheYAxisOfAnInteriorAxes_TheGutterHoldsTheLongestName(bool tight)
    {
        var builder = tight ? Plt.Create().WithSize(900, 400).TightLayout() : Plt.Create().WithSize(900, 400);
        var figure = builder
            .AddSubPlot(1, 2, 1, ax => ax.Plot([0.0, 1.0], [0.0, 1.0]))
            .AddSubPlot(1, 2, 2, ax => ax.Bar(Regions, [1.0, 2.0, 3.0], s => s.Orientation = MatPlotLibNet.Models.Series.BarOrientation.Horizontal))
            .Build();

        var areas = new MatPlotLibNet.Rendering.ChartRenderer().ComputeLayout(figure, new SvgRenderContext()).PlotAreas;
        string svg = new Transforms.SvgTransform().Render(figure);
        var drawn = Texts(svg).Where(t => Regions.Contains(t.Text)).ToList();

        Assert.Equal(Regions.Length, drawn.Count);
        double leftPlotRight = areas[0].X + areas[0].Width;
        Assert.All(drawn, t =>
        {
            double left = t.X - ChartServices.FontMetrics.Measure(t.Text, new Font { Size = t.FontSize }).Width;
            Assert.True(left >= leftPlotRight, $"'{t.Text}' starts at {left:F2}, inside the left plot (ends {leftPlotRight:F2}; tight={tight})");
            Assert.True(t.X <= areas[1].X, $"'{t.Text}' ends past its own spine (tight={tight})");
        });
    }
}
