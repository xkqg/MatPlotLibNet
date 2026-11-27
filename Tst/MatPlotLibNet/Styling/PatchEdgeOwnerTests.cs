// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>The one rule that decides the edge of a patch (<see cref="Theme.PatchEdgeOf"/>): the theme's matplotlib rule where the theme
/// draws patches as matplotlib does, the kind's own edge where it does not, and nothing else. What each kind draws is pinned
/// picture by picture in <c>PatchEdgePinTests</c>; these hold the join itself and the list of kinds that take part.</summary>
public class PatchEdgeOwnerTests
{
    private static readonly Color Fill = Color.FromHex("#B0A001");
    private static readonly Color ThemeEdge = Color.FromHex("#00AA00");
    private static readonly double OnePoint = 1.0.PointsInPixels;

    /// <summary>A kind with values of its own, to see what the join hands to whom.</summary>
    private sealed class Probe(GivenPatchEdge given, PatchEdge library) : IEdgedPatch
    {
        public Color? SeenFill { get; private set; }
        public Color? SeenThemeEdge { get; private set; }
        public int LibraryAsked { get; private set; }
        public int GivenAsked { get; private set; }

        GivenPatchEdge IEdgedPatch.GivenEdge
        {
            get
            {
                GivenAsked++;
                return given;
            }
        }

        PatchEdge IEdgedPatch.LibraryEdge(Color? fill, Color? themeEdge)
        {
            LibraryAsked++;
            SeenFill = fill;
            SeenThemeEdge = themeEdge;
            return library;
        }
    }

    /// <summary>A kind that says nothing: the defaults of the interface.</summary>
    private sealed class Silent : IEdgedPatch;

    private static readonly PatchEdge Own = new(Colors.Magenta, 7);

    [Fact]
    public void UnderTheMatplotlibThemes_TheThemeDecides_AndTheKindIsNotAskedForItsOwnEdge()
    {
        var probe = new Probe(default, Own);

        Assert.Equal(new PatchEdge(Colors.Black, OnePoint), Theme.MatplotlibClassic.PatchEdgeOf(probe, Fill));
        Assert.Equal(PatchEdge.None, Theme.MatplotlibV2.PatchEdgeOf(probe, Fill));
        Assert.Equal(0, probe.LibraryAsked);
    }

    [Fact]
    public void WhatTheAuthorGave_BeatsTheThemesEdge_ColourAndWidthEach()
    {
        var colour = new Probe(new GivenPatchEdge(Colors.Red, null), Own);
        var width = new Probe(new GivenPatchEdge(null, 3), Own);
        var none = new Probe(new GivenPatchEdge(null, 0), Own);

        Assert.Equal(new PatchEdge(Colors.Red, OnePoint), Theme.MatplotlibV2.PatchEdgeOf(colour));
        Assert.Equal(new PatchEdge(Colors.Black, 3), Theme.MatplotlibClassic.PatchEdgeOf(width));
        Assert.Equal(PatchEdge.None, Theme.MatplotlibV2.PatchEdgeOf(width));
        Assert.Equal(PatchEdge.None, Theme.MatplotlibClassic.PatchEdgeOf(none));
    }

    [Fact]
    public void UnderALibraryTheme_TheKindsOwnEdgeIsDrawn_GivenTheFillAndTheThemesPatchEdgeColour()
    {
        var probe = new Probe(new GivenPatchEdge(Colors.Red, 3), Own);
        var theme = new Theme("with an edge colour", Theme.Default) { PatchEdgeColor = ThemeEdge };

        Assert.Equal(Own, theme.PatchEdgeOf(probe, Fill));
        Assert.Equal(Fill, probe.SeenFill);
        Assert.Equal(ThemeEdge, probe.SeenThemeEdge);

        Assert.Equal(Own, Theme.Default.PatchEdgeOf(probe));
        Assert.Null(probe.SeenFill);
        Assert.Null(probe.SeenThemeEdge);
        Assert.Equal(2, probe.LibraryAsked);
    }

    /// <summary>The opacity given to a patch reaches its edge under matplotlib (<c>to_rgba(color, alpha)</c>); the edge a theme decides is
    /// made that opaque, one that is none stays none, and the kind's own edge is left as it is.</summary>
    [Fact]
    public void TheOpacityGivenToAPatch_ReachesTheEdgeTheThemeDecides_AndNoOther()
    {
        var probe = new Probe(new GivenPatchEdge(null, null, 0.3), Own);
        var red = new Probe(new GivenPatchEdge(Colors.Red, null, 0.04), Own);
        var none = new Probe(new GivenPatchEdge(Colors.Transparent, null, 0.3), Own);

        Assert.Equal(new PatchEdge(Colors.Black.WithAlpha(76), OnePoint), Theme.MatplotlibClassic.PatchEdgeOf(probe, Fill));
        Assert.Equal(new PatchEdge(Colors.Red.WithAlpha(10), OnePoint), Theme.MatplotlibV2.PatchEdgeOf(red, Fill));
        Assert.Equal(PatchEdge.None, Theme.MatplotlibV2.PatchEdgeOf(probe, Fill));
        Assert.Equal(new PatchEdge(Colors.Transparent, OnePoint), Theme.MatplotlibClassic.PatchEdgeOf(none, Fill));
        Assert.Equal(Own, Theme.Default.PatchEdgeOf(probe, Fill));
    }

