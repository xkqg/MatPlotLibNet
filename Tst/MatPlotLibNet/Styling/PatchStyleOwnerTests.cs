// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Serialization;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>The style of a patch-like shape (fill, edge and hatch) is made in one place: <c>ResolvedPatchEdge.StyleOf</c>, which joins what
/// the theme decides about the edge (<see cref="Theme.PatchEdgeOf"/>), the corner join of a shape that stands for a matplotlib collection
/// and the colour of its hatch (<see cref="PatchLook.HatchOver"/>). A renderer does not glue the resolved edge to the fill by hand, nor
/// name the theme's stroke look to say it draws a collection.</summary>
public class PatchStyleOwnerTests
{
    private static readonly Color Fill = Color.FromHex("#3366CC");
    private static readonly HatchFill Hatched = new(HatchPattern.ForwardDiagonal, null);

    private static IEdgedPatch Kind(string name) =>
        (IEdgedPatch)DrawnSeriesFixtures.Instance(name);

    // ── an edge is a value that makes its shape ─────────────────────────────────────────────────────────────────

    [Fact]
    public void AnEdge_MakesTheStyleOfAShapeFilledInAColour()
    {
        var style = new PatchEdge(Colors.Red, 2.5).StyleOf(Fill);

        Assert.Equal(new ShapeStyle(Fill, Colors.Red, 2.5), style);
        Assert.Equal(HatchPattern.None, style.Hatch);
        Assert.Equal(JoinStyle.Miter, style.StrokeJoin);
    }

    /// <summary>matplotlib's <c>Patch.draw</c>: a face of no opacity is no face unless the patch is hatched (the hatch is laid over a
    /// clear fill), and an edge of no opacity is no outline. One rule for every patch-like shape, a patch of AddPatch included.</summary>
    [Fact]
    public void AFaceOrAnEdgeOfNoOpacity_IsNone_ExceptAHatchedFaceStaysAClearFill()
    {
        var clear = new PatchEdge(Colors.Transparent, 2).StyleOf(Colors.Transparent);
        var hatched = new PatchEdge(Colors.Red, 2).StyleOf(Colors.Transparent, hatched: true);

        Assert.Null(clear.Fill);
        Assert.Null(clear.Stroke);
        Assert.Equal(Colors.Transparent, hatched.Fill);
        Assert.Equal(Colors.Red, hatched.Stroke);
    }

    [Fact]
    public void NoEdge_MakesAFilledShapeWithNoOutline()
    {
        var style = PatchEdge.None.StyleOf(Fill);

        Assert.Null(style.Stroke);
        Assert.False(style.HasVisibleStroke);
    }

    // ── what a theme decides about the edge, joined to the fill ─────────────────────────────────────────────────

    [Fact]
    public void ABar_IsEdgedAsItsThemeSays_AndKeepsTheMitreOfAPatch()
    {
        var bar = Kind(nameof(BarSeries));

        var library = Theme.Default.PatchEdgesOf(bar).StyleOf(Fill, Fill);
        var classic = Theme.MatplotlibClassic.PatchEdgesOf(bar).StyleOf(Fill, Fill);
        var v2 = Theme.MatplotlibV2.PatchEdgesOf(bar).StyleOf(Fill, Fill);

        Assert.Equal(Theme.Default.PatchEdgeOf(bar, Fill).Color, library.Stroke);
        Assert.Equal(Theme.Default.PatchEdgeOf(bar, Fill).Width, library.StrokeThickness);
        Assert.Equal(Colors.Black, classic.Stroke);
        Assert.Null(v2.Stroke);
        Assert.All(new[] { library, classic, v2 }, style => Assert.Equal(JoinStyle.Miter, style.StrokeJoin));
    }

    [Fact]
    public void TheFillIsTheOneGiven_WhileTheEdgeFollowsTheColourItIsAskedToFollow()
    {
        var body = Kind(nameof(ViolinSeries));
        var faded = Fill.WithOpacity(0.4);

        var style = Theme.Default.PatchEdgesOf(body).StyleOf(faded, follows: Fill);

        Assert.Equal(faded, style.Fill);
        Assert.Equal(PatchEdge.Pixel(Fill).Color, style.Stroke);
        Assert.Equal(1.0, style.StrokeThickness);
    }

