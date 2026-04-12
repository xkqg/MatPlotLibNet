// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies <see cref="ReferenceLine"/> behavior.</summary>
public class ReferenceLineTests
{
    /// <summary>Verifies that a reference line stores its value and orientation.</summary>
    [Fact]
    public void ReferenceLine_StoresValueAndOrientation()
    {
        var line = new ReferenceLine(5.0, Orientation.Horizontal);
        Assert.Equal(5.0, line.Value);
        Assert.Equal(Orientation.Horizontal, line.Orientation);
    }

    /// <summary>Verifies that the default line style is dashed.</summary>
    [Fact]
    public void DefaultLineStyle_IsDashed()
    {
        var line = new ReferenceLine(1.0, Orientation.Vertical);
        Assert.Equal(LineStyle.Dashed, line.LineStyle);
    }

    /// <summary>Verifies that the default line width is 1.</summary>
    [Fact]
    public void DefaultLineWidth_Is1()
    {
        var line = new ReferenceLine(1.0, Orientation.Horizontal);
        Assert.Equal(1.0, line.LineWidth);
    }

    /// <summary>Verifies that the default label is null.</summary>
    [Fact]
    public void DefaultLabel_IsNull()
    {
        var line = new ReferenceLine(1.0, Orientation.Horizontal);
        Assert.Null(line.Label);
    }

    /// <summary>Verifies that AxHLine adds a horizontal reference line.</summary>
    [Fact]
    public void Axes_AxHLine_AddsReferenceLine()
    {
        var axes = new Axes();
        var line = axes.AxHLine(5.0);
        Assert.Single(axes.ReferenceLines);
        Assert.Equal(Orientation.Horizontal, line.Orientation);
    }

    /// <summary>Verifies that AxVLine adds a vertical reference line.</summary>
    [Fact]
    public void Axes_AxVLine_AddsReferenceLine()
    {
        var axes = new Axes();
        var line = axes.AxVLine(3.0);
        Assert.Single(axes.ReferenceLines);
        Assert.Equal(Orientation.Vertical, line.Orientation);
    }
}
