// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>A colour map travels through the figure JSON as data. A map the library ships goes by its name, which every
/// reader knows, so its JSON is the bytes it always was. A map built in code goes as what it is made of: its colours,
/// their positions when it has them, and its under, over and bad colours; a reversed map as the map it reverses. Before,
/// every map went by its name alone, so a map built in code came back from ToJson/FromJson, the MCP server and the
/// transports as the registered map of that name, or as no map at all, and a calendar lost the grey that tells a blank
/// day from a zero.
/// <para>Tagged into the <c>ChartSerializerGlobalState</c> collection: the round trips read series back through the
/// process-global series registry, which <see cref="SeriesRegistryTests"/> briefly rewrites.</para></summary>
[Collection("ChartSerializerGlobalState")]
public class ColorMapSerializationTests
{
    private static readonly ChartSerializer Serializer = new();

    private static readonly Color Red = Color.FromHex("#FF0000"), Green = Color.FromHex("#00A000"), Blue = Color.FromHex("#0000FF");

    private static Figure RoundTrip(Figure figure) => Serializer.FromJson(Serializer.ToJson(figure));

    /// <summary>A heatmap with a missing cell, a negative and values on both sides of the middle, drawn through
    /// <paramref name="map"/>.</summary>
    private static Figure HeatmapWith(IColorMap? map) =>
        Plt.Create().WithSize(400, 300).AddSubPlot(1, 1, 1, ax => ax
            .Heatmap(new double[,] { { 1, double.NaN, 3 }, { -1, 5, 9 } }, s => s.ColorMap = map)).Build();

    private static IColorMap? MapOfTheOnlySeries(Figure figure) =>
        Assert.IsAssignableFrom<IColormappable>(figure.SubPlots[0].Series.Single()).ColorMap;

    // ---- a map built in code comes back as itself ------------------------------------------------------------------

    /// <summary>Maps built in code, of every kind the library has, by a name a Theory carries.</summary>
    private static readonly IReadOnlyDictionary<string, Func<IColorMap>> BuiltInCode = new Dictionary<string, Func<IColorMap>>(StringComparer.Ordinal)
    {
        ["evenly spaced, with under, over and bad colours"] = () =>
            new LinearColorMap("ramp", [Red, Green, Blue]) { UnderColor = Color.FromHex("#111111"), OverColor = Color.FromHex("#EEEEEE"), BadColor = Color.FromHex("#DDDDDD") },
        ["stops at positions of their own"] = () =>
            LinearColorMap.FromPositions("positioned", [new(0, Red), new(0.2, Green), new(1, Blue)]),
        ["listed colours, with a bad colour"] = () =>
            new ListedColorMap("listed", [Red, Green, Blue]) { BadColor = Color.FromHex("#DDDDDD"), UnderColor = Green },
        ["colours with transparency, and a translucent bad colour"] = () =>
            new LinearColorMap("fade", [Blue.WithAlpha(0), Blue]) { BadColor = Color.FromHex("#80808040") },
        ["named like a map the library ships"] = () =>
            new LinearColorMap("viridis", [Red, Blue]),
        ["made with FromList, which registers it"] = () =>
            LinearColorMap.FromList("from list s1h", [new(0, Green), new(10, Red)]),
        ["the reverse of a map built in code"] = () =>
            new ReversedColorMap(new LinearColorMap("ramp", [Red, Green, Blue]) { UnderColor = Red, BadColor = Green }),
        ["the reverse of a listed map"] = () =>
            new ReversedColorMap(new ListedColorMap("listed", [Red, Green, Blue])),
        ["the reverse of a map built in code under the name of a map the library ships"] = () =>
            new ReversedColorMap(new LinearColorMap("plasma", [Red, Blue])),
        ["the reverse of a map made with FromList"] = () =>
            new ReversedColorMap(LinearColorMap.FromList("from list s1h reversed", [new(0, Green), new(1, Blue)])),
        ["the reverse of a map the library ships"] = () =>
            new ReversedColorMap(ColorMaps.Plasma),
        ["the reverse of a reversed map the library ships"] = () =>
            new ReversedColorMap(ColorMapRegistry.Get("plasma_r")!),
    };

    /// <summary>The names of <see cref="BuiltInCode"/>.</summary>
    public static TheoryData<string> BuiltInCodeKinds { get; } = new(BuiltInCode.Keys);

