// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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

    /// <summary>A log X axis puts x = 0 a thousand decades left of it, as matplotlib's log scale clips it (printed with matplotlib
    /// 3.11.2: <c>ax.plot([1, 0, 10], [1, 2, 3])</c> on a log X axis puts the vertex at x = -422631.14 px): the line enters the plot
    /// through its left edge, one point more than the three it places, where it used to start at the first point it placed.</summary>
    [Fact]
    public void ALogXAxis_PutsANonPositiveXBeyondIt_AndTheLineEntersThroughTheLeftEdge()
    {
        string svg = Plt.Create().WithSize(600, 300)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([0.0, 1.0, 10.0, 100.0], [1.0, 2.0, 3.0, 4.0]);
                ax.SetXScale(AxisScale.Log);
            })
            .ToSvg();

        var points = svg.Elements("polyline")[0]["points"]!;
        Assert.Equal(4, points.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
        points.AssertNoNonFiniteNumber();
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("ATruncatedLabelThatFitsNothing_IsJustTheEllipsis", () =>
        {
            var root = new TreeNode
            {
                Children =
                [
                    new() { Label = "a very long process name indeed", Value = 1 },
                    new() { Label = "b", Value = 400 },
                ]
            };
            return Plt.Create().WithSize(120, 80)
                .AddSubPlot(1, 1, 1, ax => ax.Treemap(root, s => s.LabelFit = TreemapLabelFit.Truncate))
                .ToSvg();
        }, ">…<"),
        new DrawnToken("AnInteriorNode_IsColouredFromItsValueToo", () =>
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
            return Plt.Create().WithSize(400, 300)
                .AddSubPlot(1, 1, 1, ax => ax.Treemap(root, s =>
                {
                    s.ColorMap = new MatPlotLibNet.Styling.ColorMaps.LinearColorMap("t", [Colors.Red, Colors.Blue]);
                    s.VMin = 0;
                    s.VMax = 100;
                    s.ShowLabels = false;
                }))
                .ToSvg();
        }, Colors.Blue.ToHex(), Colors.Red.ToHex()) { Comparison = StringComparison.OrdinalIgnoreCase },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
