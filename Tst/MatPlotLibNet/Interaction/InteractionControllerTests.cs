// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction;

public class InteractionControllerTests
{
    private static (InteractionController ctrl, Figure figure, ChartLayout layout)
        MakeLocal(double xMin = 0, double xMax = 10, double yMin = 0, double yMax = 5)
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        figure.ChartId = "chart-1";
        figure.SubPlots[0].XAxis.Min = xMin;
        figure.SubPlots[0].XAxis.Max = xMax;
        figure.SubPlots[0].YAxis.Min = yMin;
        figure.SubPlots[0].YAxis.Max = yMax;
        var layout = ChartLayout.Create(figure, [new Rect(10, 10, 100, 50)]);
        var ctrl = InteractionController.CreateLocal(figure, layout);
        return (ctrl, figure, layout);
    }

    // ── scroll → zoom ──────────────────────────────────────────────────────────

    [Fact]
    public void HandleScroll_InsidePlot_ZoomsIn()
    {
        var (ctrl, figure, _) = MakeLocal();

        ctrl.HandleScroll(new ScrollInputArgs(60, 35, 0, -1));

        Assert.NotEqual(0.0,  figure.SubPlots[0].XAxis.Min);
        Assert.NotEqual(10.0, figure.SubPlots[0].XAxis.Max);
    }

    [Fact]
    public void HandleScroll_OutsidePlot_NoMutation()
    {
        var (ctrl, figure, _) = MakeLocal();
        ctrl.HandleScroll(new ScrollInputArgs(5, 5, 0, -1));
        Assert.Equal(0,  figure.SubPlots[0].XAxis.Min);
        Assert.Equal(10, figure.SubPlots[0].XAxis.Max);
    }

    // ── drag → pan ─────────────────────────────────────────────────────────────

    [Fact]
    public void HandlePointerPressed_LeftDrag_ThenMove_PansAxis()
    {
        var (ctrl, figure, _) = MakeLocal();

        ctrl.HandlePointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None));
        ctrl.HandlePointerMoved(new PointerInputArgs(70, 35, PointerButton.Left, ModifierKeys.None));

        // Figure's axis limits should have shifted.
        var axes = figure.SubPlots[0];
        Assert.NotEqual(0, axes.XAxis.Min);
        Assert.NotEqual(10, axes.XAxis.Max);
    }

    // ── double-click → reset ───────────────────────────────────────────────────

    [Fact]
    public void HandlePointerPressed_DoubleClick_ResetsAxis()
    {
        var (ctrl, figure, _) = MakeLocal();

        // First zoom in.
        ctrl.HandleScroll(new ScrollInputArgs(60, 35, 0, -1));
        double zoomedXMin = figure.SubPlots[0].XAxis.Min!.Value;

        // Then double-click to reset.
        ctrl.HandlePointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None, ClickCount: 2));

        Assert.Equal(0, figure.SubPlots[0].XAxis.Min!.Value, precision: 6);
        Assert.Equal(10, figure.SubPlots[0].XAxis.Max!.Value, precision: 6);
    }

    // ── Home key → reset ───────────────────────────────────────────────────────

    [Fact]
    public void HandleKeyDown_HomeKey_ResetsAxis()
    {
        var (ctrl, figure, _) = MakeLocal();
        ctrl.HandleScroll(new ScrollInputArgs(60, 35, 0, -1));
        ctrl.HandleKeyDown(new KeyInputArgs("Home"));
        Assert.Equal(0,  figure.SubPlots[0].XAxis.Min!.Value, precision: 6);
        Assert.Equal(10, figure.SubPlots[0].XAxis.Max!.Value, precision: 6);
    }

    // ── InvalidateRequested ────────────────────────────────────────────────────

    [Fact]
    public void LocalMode_MutationEvent_RaisesInvalidateRequested()
    {
        var (ctrl, _, _) = MakeLocal();
        int count = 0;
        ctrl.InvalidateRequested += () => count++;

        ctrl.HandleScroll(new ScrollInputArgs(60, 35, 0, -1));

        Assert.Equal(1, count);
    }

    [Fact]
    public void LocalMode_NotificationEvent_DoesNotRaiseInvalidateRequested()
    {
        var (ctrl, _, _) = MakeLocal();
        int count = 0;
        ctrl.InvalidateRequested += () => count++;

        // Hover is a notification event — no mutation, no invalidate.
        ctrl.HandlePointerMoved(new PointerInputArgs(60, 35, PointerButton.None, ModifierKeys.None));

        Assert.Equal(0, count);
    }

    // ── BrushSelect is a notification event ───────────────────────────────────

    [Fact]
    public void ShiftDrag_ProducesBrushSelectEvent_ViaCustomSink()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        figure.ChartId = "chart-1";
        figure.SubPlots[0].XAxis.Min = 0;
        figure.SubPlots[0].XAxis.Max = 10;
        figure.SubPlots[0].YAxis.Min = 0;
        figure.SubPlots[0].YAxis.Max = 5;
        var layout = ChartLayout.Create(figure, [new Rect(10, 10, 100, 50)]);
        var received = new List<FigureInteractionEvent>();
        var ctrl = InteractionController.Create(figure, layout, received.Add);

        ctrl.HandlePointerPressed(new PointerInputArgs(10, 10, PointerButton.Left, ModifierKeys.Shift));
        ctrl.HandlePointerReleased(new PointerInputArgs(110, 60, PointerButton.Left, ModifierKeys.Shift));

        Assert.Single(received);
        Assert.IsType<BrushSelectEvent>(received[0]);
    }

    // ── UpdateLayout rebuilds modifiers ───────────────────────────────────────

    [Fact]
    public void UpdateLayout_NewLayout_ModifiersUseUpdatedRanges()
    {
        var (ctrl, figure, _) = MakeLocal();

        // Update layout with new data range.
        figure.SubPlots[0].XAxis.Min = 100;
        figure.SubPlots[0].XAxis.Max = 200;
        var newLayout = ChartLayout.Create(figure, [new Rect(10, 10, 100, 50)]);
        ctrl.UpdateLayout(newLayout);

        // Reset should now restore the new range.
        ctrl.HandleKeyDown(new KeyInputArgs("Home"));
        Assert.Equal(100, figure.SubPlots[0].XAxis.Min!.Value, precision: 6);
        Assert.Equal(200, figure.SubPlots[0].XAxis.Max!.Value, precision: 6);
    }
}
