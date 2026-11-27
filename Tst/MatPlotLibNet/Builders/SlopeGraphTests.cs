// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The slope graph in one call: one line series for each category between two moments with a marker at each end, the
/// category's name at both ends (at the end of the line as a direct label in the line's colour, left of the start as an
/// annotation in the theme's text colour), the two moments as the X ticks on the centres of two slots, and a missing value
/// that takes the name of the point it belongs to with it. The verb sets no axis limits; it widens the X margin so the start
/// names stand clear of the Y axis.</summary>
public class SlopeGraphTests
{
    private static readonly string[] Regions = ["North", "South", "East", "West", "Central"];

    private static readonly BarGroup Start = new("2015", [42, 55, 38, 61, 47]);

    private static readonly BarGroup End = new("2025", [51, 49, 44, 66, 39]);

    private static Axes Slope(BarGroup from, BarGroup to, Action<LineSeries>? configure = null) =>
        Plt.Create().OneAxes(ax => ax.SlopeGraph(Regions, from, to, configure));

    private static LineSeries[] Lines(Axes axes) => [.. axes.Series.Cast<LineSeries>()];

    // ---- reading the picture ---------------------------------------------------------------------------------------

    /// <summary>Where a marker is drawn: the centre of its circle, in pixels.</summary>
    private readonly record struct Dot(double Cx, double Cy);

    /// <summary>The plot area in pixels.</summary>
    /// <summary>A text drawn straight into the figure: what it says, where its anchor stands, and its colour.</summary>
    private readonly record struct Name(string Text, double X, double Y, string Fill);

    /// <summary>A <see cref="Name"/> with the side of the text its anchor is on (<c>start</c>, <c>middle</c> or <c>end</c>).</summary>
    private readonly record struct FigureText(Name Text, string Anchor);

    private static string Svg(Action<AxesBuilder> configure, Theme? theme = null)
    {
        var builder = Plt.Create().WithSize(700, 500);
        if (theme is not null) builder = builder.WithTheme(theme);
        return builder.AddSubPlot(1, 1, 1, configure).Build().ToSvg();
    }

    /// <summary>The font a name is drawn in: the text size of the theme a figure wears when none is chosen.</summary>
    private static Font NameFont() => Plt.Create().Build().Theme.DataTextFont();

    private static string Svg(BarGroup from, BarGroup to) => Svg(ax => ax.SlopeGraph(Regions, from, to));

    private static Dot[] Dots(XElement series) =>
        [.. series.Each("circle").Select(c => new Dot(c.Number("cx"), c.Number("cy")))];

    private static int Runs(XElement series) => series.Each("polyline").Count();

    private static Name? EndName(XElement series) =>
        series.Children("text").Select(t => new Name(
            t.Value, t.Number("x"), t.Number("y"), (string)t.Attribute("fill")!))
            .Cast<Name?>().SingleOrDefault();

    /// <summary>The texts drawn straight into the figure, not inside a series, with the anchor they are drawn at.</summary>
    private static List<FigureText> FigureTexts(string svg) =>
        [.. svg.Direct("text")
            .Select(e => new FigureText(
                new Name(e.Value, e.Number("x"), e.Number("y"), (string)e.Attribute("fill")!),
                (string)e.Attribute("text-anchor")!))];

    /// <summary>The start names: the figure's own texts that read a category and end at their anchor.</summary>
    private static Name[] StartNames(string svg) =>
        [.. FigureTexts(svg).Where(t => t.Anchor == "end" && Regions.Contains(t.Text.Text)).Select(t => t.Text)];

    private static Name[] XTickLabels(string svg) =>
        [.. FigureTexts(svg).Where(t => t.Anchor == "middle").Select(t => t.Text)];

    // ---- one line series per category ------------------------------------------------------------------------------

    /// <summary>The whole chart is one line series for each category: no scatter on top for the markers, nothing on a second
    /// axis, the category as the line's name.</summary>
    [Fact]
    public void TheChart_IsOneLineSeriesPerCategory_AndNothingElse()
    {
        var axes = Slope(Start, End);

        var lines = Lines(axes);
        Assert.Equal(Regions.Length, lines.Length);
        Assert.Empty(axes.YAxes.Right);
        Assert.Empty(axes.XSecondarySeries);
        for (int k = 0; k < Regions.Length; k++)
        {
            Assert.Equal(Regions[k], lines[k].Label);
            Assert.Equal([Start.Values[k], End.Values[k]], lines[k].YData);
        }
    }

