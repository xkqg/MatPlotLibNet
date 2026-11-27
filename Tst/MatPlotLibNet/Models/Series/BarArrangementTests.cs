// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Where the bars of an axes stand in a draw: stacked bars on the layers before them, then the bars with no foot of their
/// own side by side, each orientation among its own. The rule is written once, so the drawing and the marks a legend keeps clear
/// of cannot arrange the bars differently, and a draw leaves nothing of an earlier draw's arrangement behind.</summary>
public class BarArrangementTests
{
    /// <summary>0 to 3 bars, each upright (V), lying down (H) or upright on a foot the user set (F), in each bar mode, after no
    /// draw or after a draw in either mode.</summary>
    public static TheoryData<string, BarMode, BarMode?> Matrix()
    {
        var data = new TheoryData<string, BarMode, BarMode?>();
        string[] shapes = ["", "V", "H", "F", "VV", "HH", "VH", "VF", "VVV", "HHH", "VVH", "VHH", "FVV", "VFH"];
        foreach (var shape in shapes)
            foreach (var mode in new[] { BarMode.Grouped, BarMode.Stacked })
                foreach (var before in new BarMode?[] { null, BarMode.Grouped, BarMode.Stacked })
                    data.Add(shape, mode, before);
        return data;
    }

    [Theory]
    [MemberData(nameof(Matrix))]
    public void ADraw_StandsEveryBarWhereTheRuleSays(string shape, BarMode mode, BarMode? before)
    {
        var (figure, bars) = Figure(shape);
        var axes = figure.SubPlots[0];
        if (before is { } earlier)
        {
            axes.BarMode = earlier;
            figure.ToSvg();
        }

        axes.BarMode = mode;
        figure.ToSvg();

        AssertArranged(shape, mode, bars);
    }

    /// <summary>The owner of the rule, asked directly, arranges the bars as a draw does.</summary>
    [Theory]
    [MemberData(nameof(Matrix))]
    public void ArrangeBars_StandsEveryBarWhereTheRuleSays(string shape, BarMode mode, BarMode? before)
    {
        var (figure, bars) = Figure(shape);
        var axes = figure.SubPlots[0];
        if (before is { } earlier)
        {
            axes.BarMode = earlier;
            axes.ArrangeBars();
        }

        axes.BarMode = mode;
        axes.ArrangeBars();

        AssertArranged(shape, mode, bars);
    }

    /// <summary>A stack of one layer is no stack: its values are not read, and the bar stands on zero.</summary>
    [Fact]
    public void ArrangeBars_OneLayer_ReadsNothingToStack()
    {
        var axes = new Axes { BarMode = BarMode.Stacked };
        var counting = new TestStackable([1.0, 2.0]);
        axes.AddSeries(counting);

        axes.ArrangeBars();

        Assert.Equal(0, counting.Reads);
    }

    /// <summary>Under side-by-side bars the owner reads no layer, and a layer that is not a bar takes no place in a group.</summary>
    [Fact]
    public void ArrangeBars_Grouped_ReadsNoLayer()
    {
        var axes = new Axes { BarMode = BarMode.Grouped };
        var counting = new TestStackable([1.0, 2.0]);
        axes.AddSeries(counting);
        var bar = axes.Bar(["A", "B"], [3.0, 4.0]);

        axes.ArrangeBars();

        Assert.Equal(0, counting.Reads);
        Assert.Equal(0.0, bar.BarGroupOffset);
        Assert.Null(bar.BarGroupWidth);
    }

    /// <summary>A stackable that is not a bar lifts the bars after it, and only bars are given a foot.</summary>
    [Fact]
    public void ArrangeBars_Stacked_AStackableThatIsNotABar_LiftsTheBarAfterIt()
    {
        var axes = new Axes { BarMode = BarMode.Stacked };
        axes.AddSeries(new TestStackable([5.0, 7.0]));
        var bar = axes.Bar(["A", "B"], [1.0, 2.0]);

        axes.ArrangeBars();

        Assert.Equal([5.0, 7.0], bar.StackedBottoms!);
        Assert.Equal(0.0, bar.BarGroupOffset);
    }

