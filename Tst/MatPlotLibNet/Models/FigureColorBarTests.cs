// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies figure-level colorbar model and rendering.</summary>
public class FigureColorBarTests
{
    [Fact]
    public void FigureColorBar_DefaultsToNull()
    {
        var fig = new Figure();
        Assert.Null(fig.FigureColorBar);
    }

    [Fact]
    public void FigureBuilder_WithColorBar_SetsFigureColorBar()
    {
        var fig = Plt.Create()
            .WithColorBar()
            .Build();
        Assert.NotNull(fig.FigureColorBar);
        Assert.True(fig.FigureColorBar!.Visible);
    }

    [Fact]
    public void FigureBuilder_WithColorBar_Configure_AppliesOptions()
    {
        var fig = Plt.Create()
            .WithColorBar(cb => cb with { Label = "Intensity" })
            .Build();
        Assert.Equal("Intensity", fig.FigureColorBar!.Label);
    }

    [Fact]
    public void FigureColorBar_RendersWithoutError()
    {
        double[,] data = { { 0.1, 0.5 }, { 0.7, 1.0 } };
        var svg = Plt.Create()
            .WithColorBar(cb => cb with { Label = "Value" })
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(data))
            .Build()
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void FigureColorBar_WithLabel_RendersLabel()
    {
        double[,] data = { { 0.0, 1.0 }, { 0.5, 0.8 } };
        var svg = Plt.Create()
            .WithColorBar(cb => cb with { Label = "Temperature" })
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(data))
            .Build()
            .ToSvg();
        Assert.Contains("Temperature", svg);
    }

    [Fact]
    public void FigureColorBar_MultipleSubplots_ProducesValidSvg()
    {
        double[,] data = { { 0.0, 1.0 }, { 0.5, 0.8 } };
        var svg = Plt.Create()
            .WithColorBar()
            .AddSubPlot(1, 2, 1, ax => ax.Heatmap(data))
            .AddSubPlot(1, 2, 2, ax => ax.Heatmap(data))
            .Build()
            .ToSvg();
        Assert.Contains("<svg", svg);
    }
}
