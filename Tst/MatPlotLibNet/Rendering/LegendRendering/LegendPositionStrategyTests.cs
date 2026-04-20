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
}
