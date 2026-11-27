// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>The size of the dots of a strip plot, a swarm plot, a point plot, a residual plot and a pair grid where their author names none.
/// seaborn 0.13.2 and matplotlib 3.11.2, read with python: <c>stripplot(size=5)</c> and <c>swarmplot(size=5)</c> draw 5 points across (the
/// <c>ax.scatter</c> sizes read <c>[25]</c> under the default and the classic style); <c>pointplot</c> sets <c>markersize = lines.linewidth * 1.8
/// * sqrt(2 * pi)</c> (6.7679 points under the default style, 4.5119 under classic, read off <c>ax.lines[0].get_markersize()</c>); <c>residplot</c>
/// and <c>pairplot</c> give <c>scatterplot</c> no size, so <c>Axes.scatter</c>'s own is drawn (<c>get_sizes()</c> reads <c>[36.]</c> under the default
/// style and <c>[20]</c> under classic). The library drew 5, 8, 6 and 3 (a radius) pixels; the matplotlib themes now draw the points of seaborn, the
/// library's own themes keep their pixels, and a size that is named is drawn as the pixels named, the resting number included.</summary>
public class DotSizeTests
{
    private static readonly double[][] Groups = [[1.0, 2.0, 3.0], [2.0, 3.0, 4.0]];
    private static readonly double[] Xs = [1, 2, 3, 4, 5, 6];
    private static readonly double[] Ys = [1.2, 1.9, 3.4, 3.8, 5.3, 5.9];
    private static readonly double[][] Variables = [[1, 2, 3, 4], [2, 1, 4, 3], [4, 3, 2, 1]];

    private static double PixelsOf(double points) => points * 100.0 / 72.0;

    private static List<double> Radii(Theme theme, Func<AxesBuilder, AxesBuilder> chart) =>
        RadiiOf(Plt.Create().WithTheme(theme).WithSize(600, 400).AddSubPlot(1, 1, 1, ax => chart(ax)).ToSvg());

    private static List<double> RadiiOf(string svg) =>
        [.. svg.Each("circle").Where(e => !e.Ancestors().Any(a => (string?)a.Attribute("class") == "legend"))
            .Select(e => e.Number("r"))];

    private static double OneRadius(Theme theme, Func<AxesBuilder, AxesBuilder> chart) => One(Radii(theme, chart));

    private static double OneRadiusOf(string svg) => One(RadiiOf(svg));

    private static double One(List<double> radii)
    {
        Assert.NotEmpty(radii);
        Assert.All(radii, r => Assert.Equal(radii[0], r, 2));
        return radii[0];
    }

    /// <summary>kind, theme, the radius in points matplotlib's seaborn draws, the radius in pixels the library's own themes draw.</summary>
    public static TheoryData<string, string, double, double> Cases => new()
    {
        { "strip", "v2", 2.5, 2.5 },
        { "strip", "classic", 2.5, 2.5 },
        { "swarm", "v2", 2.5, 2.5 },
        { "swarm", "classic", 2.5, 2.5 },
        { "point", "v2", 1.5 * 1.8 * Math.Sqrt(2 * Math.PI) / 2, 4.0 },
        { "point", "classic", 1.0 * 1.8 * Math.Sqrt(2 * Math.PI) / 2, 4.0 },
        { "resid", "v2", 3.0, 3.0 },
        { "resid", "classic", Math.Sqrt(20) / 2, 3.0 },
        { "pair", "v2", 3.0, 3.0 },
        { "pair", "classic", Math.Sqrt(20) / 2, 3.0 },
    };

