// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>Verifies SVG tooltip rendering behavior.</summary>
public class SvgTooltipTests
{
    /// <summary>Verifies that scatter series with tooltips enabled contains title elements.</summary>
    [Fact]
    public void ScatterSeries_WithTooltips_ContainsTitleElements()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTooltips()
                .Scatter([1.0, 2.0], [3.0, 4.0]))
            .ToSvg();

        Assert.Contains("<title>", svg);
    }

    /// <summary>Verifies that scatter series without tooltips does not contain title elements.</summary>
    [Fact]
    public void Tooltips_Disabled_NoTitleElements()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Scatter([1.0, 2.0], [3.0, 4.0]))
            .ToSvg();

        Assert.DoesNotContain("<title>", svg);
    }

    /// <summary>Verifies that tooltip text includes the x and y data values for each point.</summary>
    [Fact]
    public void ScatterSeries_WithTooltips_ContainsDataValues()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTooltips()
                .Scatter([1.5], [3.2]))
            .ToSvg();

        Assert.Contains("x=1.5", svg);
        Assert.Contains("y=3.2", svg);
    }
}
