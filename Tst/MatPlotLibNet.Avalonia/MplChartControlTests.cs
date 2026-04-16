// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Avalonia;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Avalonia.Tests;

public class MplChartControlTests
{
    [Fact]
    public void FigureProperty_DefaultsToNull()
    {
        var ctrl = new MplChartControl();
        Assert.Null(ctrl.Figure);
    }

    [Fact]
    public void FigureProperty_CanBeSet()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        var ctrl = new MplChartControl { Figure = figure };
        Assert.Same(figure, ctrl.Figure);
    }

    [Fact]
    public void FigureProperty_Exists()
    {
        Assert.NotNull(MplChartControl.FigureProperty);
    }

    [Fact]
    public void IsInteractiveProperty_DefaultsFalse()
    {
        var ctrl = new MplChartControl();
        Assert.False(ctrl.IsInteractive);
    }

    [Fact]
    public void IsInteractiveProperty_CanBeSetTrue()
    {
        var ctrl = new MplChartControl { IsInteractive = true };
        Assert.True(ctrl.IsInteractive);
    }

    [Fact]
    public void IsInteractiveProperty_Exists()
    {
        Assert.NotNull(MplChartControl.IsInteractiveProperty);
    }
}
