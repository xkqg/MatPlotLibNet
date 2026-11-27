// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Builders;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>The gestures the interaction tests make on a page and the payloads they read back: a hover, a wheel, a drag, a brush, Home
/// and a click for the data cursor, and what the page sends turned into the event a server applies.</summary>
public sealed class InteractionGesturesTests
{
    private static readonly Action<FigureBuilder> Chart = b => b
        .WithSize(600, 500)
        .WithServerInteraction("c", s => s.All())
        .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0, 3.0], [10.0, 40.0, 20.0], s => s.Label = "load"));

    [Fact]
    public void TheChart_IsTheOneSvgOfThePage()
    {
        using var h = InteractionScriptHarness.FromBuilder(Chart);

        Assert.Equal("svg", h.Chart().tagName, ignoreCase: true);
    }

    [Fact]
    public void AHover_ReportsTheDataUnderThePointer()
    {
        using var h = InteractionScriptHarness.FromBuilder(Chart);

        var hover = h.HoverAt(h.Svg.PointsOf("load")[1]);

        Assert.Equal(2.0, hover.Num("x"), 2e-4);
        Assert.Equal(40.0, hover.Num("y"), 3e-3);
    }

    [Fact]
    public void ABrush_BetweenTwoPoints_ReportsTheirBounds()
    {
        using var h = InteractionScriptHarness.FromBuilder(Chart);
        var mock = h.WireSignalRMock();
        var points = h.Svg.PointsOf("load");

        h.BrushBetween(points[0], points[2]);

        var brush = mock.Payload("OnBrushSelect");
        Assert.Equal(1.0, brush.Num("x1"), 2e-4);
        Assert.Equal(3.0, brush.Num("x2"), 2e-4);
    }

    [Fact]
    public void AWheel_ThenHome_SendsAZoomThenAResetThatApplyToTheFigure()
    {
        using var h = InteractionScriptHarness.FromBuilder(Chart);
        var mock = h.WireSignalRMock();
        var p = h.Svg.PointsOf("load")[1];

        h.Wheel(p);
        var zoom = mock.Payload("OnZoom");
        h.PressHome();
        var reset = mock.Payload("OnReset");

        var figure = Chart.Build();
        var zoomEvent = zoom.ToZoomEvent();
        var resetEvent = reset.ToResetEvent();
        zoomEvent.ApplyTo(figure);
        Assert.Equal(zoom.Num("xMin"), zoomEvent.XMin);
        Assert.Equal(zoom.Num("yMax"), zoomEvent.YMax);
        Assert.Equal(reset.Num("xMax"), resetEvent.XMax);
        Assert.Equal(zoom.Num("xMin"), figure.SubPlots[0].XAxis.Min);
    }

    [Fact]
    public void ADrag_SendsAPanThatCarriesTheViewItStartedFrom()
    {
        using var h = InteractionScriptHarness.FromBuilder(Chart);
        var mock = h.WireSignalRMock();
        var p = h.Svg.PointsOf("load")[1];

        h.Drag(p, new Point(p.X + 20, p.Y));

        var pan = mock.Payload("OnPan").ToPanEvent();
        Assert.NotNull(pan.InView);
        Assert.Equal(0, pan.AxesIndex);
        Assert.NotEqual(0.0, pan.DxData);
    }

    [Fact]
    public void AClickOnALine_ReportsTheAnnotationOfThePointItHit()
    {
        using var h = InteractionScriptHarness.FromBuilder(Chart);

        var annotation = h.ClickDataCursor("load", 2);

        Assert.Equal("load", annotation["seriesLabel"]);
        Assert.Equal(3.0, annotation.Num("dataX"), 2e-4);
    }

    [Fact]
    public void ACallThePageNeverMade_IsRefusedByItsName()
    {
        using var h = InteractionScriptHarness.FromBuilder(Chart);
        var mock = h.WireSignalRMock();

        Assert.ThrowsAny<Exception>(() => mock.Payload("OnReset"));
    }

    [Fact]
    public void ABuilder_BuildsItsFigure()
    {
        var figure = Chart.Build();

        Assert.Single(figure.SubPlots);
    }
}
