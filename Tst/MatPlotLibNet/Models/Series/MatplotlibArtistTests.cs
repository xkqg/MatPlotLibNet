// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using Xunit;
using MatPlotLibNet.Tests.Serialization;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>What a kind of series is to matplotlib, as one value: the call its colour is asked with, the zorder of the artist its verb
/// draws and where the theme takes its colour from. matplotlib 3.11.2 (<c>axes/_axes.py</c>, <c>lines.py</c>, <c>collections.py</c>,
/// <c>image.py</c>, <c>patches.py</c>) was read for the values: a <c>Line2D</c> has <c>zorder = 2</c>, a <c>Patch</c>, a
/// <c>Collection</c> and a <c>QuadMesh</c> 1, an <c>AxesImage</c> 0, a <c>LineCollection</c> 2, and <c>Quiver</c> 1 with its key at 1.1.</summary>
public class MatplotlibArtistTests
{
    /// <summary>Each artist beside what matplotlib makes of it: the call its colour is asked with, its zorder and the theme colour it takes.</summary>
    public static IEnumerable<object[]> Artists() =>
    [
        [new MatplotlibArtist(CycleCall.Line, 2, ThemeColorRole.None), MatplotlibArtist.Line],                       // a line is at two and asks the lines' cycle
        [new MatplotlibArtist(CycleCall.Line, 1, ThemeColorRole.None), MatplotlibArtist.Own],                        // a kind with no artist of its own asks the lines' cycle, at one
        [new MatplotlibArtist(CycleCall.Patch, 1, ThemeColorRole.None), MatplotlibArtist.Patch],                     // a patch asks the patch cycle, at one
        [new MatplotlibArtist(CycleCall.FillBetween, 1, ThemeColorRole.None) { Collection = true }, MatplotlibArtist.FillBetween],     // the patch cycle except in classic
        [new MatplotlibArtist(CycleCall.Scatter, 1, ThemeColorRole.ScatterFace) { Collection = true }, MatplotlibArtist.Scatter],      // the patch cycle except in classic, and the theme's scatter face
        [new MatplotlibArtist(CycleCall.Scatter, 1, ThemeColorRole.None) { Collection = true }, MatplotlibArtist.Dots],                // the dots of a strip or swarm plot ask like a scatter and take no colour of the theme's scatter look
        [new MatplotlibArtist(CycleCall.Neither, 1, ThemeColorRole.None) { Collection = true }, MatplotlibArtist.Mesh],                // a mesh asks no cycle, at one
        [new MatplotlibArtist(CycleCall.Neither, 0, ThemeColorRole.None), MatplotlibArtist.Image],                   // an image asks no cycle, at zero
        [new MatplotlibArtist(CycleCall.Neither, 2, ThemeColorRole.None) { Collection = true }, MatplotlibArtist.LineCollection],      // a line collection asks no cycle, at two
        [new MatplotlibArtist(CycleCall.Neither, 1, ThemeColorRole.VectorField) { Collection = true }, MatplotlibArtist.VectorField],  // a vector field asks no cycle, at one
        [new MatplotlibArtist(CycleCall.Neither, 1.1, ThemeColorRole.VectorField), MatplotlibArtist.VectorFieldKey],  // and its key stands above it
        [new MatplotlibArtist(CycleCall.Neither, 2, ThemeColorRole.BoxOutline), MatplotlibArtist.Box],               // a box plot asks no cycle, at two, and takes the theme's box outline
        [new MatplotlibArtist(CycleCall.Violin, 1, ThemeColorRole.ViolinBody) { Collection = true }, MatplotlibArtist.Violin],         // the lines' cycle except in classic, at one, and the theme's body
    ];

    [Theory]
    [MemberData(nameof(Artists))]
    public void AnArtist_IsAtItsZOrder_AndAsksItsCycle(object expected, object artist) =>
        Assert.Equal(expected, artist);

    /// <summary>Whether matplotlib cuts the artist off at the axes (printed with 3.11.2 under both styles): every artist an axes adds is
    /// <c>clip_on</c> True, a stackplot's layers too, a pie's wedges and a table are False; a kind of the library's own is cut off as every
    /// artist is (its names and values are text, which no clip cuts).</summary>
    [Fact]
    public void EveryArtistIsClipped_AKindOfTheLibrarysOwnToo_ButAPiesWedgesAndATable()
    {
        Assert.True(MatplotlibArtist.Line.ClipOn);
        Assert.True(MatplotlibArtist.Patch.ClipOn);
        Assert.True(MatplotlibArtist.Own.ClipOn);
        Assert.Equal(MatplotlibArtist.Patch with { ClipOn = false }, MatplotlibArtist.PieWedge);
        // A table also stands at 0 (printed with 3.11.2 under both styles: ax.table(...).get_zorder() is 0), under its axis.
        Assert.Equal(MatplotlibArtist.Own with { ZOrder = 0, ClipOn = false }, MatplotlibArtist.Table);
        Assert.Equal(MatplotlibArtist.StackPlot, new StackedAreaSeries([0.0, 1.0], [[1.0, 2.0]]).Artist);
        Assert.Equal(MatplotlibArtist.Line, new RelativeRotationSeries([[1.0, 2, 3]], [1.0, 1, 1], ["a"]).Artist);
    }

