// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies <see cref="ChartServices"/> parallel rendering behavior.</summary>
public class ParallelRenderingTests
{
    /// <summary>Verifies that parallel rendering of a 2x2 grid with mixed series types includes all subplot titles.</summary>
    [Fact]
    public void ParallelRender_MultipleSubplots_AllTitlesPresent()
    {
        string svg = Plt.Create()
            .AddSubPlot(2, 2, 1, ax => ax.WithTitle("TopLeft").Plot([1.0, 2.0], [3.0, 4.0]))
            .AddSubPlot(2, 2, 2, ax => ax.WithTitle("TopRight").Scatter([1.0, 2.0], [5.0, 6.0]))
            .AddSubPlot(2, 2, 3, ax => ax.WithTitle("BottomLeft").Bar(["A", "B"], [10.0, 20.0]))
            .AddSubPlot(2, 2, 4, ax => ax.WithTitle("BottomRight").Hist([1.0, 2.0, 3.0, 4.0]))
            .ToSvg();

        Assert.Contains("TopLeft", svg);
        Assert.Contains("TopRight", svg);
        Assert.Contains("BottomLeft", svg);
        Assert.Contains("BottomRight", svg);
    }

    /// <summary>Verifies that parallel rendering works correctly with a single subplot.</summary>
    [Fact]
    public void ParallelRender_SingleSubplot_Works()
    {
        string svg = Plt.Create()
            .WithTitle("Single")
            .Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0])
            .ToSvg();

        Assert.Contains("<svg", svg);
        Assert.Contains("Single", svg);
        Assert.Contains("<polyline", svg);
    }

    /// <summary>Verifies that parallel rendering of a 3x3 grid includes all nine subplot titles.</summary>
    [Fact]
    public void ParallelRender_ManySubplots_AllPresent()
    {
        var builder = Plt.Create();
        for (int i = 1; i <= 9; i++)
        {
            int idx = i; // capture by value
            builder = builder.AddSubPlot(3, 3, idx, ax => ax.WithTitle($"Sub{idx}").Plot([1.0, 2.0], [3.0, 4.0]));
        }

        string svg = builder.ToSvg();

        for (int i = 1; i <= 9; i++)
            Assert.Contains($"Sub{i}", svg);
    }
}
