// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Tests.Models.Series;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>The figure JSON carries what an axes draws besides its series: the ticks each axis places and how it writes
/// them, the axis settings, the title's style, the legend, the colour bar, the grid, the spines and every overlay. A part
/// the JSON drops is a picture that changes the moment a figure is sent over the wire, handed to the MCP server or saved
/// and opened again: the rank axis of a bump chart that counts in halves, the calendar's month names that turn into
/// column numbers, the Pareto legend that jumps from the right to the best corner, the control limits that turn grey.
/// <para>One row per part and property, reflected off the types (<see cref="DrawnAxesFixtures"/>): set to a value that
/// is not its default, the property comes back with that value; left alone, it comes back at its default, which is what
/// a JSON written before the field existed reads as.</para>
/// <para>Tagged into the <c>ChartSerializerGlobalState</c> collection: the normaliser rows below read series of a score of
/// types back through the process-global series registry, which <see cref="SeriesRegistryTests"/> briefly rewrites.</para></summary>
[Collection("ChartSerializerGlobalState")]
public class AxesDrawnPropertiesRoundTripTests
{
    private static readonly ChartSerializer Serializer = new();

    private static Figure RoundTrip(Figure figure) => Serializer.FromJson(Serializer.ToJson(figure));

    [Theory]
    [MemberData(nameof(DrawnAxesFixtures.Rows), MemberType = typeof(DrawnAxesFixtures))]
    public void ADrawnAxesProperty_ComesBackFromJson_WithTheValueItWasGiven(string part, string path)
    {
        var figure = DrawnAxesFixtures.Build(everyPropertySet: true);
        object? written = DrawnAxesFixtures.ValueOf(figure, part, path);

        object? read = DrawnAxesFixtures.ValueOf(RoundTrip(figure), part, path);

        Assert.True(DrawnSeriesFixtures.SameValue(written, read),
            $"{part}.{path}: set to {DrawnSeriesFixtures.Show(written)}, read back from JSON as {DrawnSeriesFixtures.Show(read)}.");
    }

    [Theory]
    [MemberData(nameof(DrawnAxesFixtures.Rows), MemberType = typeof(DrawnAxesFixtures))]
    public void ADrawnAxesPropertyLeftAlone_ComesBackFromJson_AtItsDefault(string part, string path)
    {
        var figure = DrawnAxesFixtures.Build(everyPropertySet: false);
        object? atRest = DrawnAxesFixtures.ValueOf(figure, part, path);

        object? read = DrawnAxesFixtures.ValueOf(RoundTrip(figure), part, path);

        Assert.True(DrawnSeriesFixtures.SameValue(atRest, read),
            $"{part}.{path}: at rest {DrawnSeriesFixtures.Show(atRest)}, read back from JSON as {DrawnSeriesFixtures.Show(read)}.");
    }

    /// <summary>A part with one setting changed and every other at its default keeps exactly that: the one setting
    /// comes back, and none of the others moves off its default. (The contract above changes every setting at once, so
    /// it never sees a part that is only partly changed.)</summary>
    [Theory]
    [InlineData("XAxis", "MajorTicks.Length")]
    [InlineData("XAxis", "MinorTicks.Visible")]
    [InlineData("XAxis", "LabelStyle.FontSize")]
    [InlineData("Annotation", "Font.Size")]
    [InlineData("Annotation", "Font.Weight")]
    [InlineData("Title", "TitleStyle.Pad")]
    [InlineData("Legend", "NCols")]
    [InlineData("ColorBar", "Label")]
    [InlineData("ZoomIndicator", "Alpha")]
    [InlineData("ZoomIndicator", "Connectors")]
    [InlineData("Grid", "Axis")]
    [InlineData("Figure", "Spacing.MarginLeft")]
    [InlineData("Figure", "InteractionTheme.TooltipOffsetY")]
    [InlineData("Figure", "EnableHighlight")]
    [InlineData("Theme", "DefaultFont.Size")]
    [InlineData("Theme", "DefaultGrid.Visible")]
    [InlineData("Theme", "Alarm.Warning")]
    [InlineData("Theme", "PropCycler")]
    public void APartWithOneSettingChanged_KeepsThatOneAndEveryOtherAtItsDefault(string part, string changed)
    {
        var figure = DrawnAxesFixtures.Build(everyPropertySet: false);
        DrawnAxesFixtures.Set(figure, part, changed, k: 3);

        var read = RoundTrip(figure);

        foreach (var path in DrawnAxesFixtures.DrawnPaths(DrawnAxesFixtures.Part(part)))
        {
            object? written = DrawnAxesFixtures.ValueOf(figure, part, path), readBack = DrawnAxesFixtures.ValueOf(read, part, path);
            Assert.True(DrawnSeriesFixtures.SameValue(written, readBack),
                $"{part}.{path} (with only {changed} changed): {DrawnSeriesFixtures.Show(written)}, read back as {DrawnSeriesFixtures.Show(readBack)}.");
        }
    }

