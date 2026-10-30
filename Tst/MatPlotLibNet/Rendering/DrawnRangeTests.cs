// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The range an axes draws is a function of the axes and the theme, and nothing about the plot area it is drawn in.
/// That is what lets something that has to know the data's extent before any rectangle exists (the aspect of a plot is the
/// ratio of its two ranges) ask the axes instead of a renderer. The renderer delegates to the same function, so the two cannot
/// disagree: these axes cover the rules the range folds (own and shared series, sticky edges, set limits, a log scale, an
/// inverted axis, reference lines).</summary>
public class DrawnRangeTests
{
    private static readonly Rect PlotArea = new(80, 60, 700, 400);

    private static Axes Lines()
    {
        var axes = new Axes();
        axes.Plot([0.0, 2.5, 5.0, 7.5, 10.0], [0.0, 0.25, 0.5, 0.75, 1.0]);
        return axes;
    }

    private static Axes Bars()
    {
        var axes = new Axes();
        axes.Bar(["A", "B", "C"], [3.0, -2.0, 5.0]);
        return axes;
    }

    private static Axes SetLimits()
    {
        var axes = Lines();
        axes.XAxis.Min = -5;
        axes.XAxis.Max = 20;
        axes.YAxis.Max = 3;
        return axes;
    }

    private static Axes Logarithmic()
    {
        var axes = new Axes();
        axes.Plot([1.0, 10.0, 100.0], [1.0, 20.0, 400.0]);
        axes.YAxis.Scale = AxisScale.Log;
        return axes;
    }

    private static Axes Inverted()
    {
        var axes = Lines();
        axes.YAxis.Inverted = true;
        return axes;
    }

    private static Axes WithReferenceLine()
    {
        var axes = Lines();
        axes.AxHLine(5.0);
        axes.AxVLine(-3.0);
        return axes;
    }

    private static Axes ShareX()
    {
        var wide = Lines();
        var narrow = new Axes();
        narrow.Bar(["A", "B"], [1.0, 2.0]);
        narrow.ShareXWith = wide;
        return narrow;
    }

    private static Axes Stacked()
    {
        var axes = new Axes();
        axes.Bar(["A", "B"], [1.0, 2.0]);
        axes.Bar(["A", "B"], [3.0, 4.0]);
        axes.BarMode = BarMode.Stacked;
        return axes;
    }

    private static Axes Empty() => new();

    public static TheoryData<string> Names { get; } = new("lines", "bars", "set-limits", "logarithmic", "inverted", "reference-lines", "shared-x", "stacked", "empty");

    private static Axes Build(string name) => name switch
    {
        "lines" => Lines(),
        "bars" => Bars(),
        "set-limits" => SetLimits(),
        "logarithmic" => Logarithmic(),
        "inverted" => Inverted(),
        "reference-lines" => WithReferenceLine(),
        "shared-x" => ShareX(),
        "stacked" => Stacked(),
        _ => Empty(),
    };

    [Theory]
    [MemberData(nameof(Names))]
    public void TheRangeAnAxesDraws_IsTheRangeItsRendererDraws(string name)
    {
        var axes = Build(name);
        var theme = Theme.MatplotlibV2;
        var renderer = new CartesianAxesRenderer(axes, PlotArea, new SvgRenderContext(), theme);

        Assert.Equal(renderer.ComputeDataRanges(), axes.ComputeDataRanges(theme));
    }

    [Fact]
    public void TheRange_DoesNotDependOnThePlotArea()
    {
        var axes = Lines();
        var theme = Theme.MatplotlibV2;

        var small = new CartesianAxesRenderer(axes, new Rect(0, 0, 50, 40), new SvgRenderContext(), theme).ComputeDataRanges();
        var large = new CartesianAxesRenderer(axes, new Rect(0, 0, 900, 700), new SvgRenderContext(), theme).ComputeDataRanges();

        Assert.Equal(small, large);
        Assert.Equal(small, axes.ComputeDataRanges(theme));
    }

    [Fact]
    public void TheThemeMargin_IsTheOneTheRangeIsPaddedWith()
    {
        var axes = Lines();

        var plain = axes.ComputeDataRanges(Theme.MatplotlibV2);
        var wide = axes.ComputeDataRanges(new Theme("wide", Theme.MatplotlibV2) { AxisXMargin = 0.3, AxisYMargin = 0.3 });

        Assert.True(wide.XMax > plain.XMax && wide.YMax > plain.YMax);
    }
}