    /// <summary>The two moments stand on the centres of two category slots, the rule every verb that names categories
    /// follows, so the X ticks that name the moments sit under the two ends of every line.</summary>
    [Fact]
    public void EveryLine_StandsOnTheCentresOfTheTwoColumns_AndHoldsItsOwnXArray()
    {
        var lines = Lines(Slope(Start, End));

        Assert.All(lines, line => Assert.Equal([0.5, 1.5], line.XData));
        Assert.NotSame(lines[0].XData, lines[1].XData);
    }

    /// <summary>A marker at each end and a line of its own width: the look of a slope graph.</summary>
    [Fact]
    public void EveryLine_HasAMarkerAtBothEnds()
    {
        var lines = Lines(Slope(Start, End));

        Assert.All(lines, line =>
        {
            Assert.Equal(MarkerStyle.Circle, line.Marker);
            Assert.Equal(8, line.MarkerSize);
            Assert.Equal(2, line.LineWidth);
        });
    }

    /// <summary>One drawn unit is one colour: five lines wear five colours of the cycle, and each line's markers wear the
    /// line's colour.</summary>
    [Fact]
    public void EveryLine_WearsOneColourOfItsOwn_ItsMarkersToo()
    {
        var groups = Svg(Start, End).SeriesGroups();

        var strokes = groups.Select(g => (string)g.Each("polyline").First().Attribute("stroke")!).ToList();
        Assert.Equal(Regions.Length, strokes.Distinct().Count());
        for (int k = 0; k < groups.Count; k++)
        {
            Assert.All(groups[k].Each("circle"), c => Assert.Equal(strokes[k], (string)c.Attribute("fill")!));
        }
    }

    // ---- the names at both ends -------------------------------------------------------------------------------------

    /// <summary>The end name is a direct label, which replaces the legend entry: it stands right of the plot area in the
    /// line's colour at the height of the end point, and a legend that is switched on lists nothing.</summary>
    [Fact]
    public void TheEndNames_AreDirectLabelsInTheLinesColour_NotLegendEntries()
    {
        Assert.All(Lines(Slope(Start, End)), line => Assert.Equal(SeriesLabelPlacement.LineEnd, line.LabelPlacement));

        string svg = Svg(ax => ax.SlopeGraph(Regions, Start, End).WithLegend());
        var plot = svg.PlotArea();
        Assert.DoesNotContain("data-legend-index=", svg);
        var groups = svg.SeriesGroups();
        for (int k = 0; k < Regions.Length; k++)
        {
            var label = Assert.IsType<Name>(EndName(groups[k]));
            Assert.Equal(Regions[k], label.Text);
            Assert.True(label.X > plot.X + plot.Width, $"'{label.Text}' starts at {label.X}, not right of the plot area ending at {plot.X + plot.Width}");
            Assert.Equal((string)groups[k].Each("polyline").First().Attribute("stroke")!, label.Fill);
        }
    }

    /// <summary>The start name is an annotation at the start value, right-aligned so it ends left of the start point, in no
    /// colour and no font of its own (so the theme's text colour decides: a colour set here would not follow the theme).</summary>
    [Fact]
    public void TheStartNames_AreAnnotationsLeftOfTheStart_WithNoColourOfTheirOwn()
    {
        var axes = Slope(Start, End);

        Assert.Equal(Regions.Length, axes.Annotations.Count);
        for (int k = 0; k < Regions.Length; k++)
        {
            var annotation = axes.Annotations[k];
            Assert.Equal(Regions[k], annotation.Text);
            Assert.Equal(Start.Values[k], annotation.Y);
            Assert.True(annotation.X < 0.5, $"the start name of '{Regions[k]}' stands at x = {annotation.X}, not left of the start column at 0.5");
            Assert.Equal(TextAlignment.Right, annotation.Alignment);
            Assert.Equal(TextVerticalAlignment.Center, annotation.VerticalAlignment);
            Assert.Null(annotation.Color);
            Assert.Null(annotation.Font);
            Assert.Equal(AnnotationCoordinates.Data, annotation.Coordinates);
            Assert.Null(annotation.ArrowTargetX);
        }
    }

