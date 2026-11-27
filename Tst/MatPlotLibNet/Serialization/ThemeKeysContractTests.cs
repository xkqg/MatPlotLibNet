// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>Each look of a theme owns its keys in the theme's record of the figure JSON (<see cref="IThemeKeys{TLook}"/>): the record
/// declares the flat keys and their order, the look writes its values under them and reads them back. The facts here hold that for
/// every look a theme has, found by reflection, so a look added to <see cref="ThemeLooks"/> or a value added to a look is named the
/// moment its keys are missing: every look implements the pair, and a look with every value off the library's (its fixture) reads back
/// from the JSON text as it was written, alone and with all the others. A value the JSON does not carry one by one is listed in
/// <see cref="NotCarried"/> with its reason; the bytes themselves are pinned by <see cref="ThemeCorpusTests"/>.</summary>
public class ThemeKeysContractTests
{
    /// <summary>The looks of a theme: every settable property of <see cref="ThemeLooks"/>.</summary>
    private static readonly PropertyInfo[] Looks =
        [.. typeof(ThemeLooks).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => property.SetMethod is not null)];

    /// <summary>Each look with every value its keys carry off the value of the library's look.</summary>
    private static readonly Dictionary<string, object> OffTheLibrary = new(StringComparer.Ordinal)
    {
        [nameof(ThemeLooks.Ticks)] = new AxisTickLook
        {
            Ladder = new TickLadder([1, 2, 5, 10], 7, 3, BinsByLength: true),
            Direction = TickDirection.In,
            Length = 5.5,
            Width = 0.75,
            Pad = 4.5,
            MinorWidth = 0.6,
            MinorPad = 3.25,
            OnAllSides = true,
            MinorsByLocator = true,
            MinorSplit = AutoMinorLocator.Auto,
            MinorGridLikeMajor = true,
        },
        [nameof(ThemeLooks.TickLabels)] = new TickLabelLook
        {
            LogThresholds = new LogLabelThresholds(0.5, 0.25),
            FormatterLimits = new PowerLimits(-5, 6),
            OffsetThreshold = 3,
            UnicodeMinus = true,
        },
        [nameof(ThemeLooks.Range)] = new AxisRangeLook
        {
            Autolimit = AutolimitMode.RoundNumbers,
            SymLogLinThresh = 2.5,
            ScatterMargin = 0.05,
            PatchesLikeAddPatch = true,
        },
        [nameof(ThemeLooks.Frame)] = new AxesFrameLook { SpineWidth = 1.25, AxisZOrder = 2.5, NoBackgroundWithoutAxes = true, ClipsEveryArtist = true },
        [nameof(ThemeLooks.Cube)] = new CubeLook
        {
            WallGrid = Theme.MatplotlibClassic.Looks.Cube.WallGrid,
            SurfaceStride = 7,
            AxisLineWidth = 1.5,
        },
        [nameof(ThemeLooks.Text)] = new TextLook
        {
            TitleScale = 1.2,
            SupTitleScale = 1.1,
            TitleWeight = FontWeight.Normal,
            TitlePad = 6.5,
            TitleOverWhatIsBelow = true,
            LabelPad = 4.75,
            SeriesTextAtFontSize = true,
            MeasuresLikeAgg = true,
            MathFonts = MathFontSet.ComputerModern,
        },
        [nameof(ThemeLooks.Figure)] = new FigureLook { LegendListsEveryEntry = true, NoPadAboveTitle = true, TightPad = 1.08 },
        [nameof(ThemeLooks.Patch)] = new PatchLook
        {
            LikeCollections = true,
            ForceEdge = false,
            EdgeColor = Colors.Black,
            EdgeWidth = 1.25,
            Alpha = 0.75,
            Hatch = new HatchColorRule(FollowsEdge: true, Colors.Blue),
        },
        [nameof(ThemeLooks.ColorBar)] = new ColorBarLook { TakesRoom = true, StepPerColour = true, ExtendTriangles = true, TickMarks = true },
        [nameof(ThemeLooks.ColorMap)] = new ColorMapLook { Default = ColorMaps.Jet, LikeLogNorm = true, CountsFromTheLeastDrawn = true },
        [nameof(ThemeLooks.Raster)] = new RasterLook { StrokesOnPixelGrid = true },
        [nameof(ThemeLooks.Strokes)] = new StrokeLook
        {
            Dashes = new DashLook { Dashed = [6, 6], Dotted = [1, 2], DashDot = [3, 5, 1, 5], ScalesWithWidth = true, Unit = 1.5 },
            Join = JoinStyle.Round,
        },
        [nameof(ThemeLooks.Cycle)] = new CycleLook { Rule = CycleRule.MatplotlibClassic, GroupsWearTheCycle = true },
        [nameof(ThemeLooks.Lines)] = new SeriesLineLook { Width = 2.25, ErrorBarCapSize = 4 },
        [nameof(ThemeLooks.Markers)] = new MarkerLook { FillTheirBox = true, LineEdge = new LineMarkerLook(0.75, Colors.Black) },
        [nameof(ThemeLooks.Scatter)] = new ScatterLook(20, 25, Colors.Blue, 1.25, Colors.Black, 0.3),
        [nameof(ThemeLooks.Box)] = Theme.MatplotlibClassic.Looks.Box,
        [nameof(ThemeLooks.Legend)] = LegendLook.Matplotlib(0.9, fancyBox: true, frameAlpha: 0.6, Colors.Red, numPoints: 2, scatterPoints: 3),
        [nameof(ThemeLooks.Dots)] = DotLook.Seaborn,
        [nameof(ThemeLooks.Verbs)] = SeabornVerbLook.Seaborn,
        [nameof(ThemeLooks.Density)] = DensityLook.Scipy,
        [nameof(ThemeLooks.Violin)] = ViolinLook.Matplotlib,
        [nameof(ThemeLooks.Wedges)] = WedgeLook.Matplotlib,
        [nameof(ThemeLooks.Table)] = TableLook.Matplotlib,
        [nameof(ThemeLooks.Treemap)] = new TreemapLook { LikeSquarify = true },
        [nameof(ThemeLooks.Image)] = ImageLook.Imshow with { Interpolation = "bilinear" },
        [nameof(ThemeLooks.Spectrogram)] = SpectrogramLook.Specgram,
        [nameof(ThemeLooks.Map)] = new MapLook { LikeCartopy = true },
        [nameof(ThemeLooks.Polar)] = PolarLook.Matplotlib,
        [nameof(ThemeLooks.VectorField)] = new VectorFieldLook
        {
            Color = Colors.Black,
            QuiverAutoScale = true,
            BarbsLikeMatplotlib = true,
            StreamArrowsLikeMatplotlib = true,
        },
    };

    /// <summary>Values of a look that no key carries one by one, with the reason: the JSON names such a look as a whole (the library's
    /// or matplotlib's), and these values are the same in both.</summary>
    private static readonly Dictionary<string, string> NotCarried = new(StringComparer.Ordinal)
    {
        ["Violin.Extrema"] = "the extrema line is the same under every theme; the JSON names the violin look as a whole (violinsLikeMatplotlib)",
        ["Table.CellFill"] = "a data cell is white under every theme; the JSON names the table look as a whole (tablesLikeMatplotlib)",
        ["Legend.ShadowColor"] = "the same under every theme; the JSON carries a legend's six matplotlib settings, the rest follow the look",
        ["Legend.ShadowOffset"] = "the same under every theme; the JSON carries a legend's six matplotlib settings, the rest follow the look",
        ["Legend.HandleLength"] = "the same under every theme; the JSON carries a legend's six matplotlib settings, the rest follow the look",
        ["Legend.HandleHeight"] = "the same under every theme; the JSON carries a legend's six matplotlib settings, the rest follow the look",
        ["Legend.HandleTextPad"] = "the same under every theme; the JSON carries a legend's six matplotlib settings, the rest follow the look",
    };

    public static TheoryData<string> LookNames { get; } = new(Looks.Select(look => look.Name));

    /// <summary>Every look of a theme, the theme's looks themselves, and every look a look holds (a stroke's dashes, the edge of a
    /// line's markers) owns its keys.</summary>
    [Fact]
    public void EveryLookOfATheme_OwnsItsKeys()
    {
        var missing = new List<string>();
        foreach (var look in Looks.Concat(Looks.SelectMany(NestedLooks)))
        {
            var type = Nullable.GetUnderlyingType(look.PropertyType) ?? look.PropertyType;
            if (!typeof(IThemeKeys<>).MakeGenericType(look.PropertyType).IsAssignableFrom(type))
                missing.Add($"{look.DeclaringType!.Name}.{look.Name} ({type.Name})");
        }

        if (!typeof(IThemeKeys<ThemeLooks>).IsAssignableFrom(typeof(ThemeLooks))) missing.Add(nameof(ThemeLooks));

        Assert.True(missing.Count == 0, "a look that does not own its keys (IThemeKeys): " + string.Join(", ", missing));
    }

    /// <summary>The looks a look holds as values of its own: a property whose type is a look.</summary>
    private static IEnumerable<PropertyInfo> NestedLooks(PropertyInfo look) =>
        SettableValues(Nullable.GetUnderlyingType(look.PropertyType) ?? look.PropertyType)
            .Where(value => (Nullable.GetUnderlyingType(value.PropertyType) ?? value.PropertyType).Name.EndsWith("Look", StringComparison.Ordinal));

    [Fact]
    public void EveryLook_HasAFixtureOffTheLibrary()
    {
        Assert.Equal(Looks.Select(look => look.Name).Order(StringComparer.Ordinal), OffTheLibrary.Keys.Order(StringComparer.Ordinal));
    }

    /// <summary>A fixture holds every value of its look off the library's, so the round trip below reads every key the look has: a value
    /// left at the library's would read back at the library's whether or not a key carries it.</summary>
    [Theory]
    [MemberData(nameof(LookNames))]
    public void ALooksFixture_HoldsEveryValueOffTheLibrary(string name)
    {
        var look = Looks.Single(property => property.Name == name);
        object? library = look.GetValue(ThemeLooks.Library);
        object fixture = OffTheLibrary[name];

        if (library is null)
        {
            Assert.NotEqual("null", Describe(fixture));
            return;
        }

        var atTheLibrary = SettableValues(look.PropertyType)
            .Where(value => !NotCarried.ContainsKey($"{name}.{value.Name}"))
            .Where(value => Describe(value.GetValue(fixture)) == Describe(value.GetValue(library)))
            .Select(value => value.Name)
            .ToList();

        Assert.True(atTheLibrary.Count == 0, $"the fixture of {name} leaves these at the library's value: {string.Join(", ", atTheLibrary)}");
    }

    [Fact]
    public void ANotCarriedEntry_NamesAValueTheFixtureLeavesAtTheLibrary()
    {
        foreach (var (entry, _) in NotCarried)
        {
            string[] parts = entry.Split('.');
            var look = Looks.Single(property => property.Name == parts[0]);
            var value = SettableValues(look.PropertyType).Single(property => property.Name == parts[1]);

            Assert.Equal(Describe(value.GetValue(look.GetValue(ThemeLooks.Library))), Describe(value.GetValue(OffTheLibrary[parts[0]])));
        }
    }

    /// <summary>A look with every value off the library's, and every other look at the library's, reads back from the JSON text of its
    /// theme as it was written, and leaves every other look at the library's: its keys say what it holds and nothing else does.</summary>
    [Theory]
    [MemberData(nameof(LookNames))]
    public void ALookOffTheLibrary_ReadsBackFromTheJsonText_AndNoOtherLookMoves(string name)
    {
        var looks = new ThemeLooks();
        var look = Looks.Single(property => property.Name == name);
        look.SetValue(looks, OffTheLibrary[name]);

        var read = ReadBack(looks);

        foreach (var other in Looks)
        {
            object? expected = other == look ? OffTheLibrary[name] : other.GetValue(ThemeLooks.Library);
            Assert.Equal(Describe(expected), Describe(other.GetValue(read)));
        }
    }

    /// <summary>Every look off the library's at once reads back as written: no two looks share a key.</summary>
    [Fact]
    public void EveryLookOffTheLibrary_ReadsBackFromTheJsonText()
    {
        var looks = new ThemeLooks();
        foreach (var look in Looks) look.SetValue(looks, OffTheLibrary[look.Name]);

        var read = ReadBack(looks);

        foreach (var look in Looks) Assert.Equal(Describe(OffTheLibrary[look.Name]), Describe(look.GetValue(read)));
    }

    /// <summary>A value made of several keys (a tick ladder, log label thresholds, formatter limits, a hatch rule, the edge of a line's
    /// markers) is named only when the keys it cannot be made without are there: one of them alone names no value, and the look keeps
    /// the library's.</summary>
    [Fact]
    public void AKeyOfAValueMadeOfSeveral_ReadAlone_NamesNoValue()
    {
        Assert.Null(AxisTickLook.ReadKeys(new ThemeDto { AxisTickSteps = [1, 2, 5, 10] }).Ladder);
        Assert.Null(AxisTickLook.ReadKeys(new ThemeDto { AxisTickSteps = [1, 2, 5, 10], AxisTickBins = 9 }).Ladder);
        Assert.Null(TickLabelLook.ReadKeys(new ThemeDto { LogLabelPowersThreshold = 1 }).LogThresholds);
        Assert.Null(TickLabelLook.ReadKeys(new ThemeDto { FormatterLimitLower = -5 }).FormatterLimits);
        Assert.Null(PatchLook.ReadKeys(new ThemeDto { HatchFollowsEdge = true }).Hatch);
        Assert.Null(LineMarkerLook.ReadKeys(new ThemeDto { LineMarkerFilledEdge = Colors.Black }));
    }

    /// <summary>A ladder read without the key that says its bins follow the axis' length counts its bins as given; a scatter look is
    /// named by any one of its six keys, and a hatch rule by its colour alone, following no edge.</summary>
    [Fact]
    public void AValueReadWithSomeOfItsKeys_TakesTheRestAtTheirRest()
    {
        Assert.Equal(new TickLadder([1, 2, 5, 10], 9, 2).Bins, AxisTickLook.ReadKeys(new ThemeDto { AxisTickSteps = [1, 2, 5, 10], AxisTickBins = 9, AxisTickMinTicks = 2 }).Ladder!.Value.Bins);
        Assert.False(AxisTickLook.ReadKeys(new ThemeDto { AxisTickSteps = [1, 2, 5, 10], AxisTickBins = 9, AxisTickMinTicks = 2 }).Ladder!.Value.BinsByLength);
        Assert.Equal(new ScatterLook(null, null, null, null, null, 0.3), ScatterLook.ReadKeys(new ThemeDto { ScatterDepthShade = 0.3 }));
        Assert.Equal(new HatchColorRule(false, Colors.Black), PatchLook.ReadKeys(new ThemeDto { HatchFallback = Colors.Black }).Hatch);
    }

    /// <summary>The looks of a theme with <paramref name="looks"/>, written as the figure JSON writes the theme and read back from the
    /// text.</summary>
    private static ThemeLooks ReadBack(ThemeLooks looks)
    {
        var theme = new Theme("keys", Theme.Default) { Looks = looks };
        string json = JsonSerializer.Serialize(ThemeDto.From(theme), ChartSerializer.ReadOptions);
        return JsonSerializer.Deserialize<ThemeDto>(json, ChartSerializer.ReadOptions)!.ToTheme().Looks;
    }

    /// <summary>The values a look (or a record a look holds) is made of: its settable properties.</summary>
    private static IEnumerable<PropertyInfo> SettableValues(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(property => property.SetMethod is not null && property.GetIndexParameters().Length == 0 && property.Name != "EqualityContract");

    /// <summary>What <paramref name="value"/> is made of, as text: a record by its values, a list by its items, a colour map and a math
    /// font set by their names, a number in the invariant culture, any other object by its identity. Two values that describe alike
    /// draw alike, though a list read back from the JSON is another array than the one written.</summary>
    private static string Describe(object? value, int depth = 0) => value switch
    {
        null => "null",
        string text => $"\"{text}\"",
        IColorMap map => $"map {map.Name}",
        MathFontSet set => $"fonts {set.Name ?? "library"}",
        Enum or bool => value.ToString()!,
        IFormattable number when value.GetType().IsPrimitive => number.ToString(null, CultureInfo.InvariantCulture),
        IEnumerable items => "[" + string.Join(",", items.Cast<object?>().Select(item => Describe(item, depth + 1))) + "]",
        _ when depth < 6 && IsRecord(value.GetType()) && SettableValues(value.GetType()).Any() =>
            value.GetType().Name + "{" + string.Join(",", SettableValues(value.GetType())
                .Select(property => property.Name + "=" + Describe(property.GetValue(value), depth + 1))) + "}",
        _ when value.GetType().IsValueType => value.ToString()!,
        _ => value.GetType().Name + "#" + RuntimeHelpers.GetHashCode(value),
    };

    private static bool IsRecord(Type type) =>
        type.IsValueType || type.GetProperty("EqualityContract", BindingFlags.NonPublic | BindingFlags.Instance) is not null;
}
