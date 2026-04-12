// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies <see cref="TickConfig"/> expanded properties and <see cref="TickDirection"/> enum (sub-phase 2D).</summary>
public class TickConfigTests
{
    /// <summary>Verifies that tick direction defaults to Out and length defaults to 3.5 — matching matplotlib's xtick.direction and xtick.major.size.</summary>
    [Fact]
    public void TickConfig_Defaults_Direction_Out_Length3Point5()
    {
        var tc = new TickConfig();
        Assert.Equal(TickDirection.Out, tc.Direction);
        Assert.Equal(3.5, tc.Length);
    }

    [Fact]
    public void TickConfig_Direction_CanBeSet()
    {
        var tc = new TickConfig { Direction = TickDirection.In };
        Assert.Equal(TickDirection.In, tc.Direction);
    }

    [Fact]
    public void TickConfig_Length_Width_CanBeSet()
    {
        var tc = new TickConfig { Length = 8.0, Width = 1.5 };
        Assert.Equal(8.0, tc.Length);
        Assert.Equal(1.5, tc.Width);
    }

    [Fact]
    public void TickConfig_Width_DefaultsToPointEight()
    {
        var tc = new TickConfig();
        Assert.Equal(0.8, tc.Width);
    }

    [Fact]
    public void TickConfig_Color_DefaultsToNull()
    {
        var tc = new TickConfig();
        Assert.Null(tc.Color);
    }

    [Fact]
    public void TickConfig_LabelSize_LabelColor_CanBeSet()
    {
        var tc = new TickConfig { LabelSize = 11.0, LabelColor = Colors.Blue };
        Assert.Equal(11.0, tc.LabelSize);
        Assert.Equal(Colors.Blue, tc.LabelColor);
    }

    [Fact]
    public void TickConfig_LabelSize_DefaultsToNull()
    {
        var tc = new TickConfig();
        Assert.Null(tc.LabelSize);
    }

    [Fact]
    public void TickConfig_LabelColor_DefaultsToNull()
    {
        var tc = new TickConfig();
        Assert.Null(tc.LabelColor);
    }

    [Fact]
    public void TickConfig_Pad_DefaultsTo3()
    {
        var tc = new TickConfig();
        Assert.Equal(3.0, tc.Pad);
    }

    [Fact]
    public void TickConfig_Pad_CanBeSet()
    {
        var tc = new TickConfig { Pad = 6.0 };
        Assert.Equal(6.0, tc.Pad);
    }

    [Fact]
    public void TickDirection_HasThreeValues()
    {
        var values = Enum.GetValues<TickDirection>();
        Assert.Equal(3, values.Length);
        Assert.Contains(TickDirection.In, values);
        Assert.Contains(TickDirection.Out, values);
        Assert.Contains(TickDirection.InOut, values);
    }
}
