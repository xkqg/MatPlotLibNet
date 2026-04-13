// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies <see cref="TickConfig"/> expanded properties and <see cref="TickDirection"/> enum (sub-phase 2D).</summary>
public class TickConfigTests
{
    /// <summary>Verifies that tick direction defaults to Out and length defaults to matplotlib's
    /// 3.5 POINTS pre-converted to pixels at 100 DPI (3.5 × 100/72 ≈ 4.861 px).</summary>
    [Fact]
    public void TickConfig_Defaults_Direction_Out_Length3Point5Points()
    {
        var tc = new TickConfig();
        Assert.Equal(TickDirection.Out, tc.Direction);
        Assert.Equal(3.5 * 100.0 / 72.0, tc.Length, 3);
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
    public void TickConfig_Width_DefaultsToMatplotlibPointEightPoints()
    {
        var tc = new TickConfig();
        // matplotlib `xtick.major.width = 0.8` POINTS → at 100 DPI = 1.111 px
        Assert.Equal(0.8 * 100.0 / 72.0, tc.Width, 3);
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
    public void TickConfig_Pad_DefaultsToMatplotlib3Point5Points()
    {
        var tc = new TickConfig();
        // matplotlib `xtick.major.pad = 3.5` POINTS → at 100 DPI = 4.861 px
        Assert.Equal(3.5 * 100.0 / 72.0, tc.Pad, 3);
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