    /// <summary>Drawn: each start name ends just left of its start marker, at the marker's height, and starts inside the plot
    /// area: the longest name has room between the Y axis and its marker, clear of the tick labels outside the spine.</summary>
    [Fact]
    public void TheStartNames_StandJustLeftOfTheirMarkers_InsideThePlotArea()
    {
        string svg = Svg(Start, End);
        var plot = svg.PlotArea();
        var groups = svg.SeriesGroups();
        var names = StartNames(svg);
        var measurer = new SvgRenderContext();
        var font = NameFont();

        Assert.Equal(Regions, names.Select(n => n.Text));
        for (int k = 0; k < Regions.Length; k++)
        {
            var marker = Dots(groups[k])[0];
            Assert.Equal(marker.Cy + 0.35 * font.Size, names[k].Y, SvgMarkupExtensions.WrittenPrecision);   // centred on the marker: the baseline a third of the size below it
            double gap = marker.Cx - names[k].X;
            Assert.True(gap > 4 && gap < 30, $"'{Regions[k]}' ends {gap} px left of its marker; the marker's radius is 4");
            double start = names[k].X - measurer.MeasureText(Regions[k], font).Width;
            Assert.True(start > plot.X, $"'{Regions[k]}' starts at {start}, left of the plot area beginning at {plot.X}");
        }
    }

    /// <summary>A start name stands on its marker by the rule the end name stands on its own: a third of the size of the
    /// theme's text below the point it names, whatever that size is.</summary>
    [Theory]
    [InlineData("Default")]
    [InlineData("Poster")]
    [InlineData("Paper")]
    public void TheStartNames_AreCentredOnTheirMarkers(string themeName)
    {
        var theme = themeName.AsTheme();
        string svg = Svg(ax => ax.SlopeGraph(Regions, Start, End), theme);
        var groups = svg.SeriesGroups();
        var names = StartNames(svg);

        for (int k = 0; k < Regions.Length; k++)
            Assert.Equal(0.35 * theme.DefaultFont.Size, names[k].Y - Dots(groups[k])[0].Cy, 6);
    }

    /// <summary>Where the end names stand clear of one another they stand on their markers by that same rule, so a start
    /// name and an end name are centred alike.</summary>
    [Fact]
    public void TheStartNames_AreCentredAsTheEndNamesAre_WhereTheEndNamesAreNotPushedApart()
    {
        string svg = Svg(Start, End);
        var groups = svg.SeriesGroups();
        var names = StartNames(svg);

        for (int k = 0; k < Regions.Length; k++)
        {
            var dots = Dots(groups[k]);
            Assert.Equal(EndName(groups[k])!.Value.Y - dots[1].Cy, names[k].Y - dots[0].Cy, 6);
        }
    }

    /// <summary>The start names wear the theme's text colour, whatever theme the figure ends up on: under the dark theme they
    /// are drawn in its light text, not in a colour fixed when the chart was built (the theme can be set after the verb).</summary>
    [Theory]
    [InlineData("Default")]
    [InlineData("Dark")]
    [InlineData("Ggplot")]
    public void TheStartNames_FollowTheThemesTextColour(string themeName)
    {
        var theme = themeName.AsTheme();

        var names = StartNames(Svg(ax => ax.SlopeGraph(Regions, Start, End), theme));

        Assert.Equal(Regions.Length, names.Length);
        Assert.All(names, n => Assert.Equal(theme.ForegroundText.ToHex(), n.Fill));
        Assert.NotEqual(Theme.Default.ForegroundText, Theme.Dark.ForegroundText);
    }

    // ---- the two moments --------------------------------------------------------------------------------------------

    /// <summary>The moments are the ticks of the X axis: a built-in locator and formatter made as one pair, so they survive
    /// JSON, one tick on each column, reading the two groups' names.</summary>
    [Fact]
    public void TheMoments_AreNamedOnTheXAxis_UnderTheTwoColumns()
    {
        var axis = Slope(Start, End).XAxis;

        Assert.IsType<FixedLocator>(axis.TickLocator);
        Assert.IsType<CategoryFormatter>(axis.TickFormatter);
        var ticks = axis.TickLocator!.Locate(0, 2);
        Assert.Equal([0.5, 1.5], ticks);
        Assert.Equal(["2015", "2025"], ticks.Select(axis.TickFormatter!.Format));

        string svg = Svg(Start, End);
        var labels = XTickLabels(svg);
        var first = Dots(svg.SeriesGroups()[0]);
        Assert.Equal(["2015", "2025"], labels.Select(l => l.Text));
        Assert.Equal(first[0].Cx, labels[0].X, 6);
        Assert.Equal(first[1].Cx, labels[1].X, 6);
    }

