// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>A colour that plays one role in several places is typed once, by its role: the drop shadow a legend and a pie lay under their
/// shapes (black at 80 of 255), and matplotlib's legend edge <c>'0.8'</c> (#CCCCCC), which the library's legend look and the matplotlib
/// themes both draw.</summary>
public class RoleColourOwnerTests
{
    [Fact]
    public void ADropShadow_IsBlackAt80()
    {
        Assert.Equal(Colors.Black.WithAlpha(80), Colors.DropShadow);
        Assert.Equal(Colors.DropShadow, new LegendLook().ShadowColor);
    }

    [Fact]
    public void TheLegendEdge_IsMatplotlibs08Grey_InTheLibrarysLookAndUnderTheMatplotlibThemes()
    {
        Assert.Equal(Color.FromHex("#CCCCCC"), LegendLook.RestingEdgeColor);
        Assert.Equal(LegendLook.RestingEdgeColor, new LegendLook().EdgeColor);
        Assert.Equal(LegendLook.RestingEdgeColor, Theme.MatplotlibV2.Looks.Legend.EdgeColor);
    }
}
