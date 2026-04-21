// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="QuiverKeySeries"/> rendering.</summary>
public class QuiverKeySeriesRenderTests
{
    [Fact]
    public void QuiverKey_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.QuiverKey(0.8, 0.9, 1.0, "1 m/s"))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void QuiverKey_SvgContainsLabelText()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.QuiverKey(0.8, 0.9, 1.0, "1 m/s"))
            .ToSvg();
        Assert.Contains("1 m/s", svg);
    }

    [Fact]
    public void QuiverKey_FluentShortcut_ProducesSvg()
    {
        string svg = Plt.Create()
            .QuiverKey(0.8, 0.9, 1.0, "1 m/s")
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

}
