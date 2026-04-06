// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies <see cref="ColorBar"/> default values and record behavior.</summary>
public class ColorBarTests
{
    /// <summary>Verifies that Visible defaults to false.</summary>
    [Fact]
    public void Default_IsNotVisible()
    {
        var cb = new ColorBar();
        Assert.False(cb.Visible);
    }

    /// <summary>Verifies that Width defaults to 20.</summary>
    [Fact]
    public void Default_Width_Is20()
    {
        var cb = new ColorBar();
        Assert.Equal(20, cb.Width);
    }

    /// <summary>Verifies that Padding defaults to 10.</summary>
    [Fact]
    public void Default_Padding_Is10()
    {
        var cb = new ColorBar();
        Assert.Equal(10, cb.Padding);
    }

    /// <summary>Verifies that with expression creates a modified copy.</summary>
    [Fact]
    public void WithExpression_CreatesModifiedCopy()
    {
        var cb = new ColorBar() with { Visible = true, Label = "Intensity" };
        Assert.True(cb.Visible);
        Assert.Equal("Intensity", cb.Label);
        Assert.Equal(20, cb.Width);
    }

    /// <summary>Verifies that ColorMap defaults to null.</summary>
    [Fact]
    public void Default_ColorMap_IsNull()
    {
        var cb = new ColorBar();
        Assert.Null(cb.ColorMap);
    }
}
