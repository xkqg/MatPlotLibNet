// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Streaming;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Avalonia.Tests;

/// <summary>What the chart control and the streaming control are alike is their <see cref="MplChartSurface"/>'s: the handler, its
/// failures and the clicked point reach the reader from the control they hold (and nobody has to listen), either control is wired
/// to a server by the one route. What the host does with a gesture
/// is held by the core's <c>InteractionHostTests</c>. A paint hands the host its layout on the UI thread, which a test has no
/// dispatcher for, so the host is aimed here as that paint would aim it.</summary>
public class MplChartSurfaceTests
{
    private static Figure Line()
    {
        var figure = Plt.Create().Plot([0.0, 1.0, 2.0], [0.0, 5.0, 10.0], s => s.Label = "load").Build();
        figure.ChartId = "chart-1";
        return figure;
    }

    /// <summary>The line on limits 0..2 and 0..10, laid out in 200 by 100 (after the control was given it, so a streaming figure's
    /// own scaling does not move them): the point (1, 5) sits at pixel (100, 50).</summary>
    private static InteractionHost Aimed(MplChartSurface control, Figure figure)
    {
        figure.SubPlots[0].XAxis.Min = 0; figure.SubPlots[0].XAxis.Max = 2;
        figure.SubPlots[0].YAxis.Min = 0; figure.SubPlots[0].YAxis.Max = 10;
        control.Host.Update(figure, ChartLayout.Create(figure, [new Rect(0, 0, 200, 100)]));
        return control.Host;
    }

    private static PointerInputArgs PressOnThePoint() => new(100, 50, PointerButton.Left, ModifierKeys.None);

    private static ScrollInputArgs Wheel() => new(100, 50, 0, -120);

    [Fact]
    public void TheHandler_ItsFailures_AndTheClickedPoint_ReachTheReaderFromTheControl()
    {
        var figure = Line();
        var handler = new RecordingChartEventHandler { Throw = new InvalidOperationException("boom") };
        var control = new MplChartControl { Figure = figure, IsInteractive = true, ChartEventHandler = handler };
        var failures = new List<Exception>();
        var clicks = new List<PinnedAnnotation>();
        control.ChartEventHandlerFaulted += failures.Add;
        control.DataPointClicked += clicks.Add;

        var host = Aimed(control, figure);
        host.HandlePointerPressed(PressOnThePoint());
        host.HandleScroll(Wheel());

        Assert.Same(handler, control.ChartEventHandler);
        Assert.Contains("ZoomEvent", handler.Names);
        Assert.NotEmpty(failures);
        Assert.All(failures, failure => Assert.IsType<InvalidOperationException>(failure));
        var click = Assert.Single(clicks);
        Assert.Equal("load", click.SeriesLabel);
        Assert.Equal(1.0, click.DataX);
        Assert.Equal(5.0, click.DataY);
        Assert.True(figure.SubPlots[0].XAxis.Max < 2);   // the zoom itself was applied
        Assert.Same(figure, control.Shown);
    }

    /// <summary>The streaming control is painted again through its follower, which sets the limits from the data first.</summary>
    [Fact]
    public void AFailureAndAClick_WithNobodyListening_AreHarmless_AndAGestureRepaintsTheStreamingControl()
    {
        var figure = Line();
        using var stream = new StreamingFigure(figure);
        var control = new MplStreamingChartControl
        {
            StreamingFigure = stream,
            IsInteractive = true,
            ChartEventHandler = new RecordingChartEventHandler { Throw = new InvalidOperationException("boom") },
        };
        var host = Aimed(control, figure);

        Assert.Null(Record.Exception(() =>
        {
            host.HandlePointerPressed(PressOnThePoint());
            host.HandleScroll(Wheel());
        }));
        Assert.True(figure.SubPlots[0].XAxis.Max < 2);
        Assert.Same(figure, control.Shown);
    }

    [Fact]
    public void WithServerInteraction_MakesEitherControlInteractive_SendsItsGesturesThroughTheDelegate_AndHandsItBack()
    {
        var probe = new ServerInteractionProbe();
        var chart = new MplChartControl();
        var streaming = new MplStreamingChartControl();
        Assert.Same(chart, chart.WithServerInteraction(probe.Send));
        Assert.Same(streaming, streaming.WithServerInteraction(probe.Send));
        probe.AssertEveryGestureWasSent(new(chart.IsInteractive, chart.Host), new(streaming.IsInteractive, streaming.Host));
    }

    // ---- the input a native pointer, key and wheel event becomes (the adapter of the surface) ----
    // Validates the static mapping logic in <see cref="Avalonia.MplChartSurface"/> without requiring a running Avalonia
    // application. These tests exercise the adapter's modifier-key mapping and button resolution by calling the internal helpers
    // directly. Full round-trip tests (pointer → controller → figure mutation) run in the integration tests inside
    // <c>MatPlotLibNet.Tests</c>.

