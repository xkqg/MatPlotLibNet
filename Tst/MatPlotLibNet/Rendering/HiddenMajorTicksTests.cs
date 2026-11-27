// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>An axis whose major ticks are hidden (<see cref="TickConfig.Visible"/> off) and whose minor ticks are not still draws its minor
/// ticks, the X axis as every Y axis does. matplotlib 3.11.2, read with python: after <c>ax.minorticks_on()</c> and
/// <c>ax.tick_params(axis="x", which="major", bottom=False, labelbottom=False)</c> a major X tick's <c>tick1line</c> is not visible and a
/// minor X tick's is; the same on Y.</summary>
public class HiddenMajorTicksTests
{
    /// <summary>A line with minor ticks on, the major ticks of both axes hidden, under <paramref name="theme"/>.</summary>
    private static Figure MinorsOnly(Theme theme)
    {
        var figure = Plt.Create().WithSize(600, 400).WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 10.0], [0.0, 10.0]).WithMinorTicks())
            .Build();
        var axes = figure.SubPlots[0];
        axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { Visible = false };
        axes.YAxis.MajorTicks = axes.YAxis.MajorTicks with { Visible = false };
        return figure;
    }

    /// <summary>The short marks drawn across <paramref name="edge"/> (a y below the plot, or an x left of it).</summary>
    private static int MarksAcross(Figure figure, bool bottom)
    {
        var plot = figure.FirstPlotArea();
        var drawn = new RecordingRenderContext();
        new ChartRenderer().Render(figure, drawn);
        return drawn.Of<DrawLineCall>().Select(call => (call.P1, call.P2))
            .Count(line => bottom
                ? Math.Abs(line.Item1.X - line.Item2.X) < 1e-9 && Math.Max(line.Item1.Y, line.Item2.Y) > plot.Y + plot.Height + 0.5
                : Math.Abs(line.Item1.Y - line.Item2.Y) < 1e-9 && Math.Min(line.Item1.X, line.Item2.X) < plot.X - 0.5);
    }

    [Theory]
    [InlineData("Default")]
    [InlineData("MatplotlibV2")]
    [InlineData("MatplotlibClassic")]
    public void HiddenMajorTicks_LeaveTheMinorTicks_OnTheXAxisAsOnTheYAxis(string name)
    {
        var theme = name.AsTheme();
        var figure = MinorsOnly(theme);

        Assert.True(MarksAcross(figure, bottom: false) > 0, "the Y axis draws its minor ticks");
        Assert.True(MarksAcross(figure, bottom: true) > 0, "the X axis draws its minor ticks too");
    }
}
