// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The dot plot in one call: one scatter series of circles for each group, the categories down the Y axis ordered by
/// the first group with the largest on top, the guide lines being the theme's grid along the Y axis, and a missing value that
/// leaves its dot out. The verb sets no axis limits.</summary>
public class DotPlotTests
{
    private static readonly string[] Languages = ["Ruby", "Go", "Rust", "Java", "C#", "Python", "Kotlin"];

    private static readonly BarGroup Older = new("2015", [31, 48, 62, 55, 67, 80, 44]);

    private static readonly BarGroup Newer = new("2025", [35, 52, 60, 58, 71, 84, 50]);

    /// <summary>The categories by the first group, largest first.</summary>
    private static readonly string[] ByOlder = ["Python", "C#", "Rust", "Java", "Go", "Kotlin", "Ruby"];

    private static Axes Dots(IReadOnlyList<BarGroup> groups, Action<ScatterSeries>? configure = null) =>
        Plt.Create().OneAxes(ax => ax.DotPlot(Languages, groups, configure));

    private static ScatterSeries[] Scatters(Axes axes) => [.. axes.Series.Cast<ScatterSeries>()];

    // ---- reading the picture ---------------------------------------------------------------------------------------

    /// <summary>Where a dot is drawn: the centre of its circle, in pixels.</summary>
    private readonly record struct Dot(double Cx, double Cy);

    /// <summary>The plot area in pixels.</summary>
    /// <summary>A text drawn straight into the figure: what it says, and where its baseline stands.</summary>
    private readonly record struct Label(string Text, double Y);

    /// <summary>A horizontal line across the plot area: where it stands and in which colour.</summary>
    private readonly record struct GuideLine(double Y, string Stroke);

    private static string Svg(Action<AxesBuilder> configure, Theme? theme = null)
    {
        var builder = Plt.Create().WithSize(700, 450);
        if (theme is not null) builder = builder.WithTheme(theme);
        return builder.AddSubPlot(1, 1, 1, configure).Build().ToSvg();
    }

    private static string Svg(params BarGroup[] groups) => Svg(ax => ax.DotPlot(Languages, groups));

    private static Dot[] Circles(XElement series) =>
        [.. series.Each("circle").Select(c => new Dot(c.Number("cx"), c.Number("cy")))];

    /// <summary>The Y tick labels, top to bottom: the figure's own texts that end at their anchor.</summary>
    private static Label[] YTickLabels(string svg) =>
        [.. svg.Direct("text").Where(e => (string?)e.Attribute("text-anchor") == "end")
            .Select(e => new Label(e.Value, e.Number("y")))
            .OrderBy(l => l.Y)];

    /// <summary>The lines drawn straight into the figure that run the width of the plot area and stay level: the grid lines
    /// of the Y axis (a tick mark is short and a spine sits inside its own group).</summary>
    private static GuideLine[] GuideLines(string svg)
    {
        var plot = svg.PlotArea();
        return [.. svg.Direct("line")
            .Where(e => Math.Abs(e.Number("x1") - plot.X) < 1e-6 && Math.Abs(e.Number("x2") - (plot.X + plot.Width)) < 1e-6)
            .Where(e => Math.Abs(e.Number("y1") - e.Number("y2")) < 1e-9)
            .Select(e => new GuideLine(e.Number("y1"), (string)e.Attribute("stroke")!))
            .OrderBy(l => l.Y)];
    }

    // ---- one scatter series per group ---------------------------------------------------------------------------------

    /// <summary>The whole chart is one scatter series for each group, named after its group: no line on top for the guides,
    /// nothing on a second axis.</summary>
    [Fact]
    public void TheChart_IsOneScatterSeriesPerGroup_AndNothingElse()
    {
        var axes = Dots([Older, Newer]);

        var scatters = Scatters(axes);
        Assert.Equal(2, scatters.Length);
        Assert.Equal("2015", scatters[0].Label);
        Assert.Equal("2025", scatters[1].Label);
        Assert.Empty(axes.YAxes.Right);
        Assert.Empty(axes.XSecondarySeries);
        Assert.Empty(axes.ReferenceLines);
        Assert.Empty(axes.Annotations);
        Assert.All(scatters, dots =>
        {
            Assert.Equal(MarkerStyle.Circle, dots.Marker);
            Assert.Equal(50, dots.MarkerSize);
        });
    }

