// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The arms of the log-axis range rules and of the treemap label fit that a happy-path render never
/// takes: an explicit limit on a log axis, a wholly non-positive range, a label that cannot even hold one
/// character, an interior node coloured from its value.</summary>
public class LogRangeArmsTests
{
    [Fact]
    public void AnExplicitLimit_IsNotPadded_OnALogAxis()
    {
        var axis = new Axis { Scale = AxisScale.Log, Min = 2, Max = 250 };

        var padded = new Range1D(2, 250).Padded(0.05, axis);

        Assert.Equal(2, padded.Lo);
        Assert.Equal(250, padded.Hi);
    }

    [Fact]
    public void AWhollyNonPositiveRange_IsLiftedToAFiniteDecade()
    {
        var axis = new Axis { Scale = AxisScale.Log };

        var lifted = new Range1D(-5, 0).PositiveForLog(axis);

        Assert.Equal(1, lifted.Hi);
        Assert.Equal(0.001, lifted.Lo);
    }

    [Fact]
    public void APositiveRange_IsLeftAlone()
    {
        var axis = new Axis { Scale = AxisScale.Log };

        var same = new Range1D(2, 250).PositiveForLog(axis);

        Assert.Equal(new Range1D(2, 250), same);
    }

    [Fact]
    public void ALinearAxis_IsNeverLifted()
    {
        var axis = new Axis();

        var same = new Range1D(-5, 0).PositiveForLog(axis);

        Assert.Equal(new Range1D(-5, 0), same);
    }

    [Fact]
    public void ALogXAxis_MasksNonPositiveX()
    {
        string svg = Plt.Create().WithSize(600, 300)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([0.0, 1.0, 10.0, 100.0], [1.0, 2.0, 3.0, 4.0]);
                ax.SetXScale(AxisScale.Log);
            })
            .ToSvg();

        var points = Regex.Match(svg, "<polyline points=\"([^\"]*)\"").Groups[1].Value;
        Assert.Equal(3, points.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
    }

    [Fact]
    public void ATruncatedLabelThatFitsNothing_IsJustTheEllipsis()
    {
        var root = new TreeNode
        {
            Children =
            [
                new() { Label = "a very long process name indeed", Value = 1 },
                new() { Label = "b", Value = 400 },
            ]
        };

        string svg = Plt.Create().WithSize(120, 80)
            .AddSubPlot(1, 1, 1, ax => ax.Treemap(root, s => s.LabelFit = TreemapLabelFit.Truncate))
            .ToSvg();

        Assert.Contains(">…<", svg);
    }

    [Fact]
    public void AnInteriorNode_IsColouredFromItsValueToo()
    {
        var root = new TreeNode
        {
            Children =
            [
                new()
                {
                    Label = "bus", ColorValue = 100,
                    Children = [new() { Label = "p", Value = 10, ColorValue = 0 }]
                },
            ]
        };

        string svg = Plt.Create().WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.Treemap(root, s =>
            {
                s.ColorMap = new MatPlotLibNet.Styling.ColorMaps.LinearColorMap("t", [Colors.Red, Colors.Blue]);
                s.VMin = 0;
                s.VMax = 100;
                s.ShowLabels = false;
            }))
            .ToSvg();

        Assert.Contains(Colors.Blue.ToHex(), svg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Colors.Red.ToHex(), svg, StringComparison.OrdinalIgnoreCase);
    }
}
