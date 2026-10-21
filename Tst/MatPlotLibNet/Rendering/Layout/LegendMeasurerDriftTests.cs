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
}
