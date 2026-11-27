// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Interpolation;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Tests;

/// <summary>Holds what the public name registries promise a caller, one rule for all of them: a name the registry does
/// not know answers <see langword="null"/>, a null name is refused, the three name tables that ignore case do, the series
/// table does not, and a second registration under a name replaces the first while the first spelling stays in the list.
/// The six registries share one table underneath; these tests say what the facades above it keep.</summary>
public class NamedRegistryFacadeTests
{
    private static StyleSheet Sheet(string name) => new(name, new Dictionary<string, object>());

    // ── The three name tables that ignore case ───────────────────────────────

    /// <summary>The interpolation and the style-sheet registry (the colour-map one answers an unknown name with null, and has its own fact).</summary>
    public static TheoryData<Func<string, object?>, Func<IEnumerable<string>>, string, string> CaseBlindRegistries => new()
    {
        { name => InterpolationRegistry.Get(name), () => InterpolationRegistry.Names, "nearest", "bicubic" },
        { name => StyleSheetRegistry.Get(name), () => StyleSheetRegistry.Names, "ggplot", "seaborn" },
    };

    [Theory]
    [MemberData(nameof(CaseBlindRegistries))]
    public void NamesIgnoreCase_AndTheBuiltInsAreListed(Func<string, object?> get, Func<IEnumerable<string>> names, string known, string builtIn)
    {
        Assert.Same(get(known), get(known.ToUpperInvariant()));
        Assert.Contains(builtIn, names(), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Each registry refuses a null name, both when it is asked for one and when one is registered under it.</summary>
    public static TheoryData<Action, Action> NullNames => new()
    {
        { () => InterpolationRegistry.Get(null!), () => InterpolationRegistry.Register(null!, NearestInterpolation.Instance) },
        { () => ColorMapRegistry.Get(null!), () => ColorMapRegistry.Register(null!, ColorMaps.Viridis) },
        { () => StyleSheetRegistry.Get(null!), () => StyleSheetRegistry.Register(null!, Sheet("x")) },
    };

    [Theory]
    [MemberData(nameof(NullNames))]
    public void ANullName_IsRefused(Action get, Action register)
    {
        Assert.Throws<ArgumentNullException>(get);
        Assert.Throws<ArgumentNullException>(register);
    }

    // ── InterpolationRegistry ────────────────────────────────────────────────

    [Fact]
    public void Interpolation_ASecondRegistrationUnderAnotherSpelling_Replaces_AndTheFirstSpellingStays()
    {
        InterpolationRegistry.Register("Ns3-Interp", NearestInterpolation.Instance);
        InterpolationRegistry.Register("NS3-INTERP", BilinearInterpolation.Instance);

        Assert.Same(BilinearInterpolation.Instance, InterpolationRegistry.Get("ns3-interp"));
        Assert.Equal(["Ns3-Interp"], InterpolationRegistry.Names.Where(n => n.Equals("ns3-interp", StringComparison.OrdinalIgnoreCase)));
    }

    // ── ColorMapRegistry ─────────────────────────────────────────────────────

    [Fact]
    public void ColorMap_NamesIgnoreCase_AndAnUnknownNameAnswersNull()
    {
        Assert.Same(ColorMapRegistry.Get("viridis"), ColorMapRegistry.Get("VIRIDIS"));
        Assert.Null(ColorMapRegistry.Get("ns3-no-such-map"));
    }

    /// <summary>The registry is process-wide and keeps a name for good (there is no removal), so the maps registered here are
    /// the test's own, never a map the library ships: a shipped map under a name of its own would read, to every other test of the
    /// process, as a map the library ships under that name (<c>MatplotlibColorTablesTests</c> holds each to a matplotlib table).</summary>
    [Fact]
    public void ColorMap_ASecondRegistrationUnderAnotherSpelling_Replaces_AndTheFirstSpellingStays()
    {
        var first = new ReversedColorMap(ColorMaps.Viridis);
        var second = new ReversedColorMap(ColorMaps.Plasma);
        ColorMapRegistry.Register("Ns3-Map", first);
        ColorMapRegistry.Register("NS3-MAP", second);

        Assert.Same(second, ColorMapRegistry.Get("ns3-map"));
        Assert.False(ColorMapRegistry.IsShipped(second));
        Assert.Equal(["Ns3-Map"], ColorMapRegistry.Names.Where(n => n.Equals("ns3-map", StringComparison.OrdinalIgnoreCase)));
    }

    // ── StyleSheetRegistry ───────────────────────────────────────────────────

    [Fact]
    public void StyleSheet_ASecondRegistrationUnderAnotherSpelling_Replaces_AndTheFirstSpellingStays()
    {
        var second = Sheet("second");
        StyleSheetRegistry.Register("Ns3-Sheet", Sheet("first"));
        StyleSheetRegistry.Register("NS3-SHEET", second);

        Assert.Same(second, StyleSheetRegistry.Get("ns3-sheet"));
        Assert.Equal(["Ns3-Sheet"], StyleSheetRegistry.Names.Where(n => n.Equals("ns3-sheet", StringComparison.OrdinalIgnoreCase)));
    }

    // ── SeriesRegistry ───────────────────────────────────────────────────────

    [Fact]
    public void Series_DiscriminatorsAreOrdinal_SoTheSpellingMatters()
    {
        var axes = new Axes();
        var dto = new SeriesDto { XData = [0.0, 1.0], YData = [0.0, 1.0] };

        Assert.NotNull(SeriesRegistry.Create("line", axes, dto));
        Assert.Null(SeriesRegistry.Create("LINE", axes, dto));
        Assert.Null(SeriesRegistry.Create("ns3-no-such-series", axes, dto));
    }

    [Fact]
    public void Series_ANullDiscriminator_IsRefused()
    {
        Assert.Throws<ArgumentNullException>(() => SeriesRegistry.Create(null!, new Axes(), new SeriesDto { XData = [0.0, 1.0], YData = [0.0, 1.0] }));
        Assert.Throws<ArgumentNullException>(() => SeriesRegistry.Register(null!, (_, _) => null));
    }

    // ── FigureExtensions transform registry ──────────────────────────────────

    [Theory]
    [InlineData(".NS3Dot")]
    [InlineData("ns3dot")]
    public void Transform_AnExtensionIsKeptLowerCase_WithOrWithoutItsDot(string registeredAs)
    {
        string path = Path.Combine(Path.GetTempPath(), $"ns3-{Guid.NewGuid():N}.ns3dot");
        FigureExtensions.RegisterTransform(registeredAs, new RecordingFigureTransform("from-registry"));
        try
        {
            new Figure().Save(path);

            Assert.Equal("from-registry", File.ReadAllText(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Transform_ARegistrationOverAnExtensionReplacesTheEarlierOne()
    {
        string path = Path.Combine(Path.GetTempPath(), $"ns3-{Guid.NewGuid():N}.ns3twice");
        FigureExtensions.RegisterTransform(".ns3twice", new RecordingFigureTransform("first"));
        FigureExtensions.RegisterTransform(".NS3TWICE", new RecordingFigureTransform("second"));
        try
        {
            new Figure().Save(path);

            Assert.Equal("second", File.ReadAllText(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    // ── AxesRenderer factory registry ────────────────────────────────────────

    [Theory]
    [InlineData(CoordinateSystem.Cartesian, typeof(CartesianAxesRenderer))]
    [InlineData(CoordinateSystem.Polar, typeof(PolarAxesRenderer))]
    [InlineData(CoordinateSystem.ThreeD, typeof(ThreeDAxesRenderer))]
    [InlineData(CoordinateSystem.Ternary, typeof(TernaryAxesRenderer))]
    public void Renderer_EachCoordinateSystemHasItsOwnRendererFromTheStart(CoordinateSystem system, Type expected)
    {
        var axes = new Axes { CoordinateSystem = system };

        var renderer = AxesRenderer.Create(axes, new Rect(0, 0, 100, 100), new SvgRenderContext(), Theme.Default);

        Assert.IsType(expected, renderer);
    }
}