    /// <summary>A moment without a name reads as an empty tick, not as an exception: a group made without a label, at either
    /// end.</summary>
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void AMomentWithoutAName_IsAnEmptyTick(bool startUnnamed, bool endUnnamed)
    {
        var from = new BarGroup(startUnnamed ? null! : "2015", Start.Values);
        var to = new BarGroup(endUnnamed ? null! : "2025", End.Values);

        var axis = Slope(from, to).XAxis;

        Assert.Equal([startUnnamed ? "" : "2015", endUnnamed ? "" : "2025"],
            axis.TickLocator!.Locate(0, 2).Select(axis.TickFormatter!.Format));
    }

    // ---- limits and room ----------------------------------------------------------------------------------------------

    /// <summary>The X margin, not a limit, makes room for the start names: a quarter of the distance between the two columns
    /// on each side, where the theme's usual 5 % leaves no more than a few pixels. A margin set after the call replaces it.</summary>
    [Fact]
    public void TheVerb_WidensTheXMargin_ToHoldTheStartNames_AndALaterMarginWins()
    {
        Assert.Equal(0.25, Slope(Start, End).XAxis.Margin);
        Assert.Null(Slope(Start, End).YAxis.Margin);

        var later = Plt.Create().OneAxes(ax => ax.SlopeGraph(Regions, Start, End).SetXMargin(0.4));
        Assert.Equal(0.4, later.XAxis.Margin);
    }

    /// <summary>Without that margin the start names would stand across the Y axis' tick labels: measured against the plain
    /// chain, the margin is what puts the whole of the longest name inside the plot area.</summary>
    [Fact]
    public void WithoutTheMargin_TheLongestStartName_WouldRunOutOfThePlotArea()
    {
        string withMargin = Svg(Start, End);
        string theThemesMargin = Svg(ax => ax.SlopeGraph(Regions, Start, End).SetXMargin(0.05));
        var measurer = new SvgRenderContext();
        var font = NameFont();
        double width = measurer.MeasureText("Central", font).Width;

        var central = StartNames(withMargin).Single(n => n.Text == "Central");
        var squeezed = StartNames(theThemesMargin).Single(n => n.Text == "Central");

        Assert.True(central.X - width > withMargin.PlotArea().X);
        Assert.True(squeezed.X - width < theThemesMargin.PlotArea().X);
    }

    // ---- missing values ---------------------------------------------------------------------------------------------

    /// <summary>A missing start (NaN) leaves no line and no start name, keeps the end point as a marker with its end name, and
    /// leaves the other categories alone; the picture holds no NaN.</summary>
    [Fact]
    public void AMissingStart_DropsTheStartName_AndKeepsTheEndPointWithItsName()
    {
        var gappy = new BarGroup("2015", [42, 55, double.NaN, 61, 47]);
        string svg = Svg(gappy, End);

        var groups = svg.SeriesGroups();
        Assert.Single(Dots(groups[2]));
        Assert.Equal(0, Runs(groups[2]));
        Assert.Equal("East", Assert.IsType<Name>(EndName(groups[2])).Text);
        Assert.Equal(["North", "South", "West", "Central"], StartNames(svg).Select(n => n.Text));
        Assert.Equal(2, Dots(groups[0]).Length);
        Assert.Equal(1, Runs(groups[0]));
        svg.AssertNoNonFiniteNumber();
        // The start name is not added at all, so no annotation holds a value that cannot be placed (nor writes it to JSON).
        Assert.Equal(["North", "South", "West", "Central"], Slope(gappy, End).Annotations.Select(a => a.Text));
    }

