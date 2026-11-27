// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>The pads a layout keeps and the gap two needs take that meet in it.</summary>
public sealed class LayoutPadsTests
{
    [Fact]
    public void TheLibrarysPads_AreFiveLeftAndBelow_EightRightAndAbove_NoneShared()
    {
        var pads = LayoutPads.Library;

        Assert.Equal(new Sides<double>(Left: 5, Bottom: 5, Top: 8, Right: 8), pads.Edges);
        Assert.Equal(0, pads.Shared);
        Assert.Equal(LayoutBounds.Library, pads.Bounds);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(30, 0, 30)]
    [InlineData(0, 30, 30)]
    [InlineData(30, 40, 60)]
    [InlineData(3, 40, 40)]
    public void TwoNeedsInAGap_StackLessWhatThePadsShare_NeverBelowEitherAlone(double first, double second, double expected)
    {
        Assert.Equal(expected, (LayoutPads.Library with { Shared = 10 }).Stacked(first, second));
    }

    [Fact]
    public void ATightLayoutUnderAMatplotlibTheme_KeepsMatplotlibsPad_OnEveryEdgeAndOnceInAGap()
    {
        var figure = Plt.Create().WithTheme(Theme.MatplotlibV2).TightLayout().Plot([0.0, 1.0], [0.0, 1.0]).Build();
        double pad = 1.08 * figure.Theme.DataTextFont().Size;

        var pads = LayoutPads.Of(figure);

        Assert.Equal(new Sides<double>(pad, pad, pad, pad), pads.Edges);
        Assert.Equal(pad, pads.Shared);
    }

    [Fact]
    public void AnyOtherLayout_KeepsTheLibrarysPads()
    {
        Assert.Equal(LayoutPads.Library, LayoutPads.Of(Plt.Create().WithTheme(Theme.MatplotlibV2).Plot([0.0, 1.0], [0.0, 1.0]).Build()));
        Assert.Equal(LayoutPads.Library, LayoutPads.Of(Plt.Create().WithTheme(Theme.Default).TightLayout().Plot([0.0, 1.0], [0.0, 1.0]).Build()));
        Assert.Equal(LayoutPads.Library, LayoutPads.Of(Plt.Create().WithTheme(Theme.MatplotlibV2).ConstrainedLayout().Plot([0.0, 1.0], [0.0, 1.0]).Build()));
    }
}