    [Fact]
    public void ANamedColour_LeavesBothCyclesAlone_AndKeepsTheRest()
    {
        var named = MatplotlibArtist.Scatter.NamingItsColours(true);

        Assert.Equal(new MatplotlibArtist(CycleCall.Neither, 1, ThemeColorRole.ScatterFace) { Collection = true }, named);
        Assert.Equal(MatplotlibArtist.Scatter, MatplotlibArtist.Scatter.NamingItsColours(false));
        Assert.Equal(MatplotlibArtist.Patch with { Call = CycleCall.Neither }, MatplotlibArtist.Patch.NamingItsColours(true));
        Assert.Equal(MatplotlibArtist.Own with { Call = CycleCall.Neither }, MatplotlibArtist.Own.NamingItsColours(true));
    }

    /// <summary>Whether matplotlib draws the artist as a <c>Collection</c> (printed with 3.11.2): a fill between two curves, a stackplot's
    /// layers and a violin's bodies are a <c>FillBetweenPolyCollection</c>, a mesh, a hexbin, a broken bar and a filled contour are
    /// collections, so are a scatter's <c>PathCollection</c>, a <c>LineCollection</c> and a <c>Quiver</c>; a <c>Line2D</c>, a
    /// <c>Rectangle</c>, a pie's <c>Wedge</c>, an <c>AxesImage</c>, a box's lines and a <c>QuiverKey</c> are not. A collection's outline
    /// takes the join of its theme, a patch keeps the mitre: the one fact a patch-like series' corners are joined by.</summary>
    [Fact]
    public void WhetherTheArtistIsACollection_IsAValueOfTheArtist()
    {
        Assert.All(new[] { MatplotlibArtist.FillBetween, MatplotlibArtist.StackPlot, MatplotlibArtist.Violin, MatplotlibArtist.Mesh,
            MatplotlibArtist.Scatter, MatplotlibArtist.Dots, MatplotlibArtist.LineCollection, MatplotlibArtist.VectorField,
            MatplotlibArtist.PatchFaces3D, MatplotlibArtist.LineFaces3D },
            artist => Assert.True(artist.Collection));
        Assert.Equal(MatplotlibArtist.Patch with { Collection = true }, MatplotlibArtist.PatchFaces3D);
        Assert.Equal(MatplotlibArtist.Own with { Collection = true }, MatplotlibArtist.LineFaces3D);
        Assert.All(new[] { MatplotlibArtist.Line, MatplotlibArtist.Own, MatplotlibArtist.Patch, MatplotlibArtist.PieWedge,
            MatplotlibArtist.Table, MatplotlibArtist.Image, MatplotlibArtist.Box, MatplotlibArtist.VectorFieldKey },
            artist => Assert.False(artist.Collection));
        Assert.Equal(MatplotlibArtist.Own with { Collection = true }, MatplotlibArtist.StackPlot);
    }

    /// <summary>The colour a theme gives a kind by its role (<see cref="ThemeColorRole"/>): every role names its own look, none names
    /// none, and inside a colour group only a violin's body keeps the theme's colour.</summary>
    [Fact]
    public void EveryRole_NamesItsOwnLook_AndOnlyAViolinsBodyKeepsItInAColourGroup()
    {
        var theme = Theme.MatplotlibV2;

        Assert.Null(SeriesSlot.ColorUnder(ThemeColorRole.None, theme, inColorGroup: false));
        Assert.Equal(theme.ViolinBodyColor, SeriesSlot.ColorUnder(ThemeColorRole.ViolinBody, theme, inColorGroup: false));
        Assert.Equal(theme.Looks.VectorField.Color, SeriesSlot.ColorUnder(ThemeColorRole.VectorField, theme, inColorGroup: false));
        Assert.Equal(theme.Looks.Scatter?.Face, SeriesSlot.ColorUnder(ThemeColorRole.ScatterFace, theme, inColorGroup: false));
        Assert.Equal(theme.Looks.Box.Outline.Color, SeriesSlot.ColorUnder(ThemeColorRole.BoxOutline, theme, inColorGroup: false));
        Assert.Equal(theme.ViolinBodyColor, SeriesSlot.ColorUnder(ThemeColorRole.ViolinBody, theme, inColorGroup: true));
        Assert.All(new[] { ThemeColorRole.VectorField, ThemeColorRole.ScatterFace, ThemeColorRole.BoxOutline },
            role => Assert.Null(SeriesSlot.ColorUnder(role, theme, inColorGroup: true)));
        Assert.Null(SeriesSlot.ColorUnder(((ThemeColorRole)99), theme, inColorGroup: false));
    }

