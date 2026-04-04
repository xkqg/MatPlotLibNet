// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.FluentApi;

public class PltTests
{
    [Fact]
    public void Figure_CreatesWithDimensions()
    {
        var fig = Plt.Figure(1024, 768);
        Assert.Equal(1024, fig.Width);
        Assert.Equal(768, fig.Height);
    }

    [Fact]
    public void Figure_DefaultDimensions()
    {
        var fig = Plt.Figure();
        Assert.Equal(800, fig.Width);
        Assert.Equal(600, fig.Height);
    }

    [Fact]
    public void Create_ReturnsFigureBuilder()
    {
        var builder = Plt.Create();
        Assert.NotNull(builder);
        Assert.IsType<FigureBuilder>(builder);
    }
}