    [Fact]
    public void AnEdgeThatIsAlreadyResolved_IsTakenAsItIs()
    {
        var bar = Kind(nameof(BarSeries));

        var style = Theme.MatplotlibClassic.PatchEdgesOf(bar).StyleOf(Fill, PatchEdge.None);

        Assert.Null(style.Stroke);
    }

    // ── a shape that stands for a collection takes the corner join of its theme ──────────────────────────────

    /// <summary>The kinds matplotlib draws as a <c>Collection</c> (printed with 3.11.2): <c>fill_between</c>, <c>stackplot</c> and a violin's
    /// bodies (<c>FillBetweenPolyCollection</c>), <c>broken_barh</c> (<c>PolyCollection</c>), <c>contour</c> and <c>contourf</c>
    /// (<c>ContourSet</c>, a <c>Collection</c>), <c>bar3d</c> and <c>voxels</c> (<c>Poly3DCollection</c>): what each is to matplotlib is its
    /// artist (<see cref="MatplotlibArtist.Collection"/>).</summary>
    [Theory]
    [InlineData(nameof(AreaSeries))]
    [InlineData(nameof(StackedAreaSeries))]
    [InlineData(nameof(ViolinSeries))]
    [InlineData(nameof(BrokenBarSeries))]
    [InlineData(nameof(ContourSeries))]
    [InlineData(nameof(ContourfSeries))]
    [InlineData(nameof(Bar3DSeries))]
    [InlineData(nameof(VoxelSeries))]
    public void AFillAndABody_AreCollections_JoinedRoundUnderMatplotlibsThemes_AndMitredUnderTheLibrarys(string kind)
    {
        var patch = Kind(kind);

        Assert.True(patch.Artist.Collection);
        Assert.Equal(JoinStyle.Round, Theme.MatplotlibV2.PatchEdgesOf(patch).StyleOf(Fill, Fill).StrokeJoin);
        Assert.Equal(JoinStyle.Round, Theme.MatplotlibClassic.PatchEdgesOf(patch).StyleOf(Fill, Fill).StrokeJoin);
        Assert.Equal(JoinStyle.Miter, Theme.Default.PatchEdgesOf(patch).StyleOf(Fill, Fill).StrokeJoin);
    }

    [Fact]
    public void EveryOtherKindOfPatchSeries_IsAPatch_AndKeepsItsMitreUnderEveryTheme()
    {
        var collections = new[] { nameof(AreaSeries), nameof(StackedAreaSeries), nameof(ViolinSeries), nameof(BrokenBarSeries),
            nameof(ContourSeries), nameof(ContourfSeries), nameof(Bar3DSeries), nameof(VoxelSeries) };
        var others = typeof(ChartSeries).Assembly.GetTypes()
            .Where(t => typeof(IEdgedPatch).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false } && typeof(ISeries).IsAssignableFrom(t))
            .Where(t => !collections.Contains(t.Name))
            .ToList();

