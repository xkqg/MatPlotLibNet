// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;
using MatPlotLibNet.Builders;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>On an axis with breaks, hover, brush select, the data cursor, wheel zoom, pan, Home and the other viewers'
/// pointers work in the units of the data. The view carries the breaks as they are cut out of the drawn space
/// (<c>data-xbreaks</c> / <c>data-ybreaks</c>: the start, the end, the place it is cut and the gap of each), and the script
/// undoes them by the rule <see cref="DataTransform.PixelToData"/> follows; it used to undo only the scale, so a pointer past a
/// break read a value smaller by the gap below it. The pointer is placed on the pixel the renderer drew a known point at, so
/// the expected value is the data itself. Round trips apply what the page sends and render again.</summary>
public class BrokenAxisInteractionTests
{
    private static readonly double[] X = [0.0, 20.0, 100.0, 150.0];
    private static readonly double[] Y = [1.0, 10.0, 5000.0, 1e5];

    // The library's own theme, which cuts a break to nothing; FigureInParts draws the parts a room apart, as the matplotlib themes do.
    private static Action<FigureBuilder> Figure(Func<AxesBuilder, AxesBuilder> axes, Action<ServerInteractionBuilder>? server = null) => b => b
        .WithSize(600, 500)
        .WithTheme(Theme.Default)
        .WithServerInteraction("c", server ?? (s => s.All()))
        .AddSubPlot(1, 1, 1, ax => axes(ax.Plot(X, Y, s => s.Label = "data")));

    private static Action<FigureBuilder> FigureInParts(Func<AxesBuilder, AxesBuilder> axes) => b => b
        .WithSize(600, 500)
        .WithTheme(Theme.MatplotlibV2)
        .WithServerInteraction("c", s => s.All())
        .AddSubPlot(1, 1, 1, ax => axes(ax.Plot(X, Y, s => s.Label = "data")));
    private static Func<AxesBuilder, AxesBuilder> BrokenX => ax => ax.WithXBreak(30, 80);
    private static Func<AxesBuilder, AxesBuilder> BrokenLogY => ax => ax.SetYScale(AxisScale.Log).WithYBreak(100, 1000);
    // ── the view carries the breaks; an axes without breaks carries nothing new ──

