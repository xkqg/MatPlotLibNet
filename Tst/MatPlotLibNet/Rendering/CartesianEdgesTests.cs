// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>What the edges opposite the primary axes draw (<see cref="CartesianEdges"/>, <see cref="OppositeTicks"/>): the mirrored ticks the
/// axis asked for, with their labels and without the minor ticks (matplotlib's <c>tick_params</c> reaches the major ticks by default); the
/// marks of a theme that marks every side, the minor ones too (matplotlib classic's <c>xtick.top</c> and <c>ytick.right</c>); nothing where an
/// axis of its own is drawn on that edge (<c>twiny</c>, <c>twinx</c>), whose ticks the edge carries instead.</summary>
public class CartesianEdgesTests
{
    [Theory]
    [InlineData(false, false, false, "None")]
    [InlineData(true, false, false, "Mirrored")]
    [InlineData(false, true, false, "Marked")]
    [InlineData(true, true, false, "Mirrored")]
    [InlineData(true, true, true, "None")]
    [InlineData(false, true, true, "None")]
    public void TheOppositeEdge_IsOwnedByAnAxisFirst_ThenMirrored_ThenMarkedByTheTheme(bool mirrored, bool onAllSides, bool owned, string expected)
    {
        var drawn = OppositeTicks.Of(mirrored, onAllSides, owned);

        Assert.Equal(expected switch
        {
            "Mirrored" => OppositeTicks.Mirrored,
            "Marked" => OppositeTicks.Marked,
            _ => OppositeTicks.None,
        }, drawn);
    }

    [Fact]
    public void Mirrored_DrawsTheMajorTicksWithTheirLabels_AndNoMinorTick_Marked_TheMarksOfBoth()
    {
        Assert.Equal(new OppositeTicks(TickInk.Both, TickInk.None), OppositeTicks.Mirrored);
        Assert.Equal(new OppositeTicks(TickInk.Marks, TickInk.Marks), OppositeTicks.Marked);
        Assert.Equal(new OppositeTicks(TickInk.None, TickInk.None), OppositeTicks.None);
    }

    [Fact]
    public void AMirroredAxes_MirrorsBothAxes_UnderTheLibrarysTheme()
    {
        var axes = new Axes();
        axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { Mirror = true };
        axes.YAxis.MajorTicks = axes.YAxis.MajorTicks with { Mirror = true };

        Assert.Equal(new CartesianEdges(OppositeTicks.Mirrored, OppositeTicks.Mirrored), CartesianEdges.Of(axes, Theme.Default));
    }

    [Fact]
    public void UnderClassic_EveryEdgeIsMarked_AndUnderTheLibrarysThemesNone()
    {
        var axes = new Axes();

        Assert.Equal(new CartesianEdges(OppositeTicks.Marked, OppositeTicks.Marked), CartesianEdges.Of(axes, Theme.MatplotlibClassic));
        Assert.Equal(new CartesianEdges(OppositeTicks.None, OppositeTicks.None), CartesianEdges.Of(axes, Theme.Default));
    }

    /// <summary>A top X axis that is drawn (with series, or linked) owns the top edge, and a right Y axis that is drawn the right edge: the
    /// mirrored ticks and the theme's marks give way there. A top or right axis with neither is drawn nowhere and owns nothing.</summary>
    [Fact]
    public void ADrawnAxisOfItsOwn_TakesTheOppositeEdge()
    {
        var axes = new Axes();
        axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { Mirror = true };
        axes.YAxis.MajorTicks = axes.YAxis.MajorTicks with { Mirror = true };
        axes.TwinY();
        var right = axes.YAxes.Right.Add();

        Assert.Equal(new CartesianEdges(OppositeTicks.Mirrored, OppositeTicks.Mirrored), CartesianEdges.Of(axes, Theme.Default));

        axes.PlotXSecondary([0.0, 1.0], [0.0, 1.0]);
        right.Axis.Link = AxisTransform.Linear(2, 0);

        Assert.Equal(new CartesianEdges(OppositeTicks.None, OppositeTicks.None), CartesianEdges.Of(axes, Theme.MatplotlibClassic));
        Assert.Same(axes.SecondaryXAxis, axes.DrawnSecondaryXAxis);
    }

