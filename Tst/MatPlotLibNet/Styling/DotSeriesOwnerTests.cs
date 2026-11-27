// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>The one rule that decides the opacity, the jitter and the size of a dot: what its author named, else what the theme's dot
/// look says (<see cref="DotLook"/>, seaborn's numbers as values), else what the kind rests at. What each kind draws is pinned
/// picture by picture in <c>DotSizeTests</c>, <c>DotAlphaTests</c> and <c>DotJitterTests</c>; these hold the merge itself, the values
/// of the two looks and the list of kinds that take part.</summary>
public class DotSeriesOwnerTests
{
    private static readonly double FivePoints = 5.0.PointsInPixels;

    /// <summary>A kind with values of its own, to see what the block hands to whom.</summary>
    private sealed class Probe(GivenDot given, RestingDot resting, double seaborn) : IDotSeries
    {
        public int SeabornAsked { get; private set; }
        public DotLook? SeenLook { get; private set; }
        public GivenDot Given => given;
        public RestingDot Resting => resting;

        public double SeabornDiameter(SeriesSlot slot, DotLook look)
        {
            SeabornAsked++;
            SeenLook = look;
            return seaborn;
        }
    }

    /// <summary>A kind that says nothing of its seaborn size: the default of the interface.</summary>
    private sealed class Plain(GivenDot given, RestingDot resting) : IDotSeries
    {
        public GivenDot Given => given;
        public RestingDot Resting => resting;
    }

    private static readonly RestingDot Rests = new(4, 0.8, 0.2);

    [Fact]
    public void TheLibraryLook_NamesNoNumber_AndSeabornsNamesEveryOne()
    {
        var library = DotLook.Library;
        Assert.False(library.LikeSeaborn);
        Assert.Null(library.Alpha);
        Assert.Null(library.Jitter);
        Assert.Null(library.Diameter);
        Assert.Null(library.MeanPerLine);
        Assert.Null(library.CapSize);
        Assert.False(library.JoinsPoints);

        var seaborn = DotLook.Seaborn;
        Assert.True(seaborn.LikeSeaborn);
        Assert.Equal(1.0, seaborn.Alpha);
        Assert.Equal(0.1, seaborn.Jitter);
        Assert.Equal(FivePoints, seaborn.Diameter);
        Assert.Equal(Math.Sqrt(2 * Math.PI), seaborn.MeanPerLine);
        Assert.Equal(0.0, seaborn.CapSize);
        Assert.True(seaborn.JoinsPoints);
    }

    [Fact]
    public void TheTwoMatplotlibThemes_WearSeabornsLook_AndEveryOtherThemeTheLibrarys()
    {
        Assert.Same(DotLook.Seaborn, Theme.MatplotlibV2.Looks.Dots);
        Assert.Same(DotLook.Seaborn, Theme.MatplotlibClassic.Looks.Dots);
        Assert.Same(DotLook.Library, Theme.Default.Looks.Dots);
        Assert.Same(DotLook.Library, Theme.Dark.Looks.Dots);
    }

    [Fact]
    public void ANumberThatIsGiven_IsDrawnAsGiven_UnderEveryTheme_AndTheKindIsNotAskedForSeabornsSize()
    {
        var kind = new Probe(new GivenDot(9, 0.4, 0.3), Rests, 7);

        foreach (var theme in new[] { Theme.MatplotlibV2, Theme.MatplotlibClassic, Theme.Default })
        {
            Assert.Equal(9, kind.DiameterUnder(theme, theme.SeriesSlot(0)));
            Assert.Equal(0.4, kind.OpacityUnder(theme));
            Assert.Equal(0.3, kind.JitterUnder(theme));
        }

        Assert.Equal(0, kind.SeabornAsked);
    }

