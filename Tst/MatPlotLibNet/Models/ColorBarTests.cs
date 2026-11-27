// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies <see cref="ColorBar"/> behaviour; what it starts from and the settings it stores are held by <see cref="Serialization.RestingValueCorpusTests"/> and the drawn-property round trips.</summary>
public class ColorBarTests
{
    // --- B7: ColorBarExtend ---

    [Fact]
    public void ColorBarExtend_HasFourMembers()
    {
        Assert.Equal(4, Enum.GetValues<ColorBarExtend>().Length);
    }

    // --- 2F: ColorBar Enrichment ---

    [Fact]
    public void ColorBarOrientation_HasTwoValues()
    {
        var values = Enum.GetValues<ColorBarOrientation>();
        Assert.Equal(2, values.Length);
        Assert.Contains(ColorBarOrientation.Vertical, values);
        Assert.Contains(ColorBarOrientation.Horizontal, values);
    }

    [Fact]
    public void WithoutABar_AskingStartsAVisibleOne()
    {
        ColorBar? none = null;

        var bar = ColorBar.AskedFor(none, null);

        Assert.True(bar.Visible);
    }

    [Fact]
    public void WithoutABar_TheEditStartsFromTheVisibleOne()
    {
        ColorBar? none = null;

        var bar = ColorBar.AskedFor(none, b => b with { Label = "Intensity" });

        Assert.True(bar.Visible);
        Assert.Equal("Intensity", bar.Label);
    }

    [Fact]
    public void TheBarAlreadySet_IsKept_EvenWhenItIsHidden()
    {
        var hidden = new ColorBar { Label = "kept" };

        var bar = ColorBar.AskedFor((ColorBar?)hidden, null);

        Assert.Same(hidden, bar);
        Assert.False(bar.Visible);
    }

    [Fact]
    public void TheBarAlreadySet_IsEditedByTheCaller()
    {
        var set = new ColorBar { Visible = true, Width = 30 };

        var bar = ColorBar.AskedFor((ColorBar?)set, b => b with { Padding = 4 });

        Assert.Equal(30, bar.Width);
        Assert.Equal(4, bar.Padding);
    }
}
