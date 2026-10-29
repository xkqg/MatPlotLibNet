// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.LegendRendering;

namespace MatPlotLibNet.Tests.Rendering.LegendRendering;

/// <summary>
/// Phase B.2 (strict-90 floor plan) — equivalence tests for the
/// <see cref="LegendPositionStrategy"/> hierarchy. Each test asserts that the
/// strategy's <c>ComputeBox</c> output matches the OLD inline switch arm
/// formula from <c>AxesRenderer.RenderLegend</c> lines 291-307 byte-for-byte.
/// </summary>
public class LegendPositionStrategyTests
{
    private static readonly Rect PlotArea = new(80, 60, 640, 480);
    private const double BoxW = 100;
    private const double BoxH = 50;

    // Pre-computed expected coordinates from the OLD inline switch:
    private const double Inset = LegendPositionStrategy.Inset;        // 10
    private const double OutsideGap = LegendPositionStrategy.OutsideGap; // 8
    private static readonly double CenterX = PlotArea.X + PlotArea.Width / 2;   // 400
    private static readonly double CenterY = PlotArea.Y + PlotArea.Height / 2;  // 300

    [Fact] public void UpperRight_MatchesLegacy()
    {
        var (x, y) = LegendPositionStrategyFactory.Create(LegendPosition.UpperRight).ComputeBox(PlotArea, BoxW, BoxH);
        Assert.Equal(PlotArea.X + PlotArea.Width - BoxW - Inset, x);
        Assert.Equal(PlotArea.Y + Inset, y);
    }

    [Fact] public void UpperLeft_MatchesLegacy()
    {
        var (x, y) = LegendPositionStrategyFactory.Create(LegendPosition.UpperLeft).ComputeBox(PlotArea, BoxW, BoxH);
        Assert.Equal(PlotArea.X + Inset, x);
        Assert.Equal(PlotArea.Y + Inset, y);
    }

    [Fact] public void LowerRight_MatchesLegacy()
    {
        var (x, y) = LegendPositionStrategyFactory.Create(LegendPosition.LowerRight).ComputeBox(PlotArea, BoxW, BoxH);
        Assert.Equal(PlotArea.X + PlotArea.Width - BoxW - Inset, x);
        Assert.Equal(PlotArea.Y + PlotArea.Height - BoxH - Inset, y);
    }

    [Fact] public void LowerLeft_MatchesLegacy()
    {
        var (x, y) = LegendPositionStrategyFactory.Create(LegendPosition.LowerLeft).ComputeBox(PlotArea, BoxW, BoxH);
        Assert.Equal(PlotArea.X + Inset, x);
        Assert.Equal(PlotArea.Y + PlotArea.Height - BoxH - Inset, y);
    }

    [Fact] public void Right_MatchesLegacy()
    {
        var (x, y) = LegendPositionStrategyFactory.Create(LegendPosition.Right).ComputeBox(PlotArea, BoxW, BoxH);
        Assert.Equal(PlotArea.X + PlotArea.Width - BoxW - Inset, x);
        Assert.Equal(CenterY - BoxH / 2, y);
    }

    [Fact] public void CenterLeft_MatchesLegacy()
    {
        var (x, y) = LegendPositionStrategyFactory.Create(LegendPosition.CenterLeft).ComputeBox(PlotArea, BoxW, BoxH);
        Assert.Equal(PlotArea.X + Inset, x);
        Assert.Equal(CenterY - BoxH / 2, y);
    }

    [Fact] public void CenterRight_MatchesLegacy()
    {
        var (x, y) = LegendPositionStrategyFactory.Create(LegendPosition.CenterRight).ComputeBox(PlotArea, BoxW, BoxH);
        Assert.Equal(PlotArea.X + PlotArea.Width - BoxW - Inset, x);
        Assert.Equal(CenterY - BoxH / 2, y);
    }

    [Fact] public void LowerCenter_MatchesLegacy()
    {
        var (x, y) = LegendPositionStrategyFactory.Create(LegendPosition.LowerCenter).ComputeBox(PlotArea, BoxW, BoxH);
        Assert.Equal(CenterX - BoxW / 2, x);
        Assert.Equal(PlotArea.Y + PlotArea.Height - BoxH - Inset, y);
    }

    [Fact] public void UpperCenter_MatchesLegacy()
    {
        var (x, y) = LegendPositionStrategyFactory.Create(LegendPosition.UpperCenter).ComputeBox(PlotArea, BoxW, BoxH);
        Assert.Equal(CenterX - BoxW / 2, x);
        Assert.Equal(PlotArea.Y + Inset, y);
    }

    [Fact] public void Center_MatchesLegacy()
    {
        var (x, y) = LegendPositionStrategyFactory.Create(LegendPosition.Center).ComputeBox(PlotArea, BoxW, BoxH);
        Assert.Equal(CenterX - BoxW / 2, x);
        Assert.Equal(CenterY - BoxH / 2, y);
    }

    [Fact] public void OutsideRight_MatchesLegacy()
    {
        var (x, y) = LegendPositionStrategyFactory.Create(LegendPosition.OutsideRight).ComputeBox(PlotArea, BoxW, BoxH);
        Assert.Equal(PlotArea.X + PlotArea.Width + OutsideGap, x);
        Assert.Equal(CenterY - BoxH / 2, y);
    }

    [Fact] public void OutsideLeft_MatchesLegacy()
    {
        var (x, y) = LegendPositionStrategyFactory.Create(LegendPosition.OutsideLeft).ComputeBox(PlotArea, BoxW, BoxH);
        Assert.Equal(PlotArea.X - BoxW - OutsideGap, x);
        Assert.Equal(CenterY - BoxH / 2, y);
    }

