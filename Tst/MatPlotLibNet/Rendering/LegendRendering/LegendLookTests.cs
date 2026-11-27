// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Rendering.LegendRendering;

/// <summary>What a legend is drawn with where its author names nothing. matplotlib 3.11.2, printed under each style:
/// <c>legend.fontsize</c> 'medium' / 'large' (the font size / 1.2 times it), <c>legend.fancybox</c> True / False,
/// <c>legend.framealpha</c> 0.8 / None (opaque), <c>legend.edgecolor</c> '0.8' / 'inherit' (the axes' edge colour, black),
/// <c>legend.numpoints</c> 1 / 2 and <c>legend.scatterpoints</c> 1 / 3. <c>HandlerNpoints.get_xdata</c> puts one marker in the middle
/// of a handle and several from 0.3 of the font size in from its start to as far in from its end, <c>HandlerLine2D</c> runs a line of
/// several markers between the first and the last, and a scatter marker stands at <c>Legend._scatteryoffsets</c> (3/8, 4/8, 2.5/8,
/// repeated) of the handle's height above its foot. <c>FancyBboxPatch</c> rounds a frame at 0.2 of the font size. The library's own themes
/// draw what they always did; a setting that is named, the resting value included, is drawn as named.</summary>
public class LegendLookTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0];
    private static readonly double[] Y = [1.0, 2.0, 1.5];

    private static readonly Theme V2 = Theme.MatplotlibV2;
    private static readonly Theme Classic = Theme.MatplotlibClassic;

    private static XDocument Doc(Theme theme, Action<AxesBuilder> chart, Func<Legend, Legend>? legend = null) =>
        Plt.Create().WithTheme(theme).WithSize(600, 400).AddSubPlot(1, 1, 1, ax =>
        {
            chart(ax);
            ax.WithLegend(l => legend?.Invoke(l) ?? l);
        }).ToSvg().Xml();

    private static Action<AxesBuilder> Line(MarkerStyle? marker = null) => ax => ax.Plot(X, Y, s => { s.Label = "series"; s.Marker = marker; });

    private static Action<AxesBuilder> Scatter() => ax => ax.Scatter(X, Y, s => s.Label = "series");

    private static XElement LegendGroup(XDocument doc) =>
        doc.Descendants().First(e => (string?)e.Attribute("class") == "legend");

    private static XElement Label(XDocument doc) => LegendGroup(doc).Each("text").First(e => e.Value == "series");

    private static double FontSize(XDocument doc) => Label(doc).Number("font-size");

    /// <summary>The frame of the legend: its first shape.</summary>
    private static XElement Frame(XDocument doc) => LegendGroup(doc).Elements().First(e => e.Name.LocalName is "rect" or "path");

    // ── the frame ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>matplotlib 3.11.2: the frame is a <c>FancyBboxPatch</c> at <c>legend.linewidth</c> None, so <c>patch.linewidth</c>:
    /// <c>ax.legend().get_frame().get_linewidth()</c> is 1.0 (points) under both styles, 1.39 px at 100 dpi. The library's own themes
    /// keep their half-pixel frame.</summary>
    [Theory]
    [InlineData("v2", 100.0 / 72)]
    [InlineData("classic", 100.0 / 72)]
    [InlineData("library", 0.5)]
    public void TheFrame_IsAsWideAsThePatchesOfItsTheme(string theme, double width)
    {
        var frame = Frame(Doc(theme switch { "v2" => V2, "classic" => Classic, _ => Theme.Default }, Line()));

        Assert.Equal(width, frame.Number("stroke-width"), 2);
    }

    /// <summary>matplotlib 3.11.2: <c>legend.framealpha</c> is the alpha of the frame patch, which a patch gives its edge as well:
    /// <c>get_frame().get_edgecolor()</c> is (0.8, 0.8, 0.8, 0.8) under the default style, opaque black under the classic one (framealpha
    /// None), and with <c>framealpha=0.5</c> (0.8, 0.8, 0.8, 0.5) and (0, 0, 0, 0.5). The library's own themes keep an opaque outline,
    /// whatever opacity the fill is given.</summary>
    [Fact]
    public void TheFramesOpacity_IsItsOutlinesToo_UnderTheMatplotlibThemes()
    {
        Assert.Equal(0.8, Frame(Doc(V2, Line())).Number("stroke-opacity"), 2);
        Assert.Null(Frame(Doc(Classic, Line())).Attribute("stroke-opacity"));
        Assert.Equal(0.5, Frame(Doc(V2, Line(), l => l with { FrameAlpha = 0.5 })).Number("stroke-opacity"), 2);
        Assert.Equal(0.5, Frame(Doc(Classic, Line(), l => l with { FrameAlpha = 0.5 })).Number("stroke-opacity"), 2);

        Assert.Null(Frame(Doc(Theme.Default, Line())).Attribute("stroke-opacity"));
        Assert.Null(Frame(Doc(Theme.Default, Line(), l => l with { FrameAlpha = 0.5 })).Attribute("stroke-opacity"));
    }

    [Fact]
    public void UnderV2_TheFrameIsRounded_At08_InLightGrey()
    {
        var frame = Frame(Doc(V2, Line()));

        Assert.Equal("path", frame.Name.LocalName);
        Assert.Equal("#CCCCCC", (string?)frame.Attribute("stroke"));
        Assert.Equal(0.8, frame.Number("fill-opacity"), 2);
    }

    [Fact]
    public void UnderClassic_TheFrameIsSquare_Opaque_InBlack()
    {
        var frame = Frame(Doc(Classic, Line()));

        Assert.Equal("rect", frame.Name.LocalName);
        Assert.Equal("#000000", (string?)frame.Attribute("stroke"));
        Assert.True(frame.Attribute("fill-opacity") is null || frame.Number("fill-opacity") == 1.0);
    }

    [Fact]
    public void UnderTheLibrarysThemes_TheFrameIsAsItAlwaysWas_Square_At08_InLightGrey()
    {
        var frame = Frame(Doc(Theme.Default, Line()));

        Assert.Equal("rect", frame.Name.LocalName);
        Assert.Equal("#CCCCCC", (string?)frame.Attribute("stroke"));
        Assert.Equal(0.8, frame.Number("fill-opacity"), 2);
    }

    [Fact]
    public void AFrameSettingThatIsNamed_IsDrawnAsNamed_TheRestingValueIncluded()
    {
        var squareV2 = Frame(Doc(V2, Line(), l => l with { FancyBox = false }));
        var roundClassic = Frame(Doc(Classic, Line(), l => l with { FancyBox = true }));
        var eightClassic = Frame(Doc(Classic, Line(), l => l with { FrameAlpha = 0.8 }));
        var redV2 = Frame(Doc(V2, Line(), l => l with { EdgeColor = Colors.Red }));

        Assert.Equal("rect", squareV2.Name.LocalName);
        Assert.Equal("path", roundClassic.Name.LocalName);
        Assert.Equal(0.8, eightClassic.Number("fill-opacity"), 2);
        Assert.Equal(Colors.Red.ToHex(), (string?)redV2.Attribute("stroke"));
    }

    [Fact]
    public void TheFramesFill_IsTheAxesColour_UnderTheMatplotlibThemes_WhereTheClassicFigureIsGrey()
    {
        var classic = Frame(Doc(Classic, Line()));
        var library = Frame(Doc(Theme.Dark, Line()));

        Assert.Equal(Classic.AxesBackground.ToHex(), (string?)classic.Attribute("fill"));   // legend.facecolor 'inherit': white, not the figure's grey
        Assert.Equal(Theme.Dark.Background.ToHex(), (string?)library.Attribute("fill"));    // the library's own themes: the figure's background
    }

    // ── the size of the text ──────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheEntriesText_IsTheFontSizeUnderV2_AndOnePointTwoTimesItUnderClassic_AndTheTickSizeUnderTheLibrarysThemes()
    {
        Assert.Equal(Math.Round(10.0.PointsInPixels, 2), FontSize(Doc(V2, Line())), 2);
        Assert.Equal(Math.Round(1.2 * 12.0.PointsInPixels, 2), FontSize(Doc(Classic, Line())), 2);
        Assert.Equal(Theme.Default.DefaultFont.Size, FontSize(Doc(Theme.Default, Line())), 2);
        Assert.Equal(10.0, FontSize(Doc(Classic, Line(), l => l with { FontSize = 10 })), 2);
    }

    // ── what the JSON keeps ───────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ALegendSettingThatIsNamed_SurvivesTheJson_AndOneThatIsNot_StaysUnnamed()
    {
        string Json(Func<Legend, Legend> change) => Plt.Create().WithTheme(Classic).AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y, s => s.Label = "series")
            .WithLegend(l => change(l))).Build().ToJson();

        string unnamed = Json(l => l with { NCols = 1, Title = "t" });
        string named = Json(l => l with { FrameAlpha = 0.8, FancyBox = false, NumPoints = 3, ScatterPoints = 2 });

        Assert.DoesNotContain("frameAlpha", unnamed);
        Assert.DoesNotContain("fancyBox", unnamed);
        Assert.Contains("\"frameAlpha\":0.8", named);
        Assert.Contains("\"fancyBox\":false", named);
        var back = ChartSerializerDefault.Current.FromJson(named).SubPlots[0].Legend;
        Assert.Equal(0.8, back.GivenFrameAlpha);
        Assert.Equal(false, back.GivenFancyBox);
        Assert.Equal(3, back.NumPoints);
        Assert.Equal(2, back.ScatterPoints);
        var unset = ChartSerializerDefault.Current.FromJson(unnamed).SubPlots[0].Legend;
        Assert.Null(unset.GivenFrameAlpha);
        Assert.Null(unset.GivenFancyBox);
    }

    [Fact]
    public void TheLook_IsTheMatplotlibThemes_AndSurvivesTheThemeJson()
    {
        var v2 = Theme.MatplotlibV2.Looks.Legend;
        var classic = Theme.MatplotlibClassic.Looks.Legend;

        Assert.Equal((1.0, true, 0.8, 1, 1), (v2.FontScale, v2.FancyBox, v2.FrameAlpha, v2.NumPoints, v2.ScatterPoints));
        Assert.Equal((1.2, false, 1.0, 2, 3), (classic.FontScale, classic.FancyBox, classic.FrameAlpha, classic.NumPoints, classic.ScatterPoints));
        Assert.Equal(Colors.Black, classic.EdgeColor);
        Assert.Same(LegendLook.Library, Theme.Default.Looks.Legend);
        Assert.Equal(classic, Theme.MatplotlibClassic.ThroughDto().Looks.Legend);
        Assert.Equal(v2, Theme.MatplotlibV2.ThroughDto().Looks.Legend);
        Assert.Equal(LegendLook.Library, Theme.Default.ThroughDto().Looks.Legend);
        Assert.Equal(classic, Theme.MatplotlibClassic.Cloned().Looks.Legend);
    }

    /// <summary>The library's own themes wear one look with the numbers a legend has always been drawn with; the matplotlib looks keep
    /// the library's shadow and handle sizes, and lay the box out and write the title as matplotlib's <c>Legend</c> does (the title the
    /// font size, not bold: printed <c>get_title().get_fontweight()</c> 'normal', size 10 / 12).</summary>
    [Fact]
    public void TheLibraryLook_HoldsTheNumbersALegendIsDrawnWith()
    {
        var library = LegendLook.Library;

        Assert.Equal((1.0, false, 0.8, 1, 1), (library.FontScale, library.FancyBox, library.FrameAlpha, library.NumPoints, library.ScatterPoints));
        Assert.Equal(Color.FromHex("#CCCCCC"), library.EdgeColor);
        Assert.Equal((0.5, 3.0, 1.0), (library.FrameWidth, library.ShadowOffset, library.TitleSizeStep));
        Assert.Equal(FontWeight.Bold, library.TitleWeight);
        Assert.Same(LegendBox.Library, library.Box);
        Assert.Equal(LegendFrameOpacity.Fill, library.FrameOpacity);
        Assert.Equal(new Color(0, 0, 0, 80), library.ShadowColor);
        Assert.Equal((2.0, 0.7, 0.8), (library.HandleLength, library.HandleHeight, library.HandleTextPad));
        Assert.Equal(LegendFace.Figure, library.Face);
        Assert.Equal(LegendScatterHandle.AtHandleStart, library.ScatterHandle);

        foreach (var look in new[] { V2.Looks.Legend, Classic.Looks.Legend })
        {
            Assert.Equal((100.0 / 72, 3.0, 0.0), (look.FrameWidth, look.ShadowOffset, look.TitleSizeStep));
            Assert.Equal(LegendFrameOpacity.Patch, look.FrameOpacity);
            Assert.Equal(FontWeight.Normal, look.TitleWeight);
            Assert.Same(LegendBox.Matplotlib, look.Box);
            Assert.Equal(library.ShadowColor, look.ShadowColor);
            Assert.Equal((2.0, 0.7, 0.8), (look.HandleLength, look.HandleHeight, look.HandleTextPad));
            Assert.Equal(LegendFace.Axes, look.Face);
            Assert.Equal(LegendScatterHandle.Spread, look.ScatterHandle);
        }

        var json = ThemeDto.From(Theme.Default);
        Assert.Null(json.LegendFontScale);
        Assert.Null(json.LegendFancyBox);
        Assert.Null(json.LegendFrameAlpha);
        Assert.Null(json.LegendEdgeColor);
        Assert.Null(json.LegendNumPoints);
        Assert.Null(json.LegendScatterPoints);
    }

    /// <summary>A theme's JSON that names no legend font scale is the library's look; one that names it is matplotlib's form, each key
    /// it lacks at that form's value.</summary>
    [Fact]
    public void TheLookTheJsonNames_IsTheLibrarysWithoutAFontScale_AndMatplotlibsWithOne()
    {
        Assert.Same(LegendLook.Library, LegendLook.ReadKeys(new ThemeDto { LegendFancyBox = true, LegendFrameAlpha = 0.5, LegendEdgeColor = Colors.Red, LegendNumPoints = 3, LegendScatterPoints = 3 }));
        var named = LegendLook.ReadKeys(new ThemeDto { LegendFontScale = 1.2 });

        Assert.Equal((1.2, false, 1.0, 1, 1), (named.FontScale, named.FancyBox, named.FrameAlpha, named.NumPoints, named.ScatterPoints));
        Assert.Equal(new Color(0, 0, 0), named.EdgeColor);
        Assert.Equal(LegendFace.Axes, named.Face);
        Assert.Equal(LegendScatterHandle.Spread, named.ScatterHandle);
        Assert.Equal(V2.Looks.Legend.FrameWidth, named.FrameWidth);           // the matplotlib form: a frame patch at patch.linewidth
        Assert.Equal(LegendFrameOpacity.Patch, named.FrameOpacity);
        Assert.Same(LegendBox.Matplotlib, named.Box);
    }

    [Fact]
    public void TheFrameAndItsShadow_AreDrawnWithTheLooksWidthColourAndOffset()
    {
        var doc = Doc(Theme.Default, Line(), l => l with { Shadow = true });
        var shapes = LegendGroup(doc).Children("rect").ToList();
        var shadow = shapes[0];
        var frame = shapes[1];

        Assert.Equal(frame.Number("x") + 3, shadow.Number("x"), 6);
        Assert.Equal(frame.Number("y") + 3, shadow.Number("y"), 6);
        Assert.Equal(0.5, frame.Number("stroke-width"), 6);
    }

    // ── the handles ───────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Where the handle of the first row starts: the text stands the handle (2 em) and its gap (0.8 em) past it.</summary>
    private static double HandleLeft(XDocument doc) => Label(doc).Number("x") - 2.8 * FontSize(doc);

    /// <summary>The foot of the handle: on the text's baseline under the matplotlib themes (an <c>HPacker</c> aligned on the baseline, the
    /// handle box's descent 0), <paramref name="below"/> under it where the library's row puts the text a pixel over the foot.</summary>
    private static double HandleFoot(XDocument doc, double below = 0) => Label(doc).Number("y") + below;

    private static List<XElement> InLegend(XDocument doc, string kind) =>
        [.. LegendGroup(doc).Each(kind)];

    [Fact]
    public void ALineHandle_RunsTheWholeHandleUnderV2_AndFromPointThreeToPointSevenOfTwoEmUnderClassic()
    {
        var v2 = Doc(V2, Line());
        var classic = Doc(Classic, Line());
        var v2Line = InLegend(v2, "line").First(l => l.Number("y1") == l.Number("y2"));
        var classicLine = InLegend(classic, "line").First(l => l.Number("y1") == l.Number("y2"));
        double v2Em = FontSize(v2), classicEm = FontSize(classic);

        Assert.Equal(HandleLeft(v2), v2Line.Number("x1"), 1);
        Assert.Equal(HandleLeft(v2) + 2 * v2Em, v2Line.Number("x2"), 1);
        Assert.Equal(HandleLeft(classic) + 0.3 * classicEm, classicLine.Number("x1"), 1);
        Assert.Equal(HandleLeft(classic) + 2 * classicEm - 0.3 * classicEm, classicLine.Number("x2"), 1);
    }

    [Fact]
    public void ALineWithMarkersHandle_ShowsOneMarkerInTheMiddleUnderV2_AndOneAtEachEndOfItsLineUnderClassic()
    {
        var v2 = Doc(V2, Line(MarkerStyle.Circle));
        var classic = Doc(Classic, Line(MarkerStyle.Circle));
        var v2Markers = InLegend(v2, "circle");
        var classicMarkers = InLegend(classic, "circle");
        double classicEm = FontSize(classic);

        Assert.Single(v2Markers);
        Assert.Equal(HandleLeft(v2) + FontSize(v2), v2Markers[0].Number("cx"), 1);
        Assert.Equal(2, classicMarkers.Count);
        Assert.Equal(HandleLeft(classic) + 0.3 * classicEm, classicMarkers[0].Number("cx"), 1);
        Assert.Equal(HandleLeft(classic) + 1.7 * classicEm, classicMarkers[1].Number("cx"), 1);
    }

    [Fact]
    public void AScatterHandle_ShowsOneMarkerAtThreeEighthsOfItsHeightUnderV2_AndThreeUnderClassic()
    {
        var v2 = Doc(V2, Scatter());
        var classic = Doc(Classic, Scatter());
        var v2Markers = InLegend(v2, "circle");
        var classicMarkers = InLegend(classic, "circle");
        double v2Em = FontSize(v2), classicEm = FontSize(classic);

        Assert.Single(v2Markers);
        Assert.Equal(HandleLeft(v2) + v2Em, v2Markers[0].Number("cx"), 1);
        Assert.Equal(HandleFoot(v2) - 3.0 / 8 * 0.7 * v2Em, v2Markers[0].Number("cy"), 1);

        Assert.Equal(3, classicMarkers.Count);
        double[] heights = [3.0 / 8, 4.0 / 8, 2.5 / 8];
        double[] xs = [0.3 * classicEm, classicEm, 1.7 * classicEm];
        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(HandleLeft(classic) + xs[i], classicMarkers[i].Number("cx"), 1);
            Assert.Equal(HandleFoot(classic) - heights[i] * 0.7 * classicEm, classicMarkers[i].Number("cy"), 1);
        }
    }

    [Fact]
    public void UnderTheLibrarysThemes_AScatterHandleKeepsItsOneMarker_WhereItAlwaysStood()
    {
        var doc = Doc(Theme.Default, Scatter());
        var marker = Assert.Single(InLegend(doc, "circle"));
        double em = FontSize(doc);

        Assert.Equal(HandleLeft(doc) + 0.35 * em, marker.Number("cx"), 1);                 // half the handle's height in from its start
        Assert.Equal(HandleFoot(doc, below: 1) - 0.35 * em, marker.Number("cy"), 1);                 // and in the middle of it
    }

    [Fact]
    public void ANumberOfMarkersThatIsNamed_IsDrawnAsNamed_UnderEveryTheme()
    {
        var threeLine = InLegend(Doc(V2, Line(MarkerStyle.Circle), l => l with { NumPoints = 3 }), "circle");
        var twoScatter = InLegend(Doc(Theme.Default, Scatter(), l => l with { ScatterPoints = 2 }), "circle");
        var oneClassic = InLegend(Doc(Classic, Line(MarkerStyle.Circle), l => l with { NumPoints = 1 }), "circle");

        Assert.Equal(3, threeLine.Count);
        Assert.Equal(2, twoScatter.Count);
        Assert.Single(oneClassic);
    }

    /// <summary>The size of a marker in a handle: the points it is given, at least one, in pixels, and no more than the handle is high.</summary>
    [Theory]
    [InlineData(6, 9.7, 6 * 100.0 / 72)]        // between the floor and the cap: its own size
    [InlineData(0, 9.7, 1 * 100.0 / 72)]        // below a point: a point
    [InlineData(-4, 9.7, 1 * 100.0 / 72)]
    [InlineData(1, 9.7, 1 * 100.0 / 72)]
    [InlineData(100, 9.7, 9.7)]                 // taller than the handle: the handle's height
    [InlineData(0.5, 0.5, 0.5)]                 // a handle lower than a point caps the floor too
    public void AMarkerInAHandle_IsItsOwnSize_AtLeastAPoint_AndNoBiggerThanTheHandleIsHigh(double points, double handleHeight, double expected)
    {
        Assert.Equal(expected, LegendLook.MarkerSizeIn(points, handleHeight), 9);
    }
}
