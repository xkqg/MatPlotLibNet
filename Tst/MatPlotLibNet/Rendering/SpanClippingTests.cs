// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A shaded span (<c>AxHSpan</c>, <c>AxVSpan</c>, and the zone of a <c>Threshold</c>) is clipped to the plot
/// area, as matplotlib's <c>axhspan</c> is: an edge beyond the drawn range runs to the plot edge, and a span wholly beyond
/// it draws nothing. Before, a threshold's zone (a span to ±<see cref="double.MaxValue"/>) put <c>y="-Infinity"
/// height="Infinity"</c> into the SVG, which a browser does not draw, and a finite span wider than the range was drawn
/// over the margins and the title. These tests read the geometry out of the SVG, not pixels.</summary>
public class SpanClippingTests
{
    private readonly record struct SvgRect(double X, double Y, double Width, double Height, string Fill, string? Opacity)
    {
        public double Bottom => Y + Height;
        public double Right => X + Width;
    }

    private readonly record struct SvgLine(double X1, double Y1, double X2, double Y2, string Stroke, bool Dashed);

    private static List<SvgRect> Rects(string svg) =>
        [.. svg.Elements("rect").Where(rect => rect["fill"] is not null)
            .Select(rect => new SvgRect(rect.Number("x"), rect.Number("y"), rect.Number("width"), rect.Number("height"), rect["fill"]!, rect["fill-opacity"]))];

    private static List<SvgLine> Lines(string svg) =>
        [.. svg.Elements("line").Where(line => line["stroke"] is not null && line["stroke-width"] is not null)
            .Select(line => new SvgLine(line.Number("x1"), line.Number("y1"), line.Number("x2"), line.Number("y2"), line["stroke"]!, line["stroke-dasharray"] is not null))];

    private const string Red = "#FF0000";

    /// <summary>The rectangle a red span with the default transparency writes.</summary>
    private static List<SvgRect> RedZones(string svg) => [.. Rects(svg).Where(r => r.Fill == Red && r.Opacity == "0.2")];

    private static readonly double[] X = Enumerable.Range(0, 10).Select(i => (double)i).ToArray();
    private static readonly double[] Y = [10, 25, 40, 55, 70, 85, 100, 90, 60, 30];