    [Theory]
    [MemberData(nameof(BuiltInCodeKinds))]
    public void AMapBuiltInCode_ComesBackFromJson_DrawingTheSameColours(string kind)
    {
        var figure = HeatmapWith(BuiltInCode[kind]());

        var read = RoundTrip(figure);

        Assert.True(DrawnSeriesFixtures.SameValue(MapOfTheOnlySeries(figure), MapOfTheOnlySeries(read)),
            $"{kind}: drawn with {DrawnSeriesFixtures.Show(MapOfTheOnlySeries(figure))}, read back as {DrawnSeriesFixtures.Show(MapOfTheOnlySeries(read))}.");
        Assert.Equal(figure.ToSvg(), read.ToSvg());
    }

    /// <summary>A map built in code is written as what it is made of, even when a map the library ships has its name:
    /// the name alone would bring back the shipped map.</summary>
    [Fact]
    public void AMapBuiltInCode_IsWrittenAsItsColours_NotByItsName()
    {
        string json = Serializer.ToJson(HeatmapWith(new LinearColorMap("viridis", [Red, Blue]) { BadColor = Color.FromHex("#DDDDDD") }));

        Assert.DoesNotContain("\"colorMapName\"", json);
        Assert.Contains("""
            "colorMap":{"type":"linear","name":"viridis","colors":["#FF0000","#0000FF"],"badColor":"#DDDDDD"}
            """.Trim(), json);
    }

    /// <summary>A map made with <see cref="LinearColorMap.FromList"/> is registered in this process only: a reader in
    /// another process (the MCP server, a transport's client) does not know its name, so it travels as data too.</summary>
    [Fact]
    public void AMapMadeWithFromList_IsWrittenAsItsColours()
    {
        string json = Serializer.ToJson(HeatmapWith(LinearColorMap.FromList("from list s1h written", [new(0, Green), new(10, Red)])));

        Assert.DoesNotContain("\"colorMapName\"", json);
        Assert.Contains("\"colorMap\":{\"type\":\"linear\",\"name\":\"from list s1h written\",\"colors\":[\"#00A000\",\"#FF0000\"],\"positions\":[0,1]}", json);
    }

    // ---- a map the library ships goes by its name, as it always did ------------------------------------------------

    /// <summary>Maps the library ships, and the reverse of one made with <see cref="ReversedColorMap"/>, which is the map
    /// the registry holds under the reversed name: each is written by that name and nothing else, so the JSON of a figure
    /// that uses one is the bytes it always was.</summary>
    [Theory]
    [InlineData("viridis")]
    [InlineData("coolwarm_r")]
    [InlineData("tab10")]
    [InlineData("a reverse made in code of plasma")]
    public void AMapTheLibraryShips_IsWrittenByItsNameAlone(string which)
    {
        IColorMap map = which == "a reverse made in code of plasma" ? new ReversedColorMap(ColorMaps.Plasma) : ColorMapRegistry.Get(which)!;

        string json = Serializer.ToJson(HeatmapWith(map));

        Assert.Contains($"\"colorMapName\":\"{map.Name}\"", json);
        Assert.DoesNotContain("\"colorMap\":", json);
        Assert.Same(ColorMapRegistry.Get(map.Name), MapOfTheOnlySeries(Serializer.FromJson(json)));
    }

    // ---- a map of a type written outside the library ---------------------------------------------------------------

    /// <summary>A colour map of a type written outside the library is code the JSON cannot carry: it is left out, and
    /// the series reads back without a map (its renderer's default).</summary>
    [Fact]
    public void AColourMapOfYourOwnType_IsLeftOutOfTheJson_AndTheSeriesReadsBackWithoutOne()
    {
        string json = Serializer.ToJson(HeatmapWith(new StripedColorMap("striped s1h")));

        Assert.DoesNotContain("\"colorMapName\"", json);
        Assert.DoesNotContain("\"colorMap\"", json);
        Assert.Null(MapOfTheOnlySeries(Serializer.FromJson(json)));
    }

    /// <summary>The reverse of such a map cannot be written either, so it is left out the same way.</summary>
    [Fact]
    public void TheReverseOfAColourMapOfYourOwnType_IsLeftOutOfTheJson()
    {
        string json = Serializer.ToJson(HeatmapWith(new ReversedColorMap(new StripedColorMap("striped s1h reversed"))));

        Assert.DoesNotContain("\"colorMap", json);
        Assert.Null(MapOfTheOnlySeries(Serializer.FromJson(json)));
    }

