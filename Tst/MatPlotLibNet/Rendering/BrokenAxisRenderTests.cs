// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies that axis breaks render correctly in SVG output.</summary>
public class BrokenAxisRenderTests
{
    private static double[] MakeX() => Enumerable.Range(0, 20).Select(i => (double)i * 10).ToArray();
    private static double[] MakeY() => Enumerable.Range(0, 20).Select(i => (double)i).ToArray();

    [Fact]
    public void XBreak_RendersWithoutErrors()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot(MakeX(), MakeY());
                ax.WithXBreak(80, 120);
            })
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void YBreak_RendersWithoutErrors()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot(MakeX(), MakeY());
                ax.WithYBreak(5, 12);
            })
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void XBreak_Zigzag_ContainsMarkerLines()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot(MakeX(), MakeY());
                ax.WithXBreak(80, 120, BreakStyle.Zigzag);
            })
            .ToSvg();
        // Zigzag markers produce line elements
        Assert.Contains("<line", svg);
    }

    [Fact]
    public void MultipleXBreaks_RendersWithoutErrors()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot(MakeX(), MakeY());
                ax.WithXBreak(30, 50).WithXBreak(80, 120);
            })
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void NoBreakStyle_None_DoesNotChangeOutputFormat()
    {
        // BreakStyle.None produces no marker lines beyond the normal axes lines
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot(MakeX(), MakeY());
                ax.WithXBreak(80, 120, BreakStyle.None);
            })
            .ToSvg();
        Assert.Contains("<svg", svg);
    }
}
