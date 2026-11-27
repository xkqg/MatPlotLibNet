// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>How far the dots of a strip plot stray from their category where their author names no jitter. seaborn 0.13.2, read with
/// inspect: <c>_CategoricalPlotter.plot_strips</c> sets <c>jlim = 0.1</c> where <c>jitter is True</c> (the default), multiplies it by the native
/// width (1 for a categorical axis) and draws <c>np.random.uniform(low=-jlim, high=+jlim)</c>: a dot moves by up to 0.1 of a category either way.
/// The library drew up to 0.2. The matplotlib themes now draw seaborn's 0.1, the library's own themes keep their 0.2, and a jitter that is
/// named is drawn as named, 0.2 included.</summary>
public class DotJitterTests
{
    private static readonly double[][] Groups = [[1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0], [2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0]];

    private static List<double> Xs(string svg) =>
        [.. svg.Each("circle").Where(e => !e.Ancestors().Any(a => (string?)a.Attribute("class") == "legend"))
            .Select(e => e.Number("cx"))];

    private static string Svg(Theme theme, double? jitter) =>
        Plt.Create().WithTheme(theme).WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Stripplot(Groups, s => { if (jitter is { } j) s.Jitter = j; })).ToSvg();

    /// <summary>The reach of a dot from its category in units of the jitter that moved it: the dot's pixel offset from where the same dot
    /// stands with no jitter, over the pixel offset one full unit of jitter makes (the dots of one seed move in proportion to the jitter).</summary>
    private static double Reach(Theme theme, double? jitter)
    {
        var rest = Xs(Svg(theme, 0));
        var moved = Xs(Svg(theme, jitter));
        var full = Xs(Svg(theme, 1.0));
        Assert.Equal(rest.Count, moved.Count);
        return rest.Select((x, i) => Math.Abs(moved[i] - x)).Max() / rest.Select((x, i) => Math.Abs(full[i] - x)).Max();
    }

    [Fact]
    public void UnderTheMatplotlibThemes_TheDotsStrayByATenthOfACategory_WhereNoJitterIsNamed()
    {
        Assert.Equal(0.1, Reach(Theme.MatplotlibV2, null), 3);
        Assert.Equal(0.1, Reach(Theme.MatplotlibClassic, null), 3);
    }

    [Fact]
    public void UnderTheLibrarysThemes_TheDotsKeepTheirTwoTenths()
    {
        Assert.Equal(0.2, Reach(Theme.Default, null), 3);
    }

    [Fact]
    public void AJitterThatIsNamed_IsDrawnAsNamed_TwoTenthsIncluded()
    {
        Assert.Equal(0.2, Reach(Theme.MatplotlibV2, 0.2), 3);
        Assert.Equal(0.35, Reach(Theme.MatplotlibV2, 0.35), 3);
        Assert.Equal(0.05, Reach(Theme.Default, 0.05), 3);
    }

    [Fact]
    public void AJitter_SurvivesTheJson_AndOneThatIsNot_StaysUnnamed()
    {
        string Json(double? jitter) => Plt.Create().WithTheme(Theme.MatplotlibV2).WithSize(600, 400).AddSubPlot(1, 1, 1, ax => ax.Stripplot(Groups, s => { if (jitter is { } j) s.Jitter = j; })).Build().ToJson();

        string unnamed = Json(null), named = Json(0.2);

        Assert.DoesNotContain("\"jitter\"", unnamed);
        Assert.Contains("\"jitter\":0.2", named);
        var again = ChartSerializerDefault.Current;
        Assert.Equal(Xs(Svg(Theme.MatplotlibV2, null)), Xs(again.FromJson(unnamed).ToSvg()));
        Assert.Equal(Xs(Svg(Theme.MatplotlibV2, 0.2)), Xs(again.FromJson(named).ToSvg()));
    }
}
