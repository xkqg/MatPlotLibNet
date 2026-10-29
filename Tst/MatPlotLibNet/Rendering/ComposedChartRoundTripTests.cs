// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Tests.Serialization;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A composed chart is several series that together draw one thing, so it is only as good as the weakest
/// thing a figure keeps. Every shape of <see cref="ComposedChartShapes"/> is held to the same five promises, one
/// Theory per promise so that each defect class turns red on its own:
/// <list type="number">
/// <item>the figure can be written as JSON at all;</item>
/// <item>read back from that JSON it draws the same picture (compared in-process, never against stored bytes);</item>
/// <item>one drawn unit wears one colour, and the legend lists exactly the units;</item>
/// <item>a missing value (NaN) is neither written into the JSON nor into the picture's geometry;</item>
/// <item>every part it is built from is held to the reflected drawn-property contracts, so a property the shape does
/// not happen to set is guarded too.</item>
/// </list>
/// The Mcp test project runs the same shapes through its spec reader.</summary>
public class ComposedChartRoundTripTests
{
    private static readonly Regex SeriesGroup = new("""<g class="series" data-series-index="([0-9]+)"[^>]*>""", RegexOptions.Compiled);
    private static readonly Regex FirstColour = new("""(?:fill|stroke)="(#[0-9A-Fa-f]{6})" """.TrimEnd(), RegexOptions.Compiled);
    private static readonly Regex LegendEntry = new("data-legend-index=", RegexOptions.Compiled);

    /// <summary>The colour each series group of an interactive SVG is drawn in: the first fill or stroke inside the group.</summary>
    private static List<string> SeriesColours(string interactiveSvg)
    {
        var starts = SeriesGroup.Matches(interactiveSvg).ToList();
        int legendAt = interactiveSvg.IndexOf("""<g class="legend" """.TrimEnd(), StringComparison.Ordinal);
        var colours = new List<string>();
        for (int k = 0; k < starts.Count; k++)
        {
            int from = starts[k].Index;
            int to = k + 1 < starts.Count ? starts[k + 1].Index : legendAt > from ? legendAt : interactiveSvg.Length;
            var match = FirstColour.Match(interactiveSvg[from..to]);
            colours.Add(match.Success ? match.Groups[1].Value.ToUpperInvariant() : "(none)");
        }
        return colours;
    }

    private static string InteractiveSvg(ComposedShape shape)
    {
        var figure = shape.Build(false).Build();
        figure.EnableLegendToggle = true;
        return figure.ToSvg();
    }

    private static string WriteJson(Figure figure, string name)
    {
        try { return figure.ToJson(); }
        catch (Exception ex) { Assert.Fail($"{name}: ToJson threw {ex.GetType().Name}: {ex.Message}"); throw; }
    }

    // ---- promise 1: the figure can be written -------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(ComposedChartShapes.Names), MemberType = typeof(ComposedChartShapes))]
    public void ToJson_DoesNotThrow(string name)
    {
        var figure = ComposedChartShapes.Named(name).Build(false).Build();

        var thrown = Record.Exception(() => figure.ToJson());

        Assert.True(thrown is null, $"{name}: ToJson threw {thrown?.GetType().Name}: {thrown?.Message}");
    }

    // ---- promise 2: read back, it draws the same picture --------------------------------------------------------

    [Theory]
    [MemberData(nameof(ComposedChartShapes.Names), MemberType = typeof(ComposedChartShapes))]
    public void ReadBackFromJson_DrawsTheSameSvg(string name)
    {
        var figure = ComposedChartShapes.Named(name).Build(false).Build();
        string drawn = figure.ToSvg();

        string json = WriteJson(figure, name);
        string readBack = ChartSerializerDefault.Current.FromJson(json).ToSvg();

        Assert.True(drawn == readBack, $"{name}: the picture changed in the round trip ({drawn.Length} B drawn, {readBack.Length} B read back, first difference at char {FirstDifference(drawn, readBack)}).");
    }

    private static int FirstDifference(string a, string b)
    {
        int i = 0;
        while (i < a.Length && i < b.Length && a[i] == b[i]) i++;
        return i;
    }

    // ---- promise 3: one drawn unit is one colour, the legend lists the units ------------------------------------

    [Theory]
    [MemberData(nameof(ComposedChartShapes.ColouredNames), MemberType = typeof(ComposedChartShapes))]
    public void EveryDrawnUnit_WearsItsOwnSingleColour(string name)
    {
        var shape = ComposedChartShapes.Named(name);

        var colours = SeriesColours(InteractiveSvg(shape));

        Assert.True(colours.Distinct().Count() == shape.Colours,
            $"{name}: expected {shape.Colours} distinct series colours, found {colours.Distinct().Count()} over {colours.Count} series groups [{string.Join(" ", colours)}].");
    }

