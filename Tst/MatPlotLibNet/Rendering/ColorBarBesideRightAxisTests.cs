// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Under the library's own themes a vertical colour bar stands in the right margin past what the axes draws past its right
/// spine (a secondary Y axis with its tick labels and its title, the mirrored Y tick labels, the end labels), and every layout keeps
/// room for both, stacked: the bar stood <see cref="ColorBar.Padding"/> from the spine and the layouts kept the larger of the two,
/// so the bar covered the secondary axis' tick labels and title (measured: the bar from x 517.1, the tick labels '1000' and '2000'
/// at 516.82 and the title at 559.82, in a 600 x 400 figure).</summary>
public sealed class ColorBarBesideRightAxisTests
{
    private static Figure Figure(string layout, bool endLabels = false)
    {
        var builder = Plt.Create().WithTheme(Theme.Default).WithSize(600, 400);
        if (layout == "tight") builder.TightLayout();
        if (layout == "constrained") builder.ConstrainedLayout();
        return builder.AddSubPlot(1, 1, 1, ax => ax
                .Heatmap(new double[,] { { 1, 2 }, { 3, 4 } })
                .WithColorBar(b => b with { Label = "intensity" })
                .WithAddRightYAxis(s => s.Plot([0.0, 1.0], [1000.0, 2000.0], l => { if (endLabels) { l.Label = "a long end label"; l.LabelPlacement = MatPlotLibNet.Models.Series.SeriesLabelPlacement.LineEnd; } })
                    .SetYLabel("secondary")))
            .Build();
    }

    /// <summary>The left edge of the bar: the leftmost rectangle of the colour bar's group.</summary>
    private static double BarLeft(XDocument svg, string group = "colorbar") =>
        svg.Each("g").Where(e => (string?)e.Attribute("class") == group)
            .SelectMany(g => g.Each("rect")).Min(r => r.Number("x"));

    /// <summary>The right edge of a text's ink: a text written from its x, unturned, ends its measured width past it; a text turned a
    /// quarter is as wide as its font is high, centred on its x.</summary>
    private static double InkRight(XElement text)
    {
        double x = text.Number("x");
        double size = double.Parse(((string?)text.Attribute("font-size") ?? "12").Replace("px", ""), CultureInfo.InvariantCulture);
        bool turned = ((string?)text.Attribute("transform") ?? "").Contains("rotate", StringComparison.Ordinal);
        if (turned) return x + size / 2;
        string anchor = (string?)text.Attribute("text-anchor") ?? "start";
        double width = FontMetricsDefault.Current.Measure(text.Value, new Font { Size = size }).Width;
        return anchor switch { "middle" => x + width / 2, "end" => x, _ => x + width };
    }

    [Theory]
    [InlineData("default")]
    [InlineData("tight")]
    [InlineData("constrained")]
    public void TheBar_StandsPastTheSecondaryAxis_ItsTickLabelsAndItsTitle(string layout)
    {
        var svg = Figure(layout).ToSvg().Xml();

        double secondaryRight = svg.Each("text").Where(e => e.Value is "1000" or "2000" or "secondary").Max(InkRight);

        Assert.True(BarLeft(svg) > secondaryRight, $"the bar starts at {BarLeft(svg)}, the secondary axis reaches {secondaryRight}");
    }

    [Theory]
    [InlineData("default")]
    [InlineData("tight")]
    public void TheBar_StandsPastTheEndLabels(string layout)
    {
        var svg = Figure(layout, endLabels: true).ToSvg().Xml();

        var label = svg.Each("text").Single(e => e.Value == "a long end label");

        Assert.True(BarLeft(svg) > InkRight(label), $"the bar starts at {BarLeft(svg)}, the end label reaches {InkRight(label)}");
    }

