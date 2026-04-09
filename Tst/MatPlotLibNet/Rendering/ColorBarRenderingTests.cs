// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies color bar rendering in SVG output.</summary>
public class ColorBarRenderingTests
{
    /// <summary>Verifies that a visible color bar renders gradient rectangles in SVG.</summary>
    [Fact]
    public void ColorBar_Visible_RendersGradientRects()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Heatmap(new double[,] { { 1, 2 }, { 3, 4 } })
                .WithColorBar())
            .ToSvg();

        // ColorBar renders as multiple small rects forming a gradient
        Assert.Contains("<rect", svg);
    }

    /// <summary>Verifies that a color bar with a label renders the label text.</summary>
    [Fact]
    public void ColorBar_WithLabel_RendersLabel()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Heatmap(new double[,] { { 1, 2 }, { 3, 4 } })
                .WithColorBar(cb => cb with { Label = "Temperature" }))
            .ToSvg();

        Assert.Contains("Temperature", svg);
    }

    /// <summary>Verifies that no color bar renders when not configured.</summary>
    [Fact]
    public void NoColorBar_NoExtraElements()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Heatmap(new double[,] { { 1, 2 }, { 3, 4 } }))
            .ToSvg();

        Assert.DoesNotContain("class=\"colorbar\"", svg);
    }

    /// <summary>Verifies that WithColorBar fluent method sets the property.</summary>
    [Fact]
    public void WithColorBar_SetsProperty()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Heatmap(new double[,] { { 1, 2 }, { 3, 4 } })
                .WithColorBar())
            .Build();

        Assert.NotNull(figure.SubPlots[0].ColorBar);
        Assert.True(figure.SubPlots[0].ColorBar!.Visible);
    }
}