    [Fact]
    public void ANumberThatIsNotGiven_IsSeabornsUnderTheMatplotlibThemes()
    {
        var kind = new Probe(default, Rests, 7);

        Assert.Equal(7, kind.DiameterUnder(Theme.MatplotlibV2, Theme.MatplotlibV2.SeriesSlot(0)));
        Assert.Same(Theme.MatplotlibV2.Looks.Dots, kind.SeenLook);
        Assert.Equal(1, kind.SeabornAsked);
        Assert.Equal(1.0, kind.OpacityUnder(Theme.MatplotlibClassic));
        Assert.Equal(0.1, kind.JitterUnder(Theme.MatplotlibClassic));
    }

    [Fact]
    public void ANumberThatIsNotGiven_IsTheKindsOwnUnderALibraryTheme_AndSeabornsSizeIsNotAsked()
    {
        var kind = new Probe(default, Rests, 7);

        Assert.Equal(4, kind.DiameterUnder(Theme.Default, Theme.Default.SeriesSlot(0)));
        Assert.Equal(0.8, kind.OpacityUnder(Theme.Default));
        Assert.Equal(0.2, kind.JitterUnder(Theme.Default));
        Assert.Equal(0, kind.SeabornAsked);
    }

    [Fact]
    public void AGivenPart_DoesNotMoveTheOthers()
    {
        var kind = new Probe(new GivenDot(Alpha: 0.5), Rests, 7);

        Assert.Equal(7, kind.DiameterUnder(Theme.MatplotlibV2, Theme.MatplotlibV2.SeriesSlot(0)));
        Assert.Equal(0.5, kind.OpacityUnder(Theme.MatplotlibV2));
        Assert.Equal(0.1, kind.JitterUnder(Theme.MatplotlibV2));
        Assert.Equal(4, kind.DiameterUnder(Theme.Default, Theme.Default.SeriesSlot(0)));
    }

    [Fact]
    public void AKindThatSaysNothingOfItsSeabornSize_IsSizedByTheLooksDiameter()
    {
        var kind = new Plain(default, Rests);

        Assert.Equal(FivePoints, kind.DiameterUnder(Theme.MatplotlibV2, Theme.MatplotlibV2.SeriesSlot(0)));
        Assert.Equal(4, kind.DiameterUnder(Theme.Default, Theme.Default.SeriesSlot(0)));
    }

    [Fact]
    public void ALookThatIsSeabornsButNamesNoNumber_FallsBackToTheKindsOwn()
    {
        var theme = new Theme("bare seaborn", Theme.Default) { Looks = ThemeLooks.Library with { Dots = new DotLook { LikeSeaborn = true } } };
        var kind = new Plain(default, Rests);

        Assert.Equal(0.8, kind.OpacityUnder(theme));
        Assert.Equal(0.2, kind.JitterUnder(theme));
        Assert.Equal(4, kind.DiameterUnder(theme, theme.SeriesSlot(0)));
    }

    [Fact]
    public void ThePointPlotsOwnSize_IsItsLineWidthTimesSqrtTwoPi_AndTheScatterKindsAreSizedByScatter()
    {
        var slot = Theme.MatplotlibV2.SeriesSlot(0);
        var point = new PointplotSeries([[1.0, 2.0]]);
        var residual = new ResidualSeries(new double[] { 1, 2, 3 }, new double[] { 1, 2, 3 });
        var pair = new PairGridSeries([[1.0, 2.0], [2.0, 1.0]]);

        Assert.Equal(slot.LineWidthOf(null, DotLook.Seaborn.PointplotLine) * Math.Sqrt(2 * Math.PI), ((IDotSeries)point).SeabornDiameter(slot, DotLook.Seaborn));
        Assert.Equal(slot.ScatterDiameter, ((IDotSeries)residual).SeabornDiameter(slot, DotLook.Seaborn));
        Assert.Equal(slot.ScatterDiameter, ((IDotSeries)pair).SeabornDiameter(slot, DotLook.Seaborn));
        Assert.Equal(FivePoints, ((IDotSeries)new StripplotSeries([[1.0]])).SeabornDiameter(slot, DotLook.Seaborn));
        Assert.Equal(FivePoints, ((IDotSeries)new SwarmplotSeries([[1.0]])).SeabornDiameter(slot, DotLook.Seaborn));
    }