    [Fact]
    public void TheView_OfAnAxesWithBreaks_CarriesEachBreakAsItIsCut_AndTheSeriesGroupToo()
    {
        // The library's own theme cuts a break to nothing; the matplotlib themes stand the parts a room apart (the next test).
        var svg = Plt.Create().WithTheme(Theme.Default).WithServerInteraction("c", s => s.All())
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 150], [1.0, 1e4]).SetXLim(0, 150).SetYLim(1, 1e4)
                .WithXBreak(100, 120).WithXBreak(30, 80).SetYScale(AxisScale.Log).WithYBreak(10, 100)).ToSvg();

        var mapped = svg.Each("g").Where(e => e.Attribute("data-xmin") is not null).ToList();
        Assert.Equal(2, mapped.Count); // the axes view and the series group
        Assert.All(mapped, e =>
        {
            // from, to, where it is cut, gap; in the order the breaks begin
            Assert.Equal("30 80 30 50 100 120 50 20", (string?)e.Attribute("data-xbreaks"));
            Assert.Equal("10 100 1 1", (string?)e.Attribute("data-ybreaks"));
        });
    }

    [Fact]
    public void TheView_OfAnAxesDrawnInParts_CarriesEachBreakAsItIsCut_AndTheRoomBetweenTheParts()
    {
        var svg = Plt.Create().WithTheme(Theme.MatplotlibV2).WithServerInteraction("c", s => s.All())
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 150.0], [1.0, 2.0]).SetXLim(0, 150).SetYLim(1, 2)
                .WithXBreak(100, 120).WithXBreak(30, 80)).ToSvg();

        var view = svg.Each("g").First(e => e.Attribute("data-xmin") is not null);
        // Parts of 30, 20 and 30: a room of 0.2 of their mean, 16/3, so the breaks remove 50 - 16/3 and 20 - 16/3.
        double room = 0.2 * 80 / 3;
        Assert.Equal(room, view.Number("data-xroom"), 12);
        var cuts = ((string)view.Attribute("data-xbreaks")!).Split(' ').Select(n => double.Parse(n, CultureInfo.InvariantCulture)).ToArray();
        Assert.Equal([30, 80, 30, 50 - room, 100, 120, 100 - (50 - room), 20 - room], cuts, new ToleranceComparer(1e-9));
        Assert.Null(view.Attribute("data-yroom"));
    }

    private sealed class ToleranceComparer(double tolerance) : IEqualityComparer<double>
    {
        public bool Equals(double a, double b) => Math.Abs(a - b) <= tolerance;

        public int GetHashCode(double value) => 0;
    }

    [Fact]
    public void TheView_OfAnAxesWithoutBreaks_CarriesNoBreaks()
    {
        var svg = Plt.Create().WithServerInteraction("c", s => s.All())
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0], [1.0, 2.0]).SetXScale(AxisScale.Log)).ToSvg();

        var mapped = svg.Xml().Descendants().Where(e => e.Attribute("data-xmin") is not null).ToList();
        Assert.NotEmpty(mapped);
        Assert.All(mapped, e => Assert.DoesNotContain(e.Attributes(),
            a => a.Name.LocalName is "data-xbreaks" or "data-ybreaks"));
    }

    // ── hover, brush, data cursor ──

    [Fact]
    public void Hover_PastABreakOnALinearXAxis_ReportsTheDataValue()
    {
        using var h = InteractionScriptHarness.FromBuilder(Figure(BrokenX));
        var points = h.Svg.PointsOf("data");

        20.0.AssertCloseTo(h.HoverAt(points[1]).Num("x"), relative: 1e-3, floor: 1.0, what: "x before the break");
        using var h2 = InteractionScriptHarness.FromBuilder(Figure(BrokenX));
        100.0.AssertCloseTo(h2.HoverAt(points[2]).Num("x"), relative: 1e-3, floor: 1.0, what: "x past the break");
    }

    [Fact]
    public void Hover_PastABreakOnALogYAxis_ReportsTheDataValue()
    {
        using var h = InteractionScriptHarness.FromBuilder(Figure(BrokenLogY));

        5000.0.AssertCloseTo(h.HoverAt(h.Svg.PointsOf("data")[2]).Num("y"), relative: 1e-2, floor: 1.0, what: "y");
    }

    [Fact]
    public void BrushSelect_AcrossABreak_ReportsTheDataBounds()
    {
        using var h = InteractionScriptHarness.FromBuilder(Figure(BrokenX));
        var mock = h.WireSignalRMock();
        var points = h.Svg.PointsOf("data");

        h.BrushBetween(points[1], points[3]);

        var brush = mock.Payload("OnBrushSelect");
        20.0.AssertCloseTo(brush.Num("x1"), relative: 1e-3, floor: 1.0, what: "x1");
        150.0.AssertCloseTo(brush.Num("x2"), relative: 1e-3, floor: 1.0, what: "x2");
    }

    [Fact]
    public void DataCursor_OnAPointPastABreak_ReportsThatPointsValue()
    {
        using var h = InteractionScriptHarness.FromBuilder(Figure(BrokenX));
        var annotation = h.ClickDataCursor("data", 2);
        100.0.AssertCloseTo(annotation.Num("dataX"), relative: 1e-3, floor: 1.0, what: "dataX");
    }

    // ── wheel, pan and Home round trips ──

    [Fact]
    public void Hover_PastABreakOfAnAxisDrawnInParts_ReportsTheDataValue()
    {
        using var h = InteractionScriptHarness.FromBuilder(FigureInParts(BrokenX));
        var points = h.Svg.PointsOf("data");

        // 0 and 20, where the line leaves the first part and comes into the second, then 100 and 150.
        Assert.Equal(6, points.Length);
        100.0.AssertCloseTo(h.HoverAt(points[4]).Num("x"), relative: 1e-3, floor: 1.0, what: "x past the break");
    }

    /// <summary>The figure's default theme draws the X axis in parts; its plot keeps its place over a zoom (matplotlib's subplot
    /// params), where the library's own theme lays the plot out again by the new tick labels.</summary>
    [Fact]
    public void WheelZoomIn_OnABrokenXAxis_AppliedToTheFigure_KeepsThePointUnderThePointer()
    {
        var configure = FigureInParts(BrokenX);
        using var h = InteractionScriptHarness.FromBuilder(configure);
        var mock = h.WireSignalRMock();
        var p = h.Svg.PointsOf("data")[4];

        h.Wheel(p);

        var zoom = mock.Payload("OnZoom");
        var figure = configure.Build();
        zoom.ToZoomEvent().ApplyTo(figure);
        var after = figure.ToSvg().PointsOf("data");

        Assert.InRange(after[4].X, p.X - 0.02, p.X + 0.02);
        Assert.InRange(after[4].Y, p.Y - 0.02, p.Y + 0.02);
    }

    [Fact]
    public void Pan_OnABrokenXAxis_AppliedToTheFigure_MovesThePointsByTheDraggedPixels()
    {
        var configure = Figure(BrokenX);
        using var h = InteractionScriptHarness.FromBuilder(configure);
        var mock = h.WireSignalRMock();
        var before = h.Svg.PointsOf("data");
        var p = before[2];

        h.Drag(p, new Point(p.X + 10, p.Y));

        var pan = mock.Payload("OnPan");
        var figure = configure.Build();
        pan.ToPanEvent().ApplyTo(figure);
        var after = figure.ToSvg().PointsOf("data");

        for (int i = 1; i <= 2; i++)
            Assert.InRange(after[i].X - before[i].X, 9.95, 10.05);
    }

    [Fact]
    public void Home_AfterAZoom_OnABrokenLogYAxis_AppliedToTheFigure_RestoresTheFirstDrawing()
    {
        var configure = Figure(ax => BrokenX(BrokenLogY(ax)));
        InteractionScriptHarness.AssertHomeRestoresTheFirstDrawing(configure);
    }

    // ── the other viewers' pointers ──

    [Fact]
    public void AnotherViewersPointer_PastABreak_IsDrawnWhereThatValueIsDrawn()
    {
        var configure = Figure(ax => BrokenX(BrokenLogY(ax)), s => s.EnableZoom().EnableViewerCursors());
        using var h = InteractionScriptHarness.FromBuilder(configure);
        var hub = h.WireJsSignalRHub();
        h.Remount(configure);
        var p = h.Svg.PointsOf("data")[2];

        hub.DeliverMessage("ReceiveCursor", "c", "viewer-2", "Viewer 2", 0, 100.0, 5000.0);

        var marker = Assert.Single(h.Document.QuerySelectorAllRaw("g[data-mpl-peer]"));
        var xy = marker.getAttribute("transform")!.Replace("translate(", "").TrimEnd(')').Split(',')
            .Select(s => double.Parse(s, CultureInfo.InvariantCulture)).ToArray();
        Assert.InRange(xy[0], p.X - 0.02, p.X + 0.02);
        Assert.InRange(xy[1], p.Y - 0.02, p.Y + 0.02);
    }

    [Fact]
    public void AnotherViewersPointer_InsideABreak_IsNotDrawn()
    {
        var configure = Figure(BrokenX, s => s.EnableZoom().EnableViewerCursors());
        using var h = InteractionScriptHarness.FromBuilder(configure);
        var hub = h.WireJsSignalRHub();
        h.Remount(configure);

        hub.DeliverMessage("ReceiveCursor", "c", "viewer-2", "Viewer 2", 0, 50.0, 10.0);

        Assert.Empty(h.Document.QuerySelectorAllRaw("g[data-mpl-peer]"));
    }
}
