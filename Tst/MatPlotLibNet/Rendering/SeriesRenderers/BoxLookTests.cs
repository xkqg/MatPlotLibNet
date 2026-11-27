// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>What the matplotlib themes draw a box plot with where its author names nothing, read with <c>inspect</c> from matplotlib 3.11.2
/// and measured on the artists <c>Axes.boxplot</c> returns. Default style (the <c>boxplot.*</c> rcParams): a black box, black whiskers
/// and caps (solid), a median in <c>C1</c> (tab:orange), fliers beyond the whiskers as open circles in black, all lines 1 pt wide
/// (<c>get_linewidth()</c> 1.0), markers 6 pt across; the mean, where asked for, a <c>C2</c> triangle (<c>'^'</c>). Classic style: a blue
/// box with blue dashed whiskers, black caps, a red median, black <c>'+'</c> fliers, a red square mean edged black, all 1 pt wide. A cap
/// is half the box wide (<c>Axes.bxp</c>: <c>capwidths = 0.5 * widths</c>; printed: box x 0.775 to 1.225, cap 0.8875 to 1.1125) and the
/// boxplot asks the colour cycle for nothing, so the line drawn after it wears <c>C0</c>. The library's own themes keep the box as it
/// was: coloured from the cycle, a red median, whiskers and no caps and no fliers.</summary>
public sealed class BoxLookTests
{
    private const double Pt = 100.0 / 72.0;

    // 9 lies beyond the fence (q1 2.25, q3 4.75, whis 1.5: the fence is at 8.5); the whiskers end at 1 and 5.
    private static readonly double[] Data = [1, 2, 3, 4, 5, 9];

    private sealed record Drawn(XDocument Svg, Rect Plot)
    {
        public double CentreX => Plot.X + Plot.Width / 2;

        public XElement? Box(Color stroke) => Svg.Each("rect").FirstOrDefault(e => (string?)e.Attribute("stroke") == stroke.ToHex()
            && (string?)e.Attribute("fill") == "none");

        /// <summary>The lines through the box's own x inside the plot (the whiskers, the caps and the median), horizontal or vertical.</summary>
        public List<XElement> Strokes(bool horizontal) => [.. Svg.Each("line").Where(e => Math.Abs(e.Number(horizontal ? "y1" : "x1") - e.Number(horizontal ? "y2" : "x2")) < 1e-6    // flat along the stroke's own axis
            && Math.Abs(e.Number(horizontal ? "x1" : "y1") - e.Number(horizontal ? "x2" : "y2")) > 1       // and a pixel or more long
            && Math.Abs((e.Number("x1") + e.Number("x2")) / 2 - CentreX) < 1.5 && Length(e) < Plot.Width * 0.9   // not a spine
            && e.Number("y1") >= Plot.Y && e.Number("y1") <= Plot.Y + Plot.Height && e.Number("y2") >= Plot.Y && e.Number("y2") <= Plot.Y + Plot.Height)];

        public List<XElement> Of(List<XElement> strokes, Color stroke) => [.. strokes.Where(e => (string?)e.Attribute("stroke") == stroke.ToHex())];

        public List<XElement> Circles() => [.. Svg.Each("circle")];
    }

    private static double Length(XElement line) => Math.Abs(line.Number("x2") - line.Number("x1")) + Math.Abs(line.Number("y2") - line.Number("y1"));

