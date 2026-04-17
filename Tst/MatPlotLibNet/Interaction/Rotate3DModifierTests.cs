// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Interaction;

public sealed class Rotate3DModifierTests
{
    private static (Rotate3DModifier modifier, List<FigureInteractionEvent> events, Figure figure) Create3D()
    {
        var figure = new Figure { ChartId = "test" };
        var axes = figure.AddSubPlot();
        axes.CoordinateSystem = CoordinateSystem.ThreeD;
        axes.Elevation = 30;
        axes.Azimuth = -60;

        var events = new List<FigureInteractionEvent>();
        var layout = new TestChartLayout(plotArea: new MatPlotLibNet.Rendering.Rect(50, 50, 400, 400));
        var modifier = new Rotate3DModifier("test", layout, e => events.Add(e), figure);
        return (modifier, events, figure);
    }

    private static (Rotate3DModifier modifier, List<FigureInteractionEvent> events) Create2D()
    {
        var figure = new Figure { ChartId = "test" };
        figure.AddSubPlot(); // Cartesian (default)

        var events = new List<FigureInteractionEvent>();
        var layout = new TestChartLayout(plotArea: new MatPlotLibNet.Rendering.Rect(50, 50, 400, 400));
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
        var (mod, events) = Create2D();
        var press = new PointerInputArgs(200, 200, PointerButton.Right, ModifierKeys.None);
        Assert.False(mod.HandlesPointerPressed(press));
    }

    [Fact]
    public void LeftDrag_DoesNotHandle()
    {
        var (mod, _, _) = Create3D();
        var press = new PointerInputArgs(200, 200, PointerButton.Left, ModifierKeys.None);
        Assert.False(mod.HandlesPointerPressed(press));
    }

    [Fact]
    public void ClickOutsidePlotArea_DoesNotHandle()
    {
        var (mod, _, _) = Create3D();
        var press = new PointerInputArgs(10, 10, PointerButton.Right, ModifierKeys.None); // outside plot area
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

    /// <summary>Minimal test-only ChartLayout that returns a fixed plot area for hit testing.</summary>
    private sealed class TestChartLayout : IChartLayout
    {
        private readonly MatPlotLibNet.Rendering.Rect _plotArea;

        public TestChartLayout(MatPlotLibNet.Rendering.Rect plotArea) => _plotArea = plotArea;

        public int AxesCount => 1;

        public MatPlotLibNet.Rendering.Rect GetPlotArea(int axesIndex) => _plotArea;

        public (double XMin, double XMax, double YMin, double YMax) GetDataRange(int axesIndex) =>
            (0, 10, 0, 10);

        public int? HitTestAxes(double pixelX, double pixelY)
        {
            if (pixelX >= _plotArea.X && pixelX <= _plotArea.X + _plotArea.Width &&
                pixelY >= _plotArea.Y && pixelY <= _plotArea.Y + _plotArea.Height)
                return 0;
            return null;
        }

        public int? HitTestLegendItem(double pixelX, double pixelY, int axesIndex) => null;
    }
}
