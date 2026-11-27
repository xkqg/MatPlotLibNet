// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Rendering.TickLocators;

/// <summary>A symmetric-log axis has no minor tick, as matplotlib 3.11.2's symlog scale places none: its minor locator is a
/// <c>SymmetricalLogLocator</c> with the scale's <c>subs</c>, which are <c>None</c> unless given, so it lays the powers the major
/// locator lays and every one of them is left out for the major standing there (<c>remove_overlapping_locs</c>). Printed with
/// <c>ax.set_yscale('symlog', linthresh=100); ax.minorticks_on()</c> over x³ for x in -50 .. 50: no minor tick in view under either
/// style. The five-way split of a step between two powers stood its marks on a linear walk across a scale that is not linear.</summary>
public sealed class SymlogMinorTicksTests
{
    private static readonly double[] X = [.. Enumerable.Range(-50, 101).Select(i => (double)i)];
    private static readonly double[] Cubes = [.. X.Select(x => x * x * x)];

    private static int RedLines(Theme theme, bool grid)
    {
        var figure = Plt.Create().WithSize(600, 400).WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot(X, Cubes).WithSymlogYScale(linthresh: 100).WithMinorTicks();
                if (grid) ax.WithGrid(g => g with { Visible = true, Axis = GridAxis.Y, Which = GridWhich.Minor, Color = Colors.Red });
            })
            .Build();
        var y = figure.SubPlots[0].YAxis;
        if (!grid) y.MinorTicks = y.MinorTicks with { Color = Colors.Red };
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);
        // RGB alone: the library's own themes draw a minor grid line at half the colour's alpha.
        return ctx.Of<DrawLineCall>().Select(c => c.Color)
            .Count(c => c.R == Colors.Red.R && c.G == Colors.Red.G && c.B == Colors.Red.B);
    }

    [Theory]
    [MemberData(nameof(ThemeCases.MatplotlibAndDefault), MemberType = typeof(ThemeCases))]
    public void ASymlogAxisWithItsMinorTicksOn_DrawsNoMinorMark(string theme) => Assert.Equal(0, RedLines(theme.AsTheme(), grid: false));

    [Theory]
    [MemberData(nameof(ThemeCases.MatplotlibAndDefault), MemberType = typeof(ThemeCases))]
    public void ASymlogAxis_DrawsNoMinorGridLine(string theme) => Assert.Equal(0, RedLines(theme.AsTheme(), grid: true));

    [Fact]
    public void TheSymlogScalesMinorLocator_LaysNothingBesideItsMajors()
    {
        IMinorTickLocator minor = Assert.IsAssignableFrom<IMinorTickLocator>(ScaleRule.SymLog.MinorLocator);

        Assert.Empty(minor.MinorsOver(new Range1D(-1e5, 1e5), [-1e5, -1e4, -1e3, -100, 0, 100, 1e3, 1e4, 1e5], 13, Theme.MatplotlibV2));
    }
}
