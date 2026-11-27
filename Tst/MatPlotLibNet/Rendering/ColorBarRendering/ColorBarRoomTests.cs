// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.ColorBarRendering;

/// <summary>Where a colour bar stands and the room it takes is one value of three: the library's margin, matplotlib's slot of the
/// axes' cell (<c>make_axes_gridspec</c>) and matplotlib's constrained margin (<c>make_axes</c> under the constrained layout). Each
/// says what it reads of the axes, how far its bar reaches and when the layout settles it.</summary>
public sealed class ColorBarRoomTests
{
    private static readonly ColorBar Beside = new() { Visible = true };
    private static readonly ColorBar Below = Beside with { Orientation = ColorBarOrientation.Horizontal };

    /// <summary>What an axes draws past its spines, counting what is asked of it.</summary>
    private sealed class CountingBands(EdgeBands ends, EdgeBands edges, double past) : IAxesBands
    {
        public int EndsAsked, EdgesAsked, PastAsked;
        public EdgeBands Ends() { EndsAsked++; return ends; }
        public EdgeBands Edges() { EdgesAsked++; return edges; }
        public double PastRightSpine() { PastAsked++; return past; }
    }

    private static CountingBands Bands() => new(
        new EdgeBands(0, 0, 0, Bottom: 22, BottomTitle: 44),
        new EdgeBands(0, Right: 12, 0, Bottom: 22, BottomTitle: 44, Overhang: new SideOverhang(0, 18)),
        past: 37);

    [Fact]
    public void TheRoom_FollowsTheThemeAndTheConstrainedLayout()
    {
        Assert.Same(ColorBarRoom.Margin, ColorBarRoom.Under(Theme.Default, constrained: false));
        Assert.Same(ColorBarRoom.Margin, ColorBarRoom.Under(Theme.Default, constrained: true));
        Assert.Same(ColorBarRoom.Slot, ColorBarRoom.Under(Theme.MatplotlibV2, constrained: false));
        Assert.Same(ColorBarRoom.ConstrainedMargin, ColorBarRoom.Under(Theme.MatplotlibClassic, constrained: true));
    }

    [Fact]
    public void OnlyTheSlot_TakesTheCell_AndBothMatplotlibRooms_AnchorTheAxesTowardsTheBar()
    {
        Assert.True(ColorBarRoom.Slot.TakesCell);
        Assert.False(ColorBarRoom.Margin.TakesCell);
        Assert.False(ColorBarRoom.ConstrainedMargin.TakesCell);
        Assert.True(ColorBarRoom.Slot.AnchorsAxes);
        Assert.True(ColorBarRoom.ConstrainedMargin.AnchorsAxes);
        Assert.False(ColorBarRoom.Margin.AnchorsAxes);
    }

    [Theory]
    [InlineData(nameof(BarRoomStage.Measure), true, false, false, true, false, false)]
    [InlineData(nameof(BarRoomStage.Sides), false, true, false, false, false, false)]
    [InlineData(nameof(BarRoomStage.Ends), false, false, false, false, true, false)]
    [InlineData(nameof(BarRoomStage.Last), false, false, true, false, false, true)]
    public void EachRoom_SettlesItsBarsAtOneStageOfTheLayout(string stageName,
        bool marginBeside, bool slotBeside, bool constrainedBeside, bool marginBelow, bool slotBelow, bool constrainedBelow)
    {
        var stage = Enum.Parse<BarRoomStage>(stageName);
        Assert.Equal(marginBeside, ColorBarRoom.Margin.SettlesAt(Beside, stage));
        Assert.Equal(slotBeside, ColorBarRoom.Slot.SettlesAt(Beside, stage));
        Assert.Equal(constrainedBeside, ColorBarRoom.ConstrainedMargin.SettlesAt(Beside, stage));
        Assert.Equal(marginBelow, ColorBarRoom.Margin.SettlesAt(Below, stage));
        Assert.Equal(slotBelow, ColorBarRoom.Slot.SettlesAt(Below, stage));
        Assert.Equal(constrainedBelow, ColorBarRoom.ConstrainedMargin.SettlesAt(Below, stage));
    }

    [Fact]
    public void TheSlot_AsksTheAxesNothing_AndStandsPastNothing()
    {
        var bands = Bands();

        Assert.Equal(0, ColorBarRoom.Slot.ClearanceOf(Beside, bands));
        Assert.Equal(0, ColorBarRoom.Slot.ClearanceOf(Below, bands));
        Assert.Equal((0, 0, 0), (bands.EndsAsked, bands.EdgesAsked, bands.PastAsked));
    }

