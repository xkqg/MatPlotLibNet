// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>A theme's property cycler hands out four things per series slot: a colour, a dash pattern, a marker and a
/// line width. Until now only the colour was drawn. The slot decides all four (<c>SeriesSlotTests</c> pins the rule) and
/// the line, scatter, step, area and error-bar series, their secondary-axis twins and their legend swatches all follow
/// it. A series that names a style of its own keeps it; one that leaves it at its default takes the slot's. A cycler
/// that lists only colours changes nothing, which is how every theme that ships is built.</summary>
public class CycledStrokeTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0, 3.0];
    private static readonly double[] Y1 = [1.0, 2.0, 1.5, 3.0];
    private static readonly double[] Y2 = [2.0, 3.0, 2.5, 4.0];
    private static readonly double[] Low = [0.1, 0.1, 0.1, 0.1];

    /// <summary>Slot 0 asks for what a new series already draws (solid, no marker, 1.5 wide); slot 1 for dashes, squares
    /// and 4 wide; slot 2 for dots and nothing else.</summary>
    private static readonly PropCycler Styled = new PropCyclerBuilder()
        .WithColors(Colors.Red, Colors.Green, Colors.Blue)
        .WithLineStyles(LineStyle.Solid, LineStyle.Dashed, LineStyle.Dotted)
        .WithMarkerStyles(MarkerStyle.None, MarkerStyle.Square, MarkerStyle.None)
        .WithLineWidths(1.5, 4.0, 1.5)
        .Build();

    private static Theme ThemeWith(PropCycler cycler) => Theme.CreateFrom(Theme.Default).WithPropCycler(cycler).Build();

    private static XDocument Doc(Theme theme, Action<AxesBuilder> build) =>
        XDocument.Parse(Plt.Create().WithTheme(theme).WithSize(600, 400).AddSubPlot(1, 1, 1, build).ToSvg());

    private static IEnumerable<XElement> Named(XContainer root, string name) =>
        root.Descendants().Where(e => e.Name.LocalName == name);

    private static IEnumerable<XElement> Strokes(XContainer root, string element, Color colour) =>
        Named(root, element).Where(e => (string?)e.Attribute("stroke") == colour.ToHex());

    private static IEnumerable<XElement> Fills(XContainer root, string element, Color colour) =>
        Named(root, element).Where(e => (string?)e.Attribute("fill") == colour.ToHex());

    private static XElement Polyline(XContainer root, Color colour) => Assert.Single(Strokes(root, "polyline", colour));

    private static XElement Legend(XDocument doc) =>
        Assert.Single(Named(doc, "g"), g => (string?)g.Attribute("class") == "legend");

    private static string? Dash(XElement e) => (string?)e.Attribute("stroke-dasharray");

    private static string Width(XElement e) => (string)e.Attribute("stroke-width")!;

    /// <summary>The dash attribute a line of this style wears when it names the style itself, under the default theme.</summary>
    private static string ReferenceDash(LineStyle style)
    {
        var doc = Doc(Theme.Default, ax => ax.Plot(X, Y1, s => s.LineStyle = style));
        return Dash(Assert.Single(Named(doc, "polyline"), p => (string?)p.Attribute("stroke") == Theme.Default.CycleColors[0].ToHex()))!;
    }

    // â”€â”€ a line â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void ALine_TakesTheDashPatternOfItsSlot()
    {
        var doc = Doc(ThemeWith(Styled), ax => ax.Plot(X, Y1).Plot(X, Y2).Plot(X, Y1));

        Assert.Null(Dash(Polyline(doc, Colors.Red)));
        Assert.Equal(ReferenceDash(LineStyle.Dashed), Dash(Polyline(doc, Colors.Green)));
        Assert.Equal(ReferenceDash(LineStyle.Dotted), Dash(Polyline(doc, Colors.Blue)));
    }

    [Fact]
    public void ALine_TakesTheWidthOfItsSlot()
    {
        var doc = Doc(ThemeWith(Styled), ax => ax.Plot(X, Y1).Plot(X, Y2));

        Assert.Equal("1.5", Width(Polyline(doc, Colors.Red)));
        Assert.Equal("4", Width(Polyline(doc, Colors.Green)));
    }

    [Fact]
    public void ALine_TakesTheMarkerOfItsSlot_OneAtEveryPoint()
    {
        var doc = Doc(ThemeWith(Styled), ax => ax.Plot(X, Y1).Plot(X, Y2));

        Assert.Empty(Fills(doc, "rect", Colors.Red));
        Assert.Equal(X.Length, Fills(doc, "rect", Colors.Green).Count());
    }

    [Fact]
    public void ALineThatNamesItsOwnStyle_KeepsIt()
    {
        var doc = Doc(ThemeWith(Styled), ax => ax.Plot(X, Y1).Plot(X, Y2, s =>
        {
            s.LineStyle = LineStyle.DashDot;
            s.LineWidth = 2.5;
            s.Marker = MarkerStyle.Circle;
        }));

        var line = Polyline(doc, Colors.Green);
        Assert.Equal(ReferenceDash(LineStyle.DashDot), Dash(line));
        Assert.Equal("2.5", Width(line));
        Assert.Equal(X.Length, Fills(doc, "circle", Colors.Green).Count());
        Assert.Empty(Fills(doc, "rect", Colors.Green));
    }

    [Fact]
    public void ALineThatNamesNoMarker_AsAnExplicitNone_KeepsNone()
    {
        var doc = Doc(ThemeWith(Styled), ax => ax.Plot(X, Y1).Plot(X, Y2, s => s.Marker = MarkerStyle.None));

        Assert.Empty(Fills(doc, "rect", Colors.Green));
    }

    // â”€â”€ a scatter â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void AScatter_TakesTheMarkerOfItsSlot_AndAnUnaskedSlotLeavesItsCircles()
    {
        var doc = Doc(ThemeWith(Styled), ax => ax.Scatter(X, Y1).Scatter(X, Y2));

        Assert.Equal(X.Length, Fills(doc, "circle", Colors.Red).Count());
        Assert.Equal(X.Length, Fills(doc, "rect", Colors.Green).Count());
        Assert.Empty(Fills(doc, "circle", Colors.Green));
    }

    [Fact]
    public void AScatterThatNamesItsOwnMarker_KeepsIt()
    {
        var doc = Doc(ThemeWith(Styled), ax => ax.Scatter(X, Y1).Scatter(X, Y2, s => s.Marker = MarkerStyle.Triangle));

        Assert.Equal(X.Length, Fills(doc, "polygon", Colors.Green).Count());
        Assert.Empty(Fills(doc, "rect", Colors.Green));
    }

    // â”€â”€ a step â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void AStep_TakesTheDashPatternWidthAndMarkerOfItsSlot()
    {
        var doc = Doc(ThemeWith(Styled), ax => ax.Step(X, Y1).Step(X, Y2));

        Assert.Null(Dash(Polyline(doc, Colors.Red)));
        var step = Polyline(doc, Colors.Green);
        Assert.Equal(ReferenceDash(LineStyle.Dashed), Dash(step));
        Assert.Equal("4", Width(step));
        Assert.Equal(X.Length, Fills(doc, "rect", Colors.Green).Count());
    }

    [Fact]
    public void AStepThatNamesItsOwnStyle_KeepsIt()
    {
        var doc = Doc(ThemeWith(Styled), ax => ax.Step(X, Y1).Step(X, Y2, s =>
        {
            s.LineStyle = LineStyle.Dotted;
            s.LineWidth = 2.5;
            s.Marker = MarkerStyle.Circle;
        }));

        var step = Polyline(doc, Colors.Green);
        Assert.Equal(ReferenceDash(LineStyle.Dotted), Dash(step));
        Assert.Equal("2.5", Width(step));
        Assert.Equal(X.Length, Fills(doc, "circle", Colors.Green).Count());
    }

    [Fact]
    public void AStepWithAMarkerOfItsOwn_DrawsItEvenWithoutACycler()
    {
        var doc = Doc(Theme.Default, ax => ax.Step(X, Y1, s => s.Marker = MarkerStyle.Square));

        Assert.Equal(X.Length, Fills(doc, "rect", Theme.Default.CycleColors[0]).Count());
    }

    [Fact]
    public void AStepsMarker_SkipsAPointTheAxesCannotPlace()
    {
        var doc = Doc(Theme.Default, ax => ax.Step(X, [1.0, double.NaN, 2.0, 3.0], s => s.Marker = MarkerStyle.Square));

        Assert.Equal(X.Length - 1, Fills(doc, "rect", Theme.Default.CycleColors[0]).Count());
    }

    [Fact]
    public void AStepThatNamesNoMarker_AsAnExplicitNone_DrawsNoneInThePlotOrTheLegend()
    {
        var doc = Doc(ThemeWith(Styled), ax => ax
            .Step(X, Y1, s => s.Label = "first").Step(X, Y2, s => { s.Label = "second"; s.Marker = MarkerStyle.None; }).WithLegend());

        Assert.Empty(Fills(doc, "rect", Colors.Green));
    }

    // â”€â”€ an area's outline â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void AnAreasOutline_TakesTheDashPatternAndWidthOfItsSlot()
    {
        var doc = Doc(ThemeWith(Styled), ax => ax.FillBetween(X, Y1).FillBetween(X, Y2));

        Assert.Null(Dash(Polyline(doc, Colors.Red)));
        var outline = Polyline(doc, Colors.Green);
        Assert.Equal(ReferenceDash(LineStyle.Dashed), Dash(outline));
        Assert.Equal("4", Width(outline));
    }

    [Fact]
    public void AnAreaThatNamesItsOwnOutline_KeepsIt()
    {
        var doc = Doc(ThemeWith(Styled), ax => ax.FillBetween(X, Y1).FillBetween(X, Y2, null, s =>
        {
            s.LineStyle = LineStyle.Dotted;
            s.LineWidth = 2.5;
        }));

        var outline = Polyline(doc, Colors.Green);
        Assert.Equal(ReferenceDash(LineStyle.Dotted), Dash(outline));
        Assert.Equal("2.5", Width(outline));
    }

    // â”€â”€ an error bar â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void AnErrorBar_TakesTheWidthOfItsSlot_ForItsBarsAndCaps()
    {
        var doc = Doc(ThemeWith(Styled), ax => ax.ErrorBar(X, Y1, Low, Low).ErrorBar(X, Y2, Low, Low));

        Assert.All(Strokes(doc, "line", Colors.Red), l => Assert.Equal("1.5", Width(l)));
        var green = Strokes(doc, "line", Colors.Green).ToList();
        Assert.NotEmpty(green);
        Assert.All(green, l => Assert.Equal("4", Width(l)));
    }

    [Fact]
    public void AnErrorBarThatNamesItsOwnWidths_KeepsThem()
    {
        var doc = Doc(ThemeWith(Styled), ax => ax.ErrorBar(X, Y1, Low, Low).ErrorBar(X, Y2, Low, Low, s =>
        {
            s.LineWidth = 2.5;
            s.CapThick = 0.5;
        }));

        var widths = Strokes(doc, "line", Colors.Green).Select(Width).Distinct().Order().ToList();
        Assert.Equal(["0.5", "2.5"], widths);
    }

    // â”€â”€ the rest of the line family: density curve, ECDF, regression, signals, sparkline, polar and 3-D lines â”€â”€â”€â”€

    private static readonly double[] Sample1 = [1.0, 2, 2, 3, 3, 3, 4, 4, 5, 7];
    private static readonly double[] Sample2 = [2.0, 3, 3, 4, 4, 4, 5, 5, 6, 8];
    private static readonly double[] Radii1 = [1.0, 2, 3, 2];
    private static readonly double[] Radii2 = [2.0, 3, 4, 3];
    private static readonly double[] Angles = [0.0, 1.5, 3.0, 4.5];

    /// <summary>One chart per family of the remaining line renderers, two series each, so the second one holds slot 1.</summary>
    public static TheoryData<string> RemainingLineFamilies => new()
    {
        "kde", "ecdf", "regression", "signal", "signalxy", "polar", "line3d",
    };

    private static Action<AxesBuilder> Two(string family) => family switch
    {
        "kde" => ax => ax.Kde(Sample1).Kde(Sample2),
        "ecdf" => ax => ax.Ecdf(Sample1).Ecdf(Sample2),
        "regression" => ax => ax.Regression(X, Y1).Regression(X, Y2),
        "signal" => ax => ax.Signal(Y1).Signal(Y2),
        "signalxy" => ax => ax.SignalXY(X, Y1).SignalXY(X, Y2),
        "polar" => ax => ax.PolarPlot(Radii1, Angles).PolarPlot(Radii2, Angles),
        "line3d" => ax => ax.Plot3D(X, Y1, Y2).Plot3D(X, Y2, Y1),
        _ => throw new ArgumentOutOfRangeException(nameof(family), family, "Not one of the remaining line families."),
    };

    /// <summary>The stroke elements a family draws in <paramref name="colour"/>: a 3-D line is one segment per pair of points.</summary>
    private static List<XElement> FamilyStrokes(XContainer root, string family, Color colour) =>
        Strokes(root, family == "line3d" ? "line" : "polyline", colour).ToList();

    [Theory]
    [MemberData(nameof(RemainingLineFamilies))]
    public void ALineOfTheRemainingFamilies_TakesTheDashPatternOfItsSlot(string family)
    {
        var doc = Doc(ThemeWith(Styled), Two(family));

        Assert.All(FamilyStrokes(doc, family, Colors.Red), e => Assert.Null(Dash(e)));
        var second = FamilyStrokes(doc, family, Colors.Green);
        Assert.NotEmpty(second);
        Assert.All(second, e => Assert.Equal(ReferenceDash(LineStyle.Dashed), Dash(e)));
    }

    [Theory]
    [MemberData(nameof(RemainingLineFamilies))]
    public void ALineOfTheRemainingFamilies_TakesTheWidthOfItsSlot(string family)
    {
        var doc = Doc(ThemeWith(Styled), Two(family));

        var first = FamilyStrokes(doc, family, Colors.Red);
        var second = FamilyStrokes(doc, family, Colors.Green);
        Assert.NotEmpty(first);
        Assert.NotEmpty(second);
        Assert.All(second, e => Assert.Equal("4", Width(e)));
        // slot 0 asks for 1.5, which the series already draws, except a regression, whose own line is 2 wide (below)
        if (family != "regression") Assert.All(first, e => Assert.Equal("1.5", Width(e)));
    }

    [Fact]
    public void ASparkline_TakesTheWidthOfItsSlot_AndKeepsItsSolidLine()
    {
        var doc = Doc(ThemeWith(Styled), ax => ax.Sparkline(Y1).Sparkline(Y2));

        Assert.Equal("1.5", Width(Polyline(doc, Colors.Red)));
        var second = Polyline(doc, Colors.Green);
        Assert.Equal("4", Width(second));
        Assert.Null(Dash(second));
    }

    [Fact]
    public void ARegressionLine_WhoseOwnWidthIsTwo_TakesTheSlotsWidthOnlyWhenTheCyclerNamesOne()
    {
        // a cycler that lists no width hands out 1.5, which names nothing: the regression keeps its 2
        var coloursOnly = ThemeWith(new PropCyclerBuilder().WithColors(Colors.Red, Colors.Green).Build());

        var doc = Doc(coloursOnly, ax => ax.Regression(X, Y1).Regression(X, Y2));

        Assert.Equal("2", Width(Polyline(doc, Colors.Red)));
        Assert.Equal("2", Width(Polyline(doc, Colors.Green)));
        // slot 0 of the styled cycler names 1.5 as well: the line keeps its own 2; slot 1 names 4 and wins
        var styled = Doc(ThemeWith(Styled), ax => ax.Regression(X, Y1).Regression(X, Y2));
        Assert.Equal("2", Width(Polyline(styled, Colors.Red)));
        Assert.Equal("4", Width(Polyline(styled, Colors.Green)));
    }

    [Theory]
    [MemberData(nameof(RemainingLineFamilies))]
    public void ALineOfTheRemainingFamiliesThatNamesItsOwnStyle_KeepsIt(string family)
    {
        Action<AxesBuilder> build = family switch
        {
            "kde" => ax => ax.Kde(Sample1).Kde(Sample2, s => { s.LineStyle = LineStyle.DashDot; s.LineWidth = 2.5; }),
            "ecdf" => ax => ax.Ecdf(Sample1).Ecdf(Sample2, s => { s.LineStyle = LineStyle.DashDot; s.LineWidth = 2.5; }),
            "regression" => ax => ax.Regression(X, Y1).Regression(X, Y2, s => { s.LineStyle = LineStyle.DashDot; s.LineWidth = 2.5; }),
            "signal" => ax => ax.Signal(Y1).Signal(Y2, 1.0, 0.0, s => { s.LineStyle = LineStyle.DashDot; s.LineWidth = 2.5; }),
            "signalxy" => ax => ax.SignalXY(X, Y1).SignalXY(X, Y2, s => { s.LineStyle = LineStyle.DashDot; s.LineWidth = 2.5; }),
            "polar" => ax => ax.PolarPlot(Radii1, Angles).PolarPlot(Radii2, Angles, s => { s.LineStyle = LineStyle.DashDot; s.LineWidth = 2.5; }),
            "line3d" => ax => ax.Plot3D(X, Y1, Y2).Plot3D(X, Y2, Y1, s => { s.LineStyle = LineStyle.DashDot; s.LineWidth = 2.5; }),
            _ => throw new ArgumentOutOfRangeException(nameof(family), family, "Not one of the remaining line families."),
        };

        var doc = Doc(ThemeWith(Styled), build);

        var second = FamilyStrokes(doc, family, Colors.Green);
        Assert.NotEmpty(second);
        Assert.All(second, e => Assert.Equal(ReferenceDash(LineStyle.DashDot), Dash(e)));
        Assert.All(second, e => Assert.Equal("2.5", Width(e)));
    }

    [Fact]
    public void ASparklineThatNamesItsOwnWidth_KeepsIt()
    {
        var doc = Doc(ThemeWith(Styled), ax => ax.Sparkline(Y1).Sparkline(Y2, s => s.LineWidth = 2.5));

        Assert.Equal("2.5", Width(Polyline(doc, Colors.Green)));
    }

    // â”€â”€ the other axis pairs follow the same slots â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void ALineOnTheRightHandAxis_FollowsTheSlotRuleAfterThePrimarySeries()
    {
        var doc = Doc(ThemeWith(Styled), ax => ax.Plot(X, Y1).WithSecondaryYAxis(y2 => y2.Plot(X, Y2)));

        Assert.Null(Dash(Polyline(doc, Colors.Red)));
        var secondary = Polyline(doc, Colors.Green);
        Assert.Equal(ReferenceDash(LineStyle.Dashed), Dash(secondary));
        Assert.Equal("4", Width(secondary));
        Assert.Equal(X.Length, Fills(doc, "rect", Colors.Green).Count());
    }

    // â”€â”€ the legend swatch shows what the series draws â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void ALegendSwatch_ShowsTheDashPatternWidthAndMarkerOfItsLine()
    {
        var doc = Doc(ThemeWith(Styled), ax => ax
            .Plot(X, Y1, s => s.Label = "first").Plot(X, Y2, s => s.Label = "second").WithLegend());

        var legend = Legend(doc);
        var swatch = Assert.Single(Strokes(legend, "line", Colors.Green));
        Assert.Equal(ReferenceDash(LineStyle.Dashed), Dash(swatch));
        Assert.Equal("4", Width(swatch));
        Assert.Single(Fills(legend, "rect", Colors.Green));
        Assert.Null(Dash(Assert.Single(Strokes(legend, "line", Colors.Red))));
    }

    [Fact]
    public void AStepsLegendSwatch_ShowsItsOwnStrokeAndTheCyclersWidth()
    {
        var doc = Doc(ThemeWith(Styled), ax => ax
            .Step(X, Y1, s => s.Label = "first").Step(X, Y2, s => s.Label = "second").WithLegend());

        var swatch = Assert.Single(Strokes(Legend(doc), "line", Colors.Green));
        Assert.Equal(ReferenceDash(LineStyle.Dashed), Dash(swatch));
        Assert.Equal("4", Width(swatch));
    }

    [Fact]
    public void AScattersLegendSwatch_ShowsTheMarkerOfItsSlot()
    {
        var doc = Doc(ThemeWith(Styled), ax => ax
            .Scatter(X, Y1, s => s.Label = "first").Scatter(X, Y2, s => s.Label = "second").WithLegend());

        var legend = Legend(doc);
        Assert.Single(Fills(legend, "rect", Colors.Green));
        Assert.Empty(Fills(legend, "circle", Colors.Green));
        Assert.Single(Fills(legend, "circle", Colors.Red));
    }

    [Fact]
    public void AnErrorBarsLegendSwatch_ShowsTheWidthOfItsSlot()
    {
        var doc = Doc(ThemeWith(Styled), ax => ax
            .ErrorBar(X, Y1, Low, Low, s => s.Label = "first").ErrorBar(X, Y2, Low, Low, s => s.Label = "second").WithLegend());

        var swatch = Strokes(Legend(doc), "line", Colors.Green).ToList();
        Assert.NotEmpty(swatch);
        Assert.All(swatch, l => Assert.Equal("4", Width(l)));
    }

    [Theory]
    [InlineData("ecdf")]
    [InlineData("regression")]
    [InlineData("signal")]
    [InlineData("signalxy")]
    [InlineData("sparkline")]
    public void ALegendSwatch_OfTheRemainingLineFamilies_ShowsTheDashPatternAndWidthOfItsSlot(string family)
    {
        Action<AxesBuilder> build = family switch
        {
            "ecdf" => ax => ax.Ecdf(Sample1, s => s.Label = "first").Ecdf(Sample2, s => s.Label = "second").WithLegend(),
            "regression" => ax => ax.Regression(X, Y1, s => s.Label = "first").Regression(X, Y2, s => s.Label = "second").WithLegend(),
            "signal" => ax => ax.Signal(Y1, 1.0, 0.0, s => s.Label = "first").Signal(Y2, 1.0, 0.0, s => s.Label = "second").WithLegend(),
            "signalxy" => ax => ax.SignalXY(X, Y1, s => s.Label = "first").SignalXY(X, Y2, s => s.Label = "second").WithLegend(),
            "sparkline" => ax => ax.Sparkline(Y1, s => s.Label = "first").Sparkline(Y2, s => s.Label = "second").WithLegend(),
            _ => throw new ArgumentOutOfRangeException(nameof(family), family, "Not one of the line families with their own swatch."),
        };

        var legend = Legend(Doc(ThemeWith(Styled), build));

        var swatch = Assert.Single(Strokes(legend, "line", Colors.Green));
        Assert.Equal("4", Width(swatch));
        if (family == "sparkline") Assert.Null(Dash(swatch)); else Assert.Equal(ReferenceDash(LineStyle.Dashed), Dash(swatch));
        Assert.Null(Dash(Assert.Single(Strokes(legend, "line", Colors.Red))));
    }

    // â”€â”€ what must not move â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static string EveryFamily(Theme theme) => Plt.Create().WithTheme(theme).WithSize(600, 400).AddSubPlot(1, 1, 1, ax => ax
        .Plot(X, Y1, s => s.Label = "line").Scatter(X, Y2, s => s.Label = "scatter").Step(X, Y1, s => s.Label = "step")
        .FillBetween(X, Y2, null, s => s.Label = "area").ErrorBar(X, Y1, Low, Low, s => s.Label = "errors")
        .Plot(X, Y2, s => s.Label = "again").WithSecondaryYAxis(y2 => y2.Plot(X, Y1, s => s.Label = "right"))
        .WithLegend()).ToSvg();

    /// <summary>The line renderers that took the slot later: a regression line is 2 wide of its own, so it is the one a
    /// careless "cycler width of 1.5" would move.</summary>
    private static string EveryRemainingFamily(Theme theme) => Plt.Create().WithTheme(theme).WithSize(600, 400).AddSubPlot(1, 1, 1, ax => ax
        .Kde(Sample1, s => s.Label = "kde").Ecdf(Sample1, s => s.Label = "ecdf").Regression(X, Y1, s => s.Label = "regression")
        .Signal(Y2, 1.0, 0.0, s => s.Label = "signal").SignalXY(X, Y1, s => s.Label = "signalxy").Sparkline(Y2, s => s.Label = "spark")
        .WithLegend()).ToSvg()
        + Plt.Create().WithTheme(theme).WithSize(400, 400).AddSubPlot(1, 1, 1, ax => ax
            .PolarPlot(Radii1, Angles).PolarPlot(Radii2, Angles)).ToSvg()
        + Plt.Create().WithTheme(theme).WithSize(400, 400).AddSubPlot(1, 1, 1, ax => ax
            .Plot3D(X, Y1, Y2).Plot3D(X, Y2, Y1)).ToSvg();

    [Fact]
    public void ACyclerThatListsOnlyColours_DrawsTheBytesOfAThemeWithoutOne()
    {
        var plain = Theme.Default;
        var coloursOnly = ThemeWith(new PropCyclerBuilder().WithColors(plain.CycleColors).Build());

        Assert.Equal(EveryFamily(plain), EveryFamily(coloursOnly));
        Assert.Equal(EveryRemainingFamily(plain), EveryRemainingFamily(coloursOnly));
    }

    [Fact]
    public void ACyclerWhoseStylesAreAllTheDefaults_DrawsTheBytesOfAThemeWithoutOne()
    {
        var plain = Theme.Default;
        var restingStyles = ThemeWith(new PropCyclerBuilder().WithColors(plain.CycleColors)
            .WithLineStyles(LineStyle.Solid, LineStyle.Solid).WithMarkerStyles(MarkerStyle.None)
            .WithLineWidths(1.5, 1.5, 1.5).Build());

        Assert.Equal(EveryFamily(plain), EveryFamily(restingStyles));
        Assert.Equal(EveryRemainingFamily(plain), EveryRemainingFamily(restingStyles));
    }

    [Fact]
    public void NoThemeThatShips_HasACycler_SoNoShippedPictureChanges()
    {
        var shipped = typeof(Theme).GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(Theme))
            .Select(p => new { p.Name, Theme = (Theme)p.GetValue(null)! })
            .ToList();

        Assert.True(shipped.Count >= 30, $"expected the shipped themes, found {shipped.Count}");
        Assert.All(shipped, t => Assert.True(t.Theme.PropCycler is null, $"{t.Name} carries a property cycler"));
    }

    [Fact]
    public void AFigureOnAStyledCycler_DrawsTheSamePictureReadBackFromJson()
    {
        var figure = Plt.Create().WithTheme(ThemeWith(Styled)).WithSize(600, 400).AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, Y1, s => s.Label = "line").Plot(X, Y2, s => s.Label = "dashed").Scatter(X, Y1).Step(X, Y2)
            .FillBetween(X, Y1).ErrorBar(X, Y2, Low, Low).WithLegend()).Build();

        string readBack = ChartSerializerDefault.Current.FromJson(figure.ToJson()).ToSvg();

        Assert.Equal(figure.ToSvg(), readBack);
    }

    /// <summary>The polar line is left out: its radii and angles are not in the JSON yet (the roadmap row "every chart type
    /// back from JSON"), so a read-back figure has no data to draw whatever the cycler says.</summary>
    [Theory]
    [InlineData("kde")]
    [InlineData("ecdf")]
    [InlineData("regression")]
    [InlineData("signal")]
    [InlineData("signalxy")]
    [InlineData("line3d")]
    [InlineData("sparkline")]
    public void ARemainingLineFamilyOnAStyledCycler_DrawsTheSamePictureReadBackFromJson(string family)
    {
        Action<AxesBuilder> build = family == "sparkline" ? ax => ax.Sparkline(Y1).Sparkline(Y2).Sparkline(Y1) : Two(family);
        var figure = Plt.Create().WithTheme(ThemeWith(Styled)).WithSize(500, 400).AddSubPlot(1, 1, 1, build).Build();

        string readBack = ChartSerializerDefault.Current.FromJson(figure.ToJson()).ToSvg();

        Assert.Equal(figure.ToSvg(), readBack);
    }
}