    /// <summary>The categories are drawn in the order of the first group's values, largest first, and the other groups follow
    /// that order: the first series reads 80, 67, 62 ... and the second the values of the same categories.</summary>
    [Fact]
    public void TheCategories_AreOrderedByTheFirstGroup_AndTheOthersFollow()
    {
        var scatters = Scatters(Dots([Older, Newer]));

        Assert.Equal([80.0, 67.0, 62.0, 55.0, 48.0, 44.0, 31.0], scatters[0].XData);
        Assert.Equal([84.0, 71.0, 60.0, 58.0, 52.0, 50.0, 35.0], scatters[1].XData);
    }

    /// <summary>The dots of a row share the Y of their category, on the centre of its slot, and the largest is in the TOP row:
    /// the axis counts upward, so the first (largest) category takes the highest slot.</summary>
    [Fact]
    public void TheLargestStandsInTheTopRow_EveryGroupOnTheSameCentres()
    {
        var scatters = Scatters(Dots([Older, Newer]));

        double[] centres = [6.5, 5.5, 4.5, 3.5, 2.5, 1.5, 0.5];
        Assert.Equal(centres, scatters[0].YData);
        Assert.Equal(centres, scatters[1].YData);
    }

    /// <summary>The names are the ticks of the Y axis: a built-in locator and formatter made as one pair, so they survive
    /// JSON, and the tick on a row reads the category whose dots stand on it.</summary>
    [Fact]
    public void TheCategories_AreNamedOnTheYAxis_EachOnItsRow()
    {
        var axes = Dots([Older, Newer]);
        var axis = axes.YAxis;

        Assert.IsType<FixedLocator>(axis.TickLocator);
        Assert.IsType<CategoryFormatter>(axis.TickFormatter);
        var ticks = axis.TickLocator!.Locate(0, Languages.Length);
        Assert.Equal([0.5, 1.5, 2.5, 3.5, 4.5, 5.5, 6.5], ticks);
        double[] rows = Scatters(axes)[0].YData;
        for (int d = 0; d < ByOlder.Length; d++)
        {
            Assert.Equal(ByOlder[d], axis.TickFormatter!.Format(rows[d]));
        }
    }

    /// <summary>Drawn: the names read from the top down in the order of the first group, each level with the dots of its
    /// category, the circles of the two groups share a row, and the rows are evenly spaced.</summary>
    [Fact]
    public void TheNames_ReadTopDown_EachLevelWithItsDots()
    {
        string svg = Svg(Older, Newer);
        var groups = svg.SeriesGroups();
        var older = Circles(groups[0]);
        var newer = Circles(groups[1]);

        var labels = YTickLabels(svg);
        Assert.Equal(ByOlder, labels.Select(l => l.Text));
        for (int d = 0; d < ByOlder.Length; d++)
        {
            // The tick label is centred on the tick, which is on the row: its baseline 0.35 of its ascent (the font size) under it.
            Assert.Equal(older[d].Cy + 0.35 * MatPlotLibNet.Styling.Theme.MatplotlibV2.TickFont().Size, labels[d].Y, SvgMarkupExtensions.WrittenPrecision);
            Assert.Equal(older[d].Cy, newer[d].Cy, SvgMarkupExtensions.WrittenPrecision);
        }

        double pitch = older[1].Cy - older[0].Cy;
        Assert.True(pitch > 0);
        for (int d = 1; d < older.Length; d++) Assert.Equal(pitch, older[d].Cy - older[d - 1].Cy, SvgMarkupExtensions.WrittenPrecision);
        // The first group's dots run down and to the left as the values fall.
        Assert.True(older.Zip(older.Skip(1), (a, b) => a.Cx > b.Cx).All(x => x));
    }

    // ---- ties and missing values in the order ---------------------------------------------------------------------------

    /// <summary>A tie keeps the order the categories were given in (a stable sort), and a category whose first value is
    /// missing goes last, below every number; the other group follows each category.</summary>
    [Fact]
    public void ATie_KeepsTheOrderGiven_AndAMissingFirstValueGoesLast()
    {
        string[] names = ["a", "b", "c", "d"];
        BarGroup first = new("first", [5, 5, double.NaN, 3]);
        BarGroup second = new("second", [1, 2, 3, 4]);

        var axes = Plt.Create().OneAxes(ax => ax.DotPlot(names, [first, second]));
        var scatters = Scatters(axes);

        double[] rows = scatters[0].YData;
        Assert.Equal(["a", "b", "d", "c"], rows.Select(axes.YAxis.TickFormatter!.Format));
        Assert.Equal([5.0, 5.0, 3.0, double.NaN], scatters[0].XData);
        Assert.Equal([1.0, 2.0, 4.0, 3.0], scatters[1].XData);
    }