    [Fact] public void OutsideTop_MatchesLegacy()
    {
        var (x, y) = LegendPositionStrategyFactory.Create(LegendPosition.OutsideTop).ComputeBox(PlotArea, BoxW, BoxH);
        Assert.Equal(CenterX - BoxW / 2, x);
        Assert.Equal(PlotArea.Y - BoxH - OutsideGap, y);
    }

    [Fact] public void OutsideBottom_MatchesLegacy()
    {
        var (x, y) = LegendPositionStrategyFactory.Create(LegendPosition.OutsideBottom).ComputeBox(PlotArea, BoxW, BoxH);
        Assert.Equal(CenterX - BoxW / 2, x);
        Assert.Equal(PlotArea.Y + PlotArea.Height + OutsideGap, y);
    }

    [Fact] public void Best_FallsBackToUpperRight()
    {
        var (x, y) = LegendPositionStrategyFactory.Create(LegendPosition.Best).ComputeBox(PlotArea, BoxW, BoxH);
        Assert.Equal(PlotArea.X + PlotArea.Width - BoxW - Inset, x);
        Assert.Equal(PlotArea.Y + Inset, y);
    }

    [Fact] public void UnknownEnumValue_FallsBackToUpperRight()
    {
        var (x, y) = LegendPositionStrategyFactory.Create((LegendPosition)999).ComputeBox(PlotArea, BoxW, BoxH);
        Assert.Equal(PlotArea.X + PlotArea.Width - BoxW - Inset, x);
        Assert.Equal(PlotArea.Y + Inset, y);
    }

    // ── Best, with the marks a chart drew ─────────────────────────────────

    /// <summary>The ten places <c>Best</c> tries, in matplotlib's order.</summary>
    private static readonly LegendPosition[] BestOrder =
    [
        LegendPosition.UpperRight, LegendPosition.UpperLeft, LegendPosition.LowerLeft, LegendPosition.LowerRight,
        LegendPosition.Right, LegendPosition.CenterLeft, LegendPosition.CenterRight, LegendPosition.LowerCenter,
        LegendPosition.UpperCenter, LegendPosition.Center,
    ];

    private static Point BoxOf(LegendPosition position) =>
        LegendPositionStrategyFactory.Create(position).ComputeBox(PlotArea, BoxW, BoxH);

    private static Point CentreOfBox(LegendPosition position)
    {
        var box = BoxOf(position);
        return new Point(box.X + BoxW / 2, box.Y + BoxH / 2);
    }

    private static MarkObstacles PointsAt(params LegendPosition[] taken)
    {
        var builder = new MarkObstacles.Builder();
        foreach (var position in taken) builder.AddPoint(CentreOfBox(position));
        return builder.Build();
    }

    [Fact] public void Best_WithNoMarks_IsTheUpperRightBox()
    {
        var best = new BestStrategy().ComputeBox(PlotArea, BoxW, BoxH, MarkObstacles.None);

        Assert.Equal(BoxOf(LegendPosition.UpperRight), best);
    }

    [Theory]
    [InlineData(1, LegendPosition.UpperLeft)]
    [InlineData(2, LegendPosition.LowerLeft)]
    [InlineData(3, LegendPosition.LowerRight)]
    [InlineData(4, LegendPosition.Right)]
    [InlineData(5, LegendPosition.CenterLeft)]
    [InlineData(7, LegendPosition.LowerCenter)]   // 6 is Center Right: the same box as Right
    [InlineData(8, LegendPosition.UpperCenter)]
    [InlineData(9, LegendPosition.Center)]
    public void Best_StandsAtTheFirstPlaceNothingCovers(int taken, LegendPosition expected)
    {
        var marks = PointsAt([.. BestOrder.Take(taken)]);

        Assert.Equal(BoxOf(expected), new BestStrategy().ComputeBox(PlotArea, BoxW, BoxH, marks));
    }

    [Fact] public void Best_WhenEveryPlaceIsCovered_StandsAtTheLeastCoveredOne()
    {
        var builder = new MarkObstacles.Builder();
        foreach (var position in BestOrder)
        {
            int covers = position == LegendPosition.LowerCenter ? 1 : 2;
            for (int i = 0; i < covers; i++) builder.AddPoint(CentreOfBox(position));
        }

        Assert.Equal(BoxOf(LegendPosition.LowerCenter), new BestStrategy().ComputeBox(PlotArea, BoxW, BoxH, builder.Build()));
    }

    /// <summary>A threshold line from one edge of the plot to the other, near the top: no vertex of it is inside any place
    /// (its ends are on the plot's edges), so counting vertices alone would keep the upper right with the line behind the
    /// legend. The segment crossing the places counts too, and the legend leaves for the first place the line misses.</summary>
    [Fact] public void Best_LeavesAPlaceAThresholdLineCrossesWithNoVertexInsideIt()
    {
        var line = new MarkObstacles.Builder()
            .AddPath([new Point(PlotArea.X, PlotArea.Y + 30), new Point(PlotArea.X + PlotArea.Width, PlotArea.Y + 30)])
            .Build();

        Assert.Equal(BoxOf(LegendPosition.LowerLeft), new BestStrategy().ComputeBox(PlotArea, BoxW, BoxH, line));
    }

    public static TheoryData<LegendPosition> FixedPositions => new(Enum.GetValues<LegendPosition>().Where(p => p != LegendPosition.Best));

    [Theory]
    [MemberData(nameof(FixedPositions))]
    public void AFixedPosition_IgnoresTheMarks(LegendPosition position)
    {
        var strategy = LegendPositionStrategyFactory.Create(position);
        var marks = PointsAt([.. BestOrder]);

        Assert.Equal(strategy.ComputeBox(PlotArea, BoxW, BoxH), strategy.ComputeBox(PlotArea, BoxW, BoxH, marks));
    }
}
