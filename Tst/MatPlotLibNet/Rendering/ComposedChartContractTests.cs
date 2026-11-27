// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Serialization;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A composed chart is several series that together draw one thing, so it is only as good as the weakest
/// thing a figure keeps. Every shape of <see cref="ComposedChartShapes"/> is held to the same promises, one Theory per
/// promise so that each defect class turns red on its own:
/// <list type="number">
/// <item>the figure can be written as JSON at all;</item>
/// <item>read back from that JSON it draws the same picture (compared in-process, never against stored bytes), with and
/// without a missing value;</item>
/// <item>one drawn unit wears one colour, and the legend lists exactly the units; a verb of one colour takes one colour
/// slot, so the next series wears the next colour;</item>
/// <item>a missing value (NaN) is neither written into the JSON nor into the picture's geometry;</item>
/// <item>every part it is built from is held to the reflected drawn-property contracts, so a property the shape does
/// not happen to set is guarded too;</item>
/// <item>the verb draws exactly what the hand chain of public calls it replaces draws;</item>
/// <item>a call the verb refuses is refused by the name of the argument and leaves the axes as it was;</item>
/// <item>the verb sets no axis limit of its own unless the chart cannot be read without it, keeps the caller's, and what
/// the caller sets after it wins.</item>
/// </list>
/// What every verb owes the callback it takes is held over the same shapes by <see cref="Builders.VerbContractTests"/>.
/// The Mcp test project runs the same shapes through its spec reader.</summary>
public class ComposedChartContractTests
{
    /// <summary>The colour each series group of an interactive SVG is drawn in: the first fill or stroke inside the group.</summary>
    private static List<string> SeriesColours(string interactiveSvg)
    {
        return [.. interactiveSvg.Elements("g").Where(group => group["class"] == "series" && group["data-series-index"] is not null)
            .Select(group => interactiveSvg.SeriesGroup((int)group.Number("data-series-index")).FirstColour())
            .Select(colour => colour.Length > 0 ? colour.ToUpperInvariant() : "(none)")];
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
    [MemberData(nameof(ComposedChartShapes.NamesAndVariants), MemberType = typeof(ComposedChartShapes))]
    public void ReadBackFromJson_DrawsTheSameSvg(string name, bool missing)
    {
        var figure = ComposedChartShapes.Named(name).Build(missing).Build();
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

        int entries = InteractiveSvg(shape).CountOf("data-legend-index=");

        Assert.True(entries == shape.LegendEntries, $"{name}: expected {shape.LegendEntries} legend entries, found {entries}.");
    }

    /// <summary>The verbs whose chart is lines of one colour: a line takes the next colour of the line cycle (matplotlib's markers
    /// and patches keep a cycle of their own, so a verb drawn as points or stems leaves the line cycle where it was).</summary>
    public static TheoryData<string> OneColourVerbs { get; } =
        new(ComposedChartShapes.All.Where(s => s is { WithCallback: not null, Colours: 1 } && DrawsLines(s)).Select(s => s.Name));

    private static bool DrawsLines(ComposedShape shape)
    {
        var handed = shape.WithCallback!.Parameters[1].Type.GetGenericArguments()[0];
        return handed == typeof(global::MatPlotLibNet.Models.Series.LineSeries)
            || (handed.IsValueType && handed.GetProperties().Any(p => p.PropertyType == typeof(global::MatPlotLibNet.Models.Series.LineSeries)));
    }

    [Theory]
    [MemberData(nameof(OneColourVerbs))]
    public void AVerbOfOneColour_TakesOneColourSlot_TheNextSeriesWearsTheNextColour(string name)
    {
        var verb = ComposedChartShapes.Named(name).AsVerbRow();

        var lines = Plt.Create().AddSubPlot(1, 1, 1, ax => ((AxesBuilder)verb.Invoke(ax, null)).Plot([0.0, 1.0], [0.0, 1.0])).Build()
            .Drawn().Of<DrawLinesCall>().Select(call => call.Color).ToList();

        Assert.Contains(Theme.Default.CycleColors[1], lines);
        Assert.DoesNotContain(Theme.Default.CycleColors[2], lines);
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

        svg.AssertNoNonFiniteNumber($"{name} with a missing value");
    }

    // ---- promise 5: every part is in a drawn-property contract --------------------------------------------------

    /// <summary>The overlay lists of an axes (annotations, reference lines, spans, trendlines, ...): the one reflected list the
    /// axes contract walks (<see cref="DrawnAxesFixtures.AxesLists"/>), whose items are neither series, axes nor Y axis slots
    /// (the slots are walked through the container, see <see cref="YAxisSlotsOf"/>).</summary>
    private static IReadOnlyList<PropertyInfo> OverlayLists => DrawnAxesFixtures.AxesLists;

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

    // ---- promise 6: the verb draws what its hand recipe draws --------------------------------------------------------

    /// <summary>The shapes that have a hand recipe, twice: without and with the missing value.</summary>
    public static TheoryData<string, bool> HandRecipes { get; } = Variants(ComposedChartShapes.All.Where(s => s.ByHand is not null));

    private static TheoryData<string, bool> Variants(IEnumerable<ComposedShape> shapes)
    {
        var data = new TheoryData<string, bool>();
        foreach (var shape in shapes)
        {
            data.Add(shape.Name, false);
            data.Add(shape.Name, true);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(HandRecipes))]
    public void TheVerb_DrawsExactlyWhatItsHandRecipeDraws(string name, bool missing)
    {
        var shape = ComposedChartShapes.Named(name);

        Assert.Equal(shape.ByHand!(missing).ToSvg(), shape.Build(missing).ToSvg());
    }

    // ---- promise 7: a refused call is refused by name and leaves the axes as it was --------------------------------

    /// <summary>The shapes whose verb has calls it refuses.</summary>
    public static TheoryData<string> Refusing { get; } = new(ComposedChartShapes.All.Where(s => s.RefusedCalls.Count > 0).Select(s => s.Name));

    [Theory]
    [MemberData(nameof(Refusing))]
    public void ACallTheVerbRefuses_IsRefusedByName_AndLeavesTheAxesAsItWas(string name)
    {
        var shape = ComposedChartShapes.Named(name);

        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            foreach (var refusal in shape.RefusedCalls)
            {
                var refused = Assert.ThrowsAny<ArgumentException>(() => refusal.Call(ax));
                if (refusal.Parameter is not null) Assert.Equal(refusal.Parameter, refused.ParamName);
                Assert.All(refusal.Says, words => Assert.Contains(words, refused.Message, StringComparison.Ordinal));
            }
        }).Build();
        var untouched = Plt.Create().AddSubPlot(1, 1, 1, _ => { }).Build();