    /// <summary>Equal values everywhere keep every category in the order given: nothing is reshuffled.</summary>
    [Fact]
    public void EqualValues_KeepTheCategoriesInTheOrderGiven()
    {
        string[] names = ["x", "y", "z"];

        var axes = Plt.Create().OneAxes(ax => ax.DotPlot(names, [new BarGroup("g", [7, 7, 7])]));

        Assert.Equal(names, Scatters(axes)[0].YData.Select(axes.YAxis.TickFormatter!.Format));
    }

    /// <summary>A missing value in a later group leaves that dot out and nothing else: the row keeps its name and its other
    /// dots, and the picture holds no NaN.</summary>
    [Fact]
    public void AMissingValue_LeavesThatDotOut_AndNothingElse()
    {
        var gappy = new BarGroup("2025", [35, 52, 60, double.NaN, 71, 84, 50]);
        string svg = Svg(Older, gappy);

        var groups = svg.SeriesGroups();
        Assert.Equal(7, Circles(groups[0]).Length);
        Assert.Equal(6, Circles(groups[1]).Length);
        Assert.Equal(ByOlder, YTickLabels(svg).Select(l => l.Text));
        svg.AssertNoNonFiniteNumber();
    }

    /// <summary>A missing first value puts its category in the bottom row without a dot of the first group, and the second
    /// group's dot is still drawn on that row.</summary>
    [Fact]
    public void AMissingFirstValue_PutsTheCategoryAtTheBottom_WithoutItsFirstDot()
    {
        var gappy = new BarGroup("2015", [31, 48, 62, 55, double.NaN, 80, 44]);
        string svg = Svg(gappy, Newer);

        var groups = svg.SeriesGroups();
        var labels = YTickLabels(svg);
        Assert.Equal("C#", labels[^1].Text);
        Assert.Equal(6, Circles(groups[0]).Length);
        var newer = Circles(groups[1]);
        Assert.Equal(7, newer.Length);
        Assert.Equal(labels[^1].Y - 0.35 * MatPlotLibNet.Styling.Theme.MatplotlibV2.TickFont().Size, newer.Max(c => c.Cy), SvgMarkupExtensions.WrittenPrecision);
        svg.AssertNoNonFiniteNumber();
    }

    // ---- the guide lines are the grid -----------------------------------------------------------------------------------

    /// <summary>The guide lines are the axes' grid along the Y axis, not reference lines of their own.</summary>
    [Fact]
    public void TheGuideLines_AreTheGridAlongTheYAxis_NotLinesOfTheirOwn()
    {
        var axes = Dots([Older, Newer]);

        Assert.Empty(axes.ReferenceLines);
        Assert.True(axes.Grid.Visible);
        Assert.Equal(GridAxis.Y, axes.Grid.Axis);
    }

    /// <summary>Drawn: one line through each category, across the whole plot area, level with the dots of its row.</summary>
    [Fact]
    public void TheGuideLines_RunThroughEveryRow_AcrossThePlot()
    {
        string svg = Svg(Older, Newer);
        var rows = Circles(svg.SeriesGroups()[0]).Select(c => c.Cy).Order().ToList();

        var lines = GuideLines(svg);

        Assert.Equal(Languages.Length, lines.Length);
        for (int d = 0; d < rows.Count; d++) Assert.Equal(rows[d], lines[d].Y, 6);
    }

    /// <summary>The guide lines wear the theme's grid, whatever theme the figure ends up on: not a colour fixed when the chart
    /// was built (the theme can be set after the verb).</summary>
    [Theory]
    [InlineData("Default")]
    [InlineData("Dark")]
    [InlineData("Ggplot")]
    public void TheGuideLines_FollowTheThemesGrid(string themeName)
    {
        var theme = themeName.AsTheme();

        var lines = GuideLines(Svg(ax => ax.DotPlot(Languages, [Older, Newer]), theme));

        Assert.Equal(Languages.Length, lines.Length);
        Assert.All(lines, line => Assert.Equal(theme.DefaultGrid.Color.ToHex(), line.Stroke));
        Assert.NotEqual(Theme.Default.DefaultGrid.Color, Theme.Dark.DefaultGrid.Color);
    }

    // ---- names that are longer than the usual margin ------------------------------------------------------------------