    [Fact]
    public void AnInkWithinAnother_KeepsWhatBothHold()
    {
        Assert.Equal(TickInk.Marks, TickInk.Both.Within(TickInk.Marks));
        Assert.Equal(TickInk.None, TickInk.Labels.Within(TickInk.Marks));
        Assert.Equal(TickInk.Labels, TickInk.Labels.Within(TickInk.Both));
        Assert.False(TickInk.None.DrawsLabels);
        Assert.False(TickInk.None.DrawsMarks);
    }

    /// <summary>An edge's opposite is the edge across the plot; each edge says how a title stands beside it and which side of the Y axis
    /// container stands on it.</summary>
    [Fact]
    public void EachEdge_NamesItsOpposite_ItsTitleRule_AndItsSideOfTheContainer()
    {
        EdgeTerms[] edges = [EdgeTerms.Bottom, EdgeTerms.Top, EdgeTerms.Left, EdgeTerms.Right];
        EdgeTerms[] opposites = [EdgeTerms.Top, EdgeTerms.Bottom, EdgeTerms.Right, EdgeTerms.Left];
        EdgeTitle[] titles = [EdgeTitle.Upright, EdgeTitle.Upright, EdgeTitle.Turned, EdgeTitle.Turned];
        AxisSide?[] sides = [AxisSide.Bottom, null, AxisSide.Left, AxisSide.Right];
        for (int i = 0; i < edges.Length; i++)
        {
            Assert.Same(opposites[i], edges[i].Opposite);
            Assert.Same(titles[i], edges[i].Title);
            Assert.Equal(sides[i], edges[i].ContainerSide);
        }
    }

    /// <summary>An upright title stands centred along the plot, its baseline past the tick labels out from the line its ticks hang from;
    /// below a bottom edge it is lower, above a top edge higher. An axis without a title draws nothing.</summary>
    [Fact]
    public void AnUprightTitle_StandsOutFromItsEdgeLine_PastTheTickLabels()
    {
        var plot = new Rect(100, 50, 400, 300);
        var axis = new Axis { Label = "x" };
        var theme = Theme.Default;
        var drawn = new RecordingRenderContext();

        EdgeTerms.Bottom.DrawTitle(drawn, new Axes(), axis, theme, plot, 350, 12);
        EdgeTerms.Top.DrawTitle(drawn, new Axes(), axis, theme, plot, 50, 12);
        EdgeTerms.Bottom.DrawTitle(drawn, new Axes(), new Axis(), theme, plot, 350, 12);

        var at = drawn.Of<DrawTextCall>().Select(call => call.Position).ToList();
        double offset = axis.XTitleBeside(EdgeTerms.Bottom, theme, 12, axis.TitleFontUnder(theme).Size).TitleOffset();
        Assert.Equal(2, at.Count);
        Assert.Equal(new Point(300, 350 + offset), at[0]);
        Assert.Equal(300, at[1].X);
        Assert.True(at[1].Y < 50);
    }

    /// <summary>A turned title stands beside a left or a right edge by the band rule every Y axis shares, centred along the plot's height.</summary>
    [Fact]
    public void ATurnedTitle_StandsBesideItsEdgeLine_ByTheYTitleBandRule()
    {
        var plot = new Rect(100, 50, 400, 300);
        var axes = new Axes();
        var axis = new Axis { Label = "y" };
        var drawn = new RecordingRenderContext();

        EdgeTerms.Right.DrawTitle(drawn, axes, axis, Theme.Default, plot, 520, 30);

        var call = Assert.Single(drawn.Of<DrawTextCall>());
        var position = call.Position;
        var extent = axis.YTitleExtent(axis.Label, axes.YAxes.TitleInputUnder(Theme.Default, AxisSide.Right, axis.MajorTicksUnder(Theme.Default).TickLabelBand(30)));
        Assert.Equal(new Point(520 + extent.TitleOffset, 200), position);
    }
}