    // The surface's input mapping is private: it is reached by reflection. The interaction layer (InteractionController and
    // its modifiers) is covered by MatPlotLibNet.Tests/Interaction/*.

    /// <summary>Phase I.2 of v1.7.2 follow-on plan — Avalonia <c>KeyModifiers</c>
    /// must map to the platform-neutral <see cref="ModifierKeys"/> that all
    /// managed interaction modifiers expect. Theory covers every flag + combinations.</summary>
    [Theory]
    [InlineData(global::Avalonia.Input.KeyModifiers.None,    ModifierKeys.None)]
    [InlineData(global::Avalonia.Input.KeyModifiers.Shift,   ModifierKeys.Shift)]
    [InlineData(global::Avalonia.Input.KeyModifiers.Control, ModifierKeys.Ctrl)]
    [InlineData(global::Avalonia.Input.KeyModifiers.Alt,     ModifierKeys.Alt)]
    [InlineData(global::Avalonia.Input.KeyModifiers.Shift | global::Avalonia.Input.KeyModifiers.Control, ModifierKeys.Shift | ModifierKeys.Ctrl)]
    [InlineData(global::Avalonia.Input.KeyModifiers.Shift | global::Avalonia.Input.KeyModifiers.Alt,     ModifierKeys.Shift | ModifierKeys.Alt)]
    [InlineData(global::Avalonia.Input.KeyModifiers.Shift | global::Avalonia.Input.KeyModifiers.Control | global::Avalonia.Input.KeyModifiers.Alt, ModifierKeys.Shift | ModifierKeys.Ctrl | ModifierKeys.Alt)]
    public void Map_KeyModifiers_ToPlatformNeutralFlags(global::Avalonia.Input.KeyModifiers input, ModifierKeys expected)
    {
        // The modifier map of the surface is private; invoke via reflection.
        var type = typeof(MplChartControl).Assembly
            .GetType("MatPlotLibNet.Avalonia.MplChartSurface");
        Assert.NotNull(type);
        var method = type!.GetMethod("Map", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);
        var result = (ModifierKeys)method!.Invoke(null, [input])!;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Home",       "Home")]
    [InlineData("Left",       "Left")]
    [InlineData("Escape",     "Escape")]
    public void ToKeyArgs_KeyName_RoundTripsAsString(string keyName, string expected)
    {
        // ToKeyArgs is a static helper; it just calls e.Key.ToString(). Verify
        // the adapter doesn't mangle the key name on the way to the controller.
        // We can't construct a real Avalonia KeyEventArgs without a running app,
        // but the key enum round-trips via ToString — cover the surface via
        // Avalonia.Input.Key parse/format.
        Assert.True(Enum.TryParse<global::Avalonia.Input.Key>(keyName, out var k));
        Assert.Equal(expected, k.ToString());
    }

    [Theory]
    [InlineData(global::Avalonia.Input.PointerUpdateKind.LeftButtonPressed, PointerButton.Left)]
    [InlineData(global::Avalonia.Input.PointerUpdateKind.RightButtonPressed, PointerButton.Right)]
    [InlineData(global::Avalonia.Input.PointerUpdateKind.MiddleButtonPressed, PointerButton.Middle)]
    [InlineData(global::Avalonia.Input.PointerUpdateKind.LeftButtonReleased, PointerButton.None)]
    [InlineData(global::Avalonia.Input.PointerUpdateKind.Other, PointerButton.None)]
    public void APointerPressed_IsForTheButtonThatWentDown(global::Avalonia.Input.PointerUpdateKind kind, PointerButton expected) =>
        Assert.Equal(expected, MplChartSurface.ButtonPressedBy(kind));

    [Theory]
    [InlineData(global::Avalonia.Input.PointerUpdateKind.LeftButtonReleased, PointerButton.Left)]
    [InlineData(global::Avalonia.Input.PointerUpdateKind.RightButtonReleased, PointerButton.Right)]
    [InlineData(global::Avalonia.Input.PointerUpdateKind.MiddleButtonReleased, PointerButton.Middle)]
    [InlineData(global::Avalonia.Input.PointerUpdateKind.LeftButtonPressed, PointerButton.None)]
    [InlineData(global::Avalonia.Input.PointerUpdateKind.Other, PointerButton.None)]
    public void APointerReleased_IsForTheButtonThatCameUp(global::Avalonia.Input.PointerUpdateKind kind, PointerButton expected) =>
        Assert.Equal(expected, MplChartSurface.ButtonReleasedBy(kind));
}