    [Fact]
    public void ASeriesThatSaysNothing_IsTheKindWithNoArtistOfItsOwn()
    {
        var series = new BareSeries();

        Assert.Equal(MatplotlibArtist.Own, series.Artist);
        Assert.Equal(CycleCall.Line, series.CycleCall);
        Assert.Equal(1, series.ArtistZOrder);
    }

    [Fact]
    public void ThePatchesOfAnAxes_StandAtThePatchArtistsZOrder()
    {
        var patch = new Rectangle(new DataPoint(0, 0), 1, 1);

        Assert.Equal(MatplotlibArtist.Patch.ZOrder, patch.ZOrder);
    }

    [Fact]
    public void AnOverlayLine_StandsAtTheLinesZOrder_UnderTheMatplotlibThemesOnly()
    {
        Assert.Equal(MatplotlibArtist.Line.ZOrder, Theme.MatplotlibV2.OverlayLinesZ);
        Assert.Equal(MatplotlibArtist.Patch.ZOrder, Theme.MatplotlibV2.SpansZ);
        Assert.Equal(double.NegativeInfinity, Theme.Default.OverlayLinesZ);
        Assert.Equal(double.NegativeInfinity, Theme.Default.SpansZ);
    }

    /// <summary>Which kinds take a colour of the theme's own where their author names none: the rest take the slot's.</summary>
    [Fact]
    public void TheKindsThatTakeAColourOfTheThemesOwn_AreFiveFamilies()
    {
        var withARole = DrawnSeriesFixtures.DrawnTypes
            .Select(type => DrawnSeriesFixtures.Instance(type.Name))
            .OfType<ChartSeries>()
            .Where(s => s.Artist.ThemeColor != ThemeColorRole.None)
            .GroupBy(s => s.Artist.ThemeColor, s => s.GetType().Name)
            .ToDictionary(g => g.Key, g => g.Order().ToArray());

        Assert.Equal(["BoxSeries"], withARole[ThemeColorRole.BoxOutline]);
        Assert.Equal(["ViolinSeries"], withARole[ThemeColorRole.ViolinBody]);
        Assert.Equal(["BarbsSeries", "QuiverKeySeries", "QuiverSeries"], withARole[ThemeColorRole.VectorField]);
        Assert.Equal(["BubbleSeries", "PolarScatterSeries", "Scatter3DSeries", "ScatterSeries", "TernaryScatterSeries"], withARole[ThemeColorRole.ScatterFace]);
        Assert.Equal(4, withARole.Count);
    }

    /// <summary>A kind whose colours depend on how its instance is set up says so through one call: a colour array or a colour map names
    /// its colours, and the series then asks no cycle.</summary>
    [Fact]
    public void AKindColouredByItsData_AsksNoCycle_OnlyWhenItIs()
    {
        Assert.Equal(CycleCall.Scatter, new ScatterSeries([1.0], [2.0]).CycleCall);
        Assert.Equal(CycleCall.Neither, new ScatterSeries([1.0], [2.0]) { C = [1.0] }.CycleCall);
        Assert.Equal(ThemeColorRole.ScatterFace, new ScatterSeries([1.0], [2.0]) { C = [1.0] }.Artist.ThemeColor);
        Assert.Equal(CycleCall.Patch, new PieSeries([1.0, 2.0]).CycleCall);
        Assert.Equal(CycleCall.Neither, new PieSeries([1.0, 2.0]) { Colors = [Colors.Red, Colors.Blue] }.CycleCall);
        Assert.Equal(2, new KdeSeries([1.0, 2, 3]) { Fill = false }.ArtistZOrder);
        Assert.Equal(1, new KdeSeries([1.0, 2, 3]) { Fill = true }.ArtistZOrder);
    }

    /// <summary>A series that says nothing about itself, the base of every kind of the library's own.</summary>
    private sealed class BareSeries : ChartSeries
    {
        public override void Accept(ISeriesVisitor visitor, RenderArea area) { }
        public override DataRangeContribution ComputeDataRange(IAxesContext context) => default;
        public override MatPlotLibNet.Serialization.SeriesDto ToSeriesDto() => new() { Type = "bare" };
    }
}