        Assert.NotEmpty(others);
        foreach (var type in others)
        {
            var patch = Kind(type.Name);

            Assert.False(patch.Artist.Collection);
            Assert.Equal(JoinStyle.Miter, Theme.MatplotlibV2.PatchEdgesOf(patch).StyleOf(Fill, Fill).StrokeJoin);
        }
    }

    // ── the colour of a hatch ───────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AHatchThatNamesNoColour_WearsTheThemesRuleOverTheEdge()
    {
        var bar = Kind(nameof(BarSeries));
        var edged = new PatchEdge(Colors.Red, 1);

        var v2 = Theme.MatplotlibV2.PatchEdgesOf(bar).StyleOf(Fill, edged, Hatched);
        var classic = Theme.MatplotlibClassic.PatchEdgesOf(bar).StyleOf(Fill, edged, Hatched);
        var library = Theme.Default.PatchEdgesOf(bar).StyleOf(Fill, edged, Hatched);
        var bare = Theme.MatplotlibV2.PatchEdgesOf(bar).StyleOf(Fill, PatchEdge.None, Hatched);

        Assert.Equal(HatchPattern.ForwardDiagonal, v2.Hatch);
        Assert.Equal(Colors.Red, v2.HatchColor);
        Assert.Equal(Colors.Black, classic.HatchColor);
        Assert.Null(library.HatchColor);
        Assert.Equal(Colors.Black, bare.HatchColor);
    }

    [Fact]
    public void AHatchColourThatIsNamed_IsDrawnAsNamed_UnderEveryTheme()
    {
        var bar = Kind(nameof(BarSeries));

        foreach (var theme in new[] { Theme.MatplotlibV2, Theme.MatplotlibClassic, Theme.Default })
        {
            var style = theme.PatchEdgesOf(bar).StyleOf(Fill, new PatchEdge(Colors.Red, 1), new HatchFill(HatchPattern.Cross, Colors.Green));

            Assert.Equal(Colors.Green, style.HatchColor);
        }
    }

    [Fact]
    public void NoHatch_LeavesTheShapeUnhatched_AndItsHatchColourUntouched()
    {
        var bar = Kind(nameof(BarSeries));

        var style = Theme.MatplotlibV2.PatchEdgesOf(bar).StyleOf(Fill, Fill, HatchFill.None);

        Assert.Equal(HatchPattern.None, style.Hatch);
        Assert.Null(style.HatchColor);
    }

    [Fact]
    public void TheOpacityGivenToAPatch_ReachesItsHatchColour_UnderTheMatplotlibThemes()
    {
        var area = (AreaSeries)DrawnSeriesFixtures.Instance(nameof(AreaSeries));
        area.Alpha = 0.5;

        var style = Theme.MatplotlibV2.PatchEdgesOf(area).StyleOf(Fill, new PatchEdge(Colors.Red, 1), Hatched);

        Assert.Equal(Colors.Red.WithOpacityGiven(0.5), style.HatchColor);
    }

    // ── the pair that makes a hatch ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AHatchFill_IsDrawnWhenItHasAPattern()
    {
        Assert.False(HatchFill.None.IsDrawn);
        Assert.False(new HatchFill(HatchPattern.None, Colors.Red).IsDrawn);
        Assert.True(new HatchFill(HatchPattern.Cross, null).IsDrawn);
    }

    [Fact]
    public void AHatchFill_IsWrittenAsItsPattern_OnlyWhenItIsDrawn()
    {
        Assert.Null(HatchFill.None.PatternOnTheWire);
        Assert.Equal(HatchPattern.Cross, new HatchFill(HatchPattern.Cross, Colors.Red).PatternOnTheWire);
    }

    [Fact]
    public void AShape_TakesAHatchFillAsItIs()
    {
        var style = ShapeStyle.Filled(Fill).WithHatch(new HatchFill(HatchPattern.Cross, Colors.Green));

        Assert.Equal(HatchPattern.Cross, style.Hatch);
        Assert.Equal(Colors.Green, style.HatchColor);
        Assert.Equal(Fill, style.Fill);
    }

    [Theory]
    [InlineData(nameof(BarSeries))]
    [InlineData(nameof(HistogramSeries))]
    [InlineData(nameof(AreaSeries))]
    [InlineData(nameof(StackedAreaSeries))]
    [InlineData(nameof(StatTileSeries))]
    public void ASeriesThatHatches_HoldsItsPatternAndColourAsOneValue_ThatItReadsAndWrites(string kind)
    {
        var series = (IHatched)DrawnSeriesFixtures.Instance(kind);

        series.LayHatching(new HatchFill(HatchPattern.Cross, Colors.Green));

        Assert.Equal(new HatchFill(HatchPattern.Cross, Colors.Green), series.Hatching);
        Assert.Equal(HatchPattern.Cross, series.Hatch);
        Assert.Equal(Colors.Green, series.HatchColor);
    }
}