    /// <summary>A dot plot is read by its names: countries, products, people. Names wider than the theme's usual left margin
    /// stand whole in the picture, the margin grows to hold them, and the plot keeps its dots.</summary>
    [Fact]
    public void LongNames_StandWholeInsideTheFigure()
    {
        string[] countries = ["Netherlands", "United Kingdom", "Germany", "Czech Republic", "France", "Switzerland"];
        string svg = Svg(ax => ax.DotPlot(countries, [new BarGroup("a", [3, 9, 5, 7, 2, 6]), new BarGroup("b", [4, 8, 6, 9, 3, 5])]));
        var plot = svg.PlotArea();

        var names = svg.Direct("text").Where(e => (string?)e.Attribute("text-anchor") == "end" && countries.Contains(e.Value))
            .ToList();

        Assert.Equal(countries.Length, names.Count);
        Assert.All(names, e =>
        {
            double size = e.Number("font-size");
            double start = e.Number("x") - FontMetricsDefault.Current.Measure(e.Value, new Font { Size = size }).Width;
            Assert.True(start >= 0, $"'{e.Value}' starts at {start}, left of the figure");
            Assert.True(start < plot.X);
        });
    }

    // ---- limits -------------------------------------------------------------------------------------------------------

    /// <summary>Every dot, the first and last rows' and the lowest and highest values' included, is a whole circle inside the
    /// plot area.</summary>
    [Fact]
    public void EveryDot_IsAWholeCircle_InsideThePlotArea()
    {
        string svg = Svg(Older, Newer);
        var plot = svg.PlotArea();
        double radius = svg.Elements("circle")[0].Number("r");

        foreach (var group in svg.SeriesGroups())
        {
            Assert.All(Circles(group), c =>
            {
                Assert.True(c.Cx - radius >= plot.X && c.Cx + radius <= plot.X + plot.Width, $"a dot at x = {c.Cx} is cut by the plot area {plot.X}..{plot.X + plot.Width}");
                Assert.True(c.Cy - radius >= plot.Y && c.Cy + radius <= plot.Y + plot.Height, $"a dot at y = {c.Cy} is cut by the plot area {plot.Y}..{plot.Y + plot.Height}");
            });
        }
    }

    /// <summary>One category is one row: its dots are drawn whole inside the plot, not on the frame.</summary>
    [Fact]
    public void OneCategory_IsOneRow_InsideThePlotArea()
    {
        string svg = Svg(ax => ax.DotPlot(["only"], [new BarGroup("a", [3]), new BarGroup("b", [5])]));
        var plot = svg.PlotArea();

        var groups = svg.SeriesGroups();
        Assert.Equal(["only"], YTickLabels(svg).Select(l => l.Text));
        Assert.All(groups.SelectMany(Circles), c => Assert.InRange(c.Cy, plot.Y + 1, plot.Y + plot.Height - 1));
        svg.AssertNoNonFiniteNumber();
    }

    // ---- colours and the legend -----------------------------------------------------------------------------------------

    /// <summary>One drawn unit is one colour: the two groups wear two colours of the cycle, every circle of a group its own,
    /// and a legend that is switched on lists exactly the groups.</summary>
    [Fact]
    public void EveryGroup_WearsOneColourOfItsOwn_AndTheLegendListsTheGroups()
    {
        var figure = Plt.Create().WithSize(700, 450).AddSubPlot(1, 1, 1, ax => ax.DotPlot(Languages, [Older, Newer])).Build();
        figure.EnableLegendToggle = true;
        string svg = figure.ToSvg();

        var groups = svg.SeriesGroups();
        var fills = groups.Select(g => g.Each("circle").Select(c => (string)c.Attribute("fill")!).Distinct().Single()).ToList();
        Assert.Equal(2, fills.Distinct().Count());
        Assert.Equal(figure.Theme.CycleColors[0].ToHex(), fills[0]);
        Assert.Equal(figure.Theme.CycleColors[1].ToHex(), fills[1]);
        Assert.Equal(["0:2015", "1:2025"], svg.LegendEntries().Select(entry => $"{entry.Index}:{entry.Label}"));
    }

    /// <summary>The legend stands in the lower right corner, the corner the order of the rows leaves empty (the largest
    /// values are at the top right, the smallest at the bottom left): it covers no dot, where the upper right would cover the
    /// largest ones. The verb names the place, whatever the data.</summary>
    [Fact]
    public void TheLegend_StandsInTheCornerTheOrderLeavesEmpty_AndCoversNoDot()
    {
        string svg = Svg(Older, Newer);
        var plot = svg.PlotArea();
        var box = svg.LegendFrame()!.Value;
        double radius = svg.Elements("circle")[0].Number("r");

        Assert.True(box.X + box.Width / 2 > plot.X + plot.Width / 2, "the legend is not in the right half of the plot");
        Assert.True(box.Y + box.Height / 2 > plot.Y + plot.Height / 2, "the legend is not in the lower half of the plot");
        foreach (var dot in svg.SeriesGroups().SelectMany(Circles))
        {
            bool covered = dot.Cx + radius > box.X && dot.Cx - radius < box.X + box.Width
                        && dot.Cy + radius > box.Y && dot.Cy - radius < box.Y + box.Height;
            Assert.False(covered, $"the legend covers the dot at ({dot.Cx}, {dot.Cy})");
        }
    }

