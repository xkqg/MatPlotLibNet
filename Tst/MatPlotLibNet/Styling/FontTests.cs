// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Verifies <see cref="Font"/> behavior.</summary>
public class FontTests
{
    // ---- the unit of a font size: pixels ---------------------------------------------------------------------------------
    // Font.Size, TextStyle.FontSize and the FontSize of a series' text are documented in pixels, the unit a figure is laid out in:
    // an SVG writes the number as it stands (font-size is a length in the picture's user units), and a size in points is 100 / 72 of
    // it, which is how the matplotlib themes store theirs.

    private static double SizeOfText(string svg, string text)
    {
        return svg.TextReading(text).Number("font-size");
    }

    [Fact]
    public void AThemeFontOfSize20_DrawsItsTextTwentyPixelsHigh()
    {
        var theme = new ThemeBuilder(Theme.Default).WithFont(f => f with { Size = 20 }).Build();
        var svg = Plt.Create().WithTheme(theme).AddSubPlot(1, 1, 1, ax => ax.SetXLabel("the label")).Build().ToSvg();

        Assert.Equal(20, SizeOfText(svg, "the label"));
    }

    [Fact]
    public void ATextStyleOfSize17_DrawsItsTextSeventeenPixelsHigh()
    {
        var svg = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.SetXLabel("the label", style => style with { FontSize = 17 })).Build().ToSvg();

        Assert.Equal(17, SizeOfText(svg, "the label"));
    }

    [Fact]
    public void TenPointsAreStoredAsPixels_ByTheMatplotlibThemes()
    {
        var svg = Plt.Create().WithTheme(Theme.MatplotlibV2).AddSubPlot(1, 1, 1, ax => ax.SetXLabel("the label")).Build().ToSvg();

        Assert.Equal(Math.Round(10 * 100.0 / 72, 2), SizeOfText(svg, "the label"));
    }

    [Fact]
    public void ATableOfFontSize15_DrawsItsCellsFifteenPixelsHigh()
    {
        var svg = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Table([["cellular"]], t => t.FontSize = 15)).Build().ToSvg();

        Assert.Equal(15, SizeOfText(svg, "cellular"));
    }
}