    /// <summary>A margin room asks the bands what its bar needs: the bars beside and below stand 37 and 44 pixels clear, and the bands asked
    /// are the ones the room names.</summary>
    private static void AssertClearsAndAsks(ColorBarRoom room, int besideEnds, int besideEdges, int besidePast)
    {
        var beside = Bands();
        var below = Bands();

        Assert.Equal(37, room.ClearanceOf(Beside, beside));
        Assert.Equal(44, room.ClearanceOf(Below, below));
        Assert.Equal((besideEnds, besideEdges, besidePast), (beside.EndsAsked, beside.EdgesAsked, beside.PastAsked));
        Assert.Equal((1, 0, 0), (below.EndsAsked, below.EdgesAsked, below.PastAsked));
    }

    [Fact]
    public void TheLibrarysMargin_AsksWhatStandsPastTheRightSpineForABarBeside_AndTheEndBandsForABarBelow() =>
        AssertClearsAndAsks(ColorBarRoom.Margin, 0, 0, 1);

    [Fact]
    public void TheConstrainedMargin_AsksTheEdgeBandsAndWhatStandsPastTheRightSpineForABarBeside()
    {
        AssertClearsAndAsks(ColorBarRoom.ConstrainedMargin, 0, 1, 1);
        // The overhanging X tick label when it stands out farther than the band past the spine.
        Assert.Equal(18, ColorBarRoom.ConstrainedMargin.ClearanceOf(Beside, new CountingBands(default, new EdgeBands(0, 12, 0, 0, Overhang: new SideOverhang(0, 18)), 5)));
    }

    [Fact]
    public void ABarReachesOnlyOnItsOwnSide()
    {
        var ctx = new SvgRenderContext();
        var plot = new Rect(50, 20, 300, 200);

        foreach (var room in new[] { ColorBarRoom.Margin, ColorBarRoom.Slot, ColorBarRoom.ConstrainedMargin })
        {
            Assert.Equal(0, room.ReachBeside(Below, plot, [], ctx, Theme.MatplotlibV2, 0));
            Assert.Equal(0, room.ReachBelow(Beside, plot, Theme.MatplotlibV2, 0));
            Assert.Equal(room.ReachBeside(Beside, plot, [], ctx, Theme.MatplotlibV2, 3), room.ReachOf(Beside, plot, [], ctx, Theme.MatplotlibV2, 3));
            Assert.Equal(room.ReachBelow(Below, plot, Theme.MatplotlibV2, 3), room.ReachOf(Below, plot, [], ctx, Theme.MatplotlibV2, 3));
        }
    }

    [Fact]
    public void InTheLibrarysMargin_ABarReachesThePaddingTheBarAndItsTextPastTheClearance()
    {
        var ctx = new SvgRenderContext();
        var plot = new Rect(50, 20, 300, 200);

        double bare = ColorBarRoom.Margin.ReachBeside(Beside, plot, [], ctx, Theme.Default, 0);
        Assert.Equal(bare + 37, ColorBarRoom.Margin.ReachBeside(Beside, null, [], ctx, Theme.Default, 37), 9);
        Assert.Equal(Beside.Padding + Beside.Width, bare - ColorBarRoom.Margin.ReachBeside(Beside with { Padding = 0, Width = 0 }, plot, [], ctx, Theme.Default, 0), 9);
        // Below: as thick as its length over its aspect where the plot is known, its resting width where it is not.
        var square = Below with { Aspect = 0 };
        Assert.Equal(ColorBarRoom.Margin.ReachBelow(square, null, Theme.Default, 0), ColorBarRoom.Margin.ReachBelow(square, plot, Theme.Default, 0), 9);
        Assert.NotEqual(ColorBarRoom.Margin.ReachBelow(Below, null, Theme.Default, 0), ColorBarRoom.Margin.ReachBelow(Below, plot, Theme.Default, 0));
    }

    [Fact]
    public void TheConstrainedMarginsBar_ReachesNothingWithoutAPlot_AndPastItsBodyWithOne()
    {
        var ctx = new SvgRenderContext();
        var plot = new Rect(50, 20, 300, 200);

        Assert.Equal(0, ColorBarRoom.ConstrainedMargin.ReachBeside(Beside, null, [], ctx, Theme.MatplotlibV2, 0));
        Assert.Equal(0, ColorBarRoom.ConstrainedMargin.ReachBelow(Below, null, Theme.MatplotlibV2, 0));
        var body = ColorBarRoom.ConstrainedMargin.BodyOf(Beside, plot, null, 4);
        Assert.Equal(plot.Right + 4 + 0.05 * plot.Width, body.X, 9);
        Assert.True(ColorBarRoom.ConstrainedMargin.ReachBeside(Beside, plot, [], ctx, Theme.MatplotlibV2, 4) > body.Right - plot.Right);
        var under = ColorBarRoom.ConstrainedMargin.BodyOf(Below, plot, null, 4);
        Assert.Equal(plot.Bottom + 4 + 0.05 * plot.Height, under.Y, 9);
        Assert.True(ColorBarRoom.ConstrainedMargin.ReachBelow(Below, plot, Theme.MatplotlibV2, 4) > under.Bottom - plot.Bottom);
    }