    private static Func<AxesBuilder, AxesBuilder> Chart(string kind, double? size) => kind switch
    {
        "strip" => ax => ax.Stripplot(Groups, s => { if (size is { } v) s.MarkerSize = v; }),
        "swarm" => ax => ax.Swarmplot(Groups, s => { if (size is { } v) s.MarkerSize = v; }),
        "point" => ax => ax.Pointplot(Groups, s => { if (size is { } v) s.MarkerSize = v; }),
        "resid" => ax => ax.Residplot(Xs, Ys, s => { if (size is { } v) s.MarkerSize = v; }),
        _ => ax => ax.PairGrid(Variables, s => { if (size is { } v) s.MarkerSize = v; }),
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void UnderTheMatplotlibThemes_ADotNamedNoSize_IsDrawnAtSeabornsPoints(string kind, string theme, double seabornRadiusPoints, double _)
    {
        Assert.Equal(PixelsOf(seabornRadiusPoints), OneRadius(theme.AsTheme(), Chart(kind, null)), 2);
    }

    public static TheoryData<string, double> Library => new() { { "strip", 2.5 }, { "swarm", 2.5 }, { "point", 4.0 }, { "resid", 3.0 }, { "pair", 3.0 } };

    [Theory]
    [MemberData(nameof(Library))]
    public void UnderTheLibrarysThemes_ADotNamedNoSize_KeepsItsPixels(string kind, double libraryRadiusPixels)
    {
        Assert.Equal(libraryRadiusPixels, OneRadius(Theme.Default, Chart(kind, null)), 2);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void ASizeThatIsNamed_IsDrawnAsThePixelsNamed_TheRestingNumberIncluded(string kind, string theme, double seabornRadiusPoints, double libraryRadiusPixels)
    {
        Assert.NotEqual(PixelsOf(seabornRadiusPoints), libraryRadiusPixels, 2);   // the resting number names something else than seaborn's size
        // the resting number is what a new series reads from MarkerSize: pixels across (a radius of half), a pair grid's own is a radius
        double named = kind == "pair" ? libraryRadiusPixels : libraryRadiusPixels * 2;
        Assert.Equal(libraryRadiusPixels, OneRadius(theme.AsTheme(), Chart(kind, named)), 2);
        Assert.Equal(libraryRadiusPixels * 2, OneRadius(theme.AsTheme(), Chart(kind, named * 2)), 2);
    }

    [Theory]
    [InlineData("strip", 5.0)]
    [InlineData("swarm", 5.0)]
    [InlineData("point", 8.0)]
    [InlineData("resid", 6.0)]
    [InlineData("pair", 3.0)]
    public void TheSizeAFreshSeriesReads_IsThePixelsItAlwaysWas(string kind, double resting)
    {
        double Read(string k) => k switch
        {
            "strip" => new StripplotSeries(Groups).MarkerSize,
            "swarm" => new SwarmplotSeries(Groups).MarkerSize,
            "point" => new PointplotSeries(Groups).MarkerSize,
            "resid" => new ResidualSeries(Xs, Ys).MarkerSize,
            _ => new PairGridSeries(Variables).MarkerSize,
        };

        Assert.Equal(resting, Read(kind));
    }

    [Theory]
    [InlineData("strip", "\"markerSize\"")]
    [InlineData("swarm", "\"markerSize\"")]
    [InlineData("point", "\"markerSize\"")]
    [InlineData("resid", "\"markerSize\"")]
    [InlineData("pair", "\"pairGridMarkerSize\"")]
    public void ASize_SurvivesTheJson_AndOneThatIsNot_StaysUnnamed(string kind, string key)
    {
        string Json(double? size) => Plt.Create().WithTheme(Theme.MatplotlibV2).AddSubPlot(1, 1, 1, ax => Chart(kind, size)(ax)).Build().ToJson();

        string unnamed = Json(null), named = Json(kind switch { "strip" or "swarm" => 5.0, "point" => 8.0, "resid" => 6.0, _ => 3.0 });

        Assert.DoesNotContain(key, unnamed);
        Assert.Contains(key, named);
        double Back(string json) => OneRadiusOf(ChartSerializerDefault.Current.FromJson(json).ToSvg());

        Assert.Equal(OneRadius(Theme.MatplotlibV2, Chart(kind, null)), Back(unnamed), 2);   // still the theme's
        Assert.Equal(OneRadius(Theme.MatplotlibV2, Chart(kind, kind switch { "strip" or "swarm" => 5.0, "point" => 8.0, "resid" => 6.0, _ => 3.0 })), Back(named), 2);
    }

}
