// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Tests.Rendering.Svg.Interaction;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>One notch of the wheel zooms a range by matplotlib's factor on every surface: a native control, a touch pinch, the
/// page's zoom script and the server-interaction script. matplotlib 3.11.2, <c>backend_bases.scroll_handler</c> (read with
/// python): <c>scale_factor = 0.85 ** event.step</c>, a step up (+1) zooming in to 0.85 of the range and a step down (-1) out to
/// 1/0.85 of it.</summary>
public class ZoomNotchTests
{
    [Fact]
    public void ANotchIn_IsMatplotlibs085_AndANotchOut_ItsInverse()
    {
        Assert.Equal(0.85, ZoomNotch.In);
        Assert.Equal(1 / 0.85, ZoomNotch.Out);
    }

    [Theory]
    [InlineData(-1.0, 0.85)]
    [InlineData(-120.0, 0.85)]
    [InlineData(1.0, 1 / 0.85)]
    [InlineData(53.0, 1 / 0.85)]
    public void AWheelTurn_ScalesTheRangeByOneNotch_InWhenItTurnsUp(double deltaY, double scale) =>
        Assert.Equal(scale, ZoomNotch.ScaleOf(deltaY));

    private static (Figure Figure, IChartLayout Layout) TenByFive()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        figure.SubPlots[0].XAxis.Min = 0;
        figure.SubPlots[0].XAxis.Max = 10;
        figure.SubPlots[0].YAxis.Min = 0;
        figure.SubPlots[0].YAxis.Max = 5;
        return (figure, ChartLayout.Create(figure, [new Rect(10, 10, 100, 50)]));
    }

    [Fact]
    public void ANativeControl_ZoomsOutByMatplotlibsNotch()
    {
        var (_, layout) = TenByFive();
        var events = new List<FigureInteractionEvent>();

        new ZoomModifier("c", layout, events.Add).OnScroll(new ScrollInputArgs(60, 35, 0, 1));

        var zoom = Assert.IsType<ZoomEvent>(Assert.Single(events));
        Assert.Equal(10 / 0.85, zoom.XMax - zoom.XMin, 9);
        Assert.Equal(5 / 0.85, zoom.YMax - zoom.YMin, 9);
    }

    [Fact]
    public void ANativeControl_ZoomsInByMatplotlibsNotch()
    {
        var (_, layout) = TenByFive();
        var events = new List<FigureInteractionEvent>();

        new ZoomModifier("c", layout, events.Add).OnScroll(new ScrollInputArgs(60, 35, 0, -1));

        var zoom = Assert.IsType<ZoomEvent>(Assert.Single(events));
        Assert.Equal(10 * 0.85, zoom.XMax - zoom.XMin, 9);
    }

    /// <summary>A pinch is the wheel: the fingers moving together to 0.86 of their distance is less than a notch out, which
    /// is 1/0.85 of the range, so it zooms nothing until it reaches 0.85.</summary>
    [Fact]
    public void APinchTogether_IsANotchOut_OnlyWhenItReachesMatplotlibsNotch()
    {
        var (figure, _) = TenByFive();
        figure.ChartId = "c";
        var events = new List<FigureInteractionEvent>();
        var host = new InteractionHost { Handler = new RecordingChartEventHandler(events) };
        host.Update(figure, ChartLayout.Create(figure, [new Rect(10, 10, 100, 50)]));
        var touch = new TouchGestureTranslator(host);

        touch.Pinched(0.86, 60, 35);
        Assert.Empty(events);

        touch.Pinched(0.84 / 0.86, 60, 35);   // together 0.84: past one notch, short of two
        var zoom = Assert.IsType<ZoomEvent>(Assert.Single(events));
        Assert.Equal(10 / 0.85, zoom.XMax - zoom.XMin, 9);
    }

    private static InteractionScriptHarness ServerChart() =>
        InteractionScriptHarness.FromBuilder(b => b
            .WithSize(600, 500)
            .WithServerInteraction("chart-1", s => s.EnableZoom())
            .Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]));

    private static double Span(object? payload, string low, string high)
    {
        var values = Assert.IsAssignableFrom<IDictionary<string, object?>>(payload);
        return Convert.ToDouble(values[high], CultureInfo.InvariantCulture) - Convert.ToDouble(values[low], CultureInfo.InvariantCulture);
    }

    private static double ViewSpan(InteractionScriptHarness h, string low, string high) =>
        double.Parse(h.GetAttribute("[data-axes-index='0']", high)!, CultureInfo.InvariantCulture)
        - double.Parse(h.GetAttribute("[data-axes-index='0']", low)!, CultureInfo.InvariantCulture);

    [Theory]
    [InlineData(-100.0, 0.85)]
    [InlineData(100.0, 1 / 0.85)]
    public void TheServerScript_ZoomsByMatplotlibsNotch(double deltaY, double scale)
    {
        using var h = ServerChart();
        var mock = h.WireSignalRMock();

        h.Simulate("svg", "wheel", e => { e.deltaY = deltaY; e.clientX = 300; e.clientY = 250; });

        var call = Assert.Single(mock.Calls);
        Assert.Equal(HubMethodNames.Zoom, call.Method);
        Assert.Equal(ViewSpan(h, "data-xmin", "data-xmax") * scale, Span(call.Payload, "xMin", "xMax"), 9);
        Assert.Equal(ViewSpan(h, "data-ymin", "data-ymax") * scale, Span(call.Payload, "yMin", "yMax"), 9);
    }

    [Theory]
    [InlineData(-100.0, 0.85)]
    [InlineData(100.0, 1 / 0.85)]
    public void ThePagesZoomScript_ZoomsByMatplotlibsNotch(double deltaY, double scale)
    {
        using var h = InteractionScriptHarness.FromBuilder(b => b.WithSize(600, 500).WithZoomPan().Plot([1.0, 2.0], [3.0, 4.0]));

        h.Simulate("svg", "wheel", e => { e.deltaY = deltaY; e.clientX = 300; e.clientY = 250; });

        var box = h.GetAttribute("svg", "viewBox")!.Split(' ').Select(v => double.Parse(v, CultureInfo.InvariantCulture)).ToArray();
        Assert.Equal(600 * scale, box[2], 9);
        Assert.Equal(500 * scale, box[3], 9);
    }
}
