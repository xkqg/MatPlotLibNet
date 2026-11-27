// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction.Modifiers;

public sealed class Rotate3DModifierTests : InteractionModifierTests<Rotate3DModifier>
{
    // Rotate3DModifier handles arrow keys for ±5° rotation, BUT only when a 3D series
    // is present (HasAnyThreeD guard). The shared harness uses a 2D Plot, so for the
    // generic-base "Escape" key probe, HandlesKeyDown returns false.
    protected override bool ShouldHandleKeyDown => false;

    protected override Rotate3DModifier CreateModifier(IChartLayout layout, Figure fig, Action<FigureInteractionEvent> sink) =>
        new("c1", layout, sink, fig);

    private static (Rotate3DModifier modifier, List<FigureInteractionEvent> events, Figure figure) Create3D()
    {
        var figure = new Figure { ChartId = "test" };
        var axes = figure.AddSubPlot();
        axes.CoordinateSystem = CoordinateSystem.ThreeD;
        axes.Elevation = 30;
        axes.Azimuth = -60;

        var events = new List<FigureInteractionEvent>();
        var layout = TestLayout(new MatPlotLibNet.Rendering.Rect(50, 50, 400, 400));
        var modifier = new Rotate3DModifier("test", layout, e => events.Add(e), figure);
        return (modifier, events, figure);
    }

    private static (Rotate3DModifier modifier, List<FigureInteractionEvent> events) Create2D()
    {
        var figure = new Figure { ChartId = "test" };
        figure.AddSubPlot(); // Cartesian (default)

        var events = new List<FigureInteractionEvent>();
        var layout = TestLayout(new MatPlotLibNet.Rendering.Rect(50, 50, 400, 400));
        var modifier = new Rotate3DModifier("test", layout, e => events.Add(e), figure);
        return (modifier, events);
    }

    [Fact]
    public void RightDragOn3DAxes_EmitsRotate3DEvent()
    {
        var (mod, events, _) = Create3D();
        var press = new PointerInputArgs(200, 200, PointerButton.Right, ModifierKeys.None);
        Assert.True(mod.HandlesPointerPressed(press));

        mod.OnPointerPressed(press);
        mod.OnPointerMoved(new PointerInputArgs(220, 210, PointerButton.Right, ModifierKeys.None));

        Assert.Single(events);
        var evt = Assert.IsType<Rotate3DEvent>(events[0]);
        Assert.Equal(10.0, evt.DeltaAzimuth, 1);  // 20px * 0.5
        Assert.Equal(-5.0, evt.DeltaElevation, 1); // -10px * 0.5
    }

    [Fact]
    public void RightDragOn2DAxes_DoesNotHandle()
    {
        var (mod, _) = Create2D();
        var press = new PointerInputArgs(200, 200, PointerButton.Right, ModifierKeys.None);
        Assert.False(mod.HandlesPointerPressed(press));
    }

    /// <summary>A left drag inside the plot area, and a right click at (10, 10) outside it, are not the modifier's.</summary>
    [Theory]
    [InlineData(200, 200, PointerButton.Left)]
    [InlineData(10, 10, PointerButton.Right)]
    public void ALeftDrag_OrAClickOutsideThePlotArea_DoesNotHandle(double x, double y, PointerButton button)
    {
        var (mod, _, _) = Create3D();
        var press = new PointerInputArgs(x, y, button, ModifierKeys.None);
        Assert.False(mod.HandlesPointerPressed(press));
    }

    [Fact]
    public void ArrowKeys_EmitRotateEvents()
    {
        var (mod, events, _) = Create3D();

        Assert.True(mod.HandlesKeyDown(new KeyInputArgs("Left")));
        mod.OnKeyDown(new KeyInputArgs("Left"));
        Assert.Single(events);
        var evt = Assert.IsType<Rotate3DEvent>(events[0]);
        Assert.Equal(-5.0, evt.DeltaAzimuth);

        mod.OnKeyDown(new KeyInputArgs("Right"));
        Assert.Equal(5.0, ((Rotate3DEvent)events[1]).DeltaAzimuth);

        mod.OnKeyDown(new KeyInputArgs("Up"));
        Assert.Equal(5.0, ((Rotate3DEvent)events[2]).DeltaElevation);

        mod.OnKeyDown(new KeyInputArgs("Down"));
        Assert.Equal(-5.0, ((Rotate3DEvent)events[3]).DeltaElevation);
    }

    [Fact]
    public void HomeKey_ResetsToDefault()
    {
        var (mod, events, figure) = Create3D();
        figure.SubPlots[0].Azimuth = 45;
        figure.SubPlots[0].Elevation = 60;

        mod.OnKeyDown(new KeyInputArgs("Home"));
        var evt = Assert.IsType<Rotate3DEvent>(events[0]);
        // Should produce deltas that bring azimuth to -60 and elevation to 30
        Assert.Equal(-105.0, evt.DeltaAzimuth, 1); // -60 - 45
        Assert.Equal(-30.0, evt.DeltaElevation, 1); // 30 - 60
    }

    [Fact]
    public void ArrowKeysOn2D_DoesNotHandle()
    {
        var (mod, _) = Create2D();
        Assert.False(mod.HandlesKeyDown(new KeyInputArgs("Left")));
    }

    [Fact]
    public void Release_StopsDrag()
    {
        var (mod, events, _) = Create3D();
        mod.OnPointerPressed(new PointerInputArgs(200, 200, PointerButton.Right, ModifierKeys.None));
        mod.OnPointerReleased(new PointerInputArgs(220, 210, PointerButton.Right, ModifierKeys.None));
        mod.OnPointerMoved(new PointerInputArgs(240, 220, PointerButton.Right, ModifierKeys.None));
        Assert.Empty(events); // no events after release
    }

