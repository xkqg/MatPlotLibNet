// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Serialization;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>How opaque a series is drawn has one rule and one entry: the opacity its author gave, else the number its theme names for
/// a series of its kind, else the kind's own (<c>OpacityUnder</c>). The kinds differ by a value, not by an entry of their own: a fill (an
/// area, a stack, a polar bar) takes the theme's number where the theme draws patches as matplotlib draws collections (python, matplotlib
/// 3.11.2: <c>fill_between</c>, <c>stackplot</c> and <c>bar</c> given no alpha are opaque under both of its styles), a surface and a
/// triangulated surface take the number a theme names for them wherever it names one (its <c>surfaceAlpha</c>), and a dot takes the one
/// its dot look names (seaborn's).</summary>
public class OpacityUnderTests
{
    private static readonly Theme[] Themes = [Theme.Default, Theme.Dark, Theme.MatplotlibV2, Theme.MatplotlibClassic];

    /// <summary>A theme that names an opacity for a surface and draws patches as the library does.</summary>
    private static readonly Theme SurfaceOnly = new("surface only", Theme.Default)
    {
        Looks = ThemeLooks.Library with { Patch = new PatchLook { Alpha = 0.5 } },
    };

    /// <summary>A theme that draws patches as matplotlib draws collections and names an opacity for them.</summary>
    private static readonly Theme CollectionsAtHalf = new("collections at half", Theme.Default)
    {
        Looks = ThemeLooks.Library with { Patch = new PatchLook { LikeCollections = true, Alpha = 0.5 } },
    };

    private static T Series<T>(Action<T>? configure = null) where T : class, ISeries
    {
        var series = (T)DrawnSeriesFixtures.Instance(typeof(T).Name);
        configure?.Invoke(series);
        return series;
    }

    // ── the author's number wins, under every theme ─────────────────────────────────────────────────────────────

    [Fact]
    public void AnOpacityThatIsGiven_IsDrawnAsGiven_UnderEveryTheme_ForEveryKind()
    {
        var area = Series<AreaSeries>(s => s.Alpha = 0.4);
        var stack = Series<StackedAreaSeries>(s => s.Alpha = 0.4);
        var bar = Series<PolarBarSeries>(s => s.Alpha = 0.4);
        var surface = Series<SurfaceSeries>(s => s.Alpha = 0.4);
        var trisurf = Series<Trisurf3DSeries>(s => s.Alpha = 0.4);
        IGivenAlpha strip = Series<StripplotSeries>(s => s.Alpha = 0.4);
        IGivenAlpha swarm = Series<SwarmplotSeries>(s => s.Alpha = 0.4);

        foreach (var theme in Themes.Append(SurfaceOnly).Append(CollectionsAtHalf))
        {
            foreach (IGivenAlpha kind in new IGivenAlpha[] { area, stack, bar, surface, trisurf, strip, swarm })
            {
                Assert.Equal(0.4, kind.OpacityUnder(theme));
            }
        }
    }

    // ── a fill: the theme's number where it draws patches as collections, else the kind's own ─────────────────────

    [Theory]
    [InlineData(nameof(AreaSeries), 0.3)]
    [InlineData(nameof(StackedAreaSeries), 0.7)]
    [InlineData(nameof(PolarBarSeries), 0.8)]
    public void AFillThatIsGivenNone_RestsUnderTheLibrarysThemes_AndIsOpaqueUnderMatplotlibs(string kind, double resting)
    {
        var fill = (IGivenAlpha)DrawnSeriesFixtures.Instance(kind);

        Assert.Null(fill.GivenAlpha);
        Assert.Equal(resting, fill.RestingAlpha);
        Assert.Equal(resting, fill.OpacityUnder(Theme.Default));
        Assert.Equal(resting, fill.OpacityUnder(Theme.Dark));
        Assert.Equal(1.0, fill.OpacityUnder(Theme.MatplotlibV2));
        Assert.Equal(1.0, fill.OpacityUnder(Theme.MatplotlibClassic));
    }

