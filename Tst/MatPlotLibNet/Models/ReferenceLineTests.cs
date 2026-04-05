// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models;

public class ReferenceLineTests
{
    [Fact]
    public void ReferenceLine_StoresValueAndOrientation()
    {
        var line = new ReferenceLine(5.0, Orientation.Horizontal);
        Assert.Equal(5.0, line.Value);
        Assert.Equal(Orientation.Horizontal, line.Orientation);
    }

    [Fact]
    public void DefaultLineStyle_IsDashed()
    {
        var line = new ReferenceLine(1.0, Orientation.Vertical);
        Assert.Equal(LineStyle.Dashed, line.LineStyle);
    }

    [Fact]
    public void DefaultLineWidth_Is1()
    {
        var line = new ReferenceLine(1.0, Orientation.Horizontal);
        Assert.Equal(1.0, line.LineWidth);
    }

    [Fact]
    public void DefaultLabel_IsNull()
    {
        var line = new ReferenceLine(1.0, Orientation.Horizontal);
        Assert.Null(line.Label);
    }

    [Fact]
    public void Axes_AxHLine_AddsReferenceLine()
    {
        var axes = new Axes();
        var line = axes.AxHLine(5.0);
        Assert.Single(axes.ReferenceLines);
        Assert.Equal(Orientation.Horizontal, line.Orientation);
    }

    [Fact]
    public void Axes_AxVLine_AddsReferenceLine()
    {
        var axes = new Axes();
        var line = axes.AxVLine(3.0);
        Assert.Single(axes.ReferenceLines);
        Assert.Equal(Orientation.Vertical, line.Orientation);
    }
}