    /// <summary>The diameter, in pixels, <c>Axes.scatter</c> draws a dot at where it is given no size: the theme's area across (6 points under
    /// the default style, sqrt(20) under classic), the slot's own resting size under a library theme.</summary>
    [Fact]
    public void TheScatterDiameter_IsTheSizeScatterDrawsAtWhereItIsGivenNone()
    {
        Assert.Equal(6.0.PointsInPixels, Theme.MatplotlibV2.SeriesSlot(0).ScatterDiameter, 9);
        Assert.Equal(Math.Sqrt(20).PointsInPixels, Theme.MatplotlibClassic.SeriesSlot(0).ScatterDiameter, 9);
        Assert.Equal(SeriesSlot.RestingScatterSize.PointsInPixels, Theme.Default.SeriesSlot(0).ScatterDiameter, 9);
    }

    [Fact]
    public void APairGridsRadius_IsHandedToTheBlockAsADiameter()
    {
        var pair = new PairGridSeries([[1.0, 2.0], [2.0, 1.0]]);
        var kind = (IDotSeries)pair;

        Assert.Equal(default, kind.Given);
        Assert.Equal(6.0, kind.Resting.Diameter);
        pair.MarkerSize = 2.5;
        Assert.Equal(5.0, kind.Given.Diameter);
        Assert.Equal(5.0, kind.DiameterUnder(Theme.Default, Theme.Default.SeriesSlot(0)));
        Assert.Equal(2.5, pair.MarkerSize);
    }

    /// <summary>Every kind of dot takes part, each by an explicit member: the block is internal, so a public type gains no public member
    /// (the surface of the release is unchanged).</summary>
    [Fact]
    public void TheKindsThatTakePart_AreHeldAsAList_AndAddNoPublicMember()
    {
        var kinds = typeof(ChartSeries).Assembly.GetTypes().Where(t => typeof(IDotSeries).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false })
            .Select(t => t.Name).Order().ToArray();

        Assert.Equal(
        [
            nameof(PairGridSeries), nameof(PointplotSeries), nameof(ResidualSeries), nameof(StripplotSeries), nameof(SwarmplotSeries),
        ], kinds);

        var leaked = typeof(ChartSeries).Assembly.GetTypes().Where(t => typeof(IDotSeries).IsAssignableFrom(t) && t.IsClass)
            .SelectMany(t => t.GetMembers(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly))
            .Where(m => m.Name is nameof(IDotSeries.Given) or nameof(IDotSeries.Resting) or nameof(IDotSeries.SeabornDiameter)).ToArray();
        Assert.Empty(leaked);
    }

    /// <summary>The parts a series was given are the parts its JSON carries, and a part it was not given stays absent: nothing the library
    /// rests at is written as if it were named.</summary>
    [Fact]
    public void TheGivenParts_AreTheOnesAKindWasGiven_AndTheRestingOnesAreNeverWritten()
    {
        var strip = new StripplotSeries([[1.0, 2.0]]);
        Assert.Equal(default, ((IDotSeries)strip).Given);
        Assert.Equal(new RestingDot(5, 0.8, 0.2), ((IDotSeries)strip).Resting);

        strip.Alpha = 0.8;
        strip.Jitter = 0.2;
        Assert.Equal(new GivenDot(null, 0.8, 0.2), ((IDotSeries)strip).Given);
        var dto = strip.ToSeriesDto();
        Assert.Equal(0.8, dto.Alpha);
        Assert.Equal(0.2, dto.Jitter);
        Assert.Null(dto.MarkerSize);

        Assert.Equal(new RestingDot(5, 0.8), ((IDotSeries)new SwarmplotSeries([[1.0]])).Resting);
        Assert.Equal(new RestingDot(8), ((IDotSeries)new PointplotSeries([[1.0]])).Resting);
        Assert.Equal(new RestingDot(6), ((IDotSeries)new ResidualSeries(new double[] { 1 }, new double[] { 1 })).Resting);
    }
}
