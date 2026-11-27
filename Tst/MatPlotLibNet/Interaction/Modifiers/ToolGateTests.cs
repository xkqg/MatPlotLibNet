// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using static MatPlotLibNet.Interaction.InteractionToolbar;

namespace MatPlotLibNet.Tests.Interaction.Modifiers;

/// <summary>The gate of a drawing tool (<see cref="ToolGate"/>): a press opens it while the toolbar holds its tool, with the left
/// button, over an axes. How the press is then read is the layout's (<c>ChartLayoutPressTests</c>).</summary>
public class ToolGateTests
{
    private static (IChartLayout Layout, InteractionToolbar Toolbar) Harness()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        axes.XAxis.Min = 0; axes.XAxis.Max = 10;
        axes.YAxis.Min = 0; axes.YAxis.Max = 5;
        return (ChartLayout.Create(figure, [new Rect(10, 10, 100, 50)]), new InteractionToolbar());
    }

    private static PointerInputArgs Press(double x, double y, PointerButton button = PointerButton.Left) =>
        new(x, y, button, ModifierKeys.None);

    [Fact]
    public void ALeftPressOverAnAxes_OpensTheGate_WhileItsToolIsActive()
    {
        var (layout, toolbar) = Harness();
        toolbar.Activate(ToolIds.Level);

        Assert.True(new ToolGate(toolbar, ToolMode.Level).Opens(Press(60, 35), layout));
    }

    [Fact]
    public void AnotherTool_ARightPress_OrAPressOffTheAxes_LeaveTheGateShut()
    {
        var (layout, toolbar) = Harness();
        var gate = new ToolGate(toolbar, ToolMode.Fibonacci);

        Assert.False(gate.Opens(Press(60, 35), layout));
        toolbar.Activate(ToolIds.Fibonacci);
        Assert.False(gate.Opens(Press(60, 35, PointerButton.Right), layout));
        Assert.False(gate.Opens(Press(500, 500), layout));
    }
}
