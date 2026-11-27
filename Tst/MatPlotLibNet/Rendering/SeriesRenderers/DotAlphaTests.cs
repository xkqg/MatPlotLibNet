// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>The opacity of the dots of a strip plot and a swarm plot where their author names none. seaborn 0.13.2, read with inspect:
/// <c>stripplot(size=5, edgecolor=default, linewidth=0, **kwargs)</c> and <c>swarmplot(size=5, edgecolor=None, linewidth=0, **kwargs)</c> pass no
/// alpha to <c>Axes.scatter</c>, whose default is opaque. The library drew them at 0.8; under the matplotlib themes they are opaque, under the
/// library's own themes they keep 0.8, and an alpha that is named is drawn as named, 0.8 included.</summary>
public class DotAlphaTests
{
    private static readonly double[][] Groups = [[1.0, 2.0, 3.0], [2.0, 3.0, 4.0]];

    private static XDocument Doc(Theme theme, Func<AxesBuilder, AxesBuilder> chart) =>
        Plt.Create().WithTheme(theme).WithSize(600, 400).AddSubPlot(1, 1, 1, ax => chart(ax)).ToSvg().Xml();

    private static double Opacity(XElement dot) => dot.Attribute("fill-opacity") is { } a ? double.Parse(a.Value, CultureInfo.InvariantCulture) : 1.0;

    private static List<XElement> Dots(XDocument doc) =>
        [.. doc.Each("circle").Where(e => !e.Ancestors().Any(a => (string?)a.Attribute("class") == "legend"))];

    public static TheoryData<string> Kinds => ["strip", "swarm"];

    private static Func<AxesBuilder, AxesBuilder> Chart(string kind, double? alpha) => kind == "strip"
        ? ax => ax.Stripplot(Groups, s => { if (alpha is { } a) s.Alpha = a; })
        : ax => ax.Swarmplot(Groups, s => { if (alpha is { } a) s.Alpha = a; });

    [Theory]
    [MemberData(nameof(Kinds))]
    public void UnderTheMatplotlibThemes_TheDotsAreOpaque_WhereNoAlphaIsNamed(string kind)
    {
        Assert.All(Dots(Doc(Theme.MatplotlibV2, Chart(kind, null))), d => Assert.Equal(1.0, Opacity(d)));
        Assert.All(Dots(Doc(Theme.MatplotlibClassic, Chart(kind, null))), d => Assert.Equal(1.0, Opacity(d)));
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void UnderTheLibrarysThemes_TheDotsKeepTheirEightTenths(string kind)
    {
        Assert.All(Dots(Doc(Theme.Default, Chart(kind, null))), d => Assert.Equal(0.8, Opacity(d), 2));
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void AnAlphaThatIsNamed_IsDrawnAsNamed_EightTenthsIncluded(string kind)
    {
        Assert.All(Dots(Doc(Theme.MatplotlibV2, Chart(kind, 0.8))), d => Assert.Equal(0.8, Opacity(d), 2));
        Assert.All(Dots(Doc(Theme.MatplotlibV2, Chart(kind, 0.4))), d => Assert.Equal(0.4, Opacity(d), 2));
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void AnAlpha_SurvivesTheJson_AndOneThatIsNot_StaysUnnamed(string kind)
    {
        string Json(double? alpha) => Plt.Create().WithTheme(Theme.MatplotlibV2).AddSubPlot(1, 1, 1, ax => Chart(kind, alpha)(ax)).Build().ToJson();

        string unnamed = Json(null), named = Json(0.8);

        Assert.DoesNotContain("\"alpha\"", unnamed);
        Assert.Contains("\"alpha\":0.8", named);
        Assert.All(Dots(ChartSerializerDefault.Current.FromJson(unnamed).ToSvg().Xml()), d => Assert.Equal(1.0, Opacity(d)));
        Assert.All(Dots(ChartSerializerDefault.Current.FromJson(named).ToSvg().Xml()), d => Assert.Equal(0.8, Opacity(d), 2));
    }

}
