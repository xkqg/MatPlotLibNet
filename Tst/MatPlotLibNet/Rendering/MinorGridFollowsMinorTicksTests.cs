// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>matplotlib draws a minor grid line at each minor tick position, so the minor grid and the minor ticks stand on
/// the same places: the axis' own minor spacing when it set one, never a place the scale cannot put (a log axis at or below
/// 0), never outside the plot. A major grid line stands only where the plot holds its tick. The minor tick marks of a 3-D
/// axis walk the same values, between its major ticks.</summary>
public class MinorGridFollowsMinorTicksTests
{
    private static bool IsBlue(Color c) => c.R == Colors.Blue.R && c.G == Colors.Blue.G && c.B == Colors.Blue.B;

    private static List<(Point P1, Point P2)> Lines(RecordingRenderContext ctx, Func<Color, bool> colour) =>
        [.. ctx.Of<DrawLineCall>()
            .Where(a => colour(a.Color))
            .Select(a => (a.P1, a.P2))];

    [Fact]
    public void AMinorGrid_StandsWhereTheMinorTicksStand_WhenTheAxisSetItsOwnMinorSpacing()
    {
        var figure = Plt.Create()
            .WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([0.0, 1.0], [0.0, 1.0])
                .SetXLim(0, 1)
                .WithMinorTicks()
                .WithGrid(g => g with { Visible = true, Axis = GridAxis.X, Which = GridWhich.Minor, Color = Colors.Blue }))
            .Build();
        var x = figure.SubPlots[0].XAxis;
        // The major ticks stand every 0.1; a minor mark halfway between each two, where the five-way split would put four.
        x.MinorTicks = x.MinorTicks with { Spacing = 0.05, Color = Colors.Red };

        var ctx = figure.Drawn();
        double[] gridXs = [.. Lines(ctx, IsBlue).Select(l => l.P1.X).Order()];
        double[] tickXs = [.. Lines(ctx, c => c == Colors.Red).Select(l => l.P1.X).Order()];

