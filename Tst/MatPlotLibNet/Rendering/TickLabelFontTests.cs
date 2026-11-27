// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A tick label is drawn in the theme's tick font at the size and in the colour its ticks name (matplotlib's <c>labelsize</c>
/// and <c>labelcolor</c>), each where given: one rule, which the drawing of a label and the size its locator spaces the ticks by both
/// read (<see cref="ThemeFontExtensions"/>, on <see cref="TickConfig"/>). The drawing applied the size and the colour itself, and the
/// locator read the size again.</summary>
public sealed class TickLabelFontTests
{
    private static readonly Font Base = new() { Size = 9, Color = Colors.Gray };

    [Fact]
    public void WithoutOverrides_TheLabelIsInTheFontItIsGiven()
    {
        Assert.Same(Base, new TickConfig().LabelFontOver(Base));
        Assert.Equal(9, new TickConfig().LabelSizeOver(Base.Size));
    }

    [Fact]
    public void TheTicksOwnSizeAndColour_AreTaken_EachWhenGiven()
    {
        Assert.Equal(new Font { Size = 14, Color = Colors.Gray }, new TickConfig { LabelSize = 14 }.LabelFontOver(Base));
        Assert.Equal(new Font { Size = 9, Color = Colors.Red }, new TickConfig { LabelColor = Colors.Red }.LabelFontOver(Base));
        Assert.Equal(new Font { Size = 14, Color = Colors.Red }, new TickConfig { LabelSize = 14, LabelColor = Colors.Red }.LabelFontOver(Base));
        Assert.Equal(14, new TickConfig { LabelSize = 14 }.LabelSizeOver(Base.Size));
    }

    /// <summary>The size the locator spaces an axis' ticks by is the size its labels are drawn at.</summary>
    [Fact]
    public void TheSizeTheLocatorReads_IsTheSizeTheLabelIsDrawnAt()
    {
        var axis = new Axis { MajorTicks = new TickConfig { LabelSize = 13 } };

        Assert.Equal(13, axis.TickLabelSizeUnder(Theme.Default));
        Assert.Equal(Theme.Default.TickFont().Size, new Axis().TickLabelSizeUnder(Theme.Default));
    }
}
