// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering.Text;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>What a figure draws on itself rather than in a subplot is drawn by one pass that both orchestrators call: the
/// SVG writer (<c>ToSvg</c>, which lays the subplots out in parallel) and <see cref="ChartRenderer.Render"/> (PNG, PDF and
/// the native controls). So the two pictures carry the same overlays at the same places, a figure without subplots still
/// draws them, the colour bar is drawn from one site, and the overlays stand above the subplots.</summary>
public class FigureOverlayPassTests
{
    private static readonly double[,] Cells = { { 1, 2 }, { 3, 4 } };

    private static RecordingRenderContext Record(Figure figure)
    {
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);
        return ctx;
    }

    private static int IndexOfText(RecordingRenderContext ctx, string text) =>
        ctx.Calls.FindIndex(c => c.Kind == "DrawText" && (string)((dynamic)c.Args!).text == text);

    private static dynamic DrawnText(RecordingRenderContext ctx, string text) =>
        ctx.Calls[IndexOfText(ctx, text)].Args!;

    private static Point SvgTextPosition(string svg, string text)
    {
        var match = Regex.Match(svg, "<text x=\"([^\"]+)\" y=\"([^\"]+)\"[^>]*>" + Regex.Escape(text) + "</text>");
        Assert.True(match.Success, $"no <text> reading '{text}' in the picture");
        return new Point(double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
            double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture));
    }

    private static int Occurrences(string haystack, string needle) =>
        Regex.Matches(haystack, Regex.Escape(needle)).Count;

    [Theory]
    [InlineData(TextVerticalAlignment.Baseline)]
    [InlineData(TextVerticalAlignment.Top)]
    [InlineData(TextVerticalAlignment.Center)]
    public void TheSvgAndTheRendererPath_DrawTheSameText_AtTheSamePlace(TextVerticalAlignment vertical)
    {
        var figure = Plt.Create().WithSize(800, 600)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1.0], [0.0, 1.0]))
            .FigText("Figure note", 0.5, 0.97, t => { t.Alignment = TextAlignment.Center; t.VerticalAlignment = vertical; })
            .Build();

        var inSvg = SvgTextPosition(figure.ToSvg(), "Figure note");
        Point recorded = DrawnText(Record(figure), "Figure note").position;

        Assert.Equal(recorded.X, inSvg.X, 0.01);
        Assert.Equal(recorded.Y, inSvg.Y, 0.01);
        Assert.Equal(400, recorded.X, 9);
    }

    [Fact]
    public void AFigureWithoutSubplots_StillDrawsItsText_OnBothPaths()
    {
        var figure = Plt.Create().WithSize(800, 600).FigText("Alone", 0.5, 0.5).Build();
        Assert.Empty(figure.SubPlots);

        var inSvg = SvgTextPosition(figure.ToSvg(), "Alone");
        Point recorded = DrawnText(Record(figure), "Alone").position;

        Assert.Equal(new Point(400, 300), inSvg);
        Assert.Equal(new Point(400, 300), recorded);
    }

    [Fact]
    public void ATextAt097_StandsInTheTopBandOfTheFigure()
    {
        var figure = Plt.Create().WithSize(800, 600).Plot([0.0, 1.0], [0.0, 1.0]).FigText("Top", 0.5, 0.97).Build();

        var inSvg = SvgTextPosition(figure.ToSvg(), "Top");
        Point recorded = DrawnText(Record(figure), "Top").position;

        Assert.Equal(18, inSvg.Y, 9);
        Assert.Equal(18, recorded.Y, 9);
        Assert.InRange(recorded.Y, 0, 0.05 * figure.Height);
    }

    [Fact]
    public void ATextOutsideTheFigure_IsNotDrawn_OnEitherPath()
    {
        var figure = Plt.Create().Plot([0.0, 1.0], [0.0, 1.0])
            .FigText("Outside", 1.5, 0.5)
            .FigText("Missing", double.NaN, 0.5)
            .FigText("Inside", 0.5, 0.5)
            .Build();

        string svg = figure.ToSvg();
        var ctx = Record(figure);

        Assert.DoesNotContain(">Outside<", svg);
        Assert.DoesNotContain(">Missing<", svg);
        Assert.Contains(">Inside<", svg);
        Assert.Equal(-1, IndexOfText(ctx, "Outside"));
        Assert.Equal(-1, IndexOfText(ctx, "Missing"));
        Assert.NotEqual(-1, IndexOfText(ctx, "Inside"));
    }

    [Fact]
    public void ATextWithoutAFont_IsDrawnInTheThemesTextFont_InItsColour_TurnedAsAsked()
    {
        var theme = Theme.Default;
        var figure = Plt.Create().WithTheme(theme).Plot([0.0, 1.0], [0.0, 1.0])
            .FigText("Turned", 0.05, 0.5, t => { t.Color = Colors.Red; t.Rotation = 90; })
            .Build();

        var drawn = DrawnText(Record(figure), "Turned");
        Font font = drawn.font;

        Assert.Equal(ThemedFontProvider.DataTextFont(theme).Family, font.Family);
        Assert.Equal(ThemedFontProvider.DataTextFont(theme).Size, font.Size);
        Assert.Equal(Colors.Red, font.Color);
        Assert.Equal(90.0, (double)drawn.rotation);
        Assert.Equal(TextAlignment.Left, (TextAlignment)drawn.alignment);
    }

    [Fact]
    public void TheTexts_StandAboveTheSubplots_AndAboveTheColourBar()
    {
        var figure = Plt.Create()
            .WithColorBar()
            .AddSubPlot(1, 1, 1, ax => ax.WithTitle("Panel").Heatmap(Cells))
            .FigText("Note", 0.5, 0.02)
            .Build();

        string svg = figure.ToSvg();
        int panel = svg.IndexOf(">Panel<", StringComparison.Ordinal);
        int colourBar = svg.IndexOf("<g class=\"figure-colorbar\">", StringComparison.Ordinal);
        int note = svg.IndexOf(">Note<", StringComparison.Ordinal);
        Assert.True(panel >= 0 && panel < colourBar && colourBar < note, $"panel {panel}, colour bar {colourBar}, note {note}");

        var ctx = Record(figure);
        int recordedPanel = IndexOfText(ctx, "Panel");
        int recordedBar = ctx.Calls.FindIndex(c => c.Kind == "BeginGroup" && (string)((dynamic)c.Args!).cssClass == "figure-colorbar");
        int recordedNote = IndexOfText(ctx, "Note");
        Assert.True(recordedPanel >= 0 && recordedPanel < recordedBar && recordedBar < recordedNote,
            $"panel {recordedPanel}, colour bar {recordedBar}, note {recordedNote}");
    }

    [Fact]
    public void TheColourBar_IsDrawnOnce_OnBothPaths()
    {
        var figure = Plt.Create().WithColorBar(cb => cb with { Label = "Z" })
            .AddSubPlot(1, 2, 1, ax => ax.Heatmap(Cells))
            .AddSubPlot(1, 2, 2, ax => ax.Heatmap(Cells))
            .Build();

        Assert.Equal(1, Occurrences(figure.ToSvg(), "<g class=\"figure-colorbar\">"));
        Assert.Equal(1, Record(figure).Calls.Count(c => c.Kind == "BeginGroup" && (string)((dynamic)c.Args!).cssClass == "figure-colorbar"));
    }

    [Fact]
    public void AFigureWithoutSubplots_StillDrawsItsColourBar_OnBothPaths()
    {
        var figure = new Figure { FigureColorBar = new ColorBar { Visible = true, Label = "Level" } };

        string svg = figure.ToSvg();
        var ctx = Record(figure);

        Assert.Equal(1, Occurrences(svg, "<g class=\"figure-colorbar\">"));
        Assert.Contains(">Level<", svg);
        Assert.NotEqual(-1, IndexOfText(ctx, "Level"));
    }

    /// <summary>The colour bar has one drawing site, the overlay pass, and both orchestrators reach the figure-level
    /// passes through the same two methods: a second hand-wired call would let the two pictures drift apart again.</summary>
    [Fact]
    public void TheFigureLevelPasses_HaveOneSite_ReachedFromBothOrchestrators()
    {
        string src = Path.Combine(BidiClassTableTests.RepoRoot(), "Src", "MatPlotLibNet");
        string chartRenderer = File.ReadAllText(Path.Combine(src, "Rendering", "ChartRenderer.cs"));
        string svgTransform = File.ReadAllText(Path.Combine(src, "Transforms", "SvgTransform.cs"));
        int colourBarCallsInSrc = Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Sum(file => Occurrences(File.ReadAllText(file), "RenderFigureColorBar("));

        // The declaration and the one call inside the overlay pass.
        Assert.Equal(2, colourBarCallsInSrc);
        Assert.Equal(2, Occurrences(chartRenderer, "RenderFigureColorBar("));
        Assert.Equal(0, Occurrences(svgTransform, "RenderFigureColorBar("));
        // The declaration and the call from Render; one call from the SVG writer.
        Assert.Equal(2, Occurrences(chartRenderer, "RenderFigureOverlays("));
        Assert.Equal(1, Occurrences(svgTransform, "RenderFigureOverlays("));
        Assert.Equal(2, Occurrences(chartRenderer, "RenderFigureUnderlays("));
        Assert.Equal(1, Occurrences(svgTransform, "RenderFigureUnderlays("));
    }
}