    /// <summary>Registered under its name (<see cref="ColorMapRegistry.Register"/>), a map of your own type travels by
    /// that name, as it always did: a reader that registered it too (this process) makes it again; another reads no map.</summary>
    [Fact]
    public void AColourMapOfYourOwnType_RegisteredUnderItsName_TravelsByThatName()
    {
        var mine = new StripedColorMap("striped s1h registered");
        ColorMapRegistry.Register(mine.Name, mine);

        string json = Serializer.ToJson(HeatmapWith(mine));

        Assert.Contains("\"colorMapName\":\"striped s1h registered\"", json);
        Assert.Same(mine, MapOfTheOnlySeries(Serializer.FromJson(json)));
    }

    /// <summary>The reverse of a registered map of your own type is written as the reverse of that name.</summary>
    [Fact]
    public void TheReverseOfARegisteredColourMapOfYourOwnType_IsWrittenAsTheReverseOfItsName()
    {
        var mine = new StripedColorMap("striped s1h registered reversed");
        ColorMapRegistry.Register(mine.Name, mine);
        var figure = HeatmapWith(new ReversedColorMap(mine));

        string json = Serializer.ToJson(figure);

        Assert.Contains("""
            "colorMap":{"type":"reversed","map":{"type":"named","name":"striped s1h registered reversed"}}
            """.Trim(), json);
        Assert.True(DrawnSeriesFixtures.SameValue(MapOfTheOnlySeries(figure), MapOfTheOnlySeries(Serializer.FromJson(json))));
    }

    /// <summary>Every tenth value is red, the rest blue: a map no description can hold.</summary>
    private sealed class StripedColorMap(string name) : IColorMap
    {
        public string Name { get; } = name;

        public Color GetColor(double value) => (int)(value * 10) % 2 == 0 ? Color.FromHex("#FF0000") : Color.FromHex("#0000FF");
    }

    // ---- the calendar that needs it ---------------------------------------------------------------------------------

    /// <summary>Under a map that starts at white a blank day and a zero look alike; a bad colour tells them apart. That
    /// map is built in code, and read back from JSON the calendar still draws its blank days grey.</summary>
    [Fact]
    public void ACalendarDrawnThroughAMapWithABadColour_ReadBackFromJson_DrawsTheSameSvg()
    {
        DateTime[] days = [new(2025, 3, 3), new(2025, 3, 4), new(2025, 3, 5)];
        double[] values = [double.NaN, 0, 4];
        var greens = new LinearColorMap("greens", [Color.FromHex("#F7FCF5"), Color.FromHex("#00441B")]) { BadColor = Color.FromHex("#DDDDDD") };
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.CalendarHeatmap(days, values, configure: s => s.ColorMap = greens)).Build();

        string drawn = figure.ToSvg();
        string readBack = RoundTrip(figure).ToSvg();

