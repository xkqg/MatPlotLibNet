// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>The lines a violin draws over its body (its extrema, its median and its mean) are a value of its theme's look
/// (<see cref="ViolinLook"/>). Under the matplotlib themes every one of them is a line of the stats, as matplotlib 3.11.2 draws them;
/// printed: <c>ax.violinplot([[1, 2, 3, 4]], showmeans=True, showmedians=True)</c> has <c>['cmeans']</c> and <c>['cmedians']</c>
/// with <c>get_linewidth()</c> [1.5] under <c>plt.style.use('default')</c> and [1.0] under <c>'classic'</c>, <c>get_linestyle()</c>
/// solid (<c>(0, None)</c>), and <c>get_color()</c> the stats colour (the violin's own colour under the default style, <c>'r'</c> under
/// the classic one). The library's own themes keep their own marks: a white median 2 px wide and a green dashed mean 1.5 px wide.</summary>
public class ViolinStatsLookTests
{
    private static readonly double[] Sample = [1.0, 2, 2, 3, 4, 4, 5, 7];

    private static XElement StatsLine(Theme theme, bool mean, Color expectedColour)
    {
        var svg = Plt.Create().WithTheme(theme).WithSize(500, 350)
            .AddSubPlot(1, 1, 1, ax => ax.Violin([Sample], s => { s.ShowExtrema = false; s.ShowMeans = mean; s.ShowMedians = !mean; }))
            .Build().ToSvg();
        return Assert.Single(svg.Each("line"), e => (string?)e.Attribute("stroke") == expectedColour.ToHex());
    }

    public static TheoryData<string, bool, string, string> MatplotlibLines => new()
    {
        { "MatplotlibV2", false, "#1F77B4", "2.08" },
        { "MatplotlibV2", true, "#1F77B4", "2.08" },
        { "MatplotlibClassic", false, "#FF0000", "1.39" },
        { "MatplotlibClassic", true, "#FF0000", "1.39" },
    };

    [Theory]
    [MemberData(nameof(MatplotlibLines))]
    public void UnderTheMatplotlibThemes_TheMedianAndTheMean_AreSolidLinesOfTheStats(string themeName, bool mean, string colour, string width)
    {
        var theme = themeName.AsTheme();

        var line = StatsLine(theme, mean, Color.FromHex(colour));

        Assert.Equal(width, (string)line.Attribute("stroke-width")!);
        Assert.Null(line.Attribute("stroke-dasharray"));
    }

    [Fact]
    public void UnderTheLibrarysThemes_TheMedianIsWhite2Pixels_AndTheMeanGreenDashed()
    {
        var median = StatsLine(Theme.Default, mean: false, Colors.White);
        var mean = StatsLine(Theme.Default, mean: true, Colors.Green);

        Assert.Equal("2", (string)median.Attribute("stroke-width")!);
        Assert.Null(median.Attribute("stroke-dasharray"));
        Assert.Equal("1.5", (string)mean.Attribute("stroke-width")!);
        Assert.NotNull(mean.Attribute("stroke-dasharray"));
    }

    [Fact]
    public void TheLooks_AreTheLibrarysUnderTheLibraryThemes_AndMatplotlibsUnderTheMatplotlibThemes()
    {
        Assert.Same(ViolinLook.Library, Theme.Default.Looks.Violin);
        Assert.Same(ViolinLook.Matplotlib, Theme.MatplotlibV2.Looks.Violin);
        Assert.Same(ViolinLook.Matplotlib, Theme.MatplotlibClassic.Looks.Violin);
        Assert.Equal(ViolinStatsLine.Stats, ViolinLook.Matplotlib.Median);
        Assert.Equal(ViolinStatsLine.Stats, ViolinLook.Matplotlib.Mean);
        Assert.Equal(ViolinStatsLine.Stats, ViolinLook.Library.Extrema);
    }

    [Theory]
    [InlineData("MatplotlibV2")]
    [InlineData("MatplotlibClassic")]
    [InlineData("Default")]
    public void TheLook_SurvivesTheThemesJson(string themeName)
    {
        var theme = themeName.AsTheme();

        var read = ThemeDto.From(theme).ToTheme();

        Assert.Same(theme.Looks.Violin, read.Looks.Violin);
    }
}