    /// <summary>An auto date formatter on an axis that has no auto date locator (JSON written by hand, say) gets a
    /// locator of its own, so it writes dates rather than failing.</summary>
    [Fact]
    public void AnAutoDateFormatterOnAnAxisWithoutItsLocator_ReadsBackWithALocatorOfItsOwn()
    {
        const string json = """{"width":400,"height":300,"subPlots":[{"xAxis":{"tickFormatter":{"type":"autoDate"}}}]}""";

        var formatter = Serializer.FromJson(json).SubPlots[0].XAxis.TickFormatter;

        Assert.IsType<AutoDateFormatter>(formatter);
        Assert.Equal("2023", formatter.Format(new DateTime(2023, 3, 1).ToOADate()));
    }

    /// <summary>The fixture refuses a part or a property it does not have, so a renamed property cannot pass for a
    /// carried one.</summary>
    [Fact]
    public void AnUnknownPartOrProperty_IsRefusedByTheFixture()
    {
        var figure = DrawnAxesFixtures.Build(everyPropertySet: false);

        Assert.Throws<ArgumentException>(() => DrawnAxesFixtures.Part("NoSuchPart"));
        Assert.Throws<ArgumentException>(() => DrawnAxesFixtures.ValueOf(figure, "Legend", "NoSuchProperty"));
    }

    // ---- the tick locators, formatters and normalisers the library ships -----------------------------------------

    private static readonly IReadOnlyDictionary<string, Func<ITickLocator>> BuiltInLocators = new Dictionary<string, Func<ITickLocator>>(StringComparer.Ordinal)
    {
        [nameof(AutoLocator)] = () => new AutoLocator(7),
        [nameof(MaxNLocator)] = () => new MaxNLocator(4),
        [nameof(MultipleLocator)] = () => new MultipleLocator(2.5, 0.5),
        [nameof(FixedLocator)] = () => new FixedLocator([1, 2.5, 7]),
        [nameof(LogLocator)] = () => new LogLocator(),
        [nameof(SymlogLocator)] = () => new SymlogLocator(2),
        [nameof(AutoDateLocator)] = () => new AutoDateLocator(),
    };

    /// <summary>Each built-in formatter, given the locator of the axis it sits on (the auto date formatter reads its
    /// locator's chosen interval).</summary>
    private static readonly IReadOnlyDictionary<string, Func<AutoDateLocator, ITickFormatter>> BuiltInFormatters = new Dictionary<string, Func<AutoDateLocator, ITickFormatter>>(StringComparer.Ordinal)
    {
        [nameof(NumericTickFormatter)] = _ => new NumericTickFormatter(),
        [nameof(LogTickFormatter)] = _ => new LogTickFormatter(),
        [nameof(BarCenterFormatter)] = _ => new BarCenterFormatter(),
        [nameof(CategoryFormatter)] = _ => new CategoryFormatter(["Mon", "Tue", "Wed"], reversed: true),
        // Names at positions of their own: two of the positions are values the formatter is asked to write, so a reading
        // that fell back to the slot rule would name 0, 1 and 3 instead.
        [$"{nameof(CategoryFormatter)} at positions"] = _ => new CategoryFormatter(["Low", "Mid", "High"], [0.5, 42.25, 45000.5]),
        [nameof(DateTickFormatter)] = _ => new DateTickFormatter { DateFormat = "dd/MM/yy" },
        [$"{nameof(DateTimeTickFormatter)} from an array"] = _ => DateTimeTickFormatter.FromArray([new DateTime(2025, 1, 1), new DateTime(2025, 6, 30, 13, 5, 0)], "yyyy MMM HH:mm"),
        [$"{nameof(DateTimeTickFormatter)} from epoch milliseconds"] = _ => DateTimeTickFormatter.FromEpochMs("HH:mm:ss"),
        [nameof(EngFormatter)] = _ => new EngFormatter { Sep = "" },
        [nameof(PercentFormatter)] = _ => new PercentFormatter(50),
        [nameof(AutoDateFormatter)] = locator => new AutoDateFormatter(locator),
    };