        Assert.NotEmpty(tickXs);
        Assert.Equal(tickXs, gridXs);
    }

    [Fact]
    public void AMinorGrid_OnALogAxis_DrawsNoLineAtAPlaceTheScaleCannotPut()
    {
        // The five-way split of a decade step runs below the first major tick, to values at or below 0 that a log axis has
        // no place for.
        var figure = Plt.Create()
            .WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 10, 100, 1000], [1.0, 10, 100, 1000])
                .SetYScale(AxisScale.Log)
                .WithGrid(g => g with { Visible = true, Axis = GridAxis.Y, Which = GridWhich.Minor, Color = Colors.Blue }))
            .Build();

        var grid = Lines(figure.Drawn(), IsBlue);

        Assert.NotEmpty(grid);
        Assert.All(grid, l => new[] { l.P1.X, l.P1.Y, l.P2.X, l.P2.Y }.AssertAllFinite("a grid line"));
    }

    private static Figure MinorGridOnly(Theme theme, AxisScale scale, bool minorTicks) => Plt.Create()
        .WithSize(600, 400)
        .WithTheme(theme)
        .AddSubPlot(1, 1, 1, ax =>
        {
            ax.Plot([1.0, 10, 100, 1000], [1.0, 10, 100, 1000]).SetYScale(scale);
            if (minorTicks) ax.WithMinorTicks();
            ax.WithGrid(g => g with { Visible = true, Axis = GridAxis.Y, Which = GridWhich.Minor, Color = Colors.Blue });
        })
        .Build();

    [Fact]
    public void UnderAMatplotlibTheme_ALogAxis_DrawsItsMinorGrid_WithNoMinorTicksSwitchedOn_AsItDoesUnderTheLibrarysThemes()
    {
        // matplotlib 3.11.2: a log axis has a LogLocator for its minor ticks from the start (24 minor ticks on 0.1..1000 in a
        // one-line figure), so grid(which='minor') draws without minorticks_on().
        int under = Lines(MinorGridOnly(Theme.MatplotlibV2, AxisScale.Log, minorTicks: false).Drawn(), IsBlue).Count;
        int library = Lines(MinorGridOnly(Theme.Default, AxisScale.Log, minorTicks: false).Drawn(), IsBlue).Count;

        Assert.True(under > 0);
        Assert.Equal(library, under);
    }

    [Fact]
    public void UnderAMatplotlibTheme_ALinearAxis_DrawsNoMinorGrid_UntilItsMinorTicksAreSwitchedOn()
    {
        // matplotlib 3.11.2: a linear axis has a NullLocator for its minor ticks, so grid(which='minor') draws nothing
        // until minorticks_on().
        Assert.Empty(Lines(MinorGridOnly(Theme.MatplotlibV2, AxisScale.Linear, minorTicks: false).Drawn(), IsBlue));
        Assert.Empty(Lines(MinorGridOnly(Theme.MatplotlibClassic, AxisScale.Linear, minorTicks: false).Drawn(), IsBlue));
        Assert.NotEmpty(Lines(MinorGridOnly(Theme.MatplotlibV2, AxisScale.Linear, minorTicks: true).Drawn(), IsBlue));
        Assert.NotEmpty(Lines(MinorGridOnly(Theme.Default, AxisScale.Linear, minorTicks: false).Drawn(), IsBlue));
    }

    [Fact]
    public void AMajorGridLine_IsDrawnOnlyWhereThePlotHoldsItsTick()
    {
        // A fixed set of ticks that runs past the range: the lines at -1 and 3 would stand over the margins.
        var ctx = new RecordingRenderContext();
        var plot = new Rect(0, 0, 400, 300);

        ctx.RenderGridLines(EdgeTerms.Bottom, [-1.0, 0.0, 1.0, 2.0, 3.0],
            new DataTransform(0, 2, 0, 2, plot), new StrokeStyle(Colors.Gray, 1.0, LineStyle.Solid), Theme.Default);

        var xs = ctx.Of<DrawLineCall>().Select(c => (double)(c.P1).X).ToArray();
        Assert.Equal([0.0, 200.0, 400.0], xs);
    }

    [Fact]
    public void AMinorGridLine_ReadsTheAxisOwnMinorSpacing()
    {
        // Majors 0, 1, 2 and a minor spacing of 0.5: lines at 0.5 and 1.5 (-0.5 and 2.5 lie outside the plot).
        var ctx = new RecordingRenderContext();
        var plot = new Rect(0, 0, 400, 300);
        var axes = new Axes();
        axes.XAxis.MinorTicks = axes.XAxis.MinorTicks with { Spacing = 0.5 };

        ctx.RenderGridLines(EdgeTerms.Bottom, [0.0, 1.0, 2.0],
            new DataTransform(0, 2, 0, 2, plot), new StrokeStyle(Colors.Gray, 1.0, LineStyle.Solid), Theme.Default, axes.XAxis);

        var xs = ctx.Of<DrawLineCall>().Select(c => (double)(c.P1).X).ToArray();
        Assert.Equal([100.0, 300.0], xs);
    }

    private static string Svg3D(double? minorSpacing)
    {
        var figure = Plt.Create()
            .WithSize(600, 500)
            .AddSubPlot(1, 1, 1, ax => ax.Scatter3D([0.0, 1, 2, 3, 4], [0.0, 1, 2, 3, 4], [0.0, 1, 2, 3, 4]))
            .Build();
        var axes = figure.SubPlots[0];
        axes.Emit3DVertexData = true;
        axes.XAxis.TickLocator = new FixedLocator([0.0, 1, 2, 3, 4]);
        axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { Color = Colors.Blue };
        axes.XAxis.MinorTicks = axes.XAxis.MinorTicks with { Visible = true, Color = Colors.Red, Spacing = minorSpacing };
        return figure.ToSvg();
    }

    [Fact]
    public void The3DMinorTickMarks_StandBetweenTheMajorTicks_AtTheAxisOwnMinorSpacing()
    {
        // Majors at 0..4 and a minor spacing of 0.5: one minor mark halfway between each two neighbouring majors, and the
        // normalised coordinate is linear in the value, so halfway in value is halfway in it.
        var svg = Svg3D(0.5);
        double[] majors = svg.NormalisedXs(Colors.Blue.ToHex());
        double[] minors = svg.NormalisedXs(Colors.Red.ToHex());

        Assert.Equal(5, majors.Length);
        Assert.Equal(4, minors.Length);
        for (int i = 0; i < minors.Length; i++)
            Assert.Equal((majors[i] + majors[i + 1]) / 2, minors[i], 4);
    }

    [Fact]
    public void The3DMinorTickMarks_StandOnTheEdge_BelowTheLowestBarToo()
    {
        // Bars from x = 0: the margins run the cube below 0 (to about -0.13 here), so the five-way split of the first major
        // step reaches -0.1, on the edge below the lowest bar. matplotlib draws every tick in view, minor ones too.
        var figure = Plt.Create()
            .WithSize(600, 500)
            .AddSubPlot(1, 1, 1, ax => ax.Bar3D([0.0, 1, 2], [0.0, 1, 2], [1.0, 2, 3]))
            .Build();
        var axes = figure.SubPlots[0];
        axes.Emit3DVertexData = true;
        axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { Color = Colors.Blue };
        axes.XAxis.MinorTicks = axes.XAxis.MinorTicks with { Visible = true, Color = Colors.Red };

        var svg = figure.ToSvg();
        double[] majors = svg.NormalisedXs(Colors.Blue.ToHex());
        double[] minors = svg.NormalisedXs(Colors.Red.ToHex());

        // The lowest major mark stands at 0; a minor mark stands below it, at -0.1.
        Assert.NotEmpty(majors);
        Assert.Contains(minors, m => m < majors[0] - 1e-6);
    }

    [Fact]
    public void The3DMajorTickMarks_StandOnTheEdge_BelowTheLowestBarToo()
    {
        // A major tick at -0.1 lies inside the cube (its margins run below 0), below the lowest bar, and is drawn there.
        var figure = Plt.Create()
            .WithSize(600, 500)
            .AddSubPlot(1, 1, 1, ax => ax.Bar3D([0.0, 1, 2], [0.0, 1, 2], [1.0, 2, 3]))
            .Build();
        var axes = figure.SubPlots[0];
        axes.Emit3DVertexData = true;
        axes.XAxis.TickLocator = new FixedLocator([-0.1, 0.0, 1, 2]);
        axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { Color = Colors.Blue };

        Assert.Equal(4, figure.ToSvg().NormalisedXs(Colors.Blue.ToHex()).Length);
    }

    [Fact]
    public void The3DMinorTickMarks_WithoutASpacing_SplitEachMajorStepInFive()
    {
        Svg3D(null).AssertMinorMarksSplitEachMajorStepIn(5, 16);   // four between each two neighbouring majors, at a fifth of the step
    }
}