    /// <summary>A missing end (NaN) leaves no line and no end name at the line end (the end label names the end point, and a
    /// line without one has none: it would otherwise stand at the start's height, beside nothing), keeps the start point
    /// with its start name, and leaves the other categories alone.</summary>
    [Fact]
    public void AMissingEnd_DropsTheEndName_AndKeepsTheStartPointWithItsName()
    {
        var gappy = new BarGroup("2025", [51, 49, double.NaN, 66, 39]);
        string svg = Svg(Start, gappy);

        var groups = svg.SeriesGroups();
        Assert.Single(Dots(groups[2]));
        Assert.Equal(0, Runs(groups[2]));
        Assert.Null(EndName(groups[2]));
        Assert.Equal(Regions, StartNames(svg).Select(n => n.Text));
        Assert.All(new[] { 0, 1, 3, 4 }, k => Assert.NotNull(EndName(groups[k])));
        svg.AssertNoNonFiniteNumber();
    }

    /// <summary>The chart draws no legend, with or without a missing end: its names stand at the ends of the lines, and a legend
    /// box that appeared only because a value is missing would cover the upper right of the plot for a name that the start
    /// name already gives.</summary>
    [Fact]
    public void TheChart_DrawsNoLegend_EvenWhenAnEndIsMissing()
    {
        var gappy = new BarGroup("2025", [51, 49, double.NaN, 66, 39]);

        Assert.DoesNotContain("""class="legend" """.TrimEnd(), Svg(Start, End));
        Assert.DoesNotContain("""class="legend" """.TrimEnd(), Svg(Start, gappy));
        Assert.False(Slope(Start, gappy).Legend.Visible);
    }

    /// <summary>The category that has no end name keeps its name for the legend of a caller who switches one on, and only that
    /// one: the others are named at the end of their lines.</summary>
    [Fact]
    public void ACategoryWithoutAnEnd_IsNamedInTheLegend_WhenOneIsDrawn()
    {
        var gappy = new BarGroup("2025", [51, 49, double.NaN, 66, 39]);

        var lines = Lines(Slope(Start, gappy));
        var figure = Plt.Create().WithSize(700, 500).AddSubPlot(1, 1, 1, ax => ax.SlopeGraph(Regions, Start, gappy).WithLegend()).Build();
        figure.EnableLegendToggle = true;
        string svg = figure.ToSvg();

        Assert.Equal(SeriesLabelPlacement.Legend, lines[2].LabelPlacement);
        Assert.All(new[] { 0, 1, 3, 4 }, k => Assert.Equal(SeriesLabelPlacement.LineEnd, lines[k].LabelPlacement));
        var entry = Assert.Single(svg.LegendEntries());
        Assert.Equal(2, entry.Index);
        Assert.Equal("East", entry.Label);
    }

    /// <summary>A category without either point draws nothing: no marker, no line, no name at either end, and the others are
    /// drawn as they were.</summary>
    [Fact]
    public void ACategoryWithoutEitherPoint_DrawsNothing()
    {
        var startGappy = new BarGroup("2015", [42, 55, double.NaN, 61, 47]);
        var endGappy = new BarGroup("2025", [51, 49, double.NaN, 66, 39]);
        string svg = Svg(startGappy, endGappy);

        var groups = svg.SeriesGroups();
        Assert.Empty(Dots(groups[2]));
        Assert.Equal(0, Runs(groups[2]));
        Assert.Null(EndName(groups[2]));
        Assert.DoesNotContain("East", StartNames(svg).Select(n => n.Text));
        Assert.Equal(4, StartNames(svg).Length);
        svg.AssertNoNonFiniteNumber();
    }

    /// <summary>An infinity cannot be placed either: it is treated as a missing value, and drops its name.</summary>
    [Fact]
    public void AnInfinity_IsAMissingValue_AndDropsItsName()
    {
        var infinite = new BarGroup("2025", [51, double.PositiveInfinity, 44, 66, 39]);
        string svg = Svg(Start, infinite);

        var groups = svg.SeriesGroups();
        Assert.Null(EndName(groups[1]));
        Assert.Single(Dots(groups[1]));
        svg.AssertNoNonFiniteNumber();
    }

    // ---- the callback -----------------------------------------------------------------------------------------------