    /// <summary>Under side-by-side bars nothing is stacked, so no layer's values are read to work out a stack. A stacked draw works the stack
    /// out once for the range, however many bars ask the range, and once for where the bars stand: each layer's values are read twice, not
    /// once per bar series plus once.</summary>
    [Theory]
    [InlineData(BarMode.Grouped, 2, 0)]
    [InlineData(BarMode.Stacked, 3, 2)]
    public void ADraw_ReadsEachLayerOnlyAsOftenAsTheBarModeNeedsTheStack(BarMode mode, int bars, int reads)
    {
        var figure = Plt.Create().WithSize(400, 300).AddSubPlot(1, 1, 1, ax => ax.SetBarMode(mode)).Build();
        var axes = figure.SubPlots[0];
        var counting = new TestStackable([1.0, 2.0]);
        axes.AddSeries(counting);
        for (int bar = 0; bar < bars; bar++) axes.Bar(["A", "B"], [3.0 + 2 * bar, 4.0 + 2 * bar]);

        figure.ToSvg();

        Assert.Equal(reads, counting.Reads);
    }

    /// <summary>A renderer of the application's own that draws through the protected <c>RenderSeries(DataTransform)</c> stands
    /// stacked bars on the stack, the stack their range is measured over, as the library's Cartesian renderer does.</summary>
    [Fact]
    public void ARendererOfYourOwn_StandsStackedBarsOnTheStack()
    {
        var axes = new Axes { BarMode = BarMode.Stacked };
        var lower = axes.Bar(["A", "B"], [1.0, 2.0]);
        var upper = axes.Bar(["A", "B"], [3.0, 4.0]);

        new OwnRenderer(axes).Render();

        Assert.Equal([0.0, 0.0], lower.StackedBottoms!);
        Assert.Equal([1.0, 2.0], upper.StackedBottoms!);
    }

    /// <summary>A renderer outside the library that draws the series of its axes over a fixed transform.</summary>
    private sealed class OwnRenderer(Axes axes)
        : AxesRenderer(axes, new Rect(0, 0, 200, 100), new SvgRenderContext(), Theme.Default)
    {
        public override void Render() => RenderSeries(new DataTransform(0, 2, 0, 10, PlotArea));
    }

    /// <summary>The bars of <paramref name="shape"/>, the values of bar i being i + 1 and 2 (i + 1), on one axes.</summary>
    internal static (Figure Figure, BarSeries[] Bars) Figure(string shape)
    {
        var figure = Plt.Create().WithSize(400, 300).AddSubPlot(1, 1, 1, _ => { }).Build();
        var axes = figure.SubPlots[0];
        var bars = new BarSeries[shape.Length];
        for (int i = 0; i < shape.Length; i++)
        {
            bars[i] = axes.Bar(["A", "B"], [i + 1.0, 2.0 * (i + 1)]);
            if (shape[i] == 'H') bars[i].Orientation = BarOrientation.Horizontal;
            if (shape[i] == 'F') bars[i].StackBaseline = [10.0, 20.0];
        }

        return (figure, bars);
    }

    /// <summary>The rule, written out: under stacked bars of more than one layer each bar with no foot of its own stands on the sum
    /// of the layers before it; then, in each orientation, more than one bar with no foot steps aside in a group 0.7 wide, every
    /// other bar keeping its own width at the centre of its slot.</summary>
    internal static void AssertArranged(string shape, BarMode mode, BarSeries[] bars)
    {
        var layers = bars.Where(bar => bar.StackBaseline is null).ToList();
        bool stacked = mode == BarMode.Stacked && layers.Count > 1;
        var below = new double[2];
        foreach (var bar in bars)
        {
            if (stacked && bar.StackBaseline is null)
            {
                Assert.Equal(below, bar.StackedBottoms);
                below = [below[0] + bar.Values[0], below[1] + bar.Values[1]];
            }
            else
            {
                Assert.Null(bar.StackedBottoms);
            }
        }

        foreach (var orientation in new[] { BarOrientation.Vertical, BarOrientation.Horizontal })
        {
            var group = bars.Where(bar => bar.Orientation == orientation && bar.Bottoms is null).ToList();
            foreach (var bar in bars.Where(bar => bar.Orientation == orientation).Except(group))
            {
                Assert.Equal(0.0, bar.BarGroupOffset);
                Assert.Null(bar.BarGroupWidth);
            }

            for (int i = 0; i < group.Count; i++)
            {
                if (group.Count > 1)
                {
                    double width = 0.7 / group.Count;
                    Assert.Equal((i - (group.Count - 1) / 2.0) * width, group[i].BarGroupOffset);
                    Assert.Equal(width, group[i].BarGroupWidth);
                }
                else
                {
                    Assert.Equal(0.0, group[i].BarGroupOffset);
                    Assert.Null(group[i].BarGroupWidth);
                }
            }
        }

        Assert.Equal(shape.Length, bars.Length);
    }
}
