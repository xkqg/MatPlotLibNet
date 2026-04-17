// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Interaction;

public sealed class ToolbarTests
{
    [Fact]
    public void CreateDefault_2DFigure_HasPanZoomHome()
    {
        var fig = new Figure();
        fig.AddSubPlot();
        var toolbar = InteractionToolbar.CreateDefault(fig);
        Assert.Contains(toolbar.Buttons, b => b.Id == "pan");
        Assert.Contains(toolbar.Buttons, b => b.Id == "zoom");
        Assert.Contains(toolbar.Buttons, b => b.Id == "home");
    }

    [Fact]
    public void CreateDefault_3DFigure_IncludesRotate3D()
    {
        var fig = new Figure();
        var axes = fig.AddSubPlot();
        axes.CoordinateSystem = CoordinateSystem.ThreeD;
        var toolbar = InteractionToolbar.CreateDefault(fig);
        Assert.Contains(toolbar.Buttons, b => b.Id == "rotate3d");
    }

    [Fact]
    public void CreateDefault_2DFigure_NoRotate3D()
    {
        var fig = new Figure();
        fig.AddSubPlot();
        var toolbar = InteractionToolbar.CreateDefault(fig);
        Assert.DoesNotContain(toolbar.Buttons, b => b.Id == "rotate3d");
    }

    [Fact]
    public void Activate_ChangesActiveTool()
    {
        var fig = new Figure();
        fig.AddSubPlot();
        var toolbar = InteractionToolbar.CreateDefault(fig);
        Assert.Equal(InteractionToolbar.ToolMode.Pan, toolbar.ActiveTool);

        toolbar.Activate("zoom");
        Assert.Equal(InteractionToolbar.ToolMode.Zoom, toolbar.ActiveTool);
    }

    [Fact]
    public void ActiveToolId_MatchesActiveTool()
    {
        var fig = new Figure();
        fig.AddSubPlot();
        var toolbar = InteractionToolbar.CreateDefault(fig);
        Assert.Equal("pan", toolbar.ActiveToolId);

        toolbar.Activate("zoom");
        Assert.Equal("zoom", toolbar.ActiveToolId);
    }

    [Fact]
    public void ToolbarButton_IsToggle_ForModalTools()
    {
        var fig = new Figure();
        fig.AddSubPlot();
        var toolbar = InteractionToolbar.CreateDefault(fig);
        Assert.True(toolbar.Buttons.First(b => b.Id == "pan").IsToggle);
        Assert.False(toolbar.Buttons.First(b => b.Id == "home").IsToggle);
    }

    [Fact]
    public void DataCursorButton_Exists()
    {
        var fig = new Figure();
        fig.AddSubPlot();
        var toolbar = InteractionToolbar.CreateDefault(fig);
        Assert.Contains(toolbar.Buttons, b => b.Id == "cursor");
    }

    [Fact]
    public void ToolbarState_CapturesCurrentState()
    {
        var fig = new Figure();
        fig.AddSubPlot();
        var toolbar = InteractionToolbar.CreateDefault(fig);
        var state = new ToolbarState(toolbar.Buttons, toolbar.ActiveToolId, 10, 10, 32, 32);
        Assert.Equal("pan", state.ActiveToolId);
        Assert.Equal(32, state.ButtonWidth);
    }
}
