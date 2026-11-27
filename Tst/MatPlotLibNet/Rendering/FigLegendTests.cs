// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A legend of the figure as a whole (matplotlib's <c>figlegend</c>): it lists the labelled series of every subplot
/// once, in subplot order, stands in a rectangle that is the figure, is static, is drawn above the subplots and the texts,
/// and is the same picture in the SVG and in every raster or native output. A second <c>FigLegend</c> adds a second legend, and
/// the constrained layout keeps room for a legend that stands outside the axes.</summary>
public class FigLegendTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0];
    private static readonly double[] Y = [1.0, 3.0, 2.0];

    /// <summary>A figure with one subplot for each label, side by side, whose own legend is off, so the only legend drawn is
    /// the figure's. A <see langword="null"/> label is an unlabelled series.</summary>
    private static FigureBuilder Subplots(params string?[] labels)
    {
        var builder = Plt.Create().WithSize(800, 600);
        for (int i = 0; i < labels.Length; i++)
        {
            string? label = labels[i];
            builder.AddSubPlot(1, labels.Length, i + 1, ax => ax.WithLegend(visible: false).Plot(X, Y, s => s.Label = label));
        }

        return builder;
    }

    /// <summary>The calls of the last legend group drawn: the figure legend is drawn after every subplot.</summary>
    private static List<CallRecord> LastLegendGroup(RecordingRenderContext ctx)
    {
        int begin = ctx.Calls.FindLastIndex(c => c is BeginGroupCall { CssClass: "legend" });
        Assert.True(begin >= 0, "no legend group was drawn");
        int end = ctx.Calls.FindIndex(begin, c => c is EndGroupCall);
        return ctx.Calls.GetRange(begin, end - begin + 1);
    }

    private static string[] LabelsOf(List<CallRecord> legendGroup) =>
        [.. legendGroup.OfType<DrawTextCall>().Select(c => c.Text)];

    private static int LegendGroups(RecordingRenderContext ctx) =>
        ctx.Of<BeginGroupCall>().Count(c => c.CssClass == "legend");

    /// <summary>The frame of the legend: its rectangle, or the bounds of the rounded path a frame with rounded corners (the
    /// matplotlib themes' default) is drawn as.</summary>
    private static Rect FrameOf(List<CallRecord> legendGroup)
    {
        var call = legendGroup.OfType<ShapeCall>().First(c => c is DrawRectangleCall or DrawPathCall);
        if (call is DrawRectangleCall rectangle) return rectangle.Rect;

        var points = ((DrawPathCall)call).Segments
            .SelectMany(segment => segment switch
            {
                MoveToSegment move => [move.Point],
                LineToSegment line => [line.Point],
                BezierSegment curve => new[] { curve.Control1, curve.Control2, curve.End },
                _ => Array.Empty<Point>(),
            }).ToList();
        return new Rect(points.Min(p => p.X), points.Min(p => p.Y), points.Max(p => p.X) - points.Min(p => p.X), points.Max(p => p.Y) - points.Min(p => p.Y));
    }

    private static Rect ExpectedFrame(Legend legend, Theme theme, string[] labels, double width = 800, double height = 600)
    {
        var layout = new LegendLayoutCalculator(theme, new RecordingRenderContext()).Compute(legend, labels, new Rect(0, 0, width, height));
        return new Rect(layout.BoxX, layout.BoxY, layout.BoxWidth, layout.BoxHeight);
    }

    private static void AssertSameRect(Rect expected, Rect actual)
    {
        Assert.Equal(expected.X, actual.X, 9);
        Assert.Equal(expected.Y, actual.Y, 9);
        Assert.Equal(expected.Width, actual.Width, 9);
        Assert.Equal(expected.Height, actual.Height, 9);
    }

    // ── The model and the builder ──────────────────────────────────────────

    [Fact]
    public void AFigure_HasNoLegend_UntilOneIsAdded()
    {
        Assert.Empty(new Figure().Legends);
        Assert.Empty(Subplots("a").Build().Legends);
    }

    [Fact]
    public void AddLegend_ListsTheLegendsInTheOrderTheyWereAdded()
    {
        var first = new Legend { Title = "first" };
        var second = new Legend { Title = "second" };
        var figure = new Figure();

        figure.AddLegend(first);
        figure.AddLegend(second);

        Assert.Equal([first, second], figure.Legends);
    }

    [Fact]
    public void AddLegend_RefusesNull()
    {
        Assert.Throws<ArgumentNullException>(() => new Figure().AddLegend(null!));
    }

    [Fact]
    public void RemoveLegend_TakesTheLegendOut_AndSaysWhetherItWasThere()
    {
        var kept = new Legend { Title = "kept" };
        var removed = new Legend { Title = "removed" };
        var figure = new Figure();
        figure.AddLegend(kept);
        figure.AddLegend(removed);

        Assert.True(figure.RemoveLegend(removed));
        Assert.False(figure.RemoveLegend(removed));
        Assert.Equal([kept], figure.Legends);
    }

    [Fact]
    public void AddingOrRemovingALegend_MarksTheFigureChanged()
    {
        var figure = new Figure();
        long before = figure.LatestChange;

        figure.AddLegend(new Legend());
        long added = figure.LatestChange;
        figure.RemoveLegend(new Legend());

        Assert.True(added > before);
        Assert.True(figure.LatestChange > added);
    }

    [Fact]
    public void FigLegend_AddsAVisibleLegend_AndReturnsTheSameBuilder()
    {
        var builder = Subplots("a");

        var returned = builder.FigLegend();

        Assert.Same(builder, returned);
        Assert.Equal([new Legend()], builder.Build().Legends);
        Assert.True(builder.Build().Legends[0].Visible);
    }

    [Fact]
    public void FigLegend_Configure_ReceivesAnewLegend_AndKeepsWhatItReturns()
    {
        Legend? received = null;

        var figure = Subplots("a").FigLegend(legend =>
        {
            received = legend;
            return legend with { Title = "Runs", NCols = 2, Position = LegendPosition.LowerLeft };
        }).Build();

        Assert.Equal(new Legend(), received);
        var legend = Assert.Single(figure.Legends);
        Assert.Equal("Runs", legend.Title);
        Assert.Equal(2, legend.NCols);
        Assert.Equal(LegendPosition.LowerLeft, legend.Position);
    }

    /// <summary>A second <c>FigLegend</c> adds a second legend, as a second <c>fig.legend()</c> does in matplotlib (3.11.2:
    /// two calls leave <c>len(fig.legends) == 2</c>); each starts from a new legend.</summary>
    [Fact]
    public void ASecondFigLegend_AddsASecondLegend()
    {
        Legend? received = null;

        var figure = Subplots("a")
            .FigLegend(legend => legend with { Title = "first", Position = LegendPosition.LowerLeft })
            .FigLegend(legend => { received = legend; return legend with { NCols = 3 }; })
            .Build();

        Assert.Equal(new Legend(), received);
        Assert.Equal(2, figure.Legends.Count);
        Assert.Equal(new Legend { Title = "first", Position = LegendPosition.LowerLeft }, figure.Legends[0]);
        Assert.Equal(new Legend { NCols = 3 }, figure.Legends[1]);
    }

    [Fact]
    public void TwoFigureLegends_AreTwoGroups_EachListingTheRowsOfEverySubplot_InTheOrderTheyWereAdded()
    {
        var figure = Subplots("a", "b")
            .FigLegend(legend => legend with { Title = "first", Position = LegendPosition.LowerLeft })
            .FigLegend(legend => legend with { Title = "second", Position = LegendPosition.UpperRight })
            .Build();

        var ctx = figure.Drawn();

        Assert.Equal(2, LegendGroups(ctx));
        Assert.Equal(["second", "a", "b"], LabelsOf(LastLegendGroup(ctx)));
        int first = ctx.Calls.FindIndex(c => c is DrawTextCall { Text: "first" });
        int second = ctx.Calls.FindIndex(c => c is DrawTextCall { Text: "second" });
        Assert.True(first >= 0 && first < second, $"first {first}, second {second}");
    }

    [Theory]
    [InlineData(LegendPosition.Best, LegendPosition.UpperRight)]
    [InlineData(LegendPosition.UpperRight, LegendPosition.UpperRight)]
    [InlineData(LegendPosition.LowerLeft, LegendPosition.LowerLeft)]
    [InlineData(LegendPosition.OutsideRight, LegendPosition.CenterRight)]
    [InlineData(LegendPosition.OutsideLeft, LegendPosition.CenterLeft)]
    [InlineData(LegendPosition.OutsideTop, LegendPosition.UpperCenter)]
    [InlineData(LegendPosition.OutsideBottom, LegendPosition.LowerCenter)]
    public void OnTheFigure_BestIsTheUpperRight_AndAnOutsidePlaceIsTheMiddleOfItsEdge(LegendPosition given, LegendPosition onTheFigure)
    {
        var legend = new Legend { Position = given, Title = "kept", NCols = 3 };

        var placed = legend.OnTheFigure(Theme.Default);

        Assert.Equal(legend with { Position = onTheFigure }, placed);
    }

    /// <summary>A legend outside the axes stands at the figure's edge, inside the picture, as matplotlib's
    /// <c>fig.legend(loc='outside center right')</c> does (3.11.2, 640 by 480: the box ends 6.9 px from the right edge, centred
    /// on the height): <c>legend.borderaxespad</c>, 0.5 of the legend's font, the distance every figure legend keeps from the edge.</summary>
    [Theory]
    [InlineData(LegendPosition.OutsideRight)]
    [InlineData(LegendPosition.OutsideLeft)]
    [InlineData(LegendPosition.OutsideTop)]
    [InlineData(LegendPosition.OutsideBottom)]
    public void AnOutsideFigureLegend_StandsInsideThePicture_AtItsEdge(LegendPosition position)
    {
        var figure = Subplots("one", "two").FigLegend(legend => legend with { Position = position }).Build();

        var frame = FrameOf(LastLegendGroup(figure.Drawn()));

        Assert.True(frame.X >= 0 && frame.Y >= 0 && frame.X + frame.Width <= 800 && frame.Y + frame.Height <= 600,
            $"the legend {frame} leaves the 800 x 600 figure");
        double fromItsEdge = position switch
        {
            LegendPosition.OutsideRight => 800 - (frame.X + frame.Width),
            LegendPosition.OutsideLeft => frame.X,
            LegendPosition.OutsideTop => frame.Y,
            _ => 600 - (frame.Y + frame.Height),
        };
        Assert.Equal(0.5 * 10.0.PointsInPixels, fromItsEdge, 9);   // legend.borderaxespad: 0.5 of the 10 pt legend font
    }

    // ── What the legend lists ──────────────────────────────────────────────

    [Fact]
    public void TheEntries_AreTheOnesTheAxesListsItself_SeriesAndHandleIncluded()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.Plot(X, Y, s => s.Label = "line"))
            .AddSubPlot(1, 2, 2, ax => ax.Scatter(X, Y, s => s.Label = "points"))
            .Build();

        var entries = figure.LegendEntries(new Legend(), new RecordingRenderContext());

        Assert.Equal(["line", "points"], entries.Select(e => e.Label));
        var line = Assert.IsType<SeriesLegendEntry>(entries[0]);
        var points = Assert.IsType<SeriesLegendEntry>(entries[1]);
        Assert.Same(figure.SubPlots[0].Series[0], line.Series);
        Assert.Same(figure.SubPlots[1].Series[0], points.Series);
        Assert.Equal(LegendSwatchKind.LineMarker, line.Swatch.Kind);
        Assert.Equal(LegendSwatchKind.Scatter, points.Swatch.Kind);
    }

    [Fact]
    public void TheLegend_ListsTheLabelledSeriesOfEverySubplot_InSubplotOrder()
    {
        var figure = Subplots("alpha", "beta", "gamma").FigLegend().Build();

        Assert.Equal(["alpha", "beta", "gamma"], LabelsOf(LastLegendGroup(figure.Drawn())));
    }

    [Fact]
    public void TheLegend_ListsTheEntriesOfOneSubplotTogether_BeforeTheNextSubplots()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.WithLegend(visible: false).Plot(X, Y, s => s.Label = "a1").Plot(X, Y, s => s.Label = "a2"))
            .AddSubPlot(1, 2, 2, ax => ax.WithLegend(visible: false).Plot(X, Y, s => s.Label = "b1"))
            .FigLegend()
            .Build();

        Assert.Equal(["a1", "a2", "b1"], LabelsOf(LastLegendGroup(figure.Drawn())));
    }

    [Fact]
    public void ASeriesWithoutALabel_HasNoEntry_AsInTheLegendOfAnAxes()
    {
        var figure = Subplots("alpha", null, "", "omega").FigLegend().Build();

        Assert.Equal(["alpha", "omega"], LabelsOf(LastLegendGroup(figure.Drawn())));
    }

    [Fact]
    public void ASeriesThatNamesItselfAtTheEndOfItsLine_LeavesTheLegend_AsInTheLegendOfAnAxes()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.WithLegend(visible: false).Plot(X, Y, s => { s.Label = "ends"; s.LabelPlacement = SeriesLabelPlacement.LineEnd; }))
            .AddSubPlot(1, 2, 2, ax => ax.WithLegend(visible: false).Plot(X, Y, s => s.Label = "listed"))
            .FigLegend()
            .Build();

        Assert.Equal(["listed"], LabelsOf(LastLegendGroup(figure.Drawn())));
    }

    /// <summary>Under the library's own themes the same label drawn the same way in two subplots is one entry: a legend for a grid
    /// of small multiples reads each name once.</summary>
    [Fact]
    public void AnEntryThatLooksTheSameInTwoSubplots_IsListedOnce_UnderTheLibrarysThemes()
    {
        var figure = Subplots("Observed", "Fit", "Observed", "Fit").WithTheme(Theme.Default).FigLegend().Build();

        Assert.Equal(["Observed", "Fit"], LabelsOf(LastLegendGroup(figure.Drawn())));
    }

    /// <summary>Under the matplotlib themes every entry of every subplot is listed, as <c>fig.legend()</c> lists them (matplotlib
    /// 3.11.2: two axes that each plot a line labelled L print <c>['L', 'L']</c>).</summary>
    [Fact]
    public void AnEntryThatLooksTheSameInTwoSubplots_IsListedForEach_UnderTheMatplotlibThemes()
    {
        var figure = Subplots("Observed", "Fit", "Observed", "Fit").WithTheme(Theme.MatplotlibV2).FigLegend().Build();

        Assert.Equal(["Observed", "Fit", "Observed", "Fit"], LabelsOf(LastLegendGroup(figure.Drawn())));
    }

    [Fact]
    public void TheSameLabelInAnotherColour_IsListedAgain()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.WithLegend(visible: false).Plot(X, Y, s => { s.Label = "Observed"; s.Color = Colors.Blue; }))
            .AddSubPlot(1, 2, 2, ax => ax.WithLegend(visible: false).Plot(X, Y, s => { s.Label = "Observed"; s.Color = Colors.Red; }))
            .FigLegend()
            .Build();

        Assert.Equal(["Observed", "Observed"], LabelsOf(LastLegendGroup(figure.Drawn())));
    }

    /// <summary>Two bars of one label and colour given different edges are two rows: the edge a bar was given is part of how its row
    /// is told apart, whether the handle carries it or reads it from the bar.</summary>
    [Fact]
    public void TheSameBarGivenAnotherEdge_IsListedAgain()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.WithLegend(visible: false).Bar(["a"], [1.0], b => { b.Label = "B"; b.Color = Colors.Blue; b.EdgeColor = Colors.Red; }))
            .AddSubPlot(1, 2, 2, ax => ax.WithLegend(visible: false).Bar(["a"], [1.0], b => { b.Label = "B"; b.Color = Colors.Blue; b.EdgeColor = Colors.Green; }))
            .FigLegend()
            .Build();

        Assert.Equal(["B", "B"], LabelsOf(LastLegendGroup(figure.Drawn())));
    }

    [Fact]
    public void TheSameLabelDrawnAnotherWay_IsListedAgain()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.WithLegend(visible: false).Plot(X, Y, s => s.Label = "Observed"))
            .AddSubPlot(1, 2, 2, ax => ax.WithLegend(visible: false).Scatter(X, Y, s => s.Label = "Observed"))
            .FigLegend()
            .Build();

        Assert.Equal(["Observed", "Observed"], LabelsOf(LastLegendGroup(figure.Drawn())));
    }

    [Fact]
    public void TheSameLabelWithAnotherLineStyle_IsListedAgain()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.WithLegend(visible: false).Plot(X, Y, s => s.Label = "Observed"))
            .AddSubPlot(1, 2, 2, ax => ax.WithLegend(visible: false).Plot(X, Y, s => { s.Label = "Observed"; s.LineStyle = LineStyle.Dashed; }))
            .FigLegend()
            .Build();

        Assert.Equal(["Observed", "Observed"], LabelsOf(LastLegendGroup(figure.Drawn())));
    }

    [Fact]
    public void TheLegendValuesOfTheFigureLegend_SuffixTheLabels_WhateverTheSubplotsLegendsSay()
    {
        var plain = Subplots("signal").FigLegend().Build();
        var withValues = Subplots("signal").FigLegend(legend => legend with { LegendValues = true }).Build();

        Assert.Equal(["signal"], LabelsOf(LastLegendGroup(plain.Drawn())));
        Assert.Equal(["signal = 2.00"], LabelsOf(LastLegendGroup(withValues.Drawn())));
    }

    [Fact]
    public void TheLegendValuesOfASubplotsOwnLegend_DoNotReachTheFigureLegend()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.WithLegendValues().WithLegend(visible: false).Plot(X, Y, s => s.Label = "signal"))
            .FigLegend()
            .Build();

        Assert.Equal(["signal"], LabelsOf(LastLegendGroup(figure.Drawn())));
    }

    // ── Nothing to list, nothing to show ───────────────────────────────────

    [Fact]
    public void AFigureLegend_WithNoLabelledSeries_DrawsNothing()
    {
        var figure = Subplots(null, "").FigLegend().Build();

        Assert.Equal(0, LegendGroups(figure.Drawn()));
        Assert.DoesNotContain("class=\"legend\"", figure.ToSvg());
    }

    [Fact]
    public void AFigureLegend_OnAFigureWithoutSubplots_DrawsNothing()
    {
        var figure = Plt.Create().FigLegend().Build();

        Assert.Empty(figure.SubPlots);
        Assert.Equal(0, LegendGroups(figure.Drawn()));
        Assert.DoesNotContain("class=\"legend\"", figure.ToSvg());
    }

    [Fact]
    public void AHiddenFigureLegend_DrawsNothing()
    {
        var figure = Subplots("a", "b").FigLegend(legend => legend with { Visible = false }).Build();

        Assert.Equal(0, LegendGroups(figure.Drawn()));
        Assert.DoesNotContain("class=\"legend\"", figure.ToSvg());
    }

    [Fact]
    public void AFigureWithoutAFigureLegend_DrawsTheLegendsOfItsSubplotsOnly()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.Plot(X, Y, s => s.Label = "a"))
            .AddSubPlot(1, 2, 2, ax => ax.Plot(X, Y, s => s.Label = "b"))
            .Build();

        Assert.Equal(2, LegendGroups(figure.Drawn()));
    }

    [Fact]
    public void TheFigureLegend_IsOneMoreGroup_BesideTheLegendsOfTheSubplots()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.Plot(X, Y, s => s.Label = "a"))
            .AddSubPlot(1, 2, 2, ax => ax.Plot(X, Y, s => s.Label = "b"))
            .FigLegend()
            .Build();

        var ctx = figure.Drawn();

        Assert.Equal(3, LegendGroups(ctx));
        Assert.Equal(["a", "b"], LabelsOf(LastLegendGroup(ctx)));
    }

    // ── Where it stands ────────────────────────────────────────────────────

    [Theory]
    [InlineData(LegendPosition.UpperRight)]
    [InlineData(LegendPosition.UpperLeft)]
    [InlineData(LegendPosition.LowerRight)]
    [InlineData(LegendPosition.LowerLeft)]
    [InlineData(LegendPosition.Right)]
    [InlineData(LegendPosition.CenterLeft)]
    [InlineData(LegendPosition.CenterRight)]
    [InlineData(LegendPosition.LowerCenter)]
    [InlineData(LegendPosition.UpperCenter)]
    [InlineData(LegendPosition.Center)]
    public void TheLegend_StandsInTheFigure_WhereItsPositionSays(LegendPosition position)
    {
        var legend = new Legend { Position = position };
        var figure = Subplots("one", "two").FigLegend(_ => legend).Build();

        var frame = FrameOf(LastLegendGroup(figure.Drawn()));

        AssertSameRect(ExpectedFrame(legend, figure.Theme, ["one", "two"]), frame);
    }

    [Fact]
    public void TheUpperRight_IsHalfTheLegendFontFromTheTopAndTheRightEdgeOfTheFigure()
    {
        var figure = Subplots("one").FigLegend(legend => legend with { Position = LegendPosition.UpperRight }).Build();

        var frame = FrameOf(LastLegendGroup(figure.Drawn()));

        Assert.Equal(800 - 0.5 * 10.0.PointsInPixels, frame.X + frame.Width, 9);   // fig.legend(loc='upper right'): borderaxespad 0.5 em
        Assert.Equal(0.5 * 10.0.PointsInPixels, frame.Y, 9);
    }

    /// <summary>Best looks for the place that covers the fewest marks of ONE axes; a figure has no such marks, so at the figure it
    /// means the upper right under every theme that leaves the place at Best (the classic theme names the upper right itself).</summary>
    [Theory]
    [InlineData("default")]
    [InlineData("classic")]
    [InlineData("dark")]
    public void ABestLegend_StandsInTheUpperRightOfTheFigure(string themeName)
    {
        var theme = themeName.AsTheme();
        var figure = Subplots("one", "two").WithTheme(theme).FigLegend().Build();
        Assert.Equal(LegendPosition.Best, figure.Legends[0].Position);

        var frame = FrameOf(LastLegendGroup(figure.Drawn()));

        AssertSameRect(ExpectedFrame(new Legend { Position = LegendPosition.UpperRight }, theme, ["one", "two"]), frame);
    }

    /// <summary>A figure legend that names no place stands where the theme says (matplotlib 3.11.2, <c>Legend.__init__</c>:
    /// <c>loc = mpl.rcParams["legend.loc"]</c>, and only then <c>if not self.isaxes and loc in [0, 'best']: loc = 'upper right'</c>),
    /// so a theme whose <c>legend.loc</c> is the lower left puts it there; only a theme that leaves it at Best gives the upper right.</summary>
    [Fact]
    public void ABestLegend_StandsWhereTheThemeSays_OnTheFigureToo()
    {
        var theme = new Theme("lower-left", Theme.Default) { DefaultLegendPosition = LegendPosition.LowerLeft };
        var figure = Subplots("one", "two").WithTheme(theme).FigLegend().Build();

        var frame = FrameOf(LastLegendGroup(figure.Drawn()));

        AssertSameRect(ExpectedFrame(new Legend { Position = LegendPosition.LowerLeft }, theme, ["one", "two"]), frame);
        Assert.Equal(600 - 10, frame.Y + frame.Height, 9);
    }

    [Fact]
    public void TheLegend_StandsInTheFigure_NotInAPlotArea()
    {
        var figure = Plt.Create().WithSize(1000, 400)
            .AddSubPlot(1, 2, 1, ax => ax.WithLegend(visible: false).Plot(X, Y, s => s.Label = "a"))
            .AddSubPlot(1, 2, 2, ax => ax.WithLegend(visible: false).Plot(X, Y, s => s.Label = "b"))
            .FigLegend(legend => legend with { Position = LegendPosition.Center })
            .Build();

        var frame = FrameOf(LastLegendGroup(figure.Drawn()));

        Assert.Equal(500, frame.X + frame.Width / 2, 9);
        Assert.Equal(200, frame.Y + frame.Height / 2, 9);
    }

    [Fact]
    public void TheLegendOfAnotherSizeFigure_StandsInThatFigure()
    {
        var figure = Subplots("one").WithSize(400, 300)
            .FigLegend(legend => legend with { Position = LegendPosition.LowerLeft }).Build();

        var frame = FrameOf(LastLegendGroup(figure.Drawn()));

        AssertSameRect(ExpectedFrame(figure.Legends[0], figure.Theme, ["one"], 400, 300), frame);
        Assert.Equal(300 - 0.5 * 10.0.PointsInPixels, frame.Y + frame.Height, 9);
    }

    [Fact]
    public void TheTitleAndTheColumns_OfTheFigureLegend_AreDrawnAsOfAnyLegend()
    {
        var figure = Subplots("a", "b", "c", "d")
            .FigLegend(legend => legend with { Title = "Runs", NCols = 2 }).Build();

        var group = LastLegendGroup(figure.Drawn());

        Assert.Equal(["Runs", "a", "b", "c", "d"], LabelsOf(group));
        AssertSameRect(ExpectedFrame(figure.Legends[0], figure.Theme, ["a", "b", "c", "d"]), FrameOf(group));
    }

    // ── The room it takes ──────────────────────────────────────────────────

    /// <summary>One subplot whose own legend is off, with or without a figure legend at <paramref name="position"/>.</summary>
    private static Figure OneSubplot(Func<FigureBuilder, FigureBuilder> layout, LegendPosition? position, string label = "a longer label")
    {
        var builder = layout(Plt.Create().WithSize(800, 600))
            .AddSubPlot(1, 1, 1, ax => ax.WithLegend(visible: false).Plot(X, Y, s => s.Label = label));
        return (position is { } at ? builder.FigLegend(legend => legend with { Position = at }) : builder).Build();
    }

    private static SubPlotSpacing SpacingOf(Figure figure) => new ChartRenderer().FrameOf(figure, new RecordingRenderContext()).Spacing;

    /// <summary>The size of the box of a figure legend that lists <paramref name="labels"/>, measured as the figure measures it.</summary>
    private static Size BoxOf(Figure figure, params string[] labels)
    {
        var layout = new LegendLayoutCalculator(figure.Theme, new RecordingRenderContext())
            .Compute(figure.Legends[0].OnTheFigure(figure.Theme), labels, new Rect(0, 0, figure.Width, figure.Height));
        return new Size(layout.BoxWidth, layout.BoxHeight);
    }

    /// <summary>The constrained layout keeps room for a figure legend that stands outside the axes, on the edge it stands at: the
    /// axes give up the box and <see cref="LegendPositionStrategy.OutsideReserve"/> past it. matplotlib 3.11.2, 640 by 480 under
    /// <c>layout='constrained'</c>: <c>fig.legend(loc='outside right upper')</c> moves the right of the axes from 0.993 to 0.862 of
    /// the width, 84 px for a box 76 px wide, and the outside left, upper and lower places move their own edge alike.</summary>
    [Theory]
    [InlineData(LegendPosition.OutsideRight)]
    [InlineData(LegendPosition.OutsideLeft)]
    [InlineData(LegendPosition.OutsideTop)]
    [InlineData(LegendPosition.OutsideBottom)]
    public void TheConstrainedLayout_KeepsRoomForAnOutsideFigureLegend_OnItsEdge(LegendPosition position)
    {
        var without = SpacingOf(OneSubplot(b => b.ConstrainedLayout(), null));
        var figure = OneSubplot(b => b.ConstrainedLayout(), position);
        var with = SpacingOf(figure);
        var box = BoxOf(figure, "a longer label");

        var (grown, kept) = position switch
        {
            LegendPosition.OutsideRight => (with.MarginRight - without.MarginRight, with.MarginLeft - without.MarginLeft),
            LegendPosition.OutsideLeft => (with.MarginLeft - without.MarginLeft, with.MarginRight - without.MarginRight),
            LegendPosition.OutsideTop => (with.MarginTop - without.MarginTop, with.MarginBottom - without.MarginBottom),
            _ => (with.MarginBottom - without.MarginBottom, with.MarginTop - without.MarginTop),
        };
        double extent = position is LegendPosition.OutsideRight or LegendPosition.OutsideLeft ? box.Width : box.Height;
        Assert.Equal(extent + LegendPositionStrategy.OutsideReserve, grown, 9);
        Assert.Equal(0, kept, 9);
    }

    [Fact]
    public void AnOutsideFigureLegend_UnderTheConstrainedLayout_StandsClearOfThePlot()
    {
        var figure = OneSubplot(b => b.ConstrainedLayout(), LegendPosition.OutsideRight);
        var plot = new ChartRenderer().ComputeLayout(figure, new RecordingRenderContext()).PlotAreas[0];

        var frame = FrameOf(LastLegendGroup(figure.Drawn()));

        Assert.True(plot.X + plot.Width + ConstrainedLayoutEngine.PadRight <= frame.X, $"plot {plot}, legend {frame}");
    }

    /// <summary>matplotlib 3.11.2 keeps room only for a legend placed outside: <c>fig.legend(loc='upper right')</c> under the
    /// constrained layout leaves the axes ending at 0.993 of the width, as with no legend at all.</summary>
    [Theory]
    [InlineData(LegendPosition.UpperRight)]
    [InlineData(LegendPosition.Best)]
    [InlineData(LegendPosition.Center)]
    public void AFigureLegendInsideTheFigure_TakesNoRoom(LegendPosition position)
    {
        Assert.Equal(SpacingOf(OneSubplot(b => b.ConstrainedLayout(), null)), SpacingOf(OneSubplot(b => b.ConstrainedLayout(), position)));
    }

    /// <summary>matplotlib 3.11.2 keeps room for a figure legend under its constrained layout only: the same
    /// <c>loc='outside right upper'</c> leaves the axes ending at 0.977 of the width under <c>tight_layout</c> and at 0.900 (the
    /// subplot parameters) with no layout, the places they end at without the legend.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheTightAndTheDefaultLayout_KeepNoRoomForAFigureLegend(bool tight)
    {
        Func<FigureBuilder, FigureBuilder> layout = tight ? b => b.TightLayout() : b => b;

        Assert.Equal(SpacingOf(OneSubplot(layout, null)), SpacingOf(OneSubplot(layout, LegendPosition.OutsideRight)));
    }

    [Fact]
    public void AHiddenOutsideFigureLegend_TakesNoRoom()
    {
        var hidden = Plt.Create().WithSize(800, 600).ConstrainedLayout()
            .AddSubPlot(1, 1, 1, ax => ax.WithLegend(visible: false).Plot(X, Y, s => s.Label = "a"))
            .FigLegend(legend => legend with { Position = LegendPosition.OutsideRight, Visible = false })
            .Build();

        Assert.Equal(SpacingOf(OneSubplot(b => b.ConstrainedLayout(), null)), SpacingOf(hidden));
    }

    /// <summary>Two legends outside the same edge share the room the wider one needs, as matplotlib's constrained layout keeps it
    /// (3.11.2: <c>outside right upper</c> and then <c>outside right lower</c> leave the axes ending where the first alone did).</summary>
    [Fact]
    public void TwoOutsideFigureLegendsOnOneEdge_ShareTheRoomOfTheWider()
    {
        var wide = Plt.Create().WithSize(800, 600).ConstrainedLayout()
            .AddSubPlot(1, 1, 1, ax => ax.WithLegend(visible: false).Plot(X, Y, s => s.Label = "a").Plot(X, Y, s => s.Label = "a much longer label"))
            .FigLegend(legend => legend with { Position = LegendPosition.OutsideRight, NCols = 2 })
            .Build();
        var both = Plt.Create().WithSize(800, 600).ConstrainedLayout()
            .AddSubPlot(1, 1, 1, ax => ax.WithLegend(visible: false).Plot(X, Y, s => s.Label = "a").Plot(X, Y, s => s.Label = "a much longer label"))
            .FigLegend(legend => legend with { Position = LegendPosition.OutsideRight, NCols = 2 })
            .FigLegend(legend => legend with { Position = LegendPosition.OutsideRight })
            .Build();

        Assert.Equal(SpacingOf(wide), SpacingOf(both));
    }

    // ── Static ─────────────────────────────────────────────────────────────

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // As in matplotlib a figure legend is only a picture: no entry carries the series index the legend toggle reads, whatever the
        // figure asks for.
        // The toggle script names the attribute in its own text, so the markup is looked for by its attribute form.
        new DrawnToken("TheFigureLegend_IsStatic_EvenWhenTheFigureTogglesSeriesByLegend (a figure legend)", () => Subplots("a", "b").WithLegendToggle().FigLegend().Build().ToSvg(), ">a<") { Absent = ["data-legend-index=\""] },
        new DrawnToken("TheFigureLegend_IsStatic_EvenWhenTheFigureTogglesSeriesByLegend (an axes legend)", () => Plt.Create().WithLegendToggle()
            .AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y, s => s.Label = "a"))
            .Build().ToSvg(), "data-legend-index=\""),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    // ── Above the subplots, the same on both paths ─────────────────────────

    [Fact]
    public void TheLegend_IsDrawnAboveTheSubplots_TheColourBar_AndTheTexts()
    {
        var figure = Plt.Create()
            .WithColorBar()
            .AddSubPlot(1, 1, 1, ax => ax.WithLegend(visible: false).WithTitle("Panel")
                .Heatmap(new double[,] { { 1, 2 }, { 3, 4 } })
                .Plot(X, Y, s => s.Label = "line"))
            .FigText("Note", 0.5, 0.02)
            .FigLegend(legend => legend with { Position = LegendPosition.UpperLeft })
            .Build();

        string svg = figure.ToSvg();
        int panel = svg.IndexOf(">Panel<", StringComparison.Ordinal);
        int colourBar = svg.IndexOf("<g class=\"figure-colorbar\">", StringComparison.Ordinal);
        int note = svg.IndexOf(">Note<", StringComparison.Ordinal);
        int legend = svg.IndexOf("<g class=\"legend\"", StringComparison.Ordinal);
        Assert.True(panel >= 0 && panel < colourBar && colourBar < note && note < legend,
            $"panel {panel}, colour bar {colourBar}, note {note}, legend {legend}");

        var ctx = figure.Drawn();
        int recordedNote = ctx.Calls.FindIndex(c => c is DrawTextCall { Text: "Note" });
        int recordedLegend = ctx.Calls.FindLastIndex(c => c is BeginGroupCall { CssClass: "legend" });
        Assert.True(recordedNote >= 0 && recordedNote < recordedLegend, $"note {recordedNote}, legend {recordedLegend}");
    }

    /// <summary><c>ToSvg</c> lays the subplots out in its own loop and <see cref="ChartRenderer.Render"/> in turn; both reach the figure
    /// legend through the one overlay pass, so on the same surface (an SVG one, which measures text) they write the same legend.</summary>
    [Fact]
    public void TheSvgAndTheRendererPath_DrawTheSameFigureLegend()
    {
        var figure = Subplots("alpha", "beta")
            .FigLegend(legend => legend with { Position = LegendPosition.LowerCenter, Title = "Runs" }).Build();

        string svg = figure.ToSvg();
        int begin = svg.LastIndexOf("<g class=\"legend\"", StringComparison.Ordinal);
        Assert.True(begin >= 0, "the SVG has no legend");
        string legendFragment = svg[begin..(svg.IndexOf("</g>", begin, StringComparison.Ordinal) + "</g>".Length)];
        var direct = new MatPlotLibNet.Rendering.Svg.SvgRenderContext();
        new ChartRenderer().Render(figure, direct);

        Assert.Contains(legendFragment, direct.GetOutput());
        Assert.Contains(">alpha<", legendFragment);
        Assert.Contains(">Runs<", legendFragment);
    }

    /// <summary>The place is the same wherever the text is measured: the frame of the recorded path stands where the layout of the
    /// figure rectangle puts it (the SVG surface measures wider text, so only the edge the box is anchored to is compared).</summary>
    [Fact]
    public void TheLegend_IsAnchoredToTheSameEdge_OnBothSurfaces()
    {
        var figure = Subplots("alpha", "beta")
            .FigLegend(legend => legend with { Position = LegendPosition.LowerLeft }).Build();

        var recorded = FrameOf(LastLegendGroup(figure.Drawn()));
        string svg = figure.ToSvg();
        var frame = svg.LegendFrames().LastOrDefault();

        Assert.True(frame.Width > 0, "the SVG legend has no frame");
        Assert.Equal(recorded.X, frame.X, 0.01);
        Assert.Equal(recorded.Y + recorded.Height, frame.Y + frame.Height, 0.01);
    }

    [Fact]
    public void ALegendOnAFigure_IsDrawnTheSameWhenTheFigureIsRenderedTwice()
    {
        var figure = Subplots("alpha", "beta").FigLegend().Build();

        Assert.Equal(figure.ToSvg(), figure.ToSvg());
        Assert.Equal(LabelsOf(LastLegendGroup(figure.Drawn())), LabelsOf(LastLegendGroup(figure.Drawn())));
    }
}