        Assert.Equal(untouched.ToJson(), figure.ToJson());
        Assert.Equal(untouched.ToSvg(), figure.ToSvg());
    }

    // ---- promise 8: the limits are the caller's ----------------------------------------------------------------------

    /// <summary>The shapes that stand for a verb.</summary>
    public static TheoryData<string> Verbs { get; } = new(ComposedChartShapes.All.Where(s => s.WithCallback is not null).Select(s => s.Name));

    [Theory]
    [MemberData(nameof(Verbs))]
    public void TheVerb_SetsNoLimitOfItsOwn_AndWhatTheCallerSetsAroundItStays(string name)
    {
        var shape = ComposedChartShapes.Named(name);
        var verb = shape.AsVerbRow();
        Axes Drawn(Func<AxesBuilder, AxesBuilder> around) =>
            Plt.Create().AddSubPlot(1, 1, 1, ax => around(ax)).Build().SubPlots[0];

        var bare = Drawn(ax => (AxesBuilder)verb.Invoke(ax, null));
        var before = Drawn(ax => (AxesBuilder)verb.Invoke(ax.SetXLim(-1, 2).SetYLim(-3, 4), null));
        var after = Drawn(ax => ((AxesBuilder)verb.Invoke(ax, null)).SetXLabel("x").SetYLabel("y").SetXLim(-1, 2).SetYLim(-3, 4));

        Assert.Equal(["x", "y"], new[] { after.XAxis.Label, after.YAxis.Label });
        Assert.Equal([-1.0, 2.0, -3.0, 4.0], new[] { after.XAxis.Min, after.XAxis.Max, after.YAxis.Min, after.YAxis.Max });
        if (shape.SetsLimits) return;
        Assert.False(bare.HasExplicitLimit, $"{name} set a limit of its own.");
        Assert.Equal([null, null, null, null], new[] { bare.XAxis.Min, bare.XAxis.Max, bare.YAxis.Min, bare.YAxis.Max });
        Assert.Equal([-1.0, 2.0, -3.0, 4.0], new[] { before.XAxis.Min, before.XAxis.Max, before.YAxis.Min, before.YAxis.Max });
    }
}