    private static readonly IReadOnlyDictionary<string, Func<INormalizer>> BuiltInNormalizers = new Dictionary<string, Func<INormalizer>>(StringComparer.Ordinal)
    {
        [nameof(LinearNormalizer)] = () => new LinearNormalizer(),
        [nameof(LogNormalizer)] = () => new LogNormalizer(),
        [nameof(TwoSlopeNormalizer)] = () => new TwoSlopeNormalizer(3),
        [nameof(BoundaryNormalizer)] = () => new BoundaryNormalizer([0, 1, 5, 10]),
        [nameof(SymLogNormalizer)] = () => new SymLogNormalizer(0.5, 2, 0.75),
        [nameof(PowerNormNormalizer)] = () => new PowerNormNormalizer(0.4),
        [$"{nameof(CenteredNormNormalizer)} with a half range"] = () => new CenteredNormNormalizer(2, 4),
        [$"{nameof(CenteredNormNormalizer)} without a half range"] = () => new CenteredNormNormalizer(-1),
        [nameof(NoNormNormalizer)] = () => new NoNormNormalizer(),
    };

    /// <summary>The names of the built-in locators.</summary>
    public static TheoryData<string> LocatorKinds { get; } = new(BuiltInLocators.Keys);

    /// <summary>The names of the built-in formatters.</summary>
    public static TheoryData<string> FormatterKinds { get; } = new(BuiltInFormatters.Keys);

    private static IEnumerable<string> PublicImplementationsOf<T>() =>
        typeof(T).Assembly.GetTypes()
            .Where(t => t.IsPublic && !t.IsAbstract && !t.IsInterface && typeof(T).IsAssignableFrom(t))
            .Select(t => t.Name)
            .Order(StringComparer.Ordinal);

    /// <summary>Every public locator, formatter and normaliser of the library is in the lists above, so one added
    /// tomorrow turns this red until the JSON carries it.</summary>
    [Fact]
    public void EveryLocatorFormatterAndNormaliserTheLibraryShips_IsCoveredByTheBuiltInTheories()
    {
        Assert.Equal(PublicImplementationsOf<ITickLocator>(), BuiltInLocators.Values.Select(f => f().GetType().Name).Distinct().Order(StringComparer.Ordinal));
        Assert.Equal(PublicImplementationsOf<ITickFormatter>(), BuiltInFormatters.Values.Select(f => f(new AutoDateLocator()).GetType().Name).Distinct().Order(StringComparer.Ordinal));
        Assert.Equal(PublicImplementationsOf<INormalizer>(), BuiltInNormalizers.Values.Select(f => f().GetType().Name).Distinct().Order(StringComparer.Ordinal));
    }

