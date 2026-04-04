// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Maui.Tests;

public class MplChartViewTests
{
    [Fact]
    public void FigureProperty_DefaultsToNull()
    {
        var view = new MplChartView();
        Assert.Null(view.Figure);
    }

    [Fact]
    public void FigureProperty_CanBeSet()
    {
        var figure = Plt.Create().WithTitle("Test").Build();
        var view = new MplChartView { Figure = figure };
        Assert.Same(figure, view.Figure);
    }

    [Fact]
    public void FigureProperty_IsBindable()
    {
        // Verify the bindable property exists
        Assert.NotNull(MplChartView.FigureProperty);
    }

    [Fact]
    public void Drawable_IsNotNull()
    {
        var view = new MplChartView();
        Assert.NotNull(view.Drawable);
    }
}