    private static string Svg(Action<AxesBuilder> configure, int? ylim = null) =>
        Plt.Create().WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot(X, Y);
                if (ylim is { } top) ax.SetYLim(0, top);
                configure(ax);
            })
            .ToSvg();

    // ---- the threshold's zone ---------------------------------------------------------------------------------------

    /// <summary>The zone above a threshold runs from the threshold's line to the top of the plot.</summary>
    [Fact]
    public void AThresholdsZoneAbove_RunsFromTheLineToThePlotTop()
    {
        string svg = Svg(ax => ax.Threshold(80, Orientation.Horizontal, ThresholdBreach.Above, Colors.Red));

        var zone = Assert.Single(RedZones(svg));
        var plot = svg.PlotArea();
        var line = Assert.Single(Lines(svg), l => l.Stroke == Red && l.Dashed);
        Assert.Equal(plot.Y, zone.Y, 0.02);
        Assert.Equal(line.Y1, zone.Bottom, 0.02);
        Assert.Equal(plot.X, zone.X, 0.02);
        Assert.Equal(plot.Width, zone.Width, 0.02);
    }

    [Fact]
    public void AThresholdsZoneBelow_RunsFromTheLineToThePlotBottom()
    {
        string svg = Svg(ax => ax.Threshold(30, Orientation.Horizontal, ThresholdBreach.Below, Colors.Red));

        var zone = Assert.Single(RedZones(svg));
        var plot = svg.PlotArea();
        var line = Assert.Single(Lines(svg), l => l.Stroke == Red && l.Dashed);
        Assert.Equal(line.Y1, zone.Y, 0.02);
        Assert.Equal(plot.Bottom, zone.Bottom, 0.02);
    }

    [Fact]
    public void AVerticalThresholdsZone_RunsToThePlotEdgeToo()
    {
        string right = Svg(ax => ax.Threshold(6, Orientation.Vertical, ThresholdBreach.Above, Colors.Red));
        string left = Svg(ax => ax.Threshold(3, Orientation.Vertical, ThresholdBreach.Below, Colors.Red));

        var zoneRight = Assert.Single(RedZones(right));
        var plotRight = right.PlotArea();
        Assert.Equal(Assert.Single(Lines(right), l => l.Stroke == Red && l.Dashed).X1, zoneRight.X, 0.02);
        Assert.Equal(plotRight.Right, zoneRight.Right, 0.02);
        Assert.Equal(plotRight.Y, zoneRight.Y, 0.02);
        Assert.Equal(plotRight.Height, zoneRight.Height, 0.02);

        var zoneLeft = Assert.Single(RedZones(left));
        var plotLeft = left.PlotArea();
        Assert.Equal(plotLeft.X, zoneLeft.X, 0.02);
        Assert.Equal(Assert.Single(Lines(left), l => l.Stroke == Red && l.Dashed).X1, zoneLeft.Right, 0.02);
    }

    /// <summary>The zone of the same threshold under explicit limits: the same geometry, worked out from the limits.</summary>
    [Fact]
    public void AThresholdsZone_UnderExplicitLimits_EndsAtTheTopOfThePlot()
    {
        string svg = Svg(ax => ax.Threshold(80, Orientation.Horizontal, ThresholdBreach.Above, Colors.Red), ylim: 100);

        var zone = Assert.Single(RedZones(svg));
        var plot = svg.PlotArea();
        Assert.Equal(plot.Y, zone.Y, 0.02);
        Assert.Equal(plot.Y + plot.Height * 0.2, zone.Bottom, 0.02);
    }

    /// <summary>A threshold above everything the chart shows has no zone in the plot: nothing is drawn for it (the line
    /// is the reference line's own business).</summary>
    [Fact]
    public void AThresholdBeyondTheRange_HasNoZoneInThePlot()
    {
        string svg = Svg(ax => ax.Threshold(5000, Orientation.Horizontal, ThresholdBreach.Above, Colors.Red), ylim: 100);

        Assert.Empty(RedZones(svg));
        svg.AssertNoNonFiniteNumber();
    }

    // ---- a finite span ----------------------------------------------------------------------------------------------

    /// <summary>A finite span wider than the range set on its axis used to be drawn over the margins, the title and the axis labels;
    /// it is the plot area now. (An axis that scales itself is ranged by the span under the matplotlib themes, as <c>axhspan</c>
    /// ranges it: <see cref="OverlayAutoscaleTests"/>.)</summary>
    [Fact]
    public void AFiniteSpanWiderThanTheRange_IsTheWholePlot()
    {
        string svg = Svg(ax => ax.AxHSpan(-1000, 1000, s => s.Color = Colors.Red), ylim: 100);

        var zone = Assert.Single(RedZones(svg));
        var plot = svg.PlotArea();
        Assert.Equal(plot.X, zone.X, 0.02);
        Assert.Equal(plot.Y, zone.Y, 0.02);
        Assert.Equal(plot.Width, zone.Width, 0.02);
        Assert.Equal(plot.Height, zone.Height, 0.02);
    }

    [Fact]
    public void AFiniteSpanAcrossTheEdge_IsCutAtThePlotEdge()
    {
        string svg = Svg(ax => ax.AxHSpan(50, 1000, s => s.Color = Colors.Red), ylim: 100);

        var zone = Assert.Single(RedZones(svg));
        var plot = svg.PlotArea();
        Assert.Equal(plot.Y, zone.Y, 0.02);
        Assert.Equal(plot.Y + plot.Height * 0.5, zone.Bottom, 0.02);
    }

    [Fact]
    public void AFiniteSpanBeyondTheRange_DrawsNothing()
    {
        string svg = Svg(ax => ax.AxHSpan(500, 600, s => s.Color = Colors.Red).AxVSpan(-90, -50, s => s.Color = Colors.Red).SetXLim(0, 9), ylim: 100);

        Assert.Empty(RedZones(svg));
    }

    /// <summary>A span inside the range is where its two edges stand: the same rectangle as before.</summary>
    [Fact]
    public void AFiniteSpanInsideTheRange_IsBetweenItsEdges()
    {
        string svg = Svg(ax => ax.AxHSpan(20, 40, s => s.Color = Colors.Red)
            .AxHLine(20, l => l.Color = Colors.Green).AxHLine(40, l => l.Color = Colors.Blue), ylim: 100);

        var zone = Assert.Single(RedZones(svg));
        var plot = svg.PlotArea();
        double lower = Assert.Single(Lines(svg), l => l.Stroke == Colors.Green.ToHex()).Y1;
        double upper = Assert.Single(Lines(svg), l => l.Stroke == Colors.Blue.ToHex()).Y1;
        Assert.Equal(upper, zone.Y, 0.02);
        Assert.Equal(lower, zone.Bottom, 0.02);
        Assert.Equal(plot.X, zone.X, 0.02);
        Assert.Equal(plot.Width, zone.Width, 0.02);
    }

    /// <summary>On a reversed axis the rectangle is the same area with a positive height (it used to be written with a
    /// negative one, which a browser does not draw).</summary>
    [Fact]
    public void ASpanOnAReversedAxis_HasAPositiveHeight()
    {
        string svg = Plt.Create().WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y).SetYLim(100, 0).AxHSpan(20, 40, s => s.Color = Colors.Red))
            .ToSvg();

        var zone = Assert.Single(RedZones(svg));
        var plot = svg.PlotArea();
        Assert.True(zone.Height > 0, $"height {zone.Height}");
        Assert.Equal(plot.Y + plot.Height * 0.2, zone.Y, 0.02);
        Assert.Equal(plot.Y + plot.Height * 0.4, zone.Bottom, 0.02);
    }

    // ---- the seam every head draws through ---------------------------------------------------------------------------

    /// <summary>Every head draws through <see cref="MatPlotLibNet.Rendering.IRenderContext"/> (SVG, Skia, the native
    /// controls, MAUI's canvas): the one owner that places a span hands each of them the same rectangle, a finite one
    /// inside the figure, never one that runs to infinity.</summary>
    [Fact]
    public void ARenderContextIsHandedTheCutZone_NeverAnInfiniteRectangle()
    {
        var figure = Plt.Create().WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y).Threshold(80, Orientation.Horizontal, ThresholdBreach.Above, Colors.Red))
            .Build();
        var ctx = new RecordingRenderContext();

        ChartRendererDefault.Current.Render(figure, ctx);

        var zone = (Rect)Assert.Single(ctx.Of<DrawRectangleCall>(),
            c => Equals(c.Fill, Colors.Red.WithAlpha(51))).Rect;
        zone.AssertFinite("the zone");
        Assert.True(zone.Y >= 0 && zone.Bottom <= 400 && zone.Height > 0, $"{zone}");
    }

    // ---- the ops dashboard sample -----------------------------------------------------------------------------------

    /// <summary>The ops dashboard sample (<c>ops_dashboard_full</c>): its CPU chart carries a threshold at 80 with the
    /// zone above it in red. The picture showed no zone, because the zone's rectangle was written at infinity.</summary>
    [Fact]
    public void TheOpsDashboardSample_DrawsItsAlarmZone()
    {
        var serviceHistory = new StateSegment[]
        {
            new(0, 6, "Up", Colors.Tab10Green),
            new(6, 9, "Degraded", Colors.Tab10Orange),
            new(9, 14, "Up", Colors.Tab10Green),
            new(14, 15, "Down", Colors.Red),
            new(15, 24, "Up", Colors.Tab10Green),
        };
        double[] hours = Enumerable.Range(0, 48).Select(i => i * 0.5).ToArray();
        double[] load = hours.Select(h => 55 + 15 * Math.Sin(h * 0.4) + (h > 20 ? 20 : 0)).ToArray();

        string svg = Plt.Create()
            .WithTitle("Ops Dashboard")
            .WithTheme(Theme.Dark)
            .WithSize(1000, 750)
            .WithGridSpec(3, 3, heightRatios: [1.0, 1.0, 1.6])
            .AddSubPlot(GridPosition.Single(0, 0), ax => ax.StatTile(12, t => { t.Label = "Participants"; t.Format = "0"; }))
            .AddSubPlot(GridPosition.Single(0, 1), ax => ax.StatTile(1, t => { t.Label = "Alerts"; t.AccentColor = Colors.Red; t.Format = "0"; }))
            .AddSubPlot(GridPosition.Single(0, 2), ax => ax.StatTile(99.4, t => { t.Label = "Uptime %"; t.Format = "0.0"; }))
            .AddSubPlot(new GridPosition(1, 2, 0, 3), ax => ax.StateTimeline(serviceHistory, s => s.Label = "API service"))
            .AddSubPlot(new GridPosition(2, 3, 0, 3), ax => ax
                .Plot(hours, load, s => s.Label = "CPU load %")
                .Threshold(80.0, Orientation.Horizontal, ThresholdBreach.Above, color: Colors.Red, label: "Alarm")
                .WithLegend().WithLegendValues())
            .TightLayout()
            .ToSvg();

        var zone = Assert.Single(RedZones(svg));
        var line = Assert.Single(Lines(svg), l => l.Stroke == Red && l.Dashed);
        var plot = Rects(svg).Single(r => r.Fill == Theme.Dark.AxesBackground.ToHex() && r.Opacity is null
                                          && Math.Abs(r.X - zone.X) < 0.02 && r.Y <= zone.Y + 0.02 && r.Bottom >= zone.Bottom - 0.02);
        Assert.Equal(plot.Y, zone.Y, 0.02);
        Assert.Equal(line.Y1, zone.Bottom, 0.02);
        Assert.Equal(plot.Width, zone.Width, 0.02);
        Assert.True(zone.Height > 0 && zone.Bottom <= plot.Bottom, $"zone {zone}");
        svg.AssertNoNonFiniteNumber();
    }
}
