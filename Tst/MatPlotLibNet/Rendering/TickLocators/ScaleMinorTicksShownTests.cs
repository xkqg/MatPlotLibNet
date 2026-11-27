// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.TickLocators;

/// <summary>Under the matplotlib themes a log or a logit axis draws its minor tick marks with no call, as matplotlib 3.11.2 does: the
/// scale sets its minor locator (<c>LogLocator(subs='auto')</c>, <c>LogitLocator(minor=True)</c>), and every minor tick it places is
/// drawn (printed under both styles: 40 of 40 minor marks of a log Y axis visible, 22 of a logit one, with no
/// <c>minorticks_on()</c>). <c>minorticks_off()</c> takes them away; here that is <c>WithMinorTicks(false)</c> or any axis whose minor
/// ticks were hidden by hand (<c>HideAllAxes</c>), and the minor grid of such an axis goes with them, one rule for both. A linear axis
/// keeps none until asked, and the library's own themes keep their look: no minor mark until asked.</summary>
public sealed class ScaleMinorTicksShownTests
{
    private static readonly double V2MinorWidth = 0.6 * 100 / 72;
    private static readonly double ClassicMinorWidth = 0.5 * 100 / 72;

    private static List<(Point P1, Point P2, double Width, Color Color)> Lines(Figure figure)
    {
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);
        return [.. ctx.Of<DrawLineCall>()
            .Select(a => (a.P1, a.P2, a.Thickness, a.Color))];
    }

    /// <summary>The marks a minor tick draws: its width, and its size of 2 points (both styles), where the classic major mark is as
    /// wide but 4 points long; the end that meets the spine may reach into it.</summary>
    private static int MinorMarks(Figure figure, double width) => Lines(figure).Count(l =>
    {
        double length = Math.Abs(l.P2.X - l.P1.X) + Math.Abs(l.P2.Y - l.P1.Y);
        return Math.Abs(l.Width - width) < 1e-9 && length > 2.7 && length < 3.6;
    });

    private static Figure LogY(Theme theme, Action<AxesBuilder>? more = null) => Plt.Create()
        .WithSize(640, 480)
        .WithTheme(theme)
        .AddSubPlot(1, 1, 1, ax =>
        {
            ax.Plot([1.0, 10, 100, 1000], [1.0, 10, 100, 1000]).SetYScale(AxisScale.Log);
            more?.Invoke(ax);
        })
        .Build();

    public static TheoryData<Theme, int, double> MinorMarksOfALogAxis => new()
    {
        { Theme.MatplotlibV2, 26, V2MinorWidth },          // 26 minor ticks in view over 0.708 .. 1412, on the left edge
        { Theme.MatplotlibClassic, 48, ClassicMinorWidth }, // 24 minor ticks in view over 1 .. 1000; the classic style marks the right edge too (ytick.right)
    };

    [Theory]
    [MemberData(nameof(MinorMarksOfALogAxis))]
    public void ALogAxis_DrawsItsMinorMarks_WithNoCall(Theme theme, int marks, double width) =>
        Assert.Equal(marks, MinorMarks(LogY(theme), width));

    /// <summary>The Y axis' minor ticks coloured red, their visibility as given (<paramref name="visible"/>) or not given.</summary>
    private static int RedMinorMarks(Theme theme, bool? visible)
    {
        var figure = LogY(theme);
        var y = figure.SubPlots[0].YAxis;
        y.MinorTicks = visible is { } shown ? y.MinorTicks with { Color = Colors.Red, Visible = shown } : y.MinorTicks with { Color = Colors.Red };
        return Lines(figure).Count(l => l.Color == Colors.Red);
    }

    [Fact]
    public void UnderTheLibrarysTheme_ALogAxis_DrawsNoMinorMark_UntilAsked()
    {
        Assert.Equal(0, RedMinorMarks(Theme.Default, visible: null));
        Assert.Equal(26, RedMinorMarks(Theme.Default, visible: true));
        Assert.Equal(26, RedMinorMarks(Theme.MatplotlibV2, visible: null));
        Assert.Equal(0, RedMinorMarks(Theme.MatplotlibV2, visible: false));
    }

    [Fact]
    public void UnderV2_ALinearAxis_DrawsNoMinorMark_UntilAsked()
    {
        var figure = Plt.Create().WithSize(640, 480).WithTheme(Theme.MatplotlibV2)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 10], [0.0, 10])).Build();

        Assert.Equal(0, MinorMarks(figure, V2MinorWidth));
    }

    [Fact]
    public void UnderV2_ALogAxisWhoseMinorTicksAreSwitchedOff_DrawsNoMinorMark_NorAMinorGridLine()
    {
        var figure = LogY(Theme.MatplotlibV2, ax => ax.WithMinorTicks(false)
            .WithGrid(g => g with { Visible = true, Axis = GridAxis.Y, Which = GridWhich.Minor, Color = Colors.Blue }));

        Assert.Equal(0, MinorMarks(figure, V2MinorWidth));
        Assert.DoesNotContain(Lines(figure), l => l.Color == Colors.Blue);
    }

    [Fact]
    public void UnderV2_HidingEveryAxis_HidesTheMinorMarksOfALogAxis()
    {
        Assert.Equal(0, MinorMarks(LogY(Theme.MatplotlibV2, ax => ax.HideAllAxes()), V2MinorWidth));
    }

    [Fact]
    public void UnderV2_ALogitAxis_DrawsItsMinorMarks_WithNoCall()
    {
        var figure = Plt.Create().WithSize(640, 480).WithTheme(Theme.MatplotlibV2)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1], [0.09, 0.9]).SetYScale(AxisScale.Logit)).Build();

        Assert.True(MinorMarks(figure, V2MinorWidth) > 0);
    }

    [Fact]
    public void UnderV2_ATopAxisLinkedToALogPrimary_DrawsItsMinorMarks_WithNoCall()
    {
        // secondary_xaxis over a log axis: its functionlog scale's minor marks are drawn (32 of 32 printed visible).
        var linked = Plt.Create().WithSize(640, 480).WithTheme(Theme.MatplotlibV2)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 1000], [0.0, 1.0]).SetXScale(AxisScale.Log)
                .WithSecondaryXAxis(s => s.Link(AxisTransform.Linear(1))))
            .Build();

        // The bottom axis' 26 and the top one's 26.
        Assert.Equal(52, MinorMarks(linked, V2MinorWidth));
    }

    [Fact]
    public void AMinorTickSwitchedOffByHand_StaysOffThroughTheJson()
    {
        var off = LogY(Theme.MatplotlibV2, ax => ax.WithMinorTicks(false));

        var back = off.RoundTripped();

        Assert.Equal(0, MinorMarks(back, V2MinorWidth));
        Assert.Equal(26, MinorMarks(LogY(Theme.MatplotlibV2).RoundTripped(), V2MinorWidth));
        Assert.Equal(off.SubPlots[0].YAxis.MinorTicks, back.SubPlots[0].YAxis.MinorTicks);
    }

    [Fact]
    public void TheTicksOfANewAxis_AreNotGivenAVisibility_AndAVisibilityThatWasGivenIsKnownAsSuch()
    {
        var axis = new Axis();

        Assert.Null(axis.MinorTicks.GivenVisible);
        Assert.Null(axis.MajorTicks.GivenVisible);
        Assert.False(axis.MinorTicks.Visible);
        Assert.True(axis.MajorTicks.Visible);
        Assert.False((axis.MinorTicks with { Visible = false }).GivenVisible);
        Assert.True((axis.MinorTicks with { Visible = true }).Visible);
        Assert.True(new TickConfig().Visible);
    }
}