    [Theory]
    [MemberData(nameof(ComposedChartShapes.Names), MemberType = typeof(ComposedChartShapes))]
    public void TheLegend_ListsExactlyTheDrawnUnitsThatAreNamed(string name)
    {
        var shape = ComposedChartShapes.Named(name);

        int entries = LegendEntry.Matches(InteractiveSvg(shape)).Count;

        Assert.True(entries == shape.LegendEntries, $"{name}: expected {shape.LegendEntries} legend entries, found {entries}.");
    }

    // ---- promise 4: a missing value stays out of the JSON and out of the geometry -------------------------------

    [Theory]
    [MemberData(nameof(ComposedChartShapes.Names), MemberType = typeof(ComposedChartShapes))]
    public void AMissingValue_IsWrittenToJson(string name)
    {
        var figure = ComposedChartShapes.Named(name).Build(true).Build();

        var thrown = Record.Exception(() => figure.ToJson());

        Assert.True(thrown is null, $"{name} with a missing value: ToJson threw {thrown?.GetType().Name}: {thrown?.Message}");
    }

    [Theory]
    [MemberData(nameof(ComposedChartShapes.Names), MemberType = typeof(ComposedChartShapes))]
    public void AMissingValue_NeverReachesTheSvgGeometry(string name)
    {
        var figure = ComposedChartShapes.Named(name).Build(true).Build();

        string svg;
        try { svg = figure.ToSvg(); }
        catch (Exception ex) { Assert.Fail($"{name} with a missing value: ToSvg threw {ex.GetType().Name}: {ex.Message}"); throw; }

        int tokens = Regex.Matches(svg, "NaN").Count;
        Assert.True(tokens == 0, $"{name} with a missing value: {tokens} NaN token(s) were written into the SVG.");
    }

    // ---- promise 5: every part is in a drawn-property contract --------------------------------------------------

    /// <summary>The overlay lists of an axes (annotations, reference lines, spans, trendlines, ...): every public list
    /// property whose items are neither series, axes nor Y axis slots (the slots are walked through the container, see
    /// <see cref="YAxisSlotsOf"/>). Reflected, so an overlay kind added tomorrow is walked too.</summary>
    private static readonly PropertyInfo[] OverlayLists =
        [.. typeof(Axes).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
            .Where(p => p.PropertyType.GetGenericArguments()[0] is var item
                && !typeof(ISeries).IsAssignableFrom(item) && item != typeof(Axes) && !IsYAxisSlot(item))];

    /// <summary>Whether <paramref name="item"/> is a Y axis slot: the container's <see cref="YAxisSlot"/>, or any list item
    /// that carries a Y axis' <see cref="YAxisSlot.Axis"/> the way a slot does.</summary>
    private static bool IsYAxisSlot(Type item) =>
        item == typeof(YAxisSlot) || item.GetProperty(nameof(YAxisSlot.Axis))?.PropertyType == typeof(Axis);

    /// <summary>Every Y axis slot of <paramref name="axes"/>, the primary included, through the container.</summary>
    private static IEnumerable<YAxisSlot> YAxisSlotsOf(Axes axes) => axes.YAxes.IdentityOrder;

    private static IEnumerable<Axes> AxesOf(Figure figure)
    {
        static IEnumerable<Axes> WithInsets(Axes axes) => axes.Insets.SelectMany(WithInsets).Prepend(axes);
        return figure.SubPlots.SelectMany(WithInsets);
    }

    /// <summary>A shape only sets the properties its picture needs; the round trip above can only see those. The
    /// reflected contracts (<see cref="SeriesDrawnPropertiesRoundTripTests"/>, <see cref="AxesDrawnPropertiesRoundTripTests"/>)
    /// set every property of a type, so a shape built on a type they skip would leave every other property of that type
    /// unguarded: the next one a JSON drops would pass unseen.</summary>
    [Theory]
    [MemberData(nameof(ComposedChartShapes.Names), MemberType = typeof(ComposedChartShapes))]
    public void EveryPartTheShapeIsBuiltFrom_IsInADrawnPropertyContract(string name)
    {
        var axes = AxesOf(ComposedChartShapes.Named(name).Build(false).Build()).ToList();
        var seriesTypes = axes.SelectMany(a => a.AllSeries).Select(s => s.GetType());
        var overlayTypes = axes.SelectMany(a => OverlayLists.SelectMany(list => ((IEnumerable)list.GetValue(a)!).Cast<object>())
                .Concat(YAxisSlotsOf(a))).Select(o => o.GetType());

        var unguarded = seriesTypes.Except(DrawnSeriesFixtures.DrawnTypes)
            .Concat(overlayTypes.Except(DrawnAxesFixtures.Parts.Select(part => part.Type)))
            .Distinct().Select(t => t.Name).ToList();

        Assert.True(unguarded.Count == 0, $"{name} is built from {string.Join(", ", unguarded)}, which no drawn-property contract covers.");
    }
}
