// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Styling.Themes;

/// <summary>What only the MatplotlibClassic theme, matplotlib's pre-2.0 default look, has: its <c>bgrcmyk</c> cycle and the polar rings its
/// grid draws (what every theme states is a row of <c>ThemeTests.Facts</c>).</summary>
public class MatplotlibClassicThemeTests
{
    /// <summary>A polar chart draws its rings in the theme's grid: black, thin and dotted under the classic theme, so dotted
    /// polylines and no circles.</summary>
    [Fact]
    public void APolarChart_UnderClassic_DrawsItsRings_InTheClassicGrid()
    {
        string svg = Plt.Create().WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax.PolarScatter([0.2, 0.5, 0.9], [0.3, 1.2, 2.5]))
            .ToSvg();

        Assert.DoesNotContain("<ellipse ", svg);
        Assert.Contains(svg.Elements("polyline"), polyline => polyline["stroke"] == "#000000" && polyline["stroke-width"] == "0.69" && polyline["stroke-dasharray"] is not null);
    }

    /// <summary>The classic cycle is the seven colours <c>bgrcmyk</c>, in that order.</summary>
    [Fact]
    public void MatplotlibClassic_CyclesBgrcmyk()
    {
        Assert.Equal(
            [Color.FromHex("#0000FF"), Color.FromHex("#008000"), Color.FromHex("#FF0000"), Color.FromHex("#00BFBF"),
             Color.FromHex("#BF00BF"), Color.FromHex("#BFBF00"), Color.FromHex("#000000")],
            Theme.MatplotlibClassic.CycleColors);
    }
}