    /// <summary>What the callback sets wins: it can put a name back in the legend instead of at the end of the line.</summary>
    [Fact]
    public void TheCallback_CanPutTheNamesBackInTheLegend()
    {
        var figure = Plt.Create().WithSize(700, 500).AddSubPlot(1, 1, 1, ax => ax
            .SlopeGraph(Regions, Start, End, line => line.LabelPlacement = SeriesLabelPlacement.Legend).WithLegend()).Build();
        figure.EnableLegendToggle = true;

        string svg = figure.ToSvg();

        Assert.Equal(Regions.Length, svg.CountOf("data-legend-index="));
        Assert.All(svg.SeriesGroups(), g => Assert.Null(EndName(g)));
    }

    // ---- the verb draws what the hand chain drew -----------------------------------------------------------------------

    /// <summary>Written to JSON and read back, the chart draws the same picture, a missing start and a missing end included:
    /// the names still stand at both ends and the moments are still the ticks, under the default theme and a dark one.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheChart_ReadBackFromJson_DrawsTheSamePicture(bool dark)
    {
        var gappy = new BarGroup("2015", [42, 55, 38, 61, double.NaN]);
        var open = new BarGroup("2025", [51, 49, double.NaN, 66, 39]);
        var builder = Plt.Create().WithSize(700, 500);
        if (dark) builder = builder.WithTheme(Theme.Dark);
        var figure = builder.AddSubPlot(1, 1, 1, ax => ax.SlopeGraph(Regions, gappy, open)).Build();
        string drawn = figure.ToSvg();

        string readBack = figure.RoundTripped().ToSvg();

        Assert.Equal(drawn, readBack);
        Assert.Equal(4, StartNames(readBack).Length);
        Assert.Equal(["2015", "2025"], XTickLabels(readBack).Select(l => l.Text));
    }

    // ---- no data to speak of ----------------------------------------------------------------------------------------

    /// <summary>No categories at all is an empty chart, as every other verb draws with no data: it does not throw, adds
    /// nothing and holds no NaN.</summary>
    [Fact]
    public void NoCategories_DrawAnEmptyChart_WithoutThrowing()
    {
        var axes = Plt.Create().OneAxes(ax => ax.SlopeGraph([], new BarGroup("2015", []), new BarGroup("2025", [])));
        string svg = Plt.Create().WithSize(700, 500)
            .AddSubPlot(1, 1, 1, ax => ax.SlopeGraph([], new BarGroup("2015", []), new BarGroup("2025", []))).ToSvg();

        Assert.Empty(axes.Series);
        Assert.Empty(axes.Annotations);
        svg.AssertNoNonFiniteNumber();
    }

    // ---- refused arguments -----------------------------------------------------------------------------------------

    /// <summary>An axes or a list of categories that is not there is refused by name.</summary>
    [Fact]
    public void ANullArgument_IsRefusedByName()
    {
        Assert.Equal("axes", Assert.Throws<ArgumentNullException>(() => ((AxesBuilder)null!).SlopeGraph(Regions, Start, End)).ParamName);
        Assert.Equal("categories", Assert.Throws<ArgumentNullException>(() => Plt.Create().OneAxes(ax => ax.SlopeGraph(null!, Start, End))).ParamName);
    }

    /// <summary>A moment that does not carry one value per category is refused, naming the moment, both counts and the
    /// parameter, in the words every verb that takes groups uses; a moment with no values at all is refused by name, not
    /// with a null reference.</summary>
    [Fact]
    public void AMomentThatDoesNotFitTheCategories_IsRefusedByName()
    {
        LengthRefusals.AssertAGroupThatDoesNotFitIsRefusedByName((from, to) => Slope(from, to), Start, End, 5);
    }

    // ---- the example in the documentation --------------------------------------------------------------------------

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        //  The example of the verb's documentation, as written there, compiles and draws.
        new DrawnToken("TheDocumentationExample_Draws", () =>
        {
            string[] regions = ["North", "South", "East", "West"];
            return Plt.Create()
                .WithTitle("Sales by region")
                .AddSubPlot(1, 1, 1, ax => ax
                    .SlopeGraph(regions,
                        new BarGroup("2015", [42.0, 55.0, 38.0, 61.0]),
                        new BarGroup("2025", [51.0, 49.0, 44.0, 66.0]))
                    .SetYLabel("Sales"))
                .ToSvg();
        }, ">North<", ">2025<", ">Sales<"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