    [Fact]
    public void TheSlotsBar_ReachesOnlyWhatStandsPastItsCell()
    {
        var ctx = new SvgRenderContext();
        var roomy = new Rect(0, 0, 2000, 2000);
        var tight = new Rect(0, 0, 100, 100);

        Assert.Equal(0, ColorBarRoom.Slot.ReachBeside(Beside, null, [], ctx, Theme.MatplotlibV2, 0));
        Assert.Equal(0, ColorBarRoom.Slot.ReachBelow(Below, null, Theme.MatplotlibV2, 0));
        Assert.Equal(0, ColorBarRoom.Slot.ReachBelow(Below, roomy with { Height = 20000 }, Theme.MatplotlibV2, 0));
        Assert.True(ColorBarRoom.Slot.ReachBeside(Beside, tight, [], ctx, Theme.MatplotlibV2, 0) > 0);
        Assert.True(ColorBarRoom.Slot.ReachBelow(Below, tight, Theme.MatplotlibV2, 0) > 0);
    }

    [Fact]
    public void TheSlotsBar_StandsInTheLastFractionOfTheCell_AndTheMarginsPastThePlotByItsPadding()
    {
        var cell = new Rect(100, 72, 620, 462);

        var slot = ColorBarRoom.Slot.BodyOf(Beside, cell, cell, 99);
        Assert.Equal(100 + 620 * 0.85, slot.X, 9);
        Assert.Equal(462 / 20.0, slot.Width, 9);
        var margin = ColorBarRoom.Margin.BodyOf(Beside, cell, null, 7);
        Assert.Equal(cell.Right + 7 + Beside.Padding, margin.X, 9);
        Assert.Equal(Beside.Width, margin.Width, 9);
        var under = ColorBarRoom.Margin.BodyOf(Below, cell, null, 7);
        Assert.Equal(cell.Bottom + 7 + Below.Padding, under.Y, 9);
    }

    [Fact]
    public void TheSlotReadsTheCells_AndTheConstrainedMarginThePlots()
    {
        var figure = Plt.Create().WithSize(640, 480).WithTheme(Theme.MatplotlibV2).AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[,] { { 0, 1 }, { 2, 3 } }))
            .WithColorBar().Build();
        var grid = new FigureGrid(figure);
        var spacing = new SubPlotSpacing { MarginLeft = 80, MarginRight = 60, MarginTop = 40, MarginBottom = 50 };

        Assert.Equal(grid.Cells(40, spacing), ColorBarRoom.Slot.AreasAt(grid, spacing));
        Assert.Equal(grid.PlotRects(40, spacing), ColorBarRoom.ConstrainedMargin.AreasAt(grid, spacing));
        var plots = grid.PlotRects(40, spacing);
        Assert.Equal(grid.FigureBarCell(40, spacing), ColorBarRoom.Slot.FigureAreaAt(grid, spacing, plots));
        Assert.Equal(Rect.Enclosing(plots), ColorBarRoom.ConstrainedMargin.FigureAreaAt(grid, spacing, plots));
    }

    [Fact]
    public void AGapTheAuthorGave_WinsInTheConstrainedMargin_AndInTheSlotOfACellThatIsNotKnown()
    {
        var plot = new Rect(50, 20, 300, 200);
        var given = Beside with { Padding = 6 };

        Assert.Equal(plot.Right + 4 + 6, ColorBarRoom.ConstrainedMargin.BodyOf(given, plot, null, 4).X, 9);
        // The slot of a renderer told no cell finds the cell the plot was kept from, with the author's gap in it.
        var cell = new Rect(100, 72, 620, 462);
        Assert.Equal(ColorBarRoom.Slot.BodyOf(given, cell, cell, 0).X, ColorBarRoom.Slot.BodyOf(given, given.KeptOf(cell), null, 0).X, 9);
    }

    /// <summary>The 8 pixels between the widest tick label of a bar beside the plot and its label are the same in the room the layout
    /// keeps and in the drawing.</summary>
    [Fact]
    public void TheGutterBesideABar_IsOneValue_TheDrawingAndTheReserveRead()
    {
        Assert.Equal(8, ColorBarRoomExtensions.TextGutterBeside);
    }
}