        Assert.Contains("#DDDDDD", drawn);
        Assert.Equal(drawn, readBack);
    }

    // ---- the colour bar and the pair grid hold a map the same way ---------------------------------------------------

    [Fact]
    public void AColourBarsMapBuiltInCode_ComesBackFromJson()
    {
        var map = new LinearColorMap("bar map", [Red, Blue]) { OverColor = Green };
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Heatmap(new double[,] { { 1, 2 }, { 3, 4 } })
            .WithColorBar(cb => cb with { ColorMap = map, Extend = ColorBarExtend.Max })).Build();

        var read = RoundTrip(figure);

        Assert.True(DrawnSeriesFixtures.SameValue(map, read.SubPlots[0].ColorBar!.ColorMap), DrawnSeriesFixtures.Show(read.SubPlots[0].ColorBar!.ColorMap));
        Assert.Equal(figure.ToSvg(), read.ToSvg());
    }

    [Fact]
    public void APairGridsOffDiagonalMapBuiltInCode_ComesBackFromJson()
    {
        var map = new LinearColorMap("density", [Color.FromHex("#FFFFFF"), Blue]) { BadColor = Red };
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.AddSeries(new PairGridSeries([[1.0, 2, 3], [2.0, 1, 3]]) { OffDiagonalColorMap = map })).Build();

        var read = Assert.IsType<PairGridSeries>(RoundTrip(figure).SubPlots[0].Series.Single());

        Assert.True(DrawnSeriesFixtures.SameValue(map, read.OffDiagonalColorMap), DrawnSeriesFixtures.Show(read.OffDiagonalColorMap));
    }

    // ---- JSON written by hand ---------------------------------------------------------------------------------------

    private static IColorMap? ReadHeatmapMap(string colorMapFields) =>
        MapOfTheOnlySeries(Serializer.FromJson(
            $$"""{"width":400,"height":300,"subPlots":[{"series":[{"type":"heatmap","heatmapData":[[1,2],[3,4]],{{colorMapFields}}}]}]}"""));

    /// <summary>A description that cannot make a map is refused with an ArgumentException that names the field, as
    /// bar positions that do not pair up with the values are: a map without colours would fail only when it is drawn.</summary>
    [Theory]
    [InlineData("""{"type":"linear","name":"x"}""", "colors")]
    [InlineData("""{"type":"linear","colors":[]}""", "colors")]
    [InlineData("""{"type":"linear","colors":["#FF0000","#0000FF"],"positions":[0]}""", "positions")]
    [InlineData("""{"type":"linear","colors":["#FF0000","#0000FF"],"positions":[1,0]}""", "positions")]
    [InlineData("""{"type":"linear","colors":["#FF0000"],"positions":[0]}""", "positions")]
    [InlineData("""{"type":"listed","colors":[]}""", "colors")]
    [InlineData("""{"type":"listed","name":"x"}""", "colors")]
    [InlineData("""{"type":"reversed"}""", "map")]
    [InlineData("""{"type":"named"}""", "name")]
    public void ADescriptionThatCannotMakeAMap_IsRefusedNamingTheField(string description, string field)
    {
        var refused = Assert.ThrowsAny<ArgumentException>(() => ReadHeatmapMap($"\"colorMap\":{description}"));

        Assert.Equal(field, refused.ParamName);
    }

    /// <summary>A name is a label, not what draws: a description written without one reads as a map with an empty name
    /// that draws its colours.</summary>
    [Theory]
    [InlineData("linear")]
    [InlineData("listed")]
    public void ADescriptionWithoutAName_ReadsAsAMapWithAnEmptyName(string kind)
    {
        var map = ReadHeatmapMap($$"""
            "colorMap":{"type":"{{kind}}","colors":["#FF0000","#0000FF"]}
            """.Trim());

        Assert.NotNull(map);
        Assert.Equal(string.Empty, map.Name);
        Assert.Equal(Red, map.GetColor(0));
        Assert.Equal(Blue, map.GetColor(1));
    }

    /// <summary>A map named by kind reads as the map registered under that name, a description wins over a name written
    /// beside it, and a name nobody registered reads as no map, as it always has.</summary>
    [Fact]
    public void ANamedDescription_ADescriptionBesideAName_AndAnUnknownName_ReadAsWritten()
    {
        Assert.Same(ColorMaps.Plasma, ReadHeatmapMap("""
            "colorMap":{"type":"named","name":"plasma"}
            """.Trim()));
        Assert.Same(ColorMaps.Plasma, ReadHeatmapMap("""
            "colorMapName":"viridis","colorMap":{"type":"named","name":"plasma"}
            """.Trim()));
        Assert.Null(ReadHeatmapMap("""
            "colorMapName":"no such map s1h"
            """.Trim()));
    }

    // ---- a colour's transparency -------------------------------------------------------------------------------------

    /// <summary>A colour with transparency keeps its alpha through JSON (<c>#RRGGBBAA</c>), wherever it stands; an opaque
    /// colour is written <c>#RRGGBB</c>, the bytes it always was.</summary>
    [Fact]
    public void AColourWithTransparency_KeepsItsAlpha_AndAnOpaqueOneIsWrittenAsBefore()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot([1.0, 2], [1.0, 2], s => s.Color = Color.FromHex("#12345680"))
            .Plot([1.0, 2], [2.0, 1], s => s.Color = Color.FromHex("#ABCDEF"))).Build();

        string json = Serializer.ToJson(figure);
        var read = Serializer.FromJson(json).SubPlots[0].Series;

        Assert.Contains("\"color\":\"#12345680\"", json);
        Assert.Contains("\"color\":\"#ABCDEF\"", json);
        Assert.Equal(Color.FromHex("#12345680"), ((LineSeries)read[0]).Color);
        Assert.Equal(Color.FromHex("#ABCDEF"), ((LineSeries)read[1]).Color);
    }

    /// <summary>A figure whose maps all ship with the library writes no description field at all.</summary>
    [Fact]
    public void AFigureOfShippedMaps_WritesNoDescriptionField()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Heatmap(new double[,] { { 1, 2 }, { 3, 4 } }, s => s.ColorMap = ColorMaps.Inferno)
            .WithColorBar(cb => cb with { ColorMap = ColorMaps.Magma })).Build();

        string json = Serializer.ToJson(figure);

        Assert.Equal(2, Regex.Matches(json, "\"colorMapName\"").Count);
        Assert.DoesNotContain("\"colorMap\":", json);
    }
}
