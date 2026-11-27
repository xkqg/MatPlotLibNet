// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Verifies <see cref="GridStyle"/> expanded properties and new enums (sub-phase 2E).</summary>
public class GridStyleTests
{
    [Fact]
    public void AxesBuilder_WithGrid_FuncOverload()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.WithGrid(g => g with { Which = GridWhich.Both, Axis = GridAxis.X }))
            .Build();
        var axes = figure.SubPlots[0];
        Assert.Equal(GridWhich.Both, axes.Grid.Which);
        Assert.Equal(GridAxis.X, axes.Grid.Axis);
    }

    [Fact]
    public void AxesBuilder_WithGrid_FuncOverload_PreservesExisting()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .ShowGrid()
                .WithGrid(g => g with { Which = GridWhich.Minor }))
            .Build();
        var axes = figure.SubPlots[0];
        Assert.True(axes.Grid.Visible);
        Assert.Equal(GridWhich.Minor, axes.Grid.Which);
    }

    /// <summary>A grid's stroke is its colour, width and dash pattern: the one place a grid becomes a line.</summary>
    [Fact]
    public void Stroke_IsTheColourTheWidthAndTheLineStyle()
    {
        var grid = new GridStyle { Color = Colors.Red, LineWidth = 1.25, LineStyle = LineStyle.Dotted };

        Assert.Equal(new StrokeStyle(Colors.Red, 1.25, LineStyle.Dotted), grid.Stroke);
    }

    /// <summary>The stroke follows a change of the grid, and does not read whether the grid is shown.</summary>
    [Fact]
    public void Stroke_FollowsTheGrid()
    {
        var grid = new GridStyle { Color = Colors.Blue, LineWidth = 2 };

        Assert.Equal(Colors.Blue, (grid with { Visible = true }).Stroke.Color);
        Assert.Equal(grid.Stroke, (grid with { Visible = true }).Stroke);
        Assert.Equal(LineStyle.Solid, grid.Stroke.Style);
    }

    /// <summary>The plain grid is matplotlib's: <c>#B0B0B0</c>, solid, 0.8 wide. It is hidden until a theme shows it and keeps the
    /// opacity every grid starts with.</summary>
    [Fact]
    public void Plain_IsMatplotlibsGridLook()
    {
        var plain = GridStyle.Plain;

        Assert.Equal(Color.FromHex("#B0B0B0"), plain.Color);
        Assert.Equal(LineStyle.Solid, plain.LineStyle);
        Assert.Equal(0.8, plain.LineWidth);
        Assert.False(plain.Visible);
        Assert.Equal(new GridStyle().Alpha, plain.Alpha);
    }

    /// <summary>The two themes that wear the plain grid shown wear exactly it, shown.</summary>
    [Fact]
    public void TheDefaultThemes_WearThePlainGridShown()
    {
        var shown = GridStyle.Plain with { Visible = true };

        Assert.Equal(shown, Theme.Default.DefaultGrid);
        Assert.Equal(shown, Theme.ColorBlindSafe.DefaultGrid);
    }

    /// <summary>The grid a figure writes nothing for is the grid at rest: what a new <see cref="GridStyle"/> says.</summary>
    [Fact]
    public void AtRest_IsWhatANewGridSays()
    {
        Assert.Equal(new GridStyle(), GridStyle.AtRest);
        Assert.Equal(Colors.GridGray, GridStyle.AtRest.Color);
        Assert.Equal(0.5, GridStyle.AtRest.LineWidth);
    }
}
