// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Verifies <see cref="GridStyle"/> expanded properties and new enums (sub-phase 2E).</summary>
public class GridStyleTests
{
    [Fact]
    public void GridStyle_Defaults_Which_Major_Axis_Both()
    {
        var gs = new GridStyle();
        Assert.Equal(GridWhich.Major, gs.Which);
        Assert.Equal(GridAxis.Both, gs.Axis);
    }

    [Fact]
    public void GridStyle_Which_Minor_CanBeSet()
    {
        var gs = new GridStyle { Which = GridWhich.Minor };
        Assert.Equal(GridWhich.Minor, gs.Which);
    }

    [Fact]
    public void GridStyle_Axis_X_CanBeSet()
    {
        var gs = new GridStyle { Axis = GridAxis.X };
        Assert.Equal(GridAxis.X, gs.Axis);
    }

    [Fact]
    public void GridStyle_Axis_Y_CanBeSet()
    {
        var gs = new GridStyle { Axis = GridAxis.Y };
        Assert.Equal(GridAxis.Y, gs.Axis);
    }

    [Fact]
    public void GridWhich_HasThreeValues()
    {
        var values = Enum.GetValues<GridWhich>();
        Assert.Equal(3, values.Length);
        Assert.Contains(GridWhich.Major, values);
        Assert.Contains(GridWhich.Minor, values);
        Assert.Contains(GridWhich.Both, values);
    }

    [Fact]
    public void GridAxis_HasThreeValues()
    {
        var values = Enum.GetValues<GridAxis>();
        Assert.Equal(3, values.Length);
        Assert.Contains(GridAxis.X, values);
        Assert.Contains(GridAxis.Y, values);
        Assert.Contains(GridAxis.Both, values);
    }

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
}