    /// <summary>The verb's legend position is a default, not a decision for the caller: a <c>WithLegend</c> after the call
    /// moves it, or switches it off.</summary>
    [Fact]
    public void ALegendAskedForAfterTheCall_WinsOverTheVerbsPosition()
    {
        var moved = Plt.Create().OneAxes(ax => ax.DotPlot(Languages, [Older, Newer]).WithLegend(LegendPosition.UpperLeft));
        string off = Svg(ax => ax.DotPlot(Languages, [Older, Newer]).WithLegend(visible: false));

        Assert.Equal(LegendPosition.LowerRight, Dots([Older, Newer]).Legend.Position);
        Assert.Equal(LegendPosition.UpperLeft, moved.Legend.Position);
        Assert.DoesNotContain("""class="legend" """.TrimEnd(), off);
    }

    // ---- the verb draws what the hand chain drew -----------------------------------------------------------------------

    /// <summary>Written to JSON and read back, the chart draws the same picture, a missing value and a missing first value
    /// included: the names are still the ticks, from the top down, and the grid is still along the Y axis.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheChart_ReadBackFromJson_DrawsTheSamePicture(bool dark)
    {
        var first = new BarGroup("2015", [31, 48, 62, 55, double.NaN, 80, 44]);
        var second = new BarGroup("2025", [35, 52, double.NaN, 58, 71, 84, 50]);
        var builder = Plt.Create().WithSize(700, 450);
        if (dark) builder = builder.WithTheme(Theme.Dark);
        var figure = builder.AddSubPlot(1, 1, 1, ax => ax.DotPlot(Languages, [first, second])).Build();
        string drawn = figure.ToSvg();

        string readBack = figure.RoundTripped().ToSvg();

        Assert.Equal(drawn, readBack);
        Assert.Equal("C#", YTickLabels(readBack)[^1].Text);
        Assert.Equal(Languages.Length, GuideLines(readBack).Length);
    }

    // ---- no data to speak of ----------------------------------------------------------------------------------------

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        //  No categories at all is an empty chart, as every other verb draws with no data: it does not throw and holds no NaN.
        new DrawnToken("NoCategories_DrawAnEmptyChart_WithoutThrowing", () => Svg(ax => ax.DotPlot([], [new BarGroup("a", [])]))) { Finite = true },
        //  The example of the verb's documentation, as written there, compiles and draws.
        new DrawnToken("TheDocumentationExample_Draws", () =>
        {
            string[] languages = ["Ruby", "Go", "Rust", "Java", "Python"];
            return Plt.Create()
                .WithTitle("Programming languages")
                .AddSubPlot(1, 1, 1, ax => ax
                    .DotPlot(languages,
                    [
                        new BarGroup("2015", [31.0, 48.0, 62.0, 55.0, 80.0]),
                        new BarGroup("2025", [35.0, 52.0, 60.0, 58.0, 84.0]),
                    ])
                    .SetXLabel("Share of developers (%)"))
                .ToSvg();
        }, ">Python<", ">2025<", ">Share of developers (%)<", ">Programming languages<"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    // ---- refused arguments -----------------------------------------------------------------------------------------

    /// <summary>A group that does not carry one value per category is refused, naming the group, both counts and the
    /// parameter, in the words every verb that takes groups uses; a group with no values at all is refused by name, not with
    /// a null reference.</summary>
    [Fact]
    public void AGroupThatDoesNotFitTheCategories_IsRefusedByName()
    {
        var shortGroup = new BarGroup("2025", [1.0, 2.0]);

        var error = Assert.Throws<ArgumentException>(() => Dots([Older, shortGroup]));
        var nullError = Assert.Throws<ArgumentException>(() => Dots([Older, new BarGroup("2025", null!)]));

        Assert.Equal("groups", error.ParamName);
        Assert.Equal(2.RefusalAgainst(7, "groups", "group '2025'"), error.Message);
        Assert.Equal("groups", nullError.ParamName);
        Assert.Equal("Group '2025' carries no values; there are 7 categories. (Parameter 'groups')", nullError.Message);
    }

}