    /// <summary>A layout that answers a hit test for a pixel on the plot area, showing 0..10 on both axes.</summary>
    private static TestChartLayout TestLayout(MatPlotLibNet.Rendering.Rect plotArea) =>
        new(plotArea, new DataRange(0, 10, 0, 10)) { HitsOnlyOnThePlot = true };

    /// <summary>Rotate3DModifier — `if (da != 0 || de != 0)` short-circuit's
    /// false arm: a rotation event with both deltas zero produces no event. Triggered
    /// by an arrow-key on a non-3D figure (HasAnyThreeD returns false → HandlesKeyDown
    /// returns false → no rotation, both deltas stay zero).</summary>
    [Fact]
    public void Rotate3D_NoThreeDFigure_KeyDown_NoRotationEvent()
    {
        var (fig, layout) = FigureLayout.Plain();
        var events = new List<FigureInteractionEvent>();
        var m = new Rotate3DModifier("c1", layout, events.Add, fig);
        const string ArrowUp = "Up";
        Assert.False(m.HandlesKeyDown(new KeyInputArgs(ArrowUp, ModifierKeys.None)));
        m.OnKeyDown(new KeyInputArgs(ArrowUp, ModifierKeys.None));
        Assert.Empty(events);
    }

    /// <summary>Rotate3DModifier — `is "Left" or "Right" or "Up" or "Down"
    /// or "Home"` pattern coverage. Each Theory row pins one of the 5 positive
    /// arms (10/10 with the negative arms already covered by the generic-base
    /// Escape probe in <see cref="ModifierTestBase{T}"/>).</summary>
    [Theory]
    [InlineData("Left")]
    [InlineData("Right")]
    [InlineData("Up")]
    [InlineData("Down")]
    [InlineData("Home")]
    public void Rotate3D_HandlesKeyDown_TrueForArrowOrHome(string key)
    {
        var (fig, layout) = FigureLayout.ThreeD();
        var m = new Rotate3DModifier("c1", layout, _ => { }, fig);
        Assert.True(m.HandlesKeyDown(new KeyInputArgs(key, ModifierKeys.None)));
    }

    /// <summary>Rotate3DModifier — switch default arm: an unknown key on a
    /// 3D figure returns (0, 0) so the `if (da != 0 || de != 0)` guard short-circuits
    /// true→false and no event is emitted.</summary>
    [Fact]
    public void Rotate3D_OnKeyDown_UnknownKey_OnThreeDFigure_NoEvent()
    {
        var (fig, layout) = FigureLayout.ThreeD();
        var events = new List<FigureInteractionEvent>();
        var m = new Rotate3DModifier("c1", layout, events.Add, fig);
        const string PageUp = "PageUp";
        m.OnKeyDown(new KeyInputArgs(PageUp, ModifierKeys.None));
        Assert.Empty(events);
    }

    /// <summary>Rotate3DModifier — `if (dx == 0 && dy == 0)` short-circuit's
    /// 4 branch points. Press inside, then move with one delta zero so dx==0 evaluates
    /// true but dy==0 evaluates false (or vice versa) → event still emits.</summary>
    [Theory]
    [InlineData(60.0, 35.0,  60.0, 40.0)]   // dx == 0, dy != 0
    [InlineData(60.0, 35.0,  65.0, 35.0)]   // dx != 0, dy == 0
    public void Rotate3D_PointerMove_PartialDelta_EmitsEvent(double pressX, double pressY, double moveX, double moveY)
    {
        var (fig, layout) = FigureLayout.ThreeD();
        var events = new List<FigureInteractionEvent>();
        var m = new Rotate3DModifier("c1", layout, events.Add, fig);
        m.OnPointerPressed(new PointerInputArgs(pressX, pressY, PointerButton.Right, ModifierKeys.None));
        m.OnPointerMoved(new PointerInputArgs(moveX, moveY, PointerButton.Right, ModifierKeys.None));
        Assert.Single(events);
        Assert.IsType<Rotate3DEvent>(events[0]);
    }

    /// <summary>Rotate3DModifier — final branch arm: dx==0 AND dy==0
    /// (both true → return). Press inside on a 3D figure, then move to the SAME
    /// position so both deltas are zero.</summary>
    [Fact]
    public void Rotate3D_PointerMove_SamePosition_NoEvent()
    {
        var (fig, layout) = FigureLayout.ThreeD();
        var events = new List<FigureInteractionEvent>();
        var m = new Rotate3DModifier("c1", layout, events.Add, fig);
        m.OnPointerPressed(new PointerInputArgs(60, 35, PointerButton.Right, ModifierKeys.None));
        m.OnPointerMoved(new PointerInputArgs(60, 35, PointerButton.Right, ModifierKeys.None));
        Assert.Empty(events);
    }

    /// <summary>Rotate3DModifier — `axesIndex &lt; _figure.SubPlots.Count`
    /// false arm: a stale layout returns an axesIndex out of range, so IsThreeD
    /// short-circuits to false and HandlesPointerPressed returns false.</summary>
    [Fact]
    public void Rotate3D_StaleAxesIndex_OutOfRange_NotHandled()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Scatter3D(new double[] { 1.0 }, new double[] { 2.0 }, new double[] { 3.0 }))
            .Build();
        var staleLayout = new TestChartLayout(new Rect(0, 0, 200, 100), new DataRange(0, 10, 0, 5)) { AxesHit = 999 };
        var m = new Rotate3DModifier("c1", staleLayout, _ => { }, fig);
        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(50, 50, PointerButton.Right, ModifierKeys.None)));
    }
}