    private static Drawn Draw(Theme theme, Action<BoxSeries>? configure = null, Action<AxesBuilder>? more = null)
    {
        var figure = Plt.Create().WithSize(500, 400).WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax =>
            {
                // The box stands at 0, the middle of the limits, half a unit wide: a box given no position stands at 1 under the
                // matplotlib themes, and one box is 0.15 wide there. These tests read the look of the box, not where it stands.
                ax.BoxPlot([Data], s => { s.Positions = [0]; s.Widths = 0.5; configure?.Invoke(s); }).SetXLim(-1, 1).SetYLim(0, 10).WithGrid(g => g with { Visible = false });
                more?.Invoke(ax);
            }).Build();
        var plot = figure.FirstPlotArea();
        return new Drawn(figure.ToSvg().Xml(), plot);
    }

    // ── the default style ────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void V2_TheBoxIsBlack_OnePointWide_AndNotFilled()
    {
        var drawn = Draw(Theme.MatplotlibV2);

        var box = Assert.IsType<XElement>(drawn.Box(Colors.Black));
        Assert.Equal(1 * Pt, box.Number("stroke-width"), 2);
    }

    [Fact]
    public void V2_TheMedianIsC1_AcrossTheBox()
    {
        var drawn = Draw(Theme.MatplotlibV2);

        var c1 = Theme.MatplotlibV2.CycleColors[1];
        var median = Assert.Single(drawn.Of(drawn.Strokes(horizontal: true), c1));
        Assert.Equal(1 * Pt, median.Number("stroke-width"), 2);
        Assert.Equal(Assert.IsType<XElement>(drawn.Box(Colors.Black)).Number("width"), Length(median), 2);
    }

    [Fact]
    public void V2_TheWhiskersAreBlackAndSolid_AndTheCapsAreHalfTheBoxWide()
    {
        var drawn = Draw(Theme.MatplotlibV2);

        var whiskers = drawn.Of(drawn.Strokes(horizontal: false), Colors.Black);
        Assert.Equal(2, whiskers.Count);
        Assert.All(whiskers, w => { Assert.Null(w.Attribute("stroke-dasharray")); Assert.Equal(1 * Pt, w.Number("stroke-width"), 2); });

        double boxWidth = Assert.IsType<XElement>(drawn.Box(Colors.Black)).Number("width");
        var caps = drawn.Of(drawn.Strokes(horizontal: true), Colors.Black);
        Assert.Equal(2, caps.Count);
        Assert.All(caps, c => Assert.Equal(boxWidth / 2, Length(c), 2));
    }

    [Fact]
    public void V2_AnObservationBeyondTheWhiskers_IsAnOpenCircleSixPointsAcross()
    {
        var drawn = Draw(Theme.MatplotlibV2);

        var flier = Assert.Single(drawn.Circles());
        Assert.Equal("none", (string?)flier.Attribute("fill"));
        Assert.Equal(Colors.Black.ToHex(), (string?)flier.Attribute("stroke"));
        Assert.Equal(1 * Pt, flier.Number("stroke-width"), 2);
        Assert.Equal(6 * Pt / 2, flier.Number("r"), 1);
        // It stands at 9 on the axis of 0 to 10, on the box's own x.
        Assert.Equal(drawn.Plot.Y + drawn.Plot.Height * (1 - 9 / 10.0), flier.Number("cy"), 1);
        Assert.Equal(drawn.CentreX, flier.Number("cx"), 1);
    }

    [Fact]
    public void V2_ShowOutliersOff_MarksNoFlier()
    {
        var drawn = Draw(Theme.MatplotlibV2, s => s.ShowOutliers = false);

        Assert.Empty(drawn.Circles());
    }

    [Fact]
    public void V2_TheMean_IsAC2Triangle()
    {
        var drawn = Draw(Theme.MatplotlibV2, s => s.ShowMeans = true);

        var c2 = Theme.MatplotlibV2.CycleColors[2];
        var mean = Assert.Single(drawn.Svg.Each("polygon"), e => (string?)e.Attribute("fill") == c2.ToHex());
        Assert.Equal(c2.ToHex(), (string?)mean.Attribute("stroke"));
        Assert.Equal(3, ((string)mean.Attribute("points")!).Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
    }

    [Fact]
    public void V2_ANamedColourColoursTheBoxTheWhiskersAndTheCaps_NotTheMedian()
    {
        var drawn = Draw(Theme.MatplotlibV2, s => s.Color = Colors.Red);

        Assert.NotNull(drawn.Box(Colors.Red));
        Assert.Equal(2, drawn.Of(drawn.Strokes(horizontal: false), Colors.Red).Count);
        Assert.Equal(2, drawn.Of(drawn.Strokes(horizontal: true), Colors.Red).Count);
        Assert.Single(drawn.Of(drawn.Strokes(horizontal: true), Theme.MatplotlibV2.CycleColors[1]));
    }

    [Fact]
    public void V2_ANamedMedianColour_IsDrawnAsNamed()
    {
        var drawn = Draw(Theme.MatplotlibV2, s => s.MedianColor = Colors.Cyan);

        Assert.Single(drawn.Of(drawn.Strokes(horizontal: true), Colors.Cyan));
    }

    /// <summary>A box a verb put in a colour group (the box of a raincloud chart) stands for one unit with the other parts of the group, so it wears the
    /// group's cycle colour as a box that names a colour does, and its median keeps the theme's: the box is not black beside a coloured violin.</summary>
    [Fact]
    public void V2_ABoxInAColourGroup_WearsTheGroupsColour_AsANamedColourDoes_AndItsMedianTheThemes()
    {
        var drawn = Draw(Theme.MatplotlibV2, s => s.ColorGroup = 0);
        var blue = Theme.MatplotlibV2.CycleColors[0];

        Assert.NotNull(drawn.Box(blue));
        Assert.Equal(2, drawn.Of(drawn.Strokes(horizontal: false), blue).Count);
        Assert.Single(drawn.Of(drawn.Strokes(horizontal: true), Theme.MatplotlibV2.CycleColors[1]));
        Assert.Null(drawn.Box(Colors.Black));
    }

    [Fact]
    public void TheColourSlots_GiveABoxInAColourGroupItsGroupsSlot()
    {
        // A raincloud's violin, box and points are one group: the violin asks the lines cycle, so the group holds its next slot.
        var box = new BoxSeries([Data]) { ColorGroup = 0 };
        var axes = new Axes();
        axes.Plot([0.0, 1.0], [1.0, 2.0]);
        axes.AddSeries(new ViolinSeries([Data]) { ColorGroup = 0 });
        axes.AddSeries(box);
        axes.Plot([0.0, 1.0], [1.0, 2.0]);

        Assert.Equal([0, 1, 1, 2], axes.ColorSlots(Theme.MatplotlibV2));
        Assert.Equal(Theme.MatplotlibV2.CycleColors[0], Theme.MatplotlibV2.SeriesSlot(0).ColorOf(box));
    }

    // ── a box that lies down ─────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void V2_ABoxThatLiesDown_HasItsCapsUpright_AndItsFlierAtItsValueAlongX()
    {
        var figure = Plt.Create().WithSize(500, 400).WithTheme(Theme.MatplotlibV2)
            .AddSubPlot(1, 1, 1, ax => ax.BoxPlot([Data], s => { s.Vert = false; s.ShowMeans = true; s.Positions = [0]; s.Widths = 0.5; }).SetXLim(0, 10).SetYLim(-1, 1).WithGrid(g => g with { Visible = false })).Build();
        var plot = figure.FirstPlotArea();
        var svg = figure.ToSvg().Xml();

        var flier = Assert.Single(svg.Each("circle"), e => (string?)e.Attribute("fill") == "none");
        Assert.Equal(plot.X + plot.Width * 9 / 10.0, flier.Number("cx"), 1);
        Assert.Equal(plot.Y + plot.Height / 2, flier.Number("cy"), 1);

        // Two caps, vertical, black, half the box high; the whiskers are horizontal.
        double boxHeight = Assert.Single(svg.Each("rect"), e => (string?)e.Attribute("stroke") == Colors.Black.ToHex()
            && (string?)e.Attribute("fill") == "none").Number("height");
        var caps = svg.Each("line").Where(e => (string?)e.Attribute("stroke") == Colors.Black.ToHex()
            && Math.Abs(e.Number("x1") - e.Number("x2")) < 1e-6 && Math.Abs(Math.Abs(e.Number("y1") - e.Number("y2")) - boxHeight / 2) < 0.05).ToList();
        Assert.Equal(2, caps.Count);
        Assert.Contains(svg.Each("polygon"), e => (string?)e.Attribute("fill") == Theme.MatplotlibV2.CycleColors[2].ToHex());
    }

    // ── the classic style ────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Classic_TheBoxIsBlue_TheMedianRed_TheWhiskersBlueAndDashed_TheCapsBlack()
    {
        var drawn = Draw(Theme.MatplotlibClassic);

        Assert.NotNull(drawn.Box(Colors.Blue));
        Assert.Single(drawn.Of(drawn.Strokes(horizontal: true), Colors.Red));
        var whiskers = drawn.Of(drawn.Strokes(horizontal: false), Colors.Blue);
        Assert.Equal(2, whiskers.Count);
        Assert.All(whiskers, w => Assert.NotNull(w.Attribute("stroke-dasharray")));
        // The caps are black and half the box wide (the plus of the flier is black too, and shorter).
        double boxWidth = Assert.IsType<XElement>(drawn.Box(Colors.Blue)).Number("width");
        Assert.Equal(2, drawn.Of(drawn.Strokes(horizontal: true), Colors.Black).Count(c => Math.Abs(Length(c) - boxWidth / 2) < 0.05));
    }

    [Fact]
    public void Classic_AFlierIsABlackPlus_SixPointsAcross()
    {
        var drawn = Draw(Theme.MatplotlibClassic);

        Assert.Empty(drawn.Circles());
        double y = drawn.Plot.Y + drawn.Plot.Height * (1 - 9 / 10.0);
        // The plus is a horizontal and a vertical stroke through the point, 6 pt long.
        var plus = drawn.Svg.Each("line").Where(e => (string?)e.Attribute("stroke") == Colors.Black.ToHex()
            && Math.Abs(Length(e) - 6 * Pt) < 0.05).ToList();
        Assert.Equal(2, plus.Count);
        Assert.All(plus, l => Assert.Equal(1 * Pt, l.Number("stroke-width"), 2));
        Assert.Contains(plus, l => Math.Abs(l.Number("y1") - y) < 0.1 && Math.Abs(l.Number("y2") - y) < 0.1);
    }

    [Fact]
    public void Classic_TheMean_IsARedSquareEdgedBlack()
    {
        var drawn = Draw(Theme.MatplotlibClassic, s => s.ShowMeans = true);

        var square = Assert.Single(drawn.Svg.Each("rect"), e => (string?)e.Attribute("fill") == Colors.Red.ToHex()
            && (string?)e.Attribute("stroke") == Colors.Black.ToHex());
        Assert.Equal(6 * Pt, square.Number("width"), 1);
    }

    // ── no cycle colour: the line after a box wears C0 ───────────────────────────────────────────────────────────

    [Fact]
    public void UnderTheMatplotlibThemes_TheLineAfterABox_WearsTheFirstColourOfTheCycle()
    {
        var v2 = Draw(Theme.MatplotlibV2, more: ax => ax.Plot([-1.0, 1.0], [1.0, 2.0]));
        var classic = Draw(Theme.MatplotlibClassic, more: ax => ax.Plot([-1.0, 1.0], [1.0, 2.0]));

        Assert.Contains(v2.Svg.Descendants(), e => (string?)e.Attribute("stroke") == Theme.MatplotlibV2.CycleColors[0].ToHex() && (string?)e.Attribute("fill") == "none"
            && e.Name.LocalName is "polyline" or "path" or "line");
        Assert.Contains(classic.Svg.Descendants(), e => (string?)e.Attribute("stroke") == Theme.MatplotlibClassic.CycleColors[0].ToHex() && (string?)e.Attribute("fill") == "none"
            && e.Name.LocalName is "polyline" or "path");
    }

    [Fact]
    public void TheColourSlots_GiveABoxNone_NamedOrNot_UnderTheMatplotlibThemes_AndOneEachUnderTheLibrarys()
    {
        // matplotlib's boxplot asks neither cycle, and a call given its colour asks none.
        var axes = new Axes();
        axes.AddSeries(new BoxSeries([Data]));
        axes.AddSeries(new BoxSeries([Data]) { Color = Colors.Red });
        axes.Plot([0.0, 1.0], [1.0, 2.0]);

        Assert.Equal([0, 0, 0], axes.ColorSlots(Theme.MatplotlibV2));
        Assert.Equal([0, 1, 2], axes.ColorSlots(Theme.Default));
    }

    [Fact]
    public void TheSlot_ColoursABoxThatNamesNone_AsTheThemesBox()
    {
        var series = new BoxSeries([Data]);

        Assert.Equal(Colors.Black, Theme.MatplotlibV2.SeriesSlot(0).ColorOf(series));
        Assert.Equal(Colors.Blue, Theme.MatplotlibClassic.SeriesSlot(0).ColorOf(series));
        Assert.Equal(Theme.Default.CycleColors[0], Theme.Default.SeriesSlot(0).ColorOf(series));
        Assert.Equal(Colors.Red, Theme.MatplotlibV2.SeriesSlot(0).ColorOf(new BoxSeries([Data]) { Color = Colors.Red }));
    }

    // ── the library's own themes keep the box ────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheLibrarysOwnTheme_DrawsTheBoxInTheCycleColour_ARedMedianTwoPixelsWide_NoCapsAndNoFlier()
    {
        var drawn = Draw(Theme.Default);

        var box = Assert.IsType<XElement>(drawn.Box(Theme.Default.CycleColors[0]));
        Assert.Equal(1.5, box.Number("stroke-width"), 2);
        var median = Assert.Single(drawn.Of(drawn.Strokes(horizontal: true), Colors.Red));
        Assert.Equal(2, median.Number("stroke-width"), 2);
        Assert.Single(drawn.Strokes(horizontal: true));   // the median alone: no caps
        Assert.Empty(drawn.Circles());
    }

    /// <summary>A standing that follows no span draws every box the one width, whatever the positions are (a position no axis can place
    /// included); one that follows it holds its share of the span between the least and the most.</summary>
    [Fact]
    public void AStandingThatFollowsNoSpan_DrawsEveryBoxItsWidth_AndOneThatFollowsIt_HoldsTheShareBetweenTheLeastAndTheMost()
    {
        Assert.False(BoxStanding.Library.WidthFollowsSpan);
        Assert.Equal(0.5, BoxStanding.Library.WidthOver(double.NaN));
        Assert.Equal(0.5, BoxStanding.Library.WidthOver(10));

        Assert.True(BoxStanding.Matplotlib.WidthFollowsSpan);
        Assert.Equal(0.15, BoxStanding.Matplotlib.WidthOver(0));
        Assert.Equal(0.45, BoxStanding.Matplotlib.WidthOver(3), 12);
        Assert.Equal(0.5, BoxStanding.Matplotlib.WidthOver(100));
    }

    // ── the theme carries it ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The mean of the library's own box: a green dot, a filled circle four pixels in radius with no outline.</summary>
    [Fact]
    public void TheLibrarysOwnTheme_DrawsTheMeanAsAGreenDotFourPixelsInRadius()
    {
        var drawn = Draw(Theme.Default, s => s.ShowMeans = true);

        var dot = Assert.Single(drawn.Circles());
        Assert.Equal(Colors.Green.ToHex(), (string?)dot.Attribute("fill"));
        Assert.Equal(4, dot.Number("r"), 9);
        Assert.Null(dot.Attribute("stroke"));
    }

    /// <summary>The library's box wears the colour of its series in the box and the whiskers, as a series that names none, a colour group
    /// or a colour given does: one rule, the look names no colour for the parts that follow the series.</summary>
    [Fact]
    public void TheLibrarysOwnTheme_ColoursTheBoxAndTheWhiskersFromTheSeries_AndAColourGivenColoursThemToo()
    {
        var cycle = Theme.Default.CycleColors[0];

        var drawn = Draw(Theme.Default);
        Assert.NotNull(drawn.Box(cycle));
        Assert.Equal(2, drawn.Of(drawn.Strokes(horizontal: false), cycle).Count);
        Assert.All(drawn.Of(drawn.Strokes(horizontal: false), cycle), w => Assert.Equal(1, w.Number("stroke-width"), 2));

        var teal = Draw(Theme.Default, s => s.Color = Colors.Teal);
        Assert.NotNull(teal.Box(Colors.Teal));
        Assert.Equal(2, teal.Of(teal.Strokes(horizontal: false), Colors.Teal).Count);
        Assert.Single(teal.Of(teal.Strokes(horizontal: true), Colors.Red));   // the median keeps its own
    }

    /// <summary>A part of a look that names no colour wears the colour of the series, the median and the caps as the outline and the
    /// whiskers: the one rule for every part, not a rule of the library's own look.</summary>
    [Fact]
    public void APartThatNamesNoColour_WearsTheColourOfTheSeries_TheMedianAndTheCapsToo()
    {
        var theme = new Theme("followers", Theme.Default)
        {
            Looks = ThemeLooks.Library with
            {
                Box = BoxLook.Library with { Median = new BoxStroke(null, 2), Caps = new BoxCaps(null, 0.5, 1) },
            },
        };
        var cycle = Theme.Default.CycleColors[0];

        var drawn = Draw(theme);
        Assert.Equal(3, drawn.Of(drawn.Strokes(horizontal: true), cycle).Count);   // the median and the two caps
        Assert.Empty(drawn.Of(drawn.Strokes(horizontal: true), Colors.Red));

        var teal = Draw(theme, s => s.Color = Colors.Teal);
        Assert.Equal(2, teal.Of(teal.Strokes(horizontal: true), Colors.Teal).Count);   // the caps follow the colour given
        Assert.Single(teal.Of(teal.Strokes(horizontal: true), cycle));                  // the median keeps the series' own, as it names none
    }

    /// <summary>A box the library draws stands as it has always stood: the first at 0 and half a unit wide wherever it is, however many
    /// stand, the axis ranged over the places they are drawn at and its ticks where the axis puts them.</summary>
    [Fact]
    public void TheLibrarysOwnTheme_StandsTheBoxesAtZeroHalfAUnitWide_AndTheMatplotlibThemesAtOneAsBxpDoes()
    {
        var series = new BoxSeries([Data, Data, Data, Data]);

        Assert.Equal(0, series.PositionIn(0, Theme.Default));
        Assert.Equal(3, series.PositionIn(3, Theme.Default));
        Assert.Equal(0.5, series.WidthIn(Theme.Default));
        Assert.Equal(1, series.PositionIn(0, Theme.MatplotlibV2));
        Assert.Equal(0.45, series.WidthIn(Theme.MatplotlibV2), 12);   // 0.15 of the span 3 between the outer boxes

        var wide = new BoxSeries([Data, Data]) { Positions = [double.NaN, 4] };
        Assert.Equal(0.5, wide.WidthIn(Theme.Default));   // a position no axis can place does not change the width of a library box
    }

    // ── the theme carries it ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Every theme names a look for its box: the library's own names <see cref="BoxLook.Library"/>, which holds the values the
    /// box has always been drawn with.</summary>
    [Fact]
    public void TheLibrarysOwnTheme_NamesTheLibraryLook_WhichHoldsTheValuesTheBoxWasAlwaysDrawnWith()
    {
        var look = Theme.Default.Looks.Box;

        Assert.Same(BoxLook.Library, look);
        Assert.Equal(new BoxStroke(null, SeriesLineLook.LibraryWidth), look.Outline);
        Assert.Equal(new BoxStroke(Colors.Red, 2), look.Median);
        Assert.Equal(new BoxStroke(null, 1), look.Whisker);
        Assert.Null(look.Caps);
        Assert.Null(look.Fliers);
        Assert.Equal(MarkerStyle.Circle, look.Mean.Marker);
        Assert.Equal(Colors.Green, look.Mean.Face);
        Assert.Null(look.Mean.Edge);
        Assert.Equal(0, look.Mean.EdgeWidth);
        Assert.Equal(8, look.Mean.Size.PointsInPixels, 9);
        Assert.Equal(BoxStanding.Library, look.Standing);
        foreach (var theme in new[] { Theme.Dark, Theme.Nord }) Assert.Same(BoxLook.Library, theme.Looks.Box);
    }

    [Fact]
    public void TheMatplotlibThemesCarryALook_TheLibrarysOwnTheLibraryLook_AndTheCloneAndTheJsonKeepIt()
    {
        var v2 = Theme.MatplotlibV2.Looks.Box;
        Assert.Equal(Colors.Black, v2.Outline.Color);
        Assert.Equal(Theme.MatplotlibV2.CycleColors[1], v2.Median.Color);
        Assert.Equal(Colors.Black, v2.Whisker.Color);
        Assert.Equal(LineStyle.Solid, v2.Whisker.Style);
        Assert.Equal(Colors.Black, v2.Caps!.Value.Color);
        Assert.Equal(0.5, v2.Caps!.Value.Share);
        Assert.Equal(1 * Pt, v2.Outline.Width, 9);
        Assert.Equal(1 * Pt, v2.Median.Width, 9);
        Assert.Equal(1 * Pt, v2.Whisker.Width, 9);
        Assert.Equal(1 * Pt, v2.Caps!.Value.Width, 9);
        Assert.Equal(MarkerStyle.Circle, v2.Fliers!.Value.Marker);
        Assert.Null(v2.Fliers!.Value.Face);
        Assert.Equal(Colors.Black, v2.Fliers!.Value.Edge);
        Assert.Equal(MarkerStyle.Triangle, v2.Mean.Marker);
        Assert.Equal(Theme.MatplotlibV2.CycleColors[2], v2.Mean.Face);
        Assert.Equal(Theme.MatplotlibV2.CycleColors[2], v2.Mean.Edge);
        Assert.Equal(6, v2.Mean.Size);
        Assert.Equal(6, v2.Fliers!.Value.Size);
        Assert.Equal(BoxStanding.Matplotlib, v2.Standing);

        var classic = Theme.MatplotlibClassic.Looks.Box;
        Assert.Equal(Colors.Blue, classic.Outline.Color);
        Assert.Equal(Colors.Red, classic.Median.Color);
        Assert.Equal(Colors.Blue, classic.Whisker.Color);
        Assert.Equal(LineStyle.Dashed, classic.Whisker.Style);
        Assert.Equal(Colors.Black, classic.Caps!.Value.Color);
        Assert.Equal(MarkerStyle.Plus, classic.Fliers!.Value.Marker);
        Assert.Equal(Colors.Black, classic.Fliers!.Value.Edge);
        Assert.Equal(MarkerStyle.Square, classic.Mean.Marker);
        Assert.Equal(Colors.Red, classic.Mean.Face);
        Assert.Equal(Colors.Black, classic.Mean.Edge);

        Assert.Same(BoxLook.Library, Theme.Default.Looks.Box);
        Assert.Equal(v2, Theme.MatplotlibV2.Cloned().Looks.Box);
        Assert.Equal(v2, Theme.MatplotlibV2.ThroughDto().Looks.Box);
        Assert.Equal(classic, Theme.MatplotlibClassic.ThroughDto().Looks.Box);
        Assert.Same(BoxLook.Library, Theme.Default.ThroughDto().Looks.Box);
    }

    /// <summary>The library's own look writes no key, so the theme's JSON is as it was (<c>{}</c> for the plain theme), and a look of
    /// matplotlib's is written whole under the flat keys it has always had.</summary>
    [Fact]
    public void TheJson_WritesNoBoxForTheLibraryLook_AndTheWholeLookForMatplotlibs()
    {
        Assert.Null(ThemeDto.From(Theme.Default).Box);
        Assert.Null(ThemeDto.From(Theme.Dark).Box);
        var v2 = Assert.IsType<BoxLookDto>(ThemeDto.From(Theme.MatplotlibV2).Box);
        Assert.Equal(Colors.Black, v2.Box);
        Assert.Equal(Colors.Black, v2.Cap);
        Assert.Equal(0.5, v2.CapShare);
        Assert.Equal(1 * Pt, v2.LineWidth, 9);
        Assert.Equal(6, v2.MarkerSize);
        Assert.Equal(MarkerStyle.Triangle, v2.MeanMarker);
    }
}