    [Fact]
    public void AFill_TakesTheNumberAThemeNamesForCollections_ButNotOneItNamesOnlyForSurfaces()
    {
        var fill = Series<AreaSeries>();

        Assert.Equal(0.5, fill.OpacityUnder(CollectionsAtHalf));
        Assert.Equal(AreaSeries.RestingAlpha, fill.OpacityUnder(SurfaceOnly));
    }

    // ── a surface: the number the theme names for it wherever it names one, else the kind's own ───────────────────

    [Theory]
    [InlineData(nameof(SurfaceSeries))]
    [InlineData(nameof(Trisurf3DSeries))]
    public void ASurfaceThatIsGivenNone_RestsUnderTheLibrarysThemes_AndIsOpaqueUnderMatplotlibs(string kind)
    {
        var surface = (IGivenAlpha)DrawnSeriesFixtures.Instance(kind);

        Assert.Null(surface.GivenAlpha);
        Assert.Equal(PatchLook.RestingAlpha, surface.RestingAlpha);
        Assert.Equal(PatchLook.RestingAlpha, surface.OpacityUnder(Theme.Default));
        Assert.Equal(1.0, surface.OpacityUnder(Theme.MatplotlibV2));
        Assert.Equal(1.0, surface.OpacityUnder(Theme.MatplotlibClassic));
    }

    /// <summary>A surface's opacity rule is stated once, by the surface interface, not copied into each kind of surface: a kind states
    /// only the opacity its author gave.</summary>
    [Theory]
    [InlineData(typeof(SurfaceSeries))]
    [InlineData(typeof(Trisurf3DSeries))]
    public void EverySurface_TakesItsOpacityRuleFromTheSurfaceInterface_AndStatesOnlyWhatItWasGiven(Type kind)
    {
        Assert.True(typeof(ISurfaceAlpha).IsAssignableFrom(kind));
        var stated = kind.GetInterfaceMap(typeof(IGivenAlpha)).TargetMethods.Where(m => m.DeclaringType == kind).Select(m => m.Name).ToList();
        Assert.Equal(["MatPlotLibNet.Styling.IGivenAlpha.get_GivenAlpha"], stated);
    }

    [Fact]
    public void ASurface_TakesTheNumberATheme_NamesForSurfaces_EvenWhereItDrawsPatchesAsTheLibraryDoes()
    {
        var surface = Series<SurfaceSeries>();

        Assert.Equal(0.5, surface.OpacityUnder(SurfaceOnly));
        Assert.Equal(0.5, surface.OpacityUnder(CollectionsAtHalf));
    }

    // ── a dot: the number its dot look names ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void ADotThatIsGivenNone_RestsUnderTheLibrarysThemes_AndTakesSeabornsUnderMatplotlibs()
    {
        IGivenAlpha dots = Series<StripplotSeries>();

        Assert.Null(dots.GivenAlpha);
        Assert.Equal(0.8, dots.RestingAlpha);
        Assert.Equal(0.8, dots.OpacityUnder(Theme.Default));
        Assert.Equal(1.0, dots.OpacityUnder(Theme.MatplotlibV2));
    }

    // ── the guard: a series that holds an opacity it may not be given is read through the one rule ────────────────

    [Fact]
    public void EverySeriesThatKeepsWhetherItsOpacityWasGiven_ReadsItThroughTheOneRule()
    {
        var keepers = typeof(ISeries).Assembly.GetTypes()
            .Where(t => typeof(ISeries).IsAssignableFrom(t) && !t.IsAbstract)
            .Where(t => t.GetProperty("GivenAlpha", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) is not null)
            .Where(t => !typeof(IGivenAlpha).IsAssignableFrom(t))
            .Select(t => t.Name)
            .ToList();

        Assert.Empty(keepers);
    }
}
