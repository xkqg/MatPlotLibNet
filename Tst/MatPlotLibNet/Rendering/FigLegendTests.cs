// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A legend of the figure as a whole (matplotlib's <c>figlegend</c>): it lists the labelled series of every subplot
/// once, in subplot order, stands in a rectangle that is the figure, is static, is drawn above the subplots and the texts,
/// and is the same picture in the SVG and in every raster or native output. A second <c>FigLegend</c> replaces the first.</summary>
public class FigLegendTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0];
    private static readonly double[] Y = [1.0, 3.0, 2.0];

    private static RecordingRenderContext Record(Figure figure)
    {
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);
        return ctx;
    }

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
        int begin = ctx.Calls.FindLastIndex(c => c.Kind == "BeginGroup" && (string)((dynamic)c.Args!).cssClass == "legend");
        Assert.True(begin >= 0, "no legend group was drawn");
        int end = ctx.Calls.FindIndex(begin, c => c.Kind == "EndGroup");
        return ctx.Calls.GetRange(begin, end - begin + 1);
    }

    private static string[] LabelsOf(List<CallRecord> legendGroup) =>
        [.. legendGroup.Where(c => c.Kind == "DrawText").Select(c => (string)((dynamic)c.Args!).text)];

    private static int LegendGroups(RecordingRenderContext ctx) =>
        ctx.Calls.Count(c => c.Kind == "BeginGroup" && (string)((dynamic)c.Args!).cssClass == "legend");

    private static Rect FrameOf(List<CallRecord> legendGroup) =>
        (Rect)((dynamic)legendGroup.First(c => c.Kind == "DrawRectangle").Args!).rect;

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
    public void AFigure_HasNoLegend_UntilOneIsSet()
    {
        Assert.Null(new Figure().Legend);
        Assert.Null(Subplots("a").Build().Legend);
    }

    [Fact]
    public void TheLegendOfAFigure_CanBeSetOnTheModel()
    {
        var legend = new Legend { Title = "Series" };

        var figure = new Figure { Legend = legend };

        Assert.Same(legend, figure.Legend);
    }

    [Fact]
    public void FigLegend_SetsAVisibleLegend_AndReturnsTheSameBuilder()
    {
        var builder = Subplots("a");

        var returned = builder.FigLegend();

        Assert.Same(builder, returned);
        Assert.Equal(new Legend(), builder.Build().Legend);
        Assert.True(builder.Build().Legend!.Visible);
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
        Assert.Equal("Runs", figure.Legend!.Title);
        Assert.Equal(2, figure.Legend.NCols);
        Assert.Equal(LegendPosition.LowerLeft, figure.Legend.Position);
    }

    /// <summary>A recorded deviation from matplotlib (which would add a second legend): one legend per figure, the latest wins
    /// and starts from a new legend, nothing of the first is kept.</summary>
    [Fact]
    public void ASecondFigLegend_ReplacesTheFirst()
    {
        Legend? received = null;

        var figure = Subplots("a")
            .FigLegend(legend => legend with { Title = "first", Position = LegendPosition.LowerLeft })
            .FigLegend(legend => { received = legend; return legend with { NCols = 3 }; })
            .Build();

        Assert.Equal(new Legend(), received);
        Assert.Null(figure.Legend!.Title);
        Assert.Equal(LegendPosition.Best, figure.Legend.Position);
        Assert.Equal(3, figure.Legend.NCols);
    }

    [Fact]
    public void ASecondFigLegend_WithoutConfigure_ReplacesTheFirstWithADefaultLegend()
    {
        var figure = Subplots("a").FigLegend(legend => legend with { Title = "first" }).FigLegend().Build();

        Assert.Equal(new Legend(), figure.Legend);
    }

    [Theory]
    [InlineData(LegendPosition.Best, LegendPosition.UpperRight)]
    [InlineData(LegendPosition.UpperRight, LegendPosition.UpperRight)]
    [InlineData(LegendPosition.LowerLeft, LegendPosition.LowerLeft)]
    [InlineData(LegendPosition.OutsideRight, LegendPosition.OutsideRight)]
    public void OnTheFigure_OnlyBest_BecomesTheUpperRight(LegendPosition given, LegendPosition onTheFigure)
    {
        var legend = new Legend { Position = given, Title = "kept", NCols = 3 };

        var placed = legend.OnTheFigure();

        Assert.Equal(legend with { Position = onTheFigure }, placed);
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
        Assert.Same(figure.SubPlots[0].Series[0], entries[0].Series);
        Assert.Same(figure.SubPlots[1].Series[0], entries[1].Series);
        Assert.Equal(LegendSwatchKind.LineMarker, entries[0].Swatch.Kind);
        Assert.Equal(LegendSwatchKind.Marker, entries[1].Swatch.Kind);
    }

    [Fact]
    public void TheLegend_ListsTheLabelledSeriesOfEverySubplot_InSubplotOrder()
    {
        var figure = Subplots("alpha", "beta", "gamma").FigLegend().Build();

        Assert.Equal(["alpha", "beta", "gamma"], LabelsOf(LastLegendGroup(Record(figure))));
    }

    [Fact]
    public void TheLegend_ListsTheEntriesOfOneSubplotTogether_BeforeTheNextSubplots()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.WithLegend(visible: false).Plot(X, Y, s => s.Label = "a1").Plot(X, Y, s => s.Label = "a2"))
            .AddSubPlot(1, 2, 2, ax => ax.WithLegend(visible: false).Plot(X, Y, s => s.Label = "b1"))
            .FigLegend()
            .Build();

        Assert.Equal(["a1", "a2", "b1"], LabelsOf(LastLegendGroup(Record(figure))));
    }

    [Fact]
    public void ASeriesWithoutALabel_HasNoEntry_AsInTheLegendOfAnAxes()
    {
        var figure = Subplots("alpha", null, "", "omega").FigLegend().Build();

        Assert.Equal(["alpha", "omega"], LabelsOf(LastLegendGroup(Record(figure))));
    }

    [Fact]
    public void ASeriesThatNamesItselfAtTheEndOfItsLine_LeavesTheLegend_AsInTheLegendOfAnAxes()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.WithLegend(visible: false).Plot(X, Y, s => { s.Label = "ends"; s.LabelPlacement = SeriesLabelPlacement.LineEnd; }))
            .AddSubPlot(1, 2, 2, ax => ax.WithLegend(visible: false).Plot(X, Y, s => s.Label = "listed"))
            .FigLegend()
            .Build();

        Assert.Equal(["listed"], LabelsOf(LastLegendGroup(Record(figure))));
    }

    /// <summary>The same label drawn the same way in two subplots is one entry: a legend for a grid of small multiples reads
    /// each name once.</summary>
    [Fact]
    public void AnEntryThatLooksTheSameInTwoSubplots_IsListedOnce()
    {
        var figure = Subplots("Observed", "Fit", "Observed", "Fit").FigLegend().Build();

        Assert.Equal(["Observed", "Fit"], LabelsOf(LastLegendGroup(Record(figure))));
    }

    [Fact]
    public void TheSameLabelInAnotherColour_IsListedAgain()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.WithLegend(visible: false).Plot(X, Y, s => { s.Label = "Observed"; s.Color = Colors.Blue; }))
            .AddSubPlot(1, 2, 2, ax => ax.WithLegend(visible: false).Plot(X, Y, s => { s.Label = "Observed"; s.Color = Colors.Red; }))
            .FigLegend()
            .Build();

        Assert.Equal(["Observed", "Observed"], LabelsOf(LastLegendGroup(Record(figure))));
    }

    [Fact]
    public void TheSameLabelDrawnAnotherWay_IsListedAgain()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.WithLegend(visible: false).Plot(X, Y, s => s.Label = "Observed"))
            .AddSubPlot(1, 2, 2, ax => ax.WithLegend(visible: false).Scatter(X, Y, s => s.Label = "Observed"))
            .FigLegend()
            .Build();

        Assert.Equal(["Observed", "Observed"], LabelsOf(LastLegendGroup(Record(figure))));
    }

    [Fact]
    public void TheSameLabelWithAnotherLineStyle_IsListedAgain()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.WithLegend(visible: false).Plot(X, Y, s => s.Label = "Observed"))
            .AddSubPlot(1, 2, 2, ax => ax.WithLegend(visible: false).Plot(X, Y, s => { s.Label = "Observed"; s.LineStyle = LineStyle.Dashed; }))
            .FigLegend()
            .Build();

        Assert.Equal(["Observed", "Observed"], LabelsOf(LastLegendGroup(Record(figure))));
    }

    [Fact]
    public void TheLegendValuesOfTheFigureLegend_SuffixTheLabels_WhateverTheSubplotsLegendsSay()
    {
        var plain = Subplots("signal").FigLegend().Build();
        var withValues = Subplots("signal").FigLegend(legend => legend with { LegendValues = true }).Build();

        Assert.Equal(["signal"], LabelsOf(LastLegendGroup(Record(plain))));
        Assert.Equal(["signal = 2.00"], LabelsOf(LastLegendGroup(Record(withValues))));
    }

    [Fact]
    public void TheLegendValuesOfASubplotsOwnLegend_DoNotReachTheFigureLegend()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.WithLegendValues().WithLegend(visible: false).Plot(X, Y, s => s.Label = "signal"))
            .FigLegend()
            .Build();

        Assert.Equal(["signal"], LabelsOf(LastLegendGroup(Record(figure))));
    }

    // ── Nothing to list, nothing to show ───────────────────────────────────

    [Fact]
    public void AFigureLegend_WithNoLabelledSeries_DrawsNothing()
    {
        var figure = Subplots(null, "").FigLegend().Build();

        Assert.Equal(0, LegendGroups(Record(figure)));
        Assert.DoesNotContain("class=\"legend\"", figure.ToSvg());
    }

    [Fact]
    public void AFigureLegend_OnAFigureWithoutSubplots_DrawsNothing()
    {
        var figure = Plt.Create().FigLegend().Build();

        Assert.Empty(figure.SubPlots);
        Assert.Equal(0, LegendGroups(Record(figure)));
        Assert.DoesNotContain("class=\"legend\"", figure.ToSvg());
    }

    [Fact]
    public void AHiddenFigureLegend_DrawsNothing()
    {
        var figure = Subplots("a", "b").FigLegend(legend => legend with { Visible = false }).Build();

        Assert.Equal(0, LegendGroups(Record(figure)));
        Assert.DoesNotContain("class=\"legend\"", figure.ToSvg());
    }

    [Fact]
    public void AFigureWithoutAFigureLegend_DrawsTheLegendsOfItsSubplotsOnly()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.Plot(X, Y, s => s.Label = "a"))
            .AddSubPlot(1, 2, 2, ax => ax.Plot(X, Y, s => s.Label = "b"))
            .Build();

        Assert.Equal(2, LegendGroups(Record(figure)));
    }

    [Fact]
    public void TheFigureLegend_IsOneMoreGroup_BesideTheLegendsOfTheSubplots()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.Plot(X, Y, s => s.Label = "a"))
            .AddSubPlot(1, 2, 2, ax => ax.Plot(X, Y, s => s.Label = "b"))
            .FigLegend()
            .Build();

        var ctx = Record(figure);

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

        var frame = FrameOf(LastLegendGroup(Record(figure)));

        AssertSameRect(ExpectedFrame(legend, figure.Theme, ["one", "two"]), frame);
    }

    [Fact]
    public void TheUpperRight_IsTenPixelsFromTheTopAndTheRightEdgeOfTheFigure()
    {
        var figure = Subplots("one").FigLegend(legend => legend with { Position = LegendPosition.UpperRight }).Build();

        var frame = FrameOf(LastLegendGroup(Record(figure)));

        Assert.Equal(800 - 10, frame.X + frame.Width, 9);
        Assert.Equal(10, frame.Y, 9);
    }

    /// <summary>Best looks for the place that covers the fewest marks of ONE axes; a figure has no such marks, so at the figure it
    /// means the upper right, under every theme (the classic theme says it, the others leave it to Best).</summary>
    [Theory]
    [InlineData("default")]
    [InlineData("classic")]
    [InlineData("dark")]
    public void ABestLegend_StandsInTheUpperRightOfTheFigure(string themeName)
    {
        var theme = themeName switch { "classic" => Theme.MatplotlibClassic, "dark" => Theme.Dark, _ => Theme.Default };
        var figure = Subplots("one", "two").WithTheme(theme).FigLegend().Build();
        Assert.Equal(LegendPosition.Best, figure.Legend!.Position);

        var frame = FrameOf(LastLegendGroup(Record(figure)));

        AssertSameRect(ExpectedFrame(new Legend { Position = LegendPosition.UpperRight }, theme, ["one", "two"]), frame);
    }

    /// <summary>The theme's own place for a legend that names none is the place for a legend among the marks of an axes; the
    /// figure has none, so a Best figure legend keeps to the upper right under a theme that says otherwise.</summary>
    [Fact]
    public void ABestLegend_KeepsToTheUpperRight_UnderAThemeThatNamesAnotherPlace()
    {
        var theme = new Theme("lower-left", Theme.Default) { DefaultLegendPosition = LegendPosition.LowerLeft };
        var figure = Subplots("one", "two").WithTheme(theme).FigLegend().Build();

        var frame = FrameOf(LastLegendGroup(Record(figure)));

        AssertSameRect(ExpectedFrame(new Legend { Position = LegendPosition.UpperRight }, theme, ["one", "two"]), frame);
        Assert.Equal(10, frame.Y, 9);
    }

    [Fact]
    public void TheLegend_StandsInTheFigure_NotInAPlotArea()
    {
        var figure = Plt.Create().WithSize(1000, 400)
            .AddSubPlot(1, 2, 1, ax => ax.WithLegend(visible: false).Plot(X, Y, s => s.Label = "a"))
            .AddSubPlot(1, 2, 2, ax => ax.WithLegend(visible: false).Plot(X, Y, s => s.Label = "b"))
            .FigLegend(legend => legend with { Position = LegendPosition.Center })
            .Build();

        var frame = FrameOf(LastLegendGroup(Record(figure)));

        Assert.Equal(500, frame.X + frame.Width / 2, 9);
        Assert.Equal(200, frame.Y + frame.Height / 2, 9);
    }

    [Fact]
    public void TheLegendOfAnotherSizeFigure_StandsInThatFigure()
    {
        var figure = Subplots("one").WithSize(400, 300)
            .FigLegend(legend => legend with { Position = LegendPosition.LowerLeft }).Build();

        var frame = FrameOf(LastLegendGroup(Record(figure)));

        AssertSameRect(ExpectedFrame(figure.Legend!, figure.Theme, ["one"], 400, 300), frame);
        Assert.Equal(300 - 10, frame.Y + frame.Height, 9);
    }

    [Fact]
    public void TheTitleAndTheColumns_OfTheFigureLegend_AreDrawnAsOfAnyLegend()
    {
        var figure = Subplots("a", "b", "c", "d")
            .FigLegend(legend => legend with { Title = "Runs", NCols = 2 }).Build();

        var group = LastLegendGroup(Record(figure));

        Assert.Equal(["Runs", "a", "b", "c", "d"], LabelsOf(group));
        AssertSameRect(ExpectedFrame(figure.Legend!, figure.Theme, ["a", "b", "c", "d"]), FrameOf(group));
    }

    // ── Static ─────────────────────────────────────────────────────────────

    /// <summary>As in matplotlib a figure legend is only a picture: no entry carries the series index the legend toggle reads,
    /// whatever the figure asks for.</summary>
    [Fact]
    public void TheFigureLegend_IsStatic_EvenWhenTheFigureTogglesSeriesByLegend()
    {
        var withFigureLegend = Subplots("a", "b").WithLegendToggle().FigLegend().Build();
        var withAxesLegend = Plt.Create().WithLegendToggle()
            .AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y, s => s.Label = "a"))
            .Build();

        string svg = withFigureLegend.ToSvg();

        // The toggle script names the attribute in its own text, so the markup is looked for by its attribute form.
        Assert.Contains(">a<", svg);
        Assert.DoesNotContain("data-legend-index=\"", svg);
        Assert.Contains("data-legend-index=\"", withAxesLegend.ToSvg());
    }

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

        var ctx = Record(figure);
        int recordedNote = ctx.Calls.FindIndex(c => c.Kind == "DrawText" && (string)((dynamic)c.Args!).text == "Note");
        int recordedLegend = ctx.Calls.FindLastIndex(c => c.Kind == "BeginGroup" && (string)((dynamic)c.Args!).cssClass == "legend");
        Assert.True(recordedNote >= 0 && recordedNote < recordedLegend, $"note {recordedNote}, legend {recordedLegend}");
    }

    /// <summary><c>ToSvg</c> lays the subplots out in parallel and <see cref="ChartRenderer.Render"/> in turn; both reach the figure
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

        var recorded = FrameOf(LastLegendGroup(Record(figure)));
        string svg = figure.ToSvg();
        var frame = Regex.Match(svg[svg.LastIndexOf("<g class=\"legend\"", StringComparison.Ordinal)..],
            "<rect x=\"([^\"]+)\" y=\"([^\"]+)\" width=\"([^\"]+)\" height=\"([^\"]+)\"");

        Assert.True(frame.Success, "the SVG legend has no frame");
        Assert.Equal(recorded.X, double.Parse(frame.Groups[1].Value, CultureInfo.InvariantCulture), 0.01);
        Assert.Equal(recorded.Y + recorded.Height, double.Parse(frame.Groups[2].Value, CultureInfo.InvariantCulture) + double.Parse(frame.Groups[4].Value, CultureInfo.InvariantCulture), 0.01);
    }

    [Fact]
    public void ALegendOnAFigure_IsDrawnTheSameWhenTheFigureIsRenderedTwice()
    {
        var figure = Subplots("alpha", "beta").FigLegend().Build();

        Assert.Equal(figure.ToSvg(), figure.ToSvg());
        Assert.Equal(LabelsOf(LastLegendGroup(Record(figure))), LabelsOf(LastLegendGroup(Record(figure))));
    }
}