    /// <summary>A series that draws many shapes (a treemap's tiles, a table's cells) resolves its edge once: the kind is asked for what
    /// its author gave once, and every shape then takes the same edge a single ask gives.</summary>
    [Fact]
    public void AResolvedEdge_AsksTheKindOnce_AndGivesEveryShapeTheEdgeASingleAskGives()
    {
        var probe = new Probe(new GivenPatchEdge(Colors.Red, null, 0.3), Own);
        var classic = Theme.MatplotlibClassic.PatchEdgesOf(probe);
        var library = Theme.Default.PatchEdgesOf(probe);

        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(Theme.MatplotlibClassic.PatchEdgeOf(probe, Fill), classic.Of(Fill));
            Assert.Equal(Theme.Default.PatchEdgeOf(probe, Fill), library.Of(Fill));
        }

        Assert.Equal(new PatchEdge(Colors.Red.WithAlpha(76), OnePoint), classic.Of(Fill));
        Assert.Equal(Own, library.Of(Fill));
        var once = new Probe(default, Own);
        var resolved = Theme.MatplotlibV2.PatchEdgesOf(once);
        resolved.Of(Fill);
        resolved.Of(null);
        Assert.Equal(1, once.GivenAsked);
    }

    [Fact]
    public void AKindThatSaysNothing_GivesNoEdgeAndDrawsNoneUnderALibraryTheme()
    {
        Assert.Equal(default, ((IEdgedPatch)new Silent()).GivenEdge);
        Assert.Equal(PatchEdge.None, ((IEdgedPatch)new Silent()).LibraryEdge(Fill, ThemeEdge));
        Assert.Equal(PatchEdge.None, Theme.Default.PatchEdgeOf(new Silent(), Fill));
        Assert.Equal(new PatchEdge(Colors.Black, OnePoint), Theme.MatplotlibClassic.PatchEdgeOf(new Silent()));
    }

    [Fact]
    public void TheLibrarysEdges_AreValuesOfPatchEdge()
    {
        Assert.Equal(new PatchEdge(Colors.White, 1), PatchEdge.WhitePixel);
        Assert.Equal(new PatchEdge(Fill, 1), PatchEdge.Pixel(Fill));
        Assert.Equal(new PatchEdge(Fill, 0.5), PatchEdge.HalfPixel(Fill));
        Assert.Equal(new PatchEdge(null, 1), PatchEdge.Pixel(null));
    }

    // ---- who takes part --------------------------------------------------------------------------------------------------------

    /// <summary>Every kind of patch series that is drawn with an edge takes part, and so does every shape of <c>AddPatch</c>, each by an
    /// explicit member: the join is internal, so a public type gains no public member (the surface of the release is unchanged).</summary>
    [Fact]
    public void TheKindsThatTakePart_AreHeldAsAList_AndAddNoPublicMember()
    {
        var kinds = typeof(ChartSeries).Assembly.GetTypes().Where(t => typeof(IEdgedPatch).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false })
            .Select(t => t.Name).Order().ToArray();

        Assert.Equal(
        [
            nameof(Arc), nameof(AreaSeries), nameof(Arrow), nameof(Bar3DSeries), nameof(BarSeries), nameof(BrokenBarSeries), nameof(Circle), nameof(ContourfSeries), nameof(ContourSeries),
            nameof(CountSeries), nameof(DonutSeries),
            nameof(Ellipse), nameof(GanttSeries), nameof(HistogramSeries), nameof(PieSeries), nameof(PlanarBar3DSeries), nameof(PolarBarSeries),
            nameof(Polygon), nameof(Rectangle), nameof(StackedAreaSeries), nameof(TableSeries), nameof(TreemapSeries),
            nameof(ViolinSeries), nameof(VoxelSeries), nameof(WaterfallSeries), nameof(Wedge),
        ], kinds);

        var leaked = typeof(ChartSeries).Assembly.GetTypes().Where(t => typeof(IEdgedPatch).IsAssignableFrom(t) && t.IsClass)
            .SelectMany(t => t.GetMembers(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly))
            .Where(m => m.Name is nameof(IEdgedPatch.GivenEdge) or nameof(IEdgedPatch.LibraryEdge) or nameof(IMatplotlibArtist.Artist)).ToArray();
        Assert.Empty(leaked);
    }
}
