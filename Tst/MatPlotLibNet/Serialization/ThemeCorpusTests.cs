// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>The bytes the figure JSON writes for each theme the library ships, pinned in a checked-in file per theme: every flat key of
/// the theme, with its value, in the order the writer puts them. The tests that read a theme back only see the values come out the
/// same, so they stay green when a key is renamed, dropped from the wire or written in another order; this fails on the first byte
/// that moves and names the theme. A change that must keep the theme JSON identical (a look that owns its keys, a rule
/// that reads a theme value) is proved by this corpus passing without a file regenerated.
/// <para>The pin is the theme's record as the figure JSON writes it (every key measured against the plain theme, whose own record is
/// the empty object), so the theme a figure rests on is pinned too, though a figure under it writes no <c>theme</c> key. The files
/// live under <c>Serialization/Golden/Themes</c>; <c>MPL_REGEN_GOLDEN=1</c> writes them.</para></summary>
[Collection("ChartSerializerGlobalState")]
public class ThemeCorpusTests
{
    private static readonly ChartSerializer Serializer = new();

    private const string Folder = "Themes";

    private static readonly Dictionary<string, Theme> Shipped =
        typeof(Theme).GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(property => property.PropertyType == typeof(Theme))
            .ToDictionary(property => property.Name, property => (Theme)property.GetValue(null)!, StringComparer.Ordinal);

    /// <summary>A theme whose default colour map was built in code: the one theme record that carries the map's description
    /// (<c>defaultColorMap</c>) rather than its name, which no shipped theme does.</summary>
    private const string CodeBuiltMap = "ThemeWithACodeBuiltColorMap";

    /// <summary>The themes pinned besides the shipped ones, each for a key no shipped theme writes.</summary>
    private static readonly Dictionary<string, Theme> Extra = new(StringComparer.Ordinal)
    {
        [CodeBuiltMap] = new Theme("code-built-map", Theme.Default)
        {
            Looks = ThemeLooks.Library with
            {
                ColorMap = ColorMapLook.Library with
                {
                    Default = new MatPlotLibNet.Styling.ColorMaps.LinearColorMap("ramp", [Colors.Red, Colors.White, Colors.Blue]),
                },
            },
        },
    };

    /// <summary>One row per shipped theme, by the name of the property that holds it.</summary>
    public static TheoryData<string> Names { get; } = new(Shipped.Keys.Order(StringComparer.Ordinal));

    /// <summary>One row per theme pinned besides the shipped ones.</summary>
    public static TheoryData<string> ExtraNames { get; } = new(Extra.Keys.Order(StringComparer.Ordinal));

    [Theory]
    [MemberData(nameof(ExtraNames))]
    public void AThemeOfAKeyNoShippedThemeWrites_WritesTheBytesPinned_AndReadsBackToThem(string name)
    {
        string bytes = ThemeBytes(Extra[name]);

        string pinned = BytePins.Pinned(Folder, name, bytes);

        Assert.Equal(pinned, bytes);
        Assert.Equal(pinned, ThemeBytes(Plt.Create().WithTheme(Extra[name]).Build().RoundTripped().Theme));
    }

    [Fact]
    public void TheCodeBuiltMap_IsWrittenAsItsDescription()
    {
        Assert.Contains("\"defaultColorMap\":", ThemeBytes(Extra[CodeBuiltMap]), StringComparison.Ordinal);
        Assert.DoesNotContain("\"defaultColorMapName\"", ThemeBytes(Extra[CodeBuiltMap]), StringComparison.Ordinal);
    }

    /// <summary>The record of <paramref name="theme"/> as the figure JSON writes it.</summary>
    private static string ThemeBytes(Theme theme) => JsonSerializer.Serialize(ThemeDto.From(theme), ChartSerializer.ReadOptions);

    /// <summary>The <c>theme</c> object of the JSON of an empty figure under <paramref name="theme"/>, or null when the figure writes none.</summary>
    private static string? ThemeKeyOfFigure(Theme theme)
    {
        using var document = JsonDocument.Parse(Serializer.ToJson(Plt.Create().WithTheme(theme).Build()));
        return document.RootElement.TryGetProperty("theme", out var element) ? element.GetRawText() : null;
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void ATheme_WritesTheBytesPinned(string name)
    {
        string bytes = ThemeBytes(Shipped[name]);

        string pinned = BytePins.Pinned(Folder, name, bytes);

        Assert.Equal(pinned, bytes);
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void ATheme_ReadBackFromItsPinnedBytes_WritesThemeBytesAgain(string name)
    {
        string pinned = BytePins.Pinned(Folder, name, ThemeBytes(Shipped[name]));
        var figure = Plt.Create().WithTheme(Shipped[name]).Build();

        string again = ThemeBytes(figure.RoundTripped().Theme);

        Assert.Equal(pinned, again);
    }

    [Fact]
    public void ThePinnedFiles_AreExactlyTheShippedThemes()
    {
        Assert.Equal(Shipped.Keys.Concat(Extra.Keys).Order(StringComparer.Ordinal), BytePins.Names(Folder));
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void AFigure_WritesTheThemeAsPinned_UnlessItRestsOnThatTheme(string name)
    {
        string? key = ThemeKeyOfFigure(Shipped[name]);

        if (ReferenceEquals(Shipped[name], Plt.Create().Build().Theme))
        {
            Assert.Null(key);
        }
        else
        {
            Assert.Equal(BytePins.Pinned(Folder, name, ThemeBytes(Shipped[name])), key);
        }
    }

    [Fact]
    public void ThePlainTheme_IsTheEmptyObject_AndAnotherThemeWritesKeys()
    {
        Assert.Equal("{}", ThemeBytes(Theme.Default));
        Assert.StartsWith("{\"name\"", ThemeBytes(Theme.Dark), StringComparison.Ordinal);
    }
}