    [Theory]
    [MemberData(nameof(LocatorKinds))]
    public void ABuiltInTickLocator_ComesBackFromJson_PlacingTheSameTicks(string kind)
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 2, 3])).Build();
        figure.SubPlots[0].YAxis.TickLocator = BuiltInLocators[kind]();

        var read = RoundTrip(figure).SubPlots[0].YAxis.TickLocator;

        Assert.True(DrawnSeriesFixtures.SameValue(figure.SubPlots[0].YAxis.TickLocator, read),
            $"{kind}: placed {DrawnSeriesFixtures.Show(figure.SubPlots[0].YAxis.TickLocator)}, read back as {DrawnSeriesFixtures.Show(read)}.");
    }

    [Theory]
    [MemberData(nameof(FormatterKinds))]
    public void ABuiltInTickFormatter_ComesBackFromJson_WritingTheSameLabels(string kind)
    {
        var locator = new AutoDateLocator();
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 2, 3])).Build();
        var axis = figure.SubPlots[0].XAxis;
        axis.TickLocator = locator;
        axis.TickFormatter = BuiltInFormatters[kind](locator);

        var readAxis = RoundTrip(figure).SubPlots[0].XAxis;

        // The auto date formatter writes what its locator last chose: both axes choose over the same eleven days.
        locator.Locate(45000, 45011);
        readAxis.TickLocator!.Locate(45000, 45011);
        Assert.True(DrawnSeriesFixtures.SameValue(axis.TickFormatter, readAxis.TickFormatter),
            $"{kind}: wrote {DrawnSeriesFixtures.Show(axis.TickFormatter)}, read back as {DrawnSeriesFixtures.Show(readAxis.TickFormatter)}.");
    }

    /// <summary>Every series of the library that takes a normaliser, as a fresh instance each call: the instances of the
    /// one list every series test shares (<see cref="AllSeriesTests.AllSeriesInstances"/>), less the series that take none.</summary>
    private static IEnumerable<ISeries> NormalizableSeries() =>
        AllSeriesTests.AllSeriesInstances.Select(row => (ISeries)((ITheoryDataRow)row).GetData()[0]!).Where(series => series is INormalizable);

    /// <summary>The type names of the series that take a normaliser.</summary>
    public static TheoryData<string> NormalizableSeriesNames { get; } = new(NormalizableSeries().Select(series => series.GetType().Name));

    /// <summary>Every built-in normaliser kind on every series that takes one.</summary>
    public static TheoryData<string, string> NormalizerKindsOnEverySeries { get; } = KindsOnEverySeries();

    private static TheoryData<string, string> KindsOnEverySeries()
    {
        var rows = new TheoryData<string, string>();
        foreach (var series in NormalizableSeries())
        {
            foreach (var kind in BuiltInNormalizers.Keys)
            {
                rows.Add(series.GetType().Name, kind);
            }
        }

        return rows;
    }

    /// <summary>A figure that holds one series of the type named <paramref name="seriesName"/>, mapping its values with
    /// <paramref name="normalizer"/>.</summary>
    private static Figure FigureWithNormalizer(string seriesName, INormalizer? normalizer)
    {
        var series = NormalizableSeries().First(candidate => candidate.GetType().Name == seriesName);
        ((INormalizable)series).Normalizer = normalizer;
        return Plt.Create().AddSubPlot(1, 1, 1, ax => ax.AddSeries(series)).Build();
    }

    private static INormalizer? NormalizerOfTheOnlySeries(Figure figure) =>
        Assert.IsAssignableFrom<INormalizable>(figure.SubPlots[0].Series.Single()).Normalizer;

    /// <summary>Every series of the library that maps its values onto a colour map keeps the normaliser it was given.
    /// One list of series and one list of kinds, so a series or a normaliser added tomorrow joins the rows on its own.</summary>
    [Theory]
    [MemberData(nameof(NormalizerKindsOnEverySeries))]
    public void ABuiltInNormaliser_ComesBackFromJson_MappingTheSameValues_OnEverySeriesThatTakesOne(string series, string kind)
    {
        var figure = FigureWithNormalizer(series, BuiltInNormalizers[kind]());

        var read = NormalizerOfTheOnlySeries(RoundTrip(figure));

        Assert.True(DrawnSeriesFixtures.SameValue(NormalizerOfTheOnlySeries(figure), read),
            $"{series}, {kind}: mapped {DrawnSeriesFixtures.Show(NormalizerOfTheOnlySeries(figure))}, read back as {DrawnSeriesFixtures.Show(read)}.");
    }

    /// <summary>The series the rows above cover are every public series type that implements
    /// <see cref="INormalizable"/>, so a new one turns this red until a shared instance of it is listed.</summary>
    [Fact]
    public void EverySeriesTheLibraryShipsThatTakesANormaliser_IsCoveredByTheNormaliserTheory()
    {
        Assert.Equal(PublicImplementationsOf<INormalizable>(), NormalizableSeries().Select(series => series.GetType().Name).Order(StringComparer.Ordinal));
    }

    /// <summary>The other arm: a series left on its default mapping writes no normaliser, so a figure that never sets one
    /// writes the bytes it wrote before the normaliser reached the JSON.</summary>
    [Theory]
    [MemberData(nameof(NormalizableSeriesNames))]
    public void ASeriesWithoutANormaliser_WritesNoNormaliserField(string series)
    {
        string json = Serializer.ToJson(FigureWithNormalizer(series, normalizer: null));

        Assert.DoesNotContain("\"normalizer\"", json);
    }

    /// <summary>A normaliser written outside the library is code the JSON cannot carry: it is left out, and the series
    /// reads back on its default mapping.</summary>
    [Theory]
    [MemberData(nameof(NormalizableSeriesNames))]
    public void AUserWrittenNormaliser_IsLeftOutOfTheJson_AndTheSeriesReadsBackOnItsDefault(string series)
    {
        var figure = FigureWithNormalizer(series, new HalvingNormalizer());

        string json = Serializer.ToJson(figure);

        Assert.DoesNotContain("\"normalizer\"", json);
        Assert.Null(NormalizerOfTheOnlySeries(Serializer.FromJson(json)));
    }

    private sealed class HalvingNormalizer : INormalizer
    {
        public double Normalize(double value, double min, double max) => 0.5 * (value - min) / (max - min);
    }

    /// <summary>A date axis pairs its formatter with its locator: read back from JSON the two are paired again, so the
    /// labels follow the interval the locator chooses for the drawn range.</summary>
    [Fact]
    public void ADateAxis_ReadBackFromJson_DrawsTheSameDateLabels()
    {
        DateTime[] days = [.. Enumerable.Range(0, 40).Select(d => new DateTime(2025, 3, 1).AddDays(d))];
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(days, [.. days.Select((_, i) => (double)(i % 7))])).Build();

        string drawn = figure.ToSvg();
        string readBack = RoundTrip(figure).ToSvg();

        Assert.Matches(">Mar [0-9]{2}<", drawn);
        Assert.Equal(drawn, readBack);
    }

    /// <summary>A description written by hand (an agent's spec, an older or a newer writer) may leave its arguments out:
    /// it then reads as the kind made with its own defaults, never as a crash.</summary>
    [Theory]
    [InlineData("""{"xAxis":{"tickLocator":{"type":"fixed"}}}""", "fixed locator")]
    [InlineData("""{"xAxis":{"tickFormatter":{"type":"category"}}}""", "category formatter")]
    [InlineData("""{"xAxis":{"tickFormatter":{"type":"date"}}}""", "date formatter")]
    [InlineData("""{"xAxis":{"tickFormatter":{"type":"dateTime"}}}""", "date-time formatter")]
    [InlineData("""{"xAxis":{"tickFormatter":{"type":"eng"}}}""", "engineering formatter")]
    [InlineData("""{"series":[{"type":"heatmap","heatmapData":[[1,2],[3,4]],"normalizer":{"type":"boundary"}}]}""", "boundary normaliser")]
    public void ADescriptionWithItsArgumentsLeftOut_ReadsAsTheKindWithItsOwnDefaults(string subPlot, string kind)
    {
        var axes = Serializer.FromJson($$"""{"width":400,"height":300,"subPlots":[{{subPlot}}]}""").SubPlots[0];

        object? read = kind switch
        {
            "fixed locator" => axes.XAxis.TickLocator,
            "boundary normaliser" => ((HeatmapSeries)axes.Series[0]).Normalizer,
            _ => axes.XAxis.TickFormatter,
        };
        object expected = kind switch
        {
            "fixed locator" => new FixedLocator([]),
            "category formatter" => new CategoryFormatter([]),
            "date formatter" => new DateTickFormatter(),
            "date-time formatter" => DateTimeTickFormatter.FromEpochMs(),
            "engineering formatter" => new EngFormatter(),
            _ => new BoundaryNormalizer([]),
        };

        Assert.True(DrawnSeriesFixtures.SameValue(expected, read), $"{kind}: read as {DrawnSeriesFixtures.Show(read)}, expected {DrawnSeriesFixtures.Show(expected)}.");
    }

    /// <summary>A category formatter on the slot rule writes no positions, so the JSON of every figure named that way is the
    /// JSON it was before positions existed.</summary>
    [Fact]
    public void ACategoryFormatterOnTheSlotRule_WritesNoPositions()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2], [1.0, 2])
            .SetXTickFormatter(new CategoryFormatter(["a", "b"], reversed: true))).Build();

        string json = Serializer.ToJson(figure);

        Assert.Contains("\"category\"", json);
        Assert.DoesNotContain("positions", json);
    }

    /// <summary>Positions that do not pair up with the names (JSON written by hand), positions written without any names
    /// among them, are refused with the parameter named, as the formatter refuses them, rather than naming the wrong
    /// ticks.</summary>
    [Theory]
    [InlineData("""{"type":"category","labels":["a","b"],"positions":[1]}""")]
    [InlineData("""{"type":"category","positions":[1]}""")]
    public void ACategoryFormatterWhosePositionsDoNotPairUp_IsRefused(string formatter)
    {
        string json = """{"width":400,"height":300,"subPlots":[{"xAxis":{"tickFormatter":""" + formatter + "}}]}";

        var refusal = Assert.Throws<ArgumentException>(() => Serializer.FromJson(json));

        Assert.Equal("positions", refusal.ParamName);
    }

    /// <summary>A title style that overrides nothing is left out of the JSON, as one never set is.</summary>
    [Fact]
    public void ATitleStyleThatOverridesNothing_IsLeftOut()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2], [1.0, 2]).WithTitle("t", style => style)).Build();

        string json = Serializer.ToJson(figure);

        Assert.NotNull(figure.SubPlots[0].TitleStyle);
        Assert.DoesNotContain("titleStyle", json);
    }

    // ---- a type written outside the library ----------------------------------------------------------------------

    /// <summary>A locator or formatter written outside the library is code the JSON cannot carry: it is left out, and
    /// the axis reads back on its own ticks and labels.</summary>
    [Fact]
    public void AUserWrittenTickLocatorAndFormatter_AreLeftOutOfTheJson_AndTheAxisReadsBackOnItsOwnTicks()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot([1.0, 2, 3], [1.0, 2, 3])
            .SetXTickLocator(new EveryThirdLocator())
            .SetXTickFormatter(new ShoutingFormatter())).Build();

        string json = Serializer.ToJson(figure);
        var read = Serializer.FromJson(json).SubPlots[0].XAxis;

        Assert.DoesNotContain("tickLocator", json);
        Assert.DoesNotContain("tickFormatter", json);
        Assert.Null(read.TickLocator);
        Assert.Null(read.TickFormatter);
    }

    private sealed class EveryThirdLocator : ITickLocator
    {
        public double[] Locate(double min, double max) => [min, (min + max) / 2, max];
    }

    private sealed class ShoutingFormatter : ITickFormatter
    {
        public string Format(double value) => $"{value}!";
    }

    // ---- the drawn picture of a part, end to end ------------------------------------------------------------------

    /// <summary>The calendar's axes: month names on the week columns, weekday names on the rows, a colour bar. Read back
    /// from JSON it draws the same picture (the calendar itself also needs a missing day to be written, which is the
    /// missing-value step's).</summary>
    [Fact]
    public void AHeatmapWithNamedTicksAndAColourBar_ReadBackFromJson_DrawsTheSameSvg()
    {
        var figure = Plt.Create().WithSize(900, 300).AddSubPlot(1, 1, 1, ax => ax
            .Heatmap(new double[,] { { 1, 2, 3, 4 }, { 5, 6, 7, 8 }, { 9, 1, 2, 3 } }, s => s.ColorMap = ColorMaps.Viridis)
            .SetXTickLocator(new FixedLocator([0, 2]))
            .SetXTickFormatter(new CategoryFormatter(["Jan", "", "Feb", ""]))
            .SetYTickLocator(new FixedLocator([0, 1, 2]))
            .SetYTickFormatter(new CategoryFormatter(["Mon", "Tue", "Wed"], reversed: true))
            .WithColorBar(cb => cb with { Label = "count", Extend = ColorBarExtend.Max })).Build();

        string drawn = figure.ToSvg();
        string readBack = RoundTrip(figure).ToSvg();

        Assert.Contains(">Feb<", drawn);
        Assert.Contains(">count<", drawn);
        Assert.Equal(drawn, readBack);
    }

    /// <summary>A grid the axes changed over the theme's grid (<c>WithGrid</c>) is the theme's grid with that change: read
    /// back from JSON it is again the theme's grid with the same change, not a plain grid with it.</summary>
    [Fact]
    public void AGridChangedOverTheThemesGrid_ReadBackFromJson_DrawsTheSameGrid()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot([1.0, 2, 3], [1.0, 3, 2])
            .WithGrid(g => g with { Visible = true, Axis = GridAxis.Y, LineStyle = LineStyle.Dashed })).Build();

        string drawn = figure.ToSvg();
        string readBack = RoundTrip(figure).ToSvg();

        Assert.Equal(drawn, readBack);
    }

    /// <summary>A grid the axes switched on (<c>ShowGrid</c>) is that grid as it stands, not the theme's: read back from
    /// JSON it stays that grid.</summary>
    [Fact]
    public void AGridSwitchedOnAsItStands_ReadBackFromJson_DrawsTheSameGrid()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 3, 2]).ShowGrid()).Build();

        string drawn = figure.ToSvg();
        string readBack = RoundTrip(figure).ToSvg();

        Assert.Equal(drawn, readBack);
    }

    /// <summary>A spine that differs from the plain spine only in its line style is written, and a JSON written before
    /// spines had a line style reads back as solid.</summary>
    [Fact]
    public void ASpineThatDiffersOnlyInItsLineStyle_KeepsItThroughJson_AndAnOlderJsonReadsSolid()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot([1.0, 2], [1.0, 2])
            .WithSpines(s => s with { Left = s.Left with { LineStyle = LineStyle.Dotted } })).Build();
        const string older = """{"width":400,"height":300,"subPlots":[{"spines":{"left":{"visible":true,"lineWidth":2}},"series":[{"type":"line","xData":[1,2],"yData":[1,2]}]}]}""";

        var read = RoundTrip(figure).SubPlots[0].Spines.Left;
        var olderRead = Serializer.FromJson(older).SubPlots[0].Spines.Left;

        Assert.Equal(LineStyle.Dotted, read.LineStyle);
        Assert.Equal(LineStyle.Solid, olderRead.LineStyle);
        Assert.Equal(2, olderRead.LineWidth);
    }

    /// <summary>An inset is an axes: read back from JSON it keeps what its writer wrote, the same as a subplot does, its
    /// overlays and its legend included.</summary>
    [Fact]
    public void AnInset_ReadBackFromJson_KeepsItsOverlaysAndLegend()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 2, 3])).Build();
        var inset = figure.SubPlots[0].AddInset(0.55, 0.55, 0.4, 0.4);
        inset.Plot([1.0, 2], [2.0, 1]).Label = "inset line";
        inset.Legend = inset.Legend with { Position = LegendPosition.LowerLeft };
        inset.AxHLine(1.5).Color = Color.FromHex("#D62728");
        inset.Annotate("peak", 1, 2).Color = Color.FromHex("#2CA02C");

        var read = RoundTrip(figure).SubPlots[0].Insets.Single();

        Assert.Equal(LegendPosition.LowerLeft, read.Legend.Position);
        Assert.Equal(Color.FromHex("#D62728"), read.ReferenceLines.Single().Color);
        Assert.Equal(Color.FromHex("#2CA02C"), read.Annotations.Single().Color);
        Assert.Equal(figure.ToSvg(), RoundTrip(figure).ToSvg());
    }

    /// <summary>A figure that sets none of the new parts writes the bytes it wrote before they reached the JSON. (The
    /// fixture holds Y axes past the old caps on purpose, so its overflow fields are written; a figure within the caps
    /// writing none is <see cref="YAxisContainerWireCompatTests.AFigureWithinTheOldCaps_WritesNoOverflowField"/>.)</summary>
    [Fact]
    public void AFigureThatSetsNoAxesPart_WritesNoneOfTheirFields()
    {
        string json = Serializer.ToJson(DrawnAxesFixtures.Build(everyPropertySet: false));

        foreach (var field in new[] { "tickLocator", "tickFormatter", "legend", "colorBar", "figureColorBar", "titleStyle", "titleLoc", "majorTicks", "minorTicks", "labelStyle", "inverted", "margin", "symLogLinThresh", "themed", "yAxisTitleGap" })
        {
            Assert.False(Regex.IsMatch(json, $"\"{field}\":"), $"'{field}' was written for a figure that never set it: {json}");
        }
    }
}
