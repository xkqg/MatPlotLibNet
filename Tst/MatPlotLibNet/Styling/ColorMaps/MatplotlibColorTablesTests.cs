// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Styling.ColorMapNormalizers;

/// <summary>A colour map the library ships under matplotlib's name is matplotlib's own table, not a hand-typed approximation of a
/// few stops: <c>Plasma</c> near its light end is <c>(252, 203, 37)</c>, not the colour between two of ten stops, <c>Blues</c> ends at
/// <c>#08306B</c> where the library's ended at <c>#084594</c>, and <c>Jet</c> stands where its segments meet (red from 0.35, green
/// from 0.125, blue to 0.65). The reference is <c>matplotlib-colormaps.json</c>, written by <c>tools/mpl_reference/generate_colormaps.py</c>
/// from matplotlib 3.11.2's own lookup (<c>colormaps[name](v, bytes=True)</c>, a byte truncated as the renderers write it) at every
/// fifth entry of each table and the last, never from the data the library holds. A level is 1/255.</summary>
public class MatplotlibColorTablesTests
{
    private static readonly Dictionary<string, (double Value, string Hex)[]> Reference = Load();

    private static Dictionary<string, (double, string)[]> Load()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Styling", "ColorMaps", "matplotlib-colormaps.json")));
        return doc.RootElement.EnumerateObject().ToDictionary(
            map => map.Name,
            map => map.Value.GetProperty("samples").EnumerateArray()
                .Select(row => (row[0].GetDouble(), row[1].GetString()!)).ToArray());
    }

    public static TheoryData<string> MapNames { get; } = new(Reference.Keys);

    [Theory]
    [MemberData(nameof(MapNames))]
    public void AMapMatplotlibHas_AnswersAsMatplotlibsLookupDoes_WithinOneLevel(string name)
    {
        var map = ColorMapRegistry.Get(name);
        Assert.NotNull(map);

        foreach (var (value, hex) in Reference[name])
        {
            var expected = Color.FromHex("#" + hex);
            var got = map.GetColor(value);
            Assert.True(Math.Abs(got.R - expected.R) <= 1 && Math.Abs(got.G - expected.G) <= 1 && Math.Abs(got.B - expected.B) <= 1,
                $"{name} at {value}: matplotlib {expected.ToHex()}, the library {got.ToHex()}");
        }
    }

    [Fact]
    public void EveryMapTheLibraryShips_IsHeldToMatplotlib_ExceptTheOnesMatplotlibHasNoTableFor()
    {
        // seaborn's maps and the Petroff cycles are not in matplotlib's colormap registry; okabe_ito is held by its own test below.
        string[] notMatplotlibs = ["rocket", "mako", "crest", "flare", "icefire", "petroff6", "petroff8", "petroff10", "okabe_ito"];

        var unchecked_ = ColorMapRegistry.Names
            .Where(name => !name.EndsWith("_r", StringComparison.OrdinalIgnoreCase)
                           && ColorMapRegistry.Get(name) is { } map && ColorMapRegistry.IsShipped(map))
            .Where(name => !Reference.ContainsKey(name.ToLowerInvariant()) && !notMatplotlibs.Contains(name.ToLowerInvariant()))
            .ToList();

        Assert.Empty(unchecked_);
    }

    [Fact]
    public void Plasma_NearItsLightEnd_IsMatplotlibsColour_NotTheBlendOfTwoOfTenStops()
    {
        // matplotlib 3.11.2: colormaps['plasma'](228) is (0.990, 0.797, 0.148), which the renderers write as (252, 203, 37). The
        // library's ten stops blended (252, 198, 40) there, five levels off in the green channel, the widest gap of the map.
        var got = ColorMaps.Plasma.GetColor(228 / 255.0);

        Assert.InRange(got.R, 251, 253);
        Assert.InRange(got.G, 202, 204);
        Assert.InRange(got.B, 36, 38);
    }

    [Fact]
    public void OkabeIto_HasMatplotlibsEightColours_AndKeepsTheOrderOfTheColourBlindSafeCycle()
    {
        // matplotlib 3.11.2 lists okabe_ito as black, orange, sky blue, bluish green, yellow, blue, vermillion, reddish purple. The
        // library lists the same eight with black last, the order the colour-blind-safe theme cycles through (orange first).
        string[] matplotlibs = ["#000000", "#E69F00", "#56B4E9", "#009E73", "#F0E442", "#0072B2", "#D55E00", "#CC79A7"];

        var map = (ListedColorMap)QualitativeColorMaps.OkabeIto;
        var library = Enumerable.Range(0, matplotlibs.Length).Select(i => map.GetColor((i + 0.5) / matplotlibs.Length)).ToList();

        Assert.Equal(matplotlibs.Select(Color.FromHex).OrderBy(c => c.ToHex()), library.OrderBy(c => c.ToHex()));
        Assert.Equal(Color.FromHex("#E69F00"), library[0]);
    }

    [Fact]
    public void AName_MatplotlibHasNoTableFor_IsRefused()
    {
        var thrown = Assert.Throws<ArgumentException>(() => MatplotlibColorTables.Of("not-a-matplotlib-map"));

        Assert.Equal("name", thrown.ParamName);
    }

    [Fact]
    public void AMapOfTheTables_IsNamedAsTheLibraryNamesIt_AndKnownToTheRegistry()
    {
        var map = MatplotlibColorTables.Of("twilight_shifted");

        Assert.Equal("twilight_shifted", map.Name);
        Assert.Same(CyclicColorMaps.TwilightShifted.GetType(), map.GetType());
    }

    [Fact]
    public void Blues_HasMatplotlibsNineStops_AndEndsDarkNavy()
    {
        // matplotlib's _Blues_data has 9 colours, #F7FBFF to #08306B, evenly spaced: the map is at each of them at k / 8.
        string[] stops = ["#F7FBFF", "#DEEBF7", "#C6DBEF", "#9ECAE1", "#6BAED6", "#4292C6", "#2171B5", "#08519C", "#08306B"];

        for (int k = 0; k < stops.Length; k++)
            Assert.Equal(Color.FromHex(stops[k]), ColorMaps.Blues.GetColor(k / 8.0));
    }

    [Fact]
    public void Reds_EndsAtMatplotlibsDarkRed()
    {
        // matplotlib's _Reds_data ends (0.40392157, 0.0, 0.05098039) = #67000D.
        Assert.Equal(Color.FromHex("#67000D"), ColorMaps.Reds.GetColor(1.0));
    }

    [Theory]
    [InlineData(0.35, 0.0, 0.9, 0.967742)]
    [InlineData(0.66, 1.0, 0.925926, 0.0)]
    [InlineData(0.89, 1.0, 0.074074, 0.0)]
    public void Jet_StandsWhereItsSegmentsMeet(double position, double red, double green, double blue)
    {
        // matplotlib's _jet_data: red (0, 0), (0.35, 0), (0.66, 1), (0.89, 1), (1, 0.5); green (0, 0), (0.125, 0), (0.375, 1),
        // (0.64, 1), (0.91, 0), (1, 0); blue (0, 0.5), (0.11, 1), (0.34, 1), (0.65, 0), (1, 0). The values are those lines read
        // at the position; the library's nine even stops put a mid-range value up to 17 levels away.
        var got = ColorMaps.Jet.GetColor(position);

        Assert.InRange(got.R, red * 255 - 1, red * 255 + 1);
        Assert.InRange(got.G, green * 255 - 1, green * 255 + 1);
        Assert.InRange(got.B, blue * 255 - 1, blue * 255 + 1);
    }

    /// <summary>A name the tables do not hold is refused, however near it stands to one they do: every listed name with one of its
    /// letters changed, and a name of no letter.</summary>
    [Fact]
    public void ANameTheTablesDoNotHold_IsRefused_HoweverNearItIs()
    {
        var tables = ColorMapRegistry.Names.Select(name => name.ToLowerInvariant()).Where(HoldsATable).ToList();
        var near = tables.SelectMany(name => Enumerable.Range(0, name.Length).Select(at => name[..at] + '~' + name[(at + 1)..])).Append("");

        Assert.NotEmpty(tables);
        Assert.All(near, name => Assert.Throws<ArgumentException>(() => MatplotlibColorTables.Of(name)));
    }

    /// <summary>Whether matplotlib's tables hold the name.</summary>
    private static bool HoldsATable(string name)
    {
        try
        {
            MatplotlibColorTables.Of(name);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