    [Fact]
    public void TheBar_StandsPastTheMirroredYTickLabels()
    {
        var svg = Plt.Create().WithTheme(Theme.Default).WithSize(600, 400).TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } }).WithColorBar().WithYTicksMirrored()).Build().ToSvg().Xml();

        var outsideTheBar = svg.Each("text").Where(e => OutsideTheBar(e)).ToList();
        double mirrored = outsideTheBar.Max(InkRight);

        Assert.True(mirrored > PlotRight(svg), "the mirrored tick labels stand past the right spine");
        Assert.True(BarLeft(svg) > mirrored, $"the bar starts at {BarLeft(svg)}, the mirrored tick labels reach {mirrored}");
    }

    [Fact]
    public void WithNothingPastTheRightSpine_TheBarStandsItsPaddingFromThePlot_AsBefore()
    {
        var figure = Plt.Create().WithTheme(Theme.Default).WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } }).WithColorBar()).Build();
        var svg = figure.ToSvg().Xml();

        Assert.Equal(PlotRight(svg) + new ColorBar().Padding, BarLeft(svg), 1);
    }

    [Theory]
    [InlineData("default")]
    [InlineData("tight")]
    [InlineData("constrained")]
    public void TheFiguresBar_StandsPastTheSecondaryAxisOfTheLastColumn(string layout)
    {
        var builder = Plt.Create().WithTheme(Theme.Default).WithSize(600, 400).WithColorBar(b => b with { Label = "intensity" });
        if (layout == "tight") builder.TightLayout();
        if (layout == "constrained") builder.ConstrainedLayout();
        var svg = builder
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } })
                .WithAddRightYAxis(s => s.Plot([0.0, 1.0], [1000.0, 2000.0]).SetYLabel("secondary")))
            .Build().ToSvg().Xml();

        double secondaryRight = svg.Each("text").Where(e => e.Value is "1000" or "2000" or "secondary").Max(InkRight);
        var label = svg.Each("text").Single(e => e.Value == "intensity");

        Assert.True(BarLeft(svg, "figure-colorbar") > secondaryRight, $"the bar starts at {BarLeft(svg, "figure-colorbar")}, the secondary axis reaches {secondaryRight}");
        Assert.True(InkRight(label) <= 600, $"the bar's label reaches {InkRight(label)} in a figure 600 wide");
    }


    private static bool OutsideTheBar(XElement e) => e.Ancestors().All(a => (string?)a.Attribute("class") != "colorbar");

    /// <summary>The right spine: the right edge of the widest rectangle drawn outside the bar that is narrower than the figure (the
    /// axes' background fills the plot).</summary>
    private static double PlotRight(XDocument svg) =>
        svg.Each("rect").Where(e => OutsideTheBar(e) && e.Number("width") < 600).Max(r => r.Number("x") + r.Number("width"));

    /// <summary>Under the matplotlib themes and the constrained layout the bar stands in the margin past the axes' decorations, a twin
    /// Y axis included: matplotlib 3.11.2, printed (6 x 4 inches, layout="constrained", imshow, twinx with a line and a Y label,
    /// fig.colorbar(im, ax=ax)): the twin's tight box ends at x 524.66 and the bar starts at 544.77. The library stood it past the
    /// mirrored tick labels only (measured: the bar from 512.94, the second axis reaching 541.38 under v2).</summary>
    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    public void UnderTheMatplotlibThemesAndTheConstrainedLayout_TheBarStandsPastTheSecondaryAxis(string themeId)
    {
        var theme = themeId.AsTheme();
        var svg = Plt.Create().WithTheme(theme).WithSize(600, 400).ConstrainedLayout()
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } }).WithColorBar()
                .WithAddRightYAxis(s => s.Plot([0.0, 1.0], [1000.0, 2000.0]).SetYLabel("secondary"))).Build().ToSvg().Xml();

        double secondaryRight = svg.Each("text").Where(e => e.Value is "1000" or "2000" or "secondary").Max(InkRight);

        Assert.True(BarLeft(svg) > secondaryRight, $"the bar starts at {BarLeft(svg)}, the secondary axis reaches {secondaryRight}");
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    public void UnderTheMatplotlibThemesAndTheConstrainedLayout_TheFiguresBarStandsPastTheSecondaryAxis(string themeId)
    {
        var theme = themeId.AsTheme();
        var svg = Plt.Create().WithTheme(theme).WithSize(600, 400).ConstrainedLayout().WithColorBar()
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } })
                .WithAddRightYAxis(s => s.Plot([0.0, 1.0], [1000.0, 2000.0]).SetYLabel("secondary"))).Build().ToSvg().Xml();

        double secondaryRight = svg.Each("text").Where(e => e.Value is "1000" or "2000" or "secondary").Max(InkRight);

        Assert.True(BarLeft(svg, "figure-colorbar") > secondaryRight, $"the bar starts at {BarLeft(svg, "figure-colorbar")}, the secondary axis reaches {secondaryRight}");
    }

    [Theory]
    [InlineData("tight")]
    [InlineData("constrained")]
    public void TheLayout_KeepsRoomForTheSecondaryAxisAndTheBarsLabel_Stacked(string layout)
    {
        var svg = Figure(layout).ToSvg().Xml();

        var label = svg.Each("text").Single(e => e.Value == "intensity");

        Assert.True(InkRight(label) <= 600, $"the bar's label reaches {InkRight(label)} in a figure 600 wide");
    }
}
