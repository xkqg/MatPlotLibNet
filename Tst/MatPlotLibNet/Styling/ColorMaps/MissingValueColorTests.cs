// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Styling.ColorMapNormalizers;

/// <summary>NaN is the library's one missing value, and a colormap draws it in its bad colour: the colour
/// <see cref="IColorMap.GetBadColor"/> names, transparent when the map names none (matplotlib's default bad colour,
/// RGBA 0,0,0,0). A colormap never computes a stop index from NaN. On net8.0 the cast of NaN to <c>int</c> is
/// <c>int.MinValue</c>, so the evenly spaced and the listed lookups threw IndexOutOfRangeException there; net10.0
/// saturates the cast to 0 and hid it. This test project runs on net10.0 only, so these facts pin the contract itself:
/// with a bad colour set, no index computed from NaN can produce it.</summary>
public class MissingValueColorTests
{
    private static readonly Color Bad = Color.FromHex("#13579B");
    private static readonly Color Low = Color.FromHex("#000080");
    private static readonly Color High = Color.FromHex("#FFFF00");

    // ---- the colormap itself ------------------------------------------------------------------------------------

    [Fact]
    public void AnEvenlySpacedMap_DrawsNaN_InItsBadColour() =>
        Assert.Equal(Bad, new LinearColorMap("even", [Low, High]) { BadColor = Bad }.GetColor(double.NaN));

    [Fact]
    public void AnEvenlySpacedMap_WithoutABadColour_DrawsNaN_Transparent() =>
        Assert.Equal(Colors.Transparent, new LinearColorMap("even", [Low, High]).GetColor(double.NaN));

    [Fact]
    public void APositionedMap_DrawsNaN_Transparent() =>
        Assert.Equal(Colors.Transparent,
            LinearColorMap.FromPositions("positioned", [new(0, Low), new(0.3, Bad), new(1, High)]).GetColor(double.NaN));

    [Fact]
    public void AListedMap_DrawsNaN_Transparent_NotItsFirstColour() =>
        Assert.Equal(Colors.Transparent, new ListedColorMap("listed", [Low, High]).GetColor(double.NaN));

    [Fact]
    public void AListedMap_DrawsNaN_InItsBadColour() =>
        Assert.Equal(Bad, new ListedColorMap("listed", [Low, High]) { BadColor = Bad }.GetColor(double.NaN));

    [Fact]
    public void AReversedMap_DrawsNaN_InTheBadColourOfTheMapItReverses() =>
        Assert.Equal(Bad, new ReversedColorMap(new LinearColorMap("even", [Low, High]) { BadColor = Bad }).GetColor(double.NaN));

    /// <summary>Every map the library ships, by name, draws a missing value transparent.</summary>
    public static TheoryData<string> RegisteredNames { get; } = new(ColorMapRegistry.Names.Order(StringComparer.Ordinal));

    [Theory]
    [MemberData(nameof(RegisteredNames))]
    public void EveryRegisteredMap_DrawsNaN_Transparent(string name) =>
        Assert.Equal(Colors.Transparent, ColorMapRegistry.Get(name)!.GetColor(double.NaN));

    [Fact]
    public void AValueOnTheScale_KeepsItsColour() =>
        Assert.Equal(High, new LinearColorMap("even", [Low, High]) { BadColor = Bad }.GetColor(1.0));

    // ---- the one place a renderer maps a data value -------------------------------------------------------------

    /// <summary>Normalizes like <see cref="LinearNormalizer"/>, but throws when it is handed NaN.</summary>
    private sealed class NaNRefusingNormalizer : INormalizer
    {
        public double Normalize(double value, double min, double max) =>
            double.IsNaN(value) ? throw new InvalidOperationException("NaN reached the normalizer") : (value - min) / (max - min);
    }

    [Fact]
    public void AMissingDataValue_NeverReachesTheNormalizer()
    {
        var map = new LinearColorMap("even", [Low, High]) { BadColor = Bad };

        var colour = map.ColorOf(double.NaN, new NaNRefusingNormalizer(), 0, 10);

        Assert.Equal(Bad, colour);
    }

    [Fact]
    public void ADataValue_IsNormalisedOverTheRange_ThenLookedUp()
    {
        var map = new LinearColorMap("even", [Low, High]);

        Assert.Equal(map.GetColor(0.5), map.ColorOf(5, LinearNormalizer.Instance, 0, 10));
    }

    /// <summary>Every built-in normalizer: <see cref="BoundaryNormalizer"/> turns NaN into its last bin,
    /// <see cref="SymLogNormalizer"/> throws on it (Math.Sign), <see cref="PowerNormNormalizer"/> with gamma 0 answers 1.
    /// A missing value is decided before any of them is asked.</summary>
    public static TheoryData<string, INormalizer> Normalizers { get; } = new()
    {
        { "linear", LinearNormalizer.Instance },
        { "log", new LogNormalizer() },
        { "two-slope", new TwoSlopeNormalizer(5) },
        { "boundary", new BoundaryNormalizer([0, 2, 4, 6, 8, 10]) },
        { "symlog", new SymLogNormalizer() },
        { "power gamma 0", new PowerNormNormalizer(0) },
        { "centred", new CenteredNormNormalizer(5) },
        { "no-norm", NoNormNormalizer.Instance },
    };

    [Theory]
    [MemberData(nameof(Normalizers))]
    public void AMissingDataValue_IsTheBadColour_UnderEveryBuiltInNormalizer(string name, INormalizer normalizer)
    {
        var map = new LinearColorMap("even", [Low, High]) { BadColor = Bad };

        var colour = map.ColorOf(double.NaN, normalizer, 0, 10);

        Assert.True(colour == Bad, $"{name}: a missing value came out {colour.ToHex()} alpha {colour.A}, not the bad colour.");
    }

    /// <summary>Infinity is not missing: it lies beyond the scale and is drawn in the end colour, as matplotlib draws an
    /// over-range value in the top colour when the map sets no over colour.</summary>
    [Fact]
    public void AnInfiniteDataValue_IsNotMissing_ItTakesTheEndColour()
    {
        var map = new LinearColorMap("even", [Low, High]) { BadColor = Bad };

        Assert.Equal(High, map.ColorOf(double.PositiveInfinity, LinearNormalizer.Instance, 0, 10));
        Assert.Equal(Low, map.ColorOf(double.NegativeInfinity, LinearNormalizer.Instance, 0, 10));
    }
}
