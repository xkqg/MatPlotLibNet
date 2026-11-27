// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Rendering.LegendRendering;

/// <summary>
/// When an axes draws a legend that nobody asked for with <c>WithLegend()</c>. The rule is the library's own and is written on
/// <see cref="Models.Axes.Legend"/> and <see cref="Models.Legend.Visible"/>: an axes starts with a visible legend, so every series
/// that carries a <c>Label</c> is listed, and an axes whose series carry none draws nothing, whatever the legend says. The rule is the
/// same under every theme and for every kind of axes, a 3-D scatter among them (a test of the 3-D scatter's marker once took its
/// legend entry for an accident of its own helper). <c>WithLegend(visible: false)</c> is how an author turns it off, and
/// <c>RestingDefaultsTests</c> holds the resting value. matplotlib draws no legend until <c>ax.legend()</c> is called; the library
/// keeps its own rule, which every picture of the cookbook and the wiki relies on, and the one place that reads it is the legend's
/// measurer and renderer.
/// </summary>
public class LegendAutoShowTests
{
    private const string LegendGroup = """<g class="legend" """;

    private static readonly double[] V = [1.0, 2.0, 3.0];

    private static bool DrawsALegend(Action<AxesBuilder> chart, Theme? theme = null) =>
        Plt.Create().WithSize(400, 300).WithTheme(theme ?? Theme.Default).AddSubPlot(1, 1, 1, chart).Build().ToSvg()
            .Contains(LegendGroup.TrimEnd(), StringComparison.Ordinal);

    [Theory]
    [MemberData(nameof(ThemeCases.MatplotlibAndDefault), MemberType = typeof(ThemeCases))]
    public void ALabelledLine_IsListedWithNoCallToWithLegend(string theme)
    {
        Assert.True(DrawsALegend(ax => ax.Plot(V, V, s => s.Label = "q"), theme.AsTheme()));
    }

    [Theory]
    [MemberData(nameof(ThemeCases.MatplotlibAndDefault), MemberType = typeof(ThemeCases))]
    public void ALabelledScatter3D_IsListedWithNoCallToWithLegend(string theme)
    {
        Assert.True(DrawsALegend(ax => ax.Scatter3D(V, V, V, s => s.Label = "q"), theme.AsTheme()));
    }

    [Fact]
    public void AnAxesWhoseSeriesCarryNoLabel_DrawsNoLegend()
    {
        Assert.False(DrawsALegend(ax => ax.Plot(V, V)));
        Assert.False(DrawsALegend(ax => ax.Scatter3D(V, V, V)));
    }

    [Fact]
    public void ALegendSwitchedOff_IsNotDrawnForALabelledSeries()
    {
        Assert.False(DrawsALegend(ax => ax.WithLegend(visible: false).Plot(V, V, s => s.Label = "q")));
        Assert.False(DrawsALegend(ax => ax.WithLegend(visible: false).Scatter3D(V, V, V, s => s.Label = "q")));
    }
}
