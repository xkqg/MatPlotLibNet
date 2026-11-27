// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The one place an axis direction picks the X or the Y member of a pair (<see cref="AlongExtensions"/>): each member
/// answers the X member along X and the Y member otherwise, and builds a point from a distance along and one across.</summary>
public class AlongExtensionsTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheAxisAndItsBreaks_AreTheOnesAlongTheDirection(bool isX)
    {
        var axes = new Axes();
        axes.AddXBreak(2, 3);
        axes.AddYBreak(5, 6);
        axes.AddYBreak(7, 8);

        Assert.Same(isX ? axes.XAxis : axes.YAxis, axes.AxisAlong(isX));
        Assert.Same(isX ? axes.XBreaks : axes.YBreaks, axes.BreaksAlong(isX));
    }

    [Fact]
    public void ARectangle_HasItsWidthStartAndEndAlongX_AndItsHeightTopAndBottomAlongY()
    {
        var rect = new Rect(10, 20, 300, 40);

        Assert.Equal(300, rect.LengthAlong(true));
        Assert.Equal(40, rect.LengthAlong(false));
        Assert.Equal(10, rect.StartAlong(true));
        Assert.Equal(20, rect.StartAlong(false));
        Assert.Equal(310, rect.EndAlong(true));
        Assert.Equal(60, rect.EndAlong(false));
    }

    [Fact]
    public void ALabel_ReachesItsHeightAcrossAnEdgeAlongX_AndItsWidthAcrossOneAlongY()
    {
        var size = new TextExtent(42, 12, 9);

        Assert.Equal(12, size.ExtentAcross(true));
        Assert.Equal(42, size.ExtentAcross(false));
    }

    [Fact]
    public void ARange_HasItsXEndsAlongX_AndItsYEndsAlongY_InTheOrderGiven()
    {
        var range = new DataRange(1, 2, 30, -4);

        Assert.Equal(new Range1D(1, 2), range.Along(true));
        Assert.Equal(new Range1D(30, -4), range.Along(false));
    }

    [Fact]
    public void AContribution_HasItsXEndsAlongX_AndItsYEndsAlongY_MissingOnesIncluded()
    {
        var contribution = new DataRangeContribution(1, null, null, 4);

        Assert.Equal(1, contribution.MinAlong(true));
        Assert.Null(contribution.MaxAlong(true));
        Assert.Null(contribution.MinAlong(false));
        Assert.Equal(4, contribution.MaxAlong(false));
    }

    [Fact]
    public void AnAxesContext_HasItsXLimitsAlongX_AndItsYLimitsAlongY()
    {
        var axes = new Axes();
        axes.XAxis.Min = 1;
        axes.XAxis.Max = 2;
        axes.YAxis.Min = 3;
        IAxesContext context = new AxesContextAdapter(axes);

        Assert.Equal(1, context.LimitMinAlong(true));
        Assert.Equal(2, context.LimitMaxAlong(true));
        Assert.Equal(3, context.LimitMinAlong(false));
        Assert.Null(context.LimitMaxAlong(false));
    }

    [Fact]
    public void APoint_StandsAtTheDistanceAlongOnXAndAcrossOnY_OrTheOtherWayRound()
    {
        Assert.Equal(new Point(5, 9), Point.Along(true, 5, 9));
        Assert.Equal(new Point(9, 5), Point.Along(false, 5, 9));
    }

    [Fact]
    public void APoint_HasItsXAlongXAndItsYAcross_AndTheOtherWayRoundAlongY()
    {
        var point = new Point(5, 9);

        Assert.Equal(5, point.CoordinateAlong(true));
        Assert.Equal(9, point.CoordinateAlong(false));
        Assert.Equal(9, point.CoordinateAcross(true));
        Assert.Equal(5, point.CoordinateAcross(false));
    }

    [Fact]
    public void ARectangle_StandsFromItsStartAlongAndAcross_OrTheOtherWayRound()
    {
        Assert.Equal(new Rect(10, 20, 300, 40), Rect.Along(true, 10, 300, 20, 40));
        Assert.Equal(new Rect(20, 10, 40, 300), Rect.Along(false, 10, 300, 20, 40));
    }

    [Fact]
    public void ARectangle_HasItsCentreAlong_AndMovesItsStartAlongAlone()
    {
        var rect = new Rect(10, 20, 300, 40);

        Assert.Equal(160, rect.CentreAlong(true));
        Assert.Equal(40, rect.CentreAlong(false));
        Assert.Equal(new Rect(7, 20, 300, 40), rect.WithStartAlong(true, 7));
        Assert.Equal(new Rect(10, 7, 300, 40), rect.WithStartAlong(false, 7));
    }

    /// <summary>A fraction of the way along counts the way a value grows on the screen: rightward from the left edge along X, upward
    /// from the bottom edge along Y (a colour bar's ticks, its level lines).</summary>
    [Fact]
    public void AFractionAlongARectangle_GrowsRightwardAlongX_AndUpwardAlongY()
    {
        var rect = new Rect(10, 20, 300, 40);

        Assert.Equal(10 + 300 * 0.25, rect.FractionAlong(true, 0.25));
        Assert.Equal(20 + 40 * (1 - 0.25), rect.FractionAlong(false, 0.25));
    }

    /// <summary>Values grow rightward along X and upward along Y, against the pixels: a rectangle's low value end is its left edge or
    /// its bottom edge, its high value end its right edge or its top edge, and a step past an end is the growth times its length.</summary>
    [Fact]
    public void ValuesGrowRightwardAlongX_AndUpwardAlongY()
    {
        var rect = new Rect(10, 20, 300, 40);

        Assert.Equal(10, rect.ValueStartAlong(true));
        Assert.Equal(60, rect.ValueStartAlong(false));
        Assert.Equal(310, rect.ValueEndAlong(true));
        Assert.Equal(20, rect.ValueEndAlong(false));
        Assert.Equal(1, Rect.ValueGrowthAlong(true));
        Assert.Equal(-1, Rect.ValueGrowthAlong(false));
    }


    [Fact]
    public void AnAxesContext_HasItsXScaleAlongX_AndItsYScaleAlongY()
    {
        var axes = new Axes();
        axes.XAxis.Scale = AxisScale.Log;
        IAxesContext context = new AxesContextAdapter(axes);

        Assert.Equal(AxisScale.Log, context.ScaleAlong(true));
        Assert.Equal(AxisScale.Linear, context.ScaleAlong(false));
    }

    [Fact]
    public void AnOrientation_IsHorizontalAlongX_AndVerticalAlongY()
    {
        Assert.Equal(Orientation.Horizontal, Orientation.Along(true));
        Assert.Equal(Orientation.Vertical, Orientation.Along(false));
    }

    /// <summary>A bar's orientation names the axis its values run along: a bar lies down (horizontal) when its values run along X.</summary>
    [Fact]
    public void ABarsOrientation_LiesDownWhenItsValuesRunAlongX()
    {
        Assert.Equal(BarOrientation.Horizontal, BarOrientation.ValuesAlong(true));
        Assert.Equal(BarOrientation.Vertical, BarOrientation.ValuesAlong(false));
    }

    /// <summary>Each orientation says once which way it runs: a line, a span, a colour bar and a Sankey diagram that are horizontal
    /// run along X; bars that lie down have their values along X; and the way back from a direction gives the same orientation.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EveryOrientation_SaysWhetherItRunsAlongX_AndADirectionGivesItBack(bool alongX)
    {
        Assert.Equal(alongX, Orientation.Along(alongX).RunsAlongX);
        Assert.Equal(alongX, BarOrientation.ValuesAlong(alongX).ValuesAlongX);
        Assert.Equal(alongX, (alongX ? ColorBarOrientation.Horizontal : ColorBarOrientation.Vertical).RunsAlongX);
        Assert.Equal(alongX, (alongX ? SankeyOrientation.Horizontal : SankeyOrientation.Vertical).FlowsAlongX);
    }

    /// <summary>The scale bars measure their values on: the Y axis' for upright bars, the X axis' for bars that lie down.</summary>
    [Fact]
    public void TheValueScaleOfBars_IsTheScaleOfTheAxisTheirValuesRunAlong()
    {
        var axes = new Axes();
        axes.XAxis.Scale = AxisScale.Log;
        IAxesContext context = new AxesContextAdapter(axes);

        Assert.Same(context.XScale.Rule, BarOrientation.Horizontal.ValueScale(context));
        Assert.Same(context.YScale.Rule, BarOrientation.Vertical.ValueScale(context));
    }

    [Fact]
    public void ARectangle_TakesANewLengthAlongADirection_KeepingItsStartAndItsSizeAcross()
    {
        var rect = new Rect(10, 20, 300, 40);

        Assert.Equal(new Rect(10, 20, 120, 40), rect.WithLengthAlong(true, 120));
        Assert.Equal(new Rect(10, 20, 300, 12), rect.WithLengthAlong(false, 12));
    }

    [Fact]
    public void ASize_HasItsWidthAlongX_AndItsHeightAlongY()
    {
        Assert.Equal(30, new Size(30, 7).LengthAlong(true));
        Assert.Equal(7, new Size(30, 7).LengthAlong(false));
    }

    /// <summary>A cell stands on the far edge of its grid along X when it touches the right edge, along Y when it touches the
    /// bottom one.</summary>
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ACell_StandsOnTheFarEdgeAlongADirection(bool right, bool bottom)
    {
        var cell = new Sides<bool>(Left: false, Bottom: bottom, Top: false, Right: right);

        Assert.Equal(right, cell.EndAlong(true));
        Assert.Equal(bottom, cell.EndAlong(false));
    }

    /// <summary>A direction starts at the left side across the grid and at the top side down it.</summary>
    [Fact]
    public void FourSides_StartAtTheLeftAcrossAndAtTheTopDown()
    {
        var sides = new Sides<double>(Left: 1, Bottom: 2, Top: 3, Right: 4);

        Assert.Equal(1, sides.StartAlong(true));
        Assert.Equal(3, sides.StartAlong(false));
        Assert.Equal(4, sides.EndAlong(true));
        Assert.Equal(2, sides.EndAlong(false));
    }

    /// <summary>Writing the side a direction ends at changes the right side across the grid and the bottom side down it, and no
    /// other side.</summary>
    [Fact]
    public void FourSides_TakeANewEndAtTheRightAcrossAndAtTheBottomDown()
    {
        var sides = new Sides<double>(Left: 1, Bottom: 2, Top: 3, Right: 4);

        Assert.Equal(new Sides<double>(Left: 1, Bottom: 2, Top: 3, Right: 9), sides.WithEndAlong(true, 9));
        Assert.Equal(new Sides<double>(Left: 1, Bottom: 9, Top: 3, Right: 4), sides.WithEndAlong(false, 9));
    }

    /// <summary>The gap between columns runs across the grid, the gap between rows down it.</summary>
    [Fact]
    public void TheBoundsOfAGap_AreTheColumnGapsAcrossAndTheRowGapsDown()
    {
        var bounds = LayoutBounds.Library with { ColumnGap = new MarginBounds(1, 2), RowGap = new MarginBounds(3, 4) };

        Assert.Equal(new MarginBounds(1, 2), bounds.GapAlong(true));
        Assert.Equal(new MarginBounds(3, 4), bounds.GapAlong(false));
    }

    /// <summary>A contribution that reaches from one value to another on the X axis along X, on the Y axis otherwise.</summary>
    [Fact]
    public void AContributionAlongADirection_ReachesOnlyThatAxis()
    {
        Assert.Equal(new DataRangeContribution(1, 4, null, null), DataRangeContribution.Along(true, 1, 4));
        Assert.Equal(new DataRangeContribution(null, null, 1, 4), DataRangeContribution.Along(false, 1, 4));
    }

    private static readonly DataRangeContribution Upright = new(1, 2, 3, 4,
        StickyXMin: 5, StickyXMax: 6, StickyYMin: 7, StickyYMax: 8, StickyZMin: 9, StickyZMax: 10, ZMin: 11, ZMax: 12);

    /// <summary>A range measured upright is put on the axes upright bars stand on as it is.</summary>
    [Fact]
    public void OnAxes_UprightBars_KeepTheRangeAsMeasured()
    {
        Assert.Equal(Upright, Upright.OnAxes(BarOrientation.Vertical));
    }

    /// <summary>A range measured upright is put on the axes bars that lie down stand on with X and Y swapped.</summary>
    [Fact]
    public void OnAxes_BarsThatLieDown_SwapXAndY_TheirStickyEdgesWithThem_AndKeepZ()
    {
        var lying = Upright.OnAxes(BarOrientation.Horizontal);

        Assert.Equal(new DataRangeContribution(3, 4, 1, 2,
            StickyXMin: 7, StickyXMax: 8, StickyYMin: 5, StickyYMax: 6, StickyZMin: 9, StickyZMax: 10, ZMin: 11, ZMax: 12), lying);
    }

    /// <summary>A pair holds one value per direction: a direction picks its own, by reference, and a pair with one member changed keeps
    /// the other.</summary>
    [Fact]
    public void APair_AnswersItsXAlongX_AndItsYAlongY_AndChangesOnlyTheMemberAlongADirection()
    {
        var pair = new AxisPair<string>("x", "y");

        Assert.Equal("x", pair.Along(true));
        Assert.Equal("y", pair.Along(false));
        Assert.True(Unsafe.AreSame(in pair.X, in pair.Along(true)));
        var acrossChanged = pair.With(true, "z");
        Assert.Equal("z", acrossChanged.X);
        Assert.Equal("y", acrossChanged.Y);
        var downChanged = pair.With(false, "z");
        Assert.Equal("x", downChanged.X);
        Assert.Equal("z", downChanged.Y);
    }

    /// <summary>A point with its coordinate along a direction set, and the share of a rectangle a coordinate stands at, counted the way
    /// a value grows on the screen (<c>FractionAlong</c> read back).</summary>
    [Fact]
    public void APointsCoordinateAlongADirection_IsSet_AndAFractionAlongARectangle_IsReadBack()
    {
        var point = new Point(1, 2);
        var rect = new Rect(10, 20, 200, 40);

        Assert.Equal(new Point(9, 2), point.WithCoordinateAlong(true, 9));
        Assert.Equal(new Point(1, 9), point.WithCoordinateAlong(false, 9));
        Assert.Equal(0.25, rect.FractionAt(true, rect.FractionAlong(true, 0.25)));
        Assert.Equal(0.25, rect.FractionAt(false, rect.FractionAlong(false, 0.25)));
    }

    /// <summary>A straight segment runs along X when its ends share their Y, along Y when they share their X; a slanted one and a
    /// point run neither way, and only the slanted one (its ends share neither coordinate) is slanted. A missing coordinate shares
    /// nothing.</summary>
    [Theory]
    [InlineData(0, 5, 9, 5, true, false)]
    [InlineData(4, 0, 4, 9, false, false)]
    [InlineData(0, 0, 3, 4, null, true)]
    [InlineData(2, 2, 2, 2, null, false)]
    [InlineData(double.NaN, 5, 9, 5, true, false)]
    [InlineData(double.NaN, 0, 3, double.NaN, null, true)]
    public void AStraightSegment_RunsAlongTheDirectionItsEndsDoNotShare(double x1, double y1, double x2, double y2, bool? alongX, bool slanted)
    {
        var segment = new MatPlotLibNet.Rendering.SeriesRenderers.PixelSegment(new Point(x1, y1), new Point(x2, y2));

        Assert.Equal(alongX, segment.StraightAlongX);
        Assert.Equal(slanted, segment.Slanted);
    }
}
