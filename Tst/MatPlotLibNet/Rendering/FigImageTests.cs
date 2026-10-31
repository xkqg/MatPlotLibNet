// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>An image placed on the figure itself (matplotlib's <c>figimage</c>): its pixels are the cells of an array, its place
/// is a pair of pixel offsets from the bottom-left corner of the figure, each cell is one pixel (no resampling), and it is drawn
/// under every subplot, in the SVG and in every raster or native output alike.</summary>
public class FigImageTests
{
    private static readonly double[,] Two = { { 1, 2, 3 }, { 4, 5, 6 } };

    private static RecordingRenderContext Record(Figure figure)
    {
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);
        return ctx;
    }

    private static dynamic Args(CallRecord call) => call.Args!;

    /// <summary>The rectangles one pixel across: the cells of an image drawn at its native size, in the order they were drawn.</summary>
    private static List<CallRecord> CellCalls(RecordingRenderContext ctx) =>
        [.. ctx.Calls.Where(c => c.Kind == "DrawRectangle" && ((Rect)Args(c).rect) is { Width: 1, Height: 1 })];

    private static List<Rect> Cells(RecordingRenderContext ctx) => [.. CellCalls(ctx).Select(c => (Rect)Args(c).rect)];

    private static List<Color> Fills(RecordingRenderContext ctx) => [.. CellCalls(ctx).Select(c => (Color)Args(c).fill)];

    private static Func<FigureBuilder> OneImage(double[,] data, double x = 0, double y = 0, Action<FigureImage>? configure = null) =>
        () => Plt.Create().WithSize(800, 600).FigImage(data, x, y, configure);

    private static readonly Regex SvgCell = new("<rect x=\"([^\"]+)\" y=\"([^\"]+)\" width=\"1\" height=\"1\"");

    private static List<Rect> SvgCells(string svg) =>
        [.. SvgCell.Matches(svg).Select(m => new Rect(
            double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
            double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture), 1, 1))];

    // ── The model and the builder ──────────────────────────────────────────

    [Fact]
    public void ANewImage_HoldsItsPixelsAndItsPlace_InTheResolutionsDefaults()
    {
        var image = new FigureImage(Two, 12, 34);

        Assert.Same(Two, image.Data);
        Assert.Equal(12, image.XOffset);
        Assert.Equal(34, image.YOffset);
        Assert.Null(image.ColorMap);
        Assert.Null(image.Normalizer);
        Assert.Null(image.VMin);
        Assert.Null(image.VMax);
        Assert.Equal(1.0, image.Alpha);
    }

    [Fact]
    public void AnImageWithNoPixels_IsRefused() =>
        Assert.Throws<ArgumentNullException>("data", () => new FigureImage(null!, 0, 0));

    [Fact]
    public void AFigure_HasNoImages_UntilOneIsAdded() => Assert.Empty(new Figure().Images);

    [Fact]
    public void AddImage_AddsTheImagesInOrder_AndReturnsEach()
    {
        var figure = new Figure();

        var first = figure.AddImage(Two, 1, 2);
        var second = figure.AddImage(new double[,] { { 9 } });

        Assert.Equal(new[] { first, second }, figure.Images);
        Assert.Equal(1, first.XOffset);
        Assert.Equal(2, first.YOffset);
        Assert.Equal(0, second.XOffset);
        Assert.Equal(0, second.YOffset);
    }

    [Fact]
    public void FigImage_AddsAConfiguredImage_AndReturnsTheSameBuilder()
    {
        var builder = Plt.Create();
        var map = ColorMaps.Viridis;
        var normalizer = new LogNormalizer();

        var returned = builder.FigImage(Two, 5, 6, image =>
        {
            image.ColorMap = map;
            image.Normalizer = normalizer;
            image.VMin = 0;
            image.VMax = 10;
            image.Alpha = 0.5;
        });

        Assert.Same(builder, returned);
        var image = Assert.Single(builder.Build().Images);
        Assert.Same(Two, image.Data);
        Assert.Equal(5, image.XOffset);
        Assert.Equal(6, image.YOffset);
        Assert.Same(map, image.ColorMap);
        Assert.Same(normalizer, image.Normalizer);
        Assert.Equal(0, image.VMin);
        Assert.Equal(10, image.VMax);
        Assert.Equal(0.5, image.Alpha);
    }

    [Fact]
    public void FigImage_WithoutOffsetsOrConfigure_StandsInTheBottomLeftCorner_AndAddsNoAxes()
    {
        var figure = Plt.Create().FigImage(Two).Build();

        var image = Assert.Single(figure.Images);
        Assert.Equal(0, image.XOffset);
        Assert.Equal(0, image.YOffset);
        Assert.Null(image.ColorMap);
        Assert.Empty(figure.SubPlots);
    }

    [Fact]
    public void FigImage_WithNoPixels_IsRefusedAtTheCall() =>
        Assert.Throws<ArgumentNullException>("data", () => Plt.Create().FigImage(null!));

    [Fact]
    public void EachBuild_HasAnImageOfItsOwn()
    {
        var builder = Plt.Create().FigImage(Two);
        var first = builder.Build();
        var second = builder.Build();

        first.Images[0].Alpha = 0.25;

        Assert.NotSame(first.Images[0], second.Images[0]);
        Assert.Equal(1.0, second.Images[0].Alpha);
    }

    // ── Where it stands, how big it is ─────────────────────────────────────

    /// <summary>The offsets count pixels from the bottom-left corner of the figure, as figimage's xo and yo do, and the first row
    /// of the array is the top row of the image: a 2 by 3 image at (10, 20) of a 600 pixel high figure covers y 578 to 580.</summary>
    [Fact]
    public void TheImage_StandsAtItsPixelOffset_FromTheBottomLeftCorner_FirstRowOnTop()
    {
        var ctx = Record(OneImage(Two, 10, 20)().Build());

        Assert.Equal(
            [new Rect(10, 578, 1, 1), new Rect(11, 578, 1, 1), new Rect(12, 578, 1, 1),
             new Rect(10, 579, 1, 1), new Rect(11, 579, 1, 1), new Rect(12, 579, 1, 1)],
            Cells(ctx));
    }

    [Theory]
    [InlineData(800, 600)]
    [InlineData(400, 300)]
    [InlineData(1600, 1200)]
    public void EachCell_IsOnePixel_WhateverTheSizeOfTheFigure(double width, double height)
    {
        var figure = Plt.Create().WithSize(width, height).FigImage(Two, 0, 0).Build();

        var cells = Cells(Record(figure));

        Assert.Equal(6, cells.Count);
        Assert.All(cells, cell => Assert.Equal((1.0, 1.0), (cell.Width, cell.Height)));
        Assert.Equal(new Rect(0, height - 2, 1, 1), cells[0]);
    }

    [Fact]
    public void EachCell_IsOnePixelOfTheLayout_AtAnyResolution()
    {
        var figure = Plt.Create().WithSize(800, 600).WithDpi(192).FigImage(Two, 4, 8).Build();

        Assert.Equal(6, Cells(Record(figure)).Count);
        Assert.Equal(new Rect(4, 590, 1, 1), Cells(Record(figure))[0]);
    }

    [Fact]
    public void ABigImage_IsNotResampled_ItIsOneRectangleForEachCell()
    {
        var data = new double[40, 50];
        for (int r = 0; r < 40; r++)
            for (int c = 0; c < 50; c++)
                data[r, c] = r * 50 + c;

        var cells = Cells(Record(OneImage(data)().Build()));

        Assert.Equal(2000, cells.Count);
        Assert.Equal(new Rect(0, 560, 1, 1), cells[0]);
        Assert.Equal(new Rect(49, 599, 1, 1), cells[^1]);
    }

    [Fact]
    public void AnImageThatReachesPastTheFigure_KeepsItsCells_TheEdgeCutsThem()
    {
        var cells = Cells(Record(OneImage(Two, -1, 599)().Build()));

        Assert.Equal(6, cells.Count);
        Assert.Equal(new Rect(-1, -1, 1, 1), cells[0]);
    }

    [Theory]
    [InlineData(double.NaN, 0)]
    [InlineData(0, double.NaN)]
    [InlineData(double.PositiveInfinity, 0)]
    [InlineData(0, double.NegativeInfinity)]
    public void AnImageAtNoPlace_IsNotDrawn(double x, double y)
    {
        var figure = OneImage(Two, x, y)().Build();

        Assert.Empty(Cells(Record(figure)));
        Assert.Empty(SvgCells(figure.ToSvg()));
    }

    /// <summary>An image shows when it has a place on the figure and at least one pixel. The data table and the MCP reader both
    /// ask this one rule what the picture carries of its own.</summary>
    [Theory]
    [InlineData(2, 3, 0, 0, true)]
    [InlineData(1, 1, -5, 1000, true)]
    [InlineData(0, 3, 0, 0, false)]
    [InlineData(2, 0, 0, 0, false)]
    [InlineData(2, 3, double.NaN, 0, false)]
    [InlineData(2, 3, 0, double.PositiveInfinity, false)]
    public void AnImageShows_WhenItHasAPlaceAndAPixel(int rows, int columns, double x, double y, bool shows) =>
        Assert.Equal(shows, new FigureImage(new double[rows, columns], x, y).IsShownOn(600));

    [Theory]
    [InlineData(0, 3)]
    [InlineData(2, 0)]
    public void AnImageWithNoRowsOrNoColumns_DrawsNothing_NotEvenItsOpacity(int rows, int columns)
    {
        var figure = OneImage(new double[rows, columns], 0, 0, image => image.Alpha = 0.5)().Build();

        var ctx = Record(figure);

        Assert.Empty(Cells(ctx));
        Assert.Equal(0, ctx.CountOf("SetOpacity"));
    }

    // ── Colours ────────────────────────────────────────────────────────────

    [Fact]
    public void WithoutAColourMap_TheCellsAreColouredLikeAnImageSeries_OverTheRangeOfTheValues()
    {
        var fills = Fills(Record(OneImage(Two)().Build()));

        Assert.Equal(ColorMaps.Viridis.GetColor(0), fills[0]);
        Assert.Equal(ColorMaps.Viridis.GetColor(1), fills[^1]);
        Assert.Equal(ColorMaps.Viridis.GetColor(0.4), fills[2]);
    }

    [Fact]
    public void TheColourMap_ColoursTheCells()
    {
        var map = new LinearColorMap("test", [Colors.Red, Colors.Blue]);
        var data = new double[,] { { 0, 1 } };

        var fills = Fills(Record(OneImage(data, 0, 0, image => image.ColorMap = map)().Build()));

        Assert.Equal([Colors.Red, Colors.Blue], fills);
    }

    [Fact]
    public void VMinAndVMax_AreTheColourRange()
    {
        var map = new LinearColorMap("test", [Colors.Red, Colors.Blue]);
        var data = new double[,] { { 1, 2 } };

        var fills = Fills(Record(OneImage(data, 0, 0, image => { image.ColorMap = map; image.VMin = 0; image.VMax = 4; })().Build()));

        Assert.Equal([map.GetColor(0.25), map.GetColor(0.5)], fills);
    }

    [Fact]
    public void TheNormalizer_MapsTheValuesOntoTheColourMap()
    {
        var map = new LinearColorMap("test", [Colors.Red, Colors.Blue]);
        var normalizer = new LogNormalizer();
        var data = new double[,] { { 1, 10, 100 } };

        var fills = Fills(Record(OneImage(data, 0, 0, image => { image.ColorMap = map; image.Normalizer = normalizer; })().Build()));

        Assert.Equal([map.GetColor(normalizer.Normalize(1, 1, 100)), map.GetColor(normalizer.Normalize(10, 1, 100)), map.GetColor(normalizer.Normalize(100, 1, 100))], fills);
    }

    /// <summary>A missing value (NaN) is the one gap: no cell is painted for it (the colour map names no bad colour, so it is
    /// transparent), the other cells keep their place, and the value is left out of the range the others are coloured over.</summary>
    [Fact]
    public void ANaNCell_IsNoCell_AndLeavesTheRangeAlone()
    {
        var data = new double[,] { { 1, double.NaN }, { 3, 4 } };

        var ctx = Record(OneImage(data)().Build());

        Assert.Equal([new Rect(0, 598, 1, 1), new Rect(0, 599, 1, 1), new Rect(1, 599, 1, 1)], Cells(ctx));
        Assert.Equal(ColorMaps.Viridis.GetColor(0), Fills(ctx)[0]);
        Assert.Equal(ColorMaps.Viridis.GetColor(1), Fills(ctx)[^1]);
    }

    [Fact]
    public void ANaNCell_IsDrawnInTheBadColour_WhenTheColourMapNamesOne()
    {
        var bad = Color.FromHex("#13579B");
        var map = new LinearColorMap("test", [Colors.Red, Colors.Blue]) { BadColor = bad };
        var data = new double[,] { { 0, double.NaN, 1 } };

        var ctx = Record(OneImage(data, 0, 0, image => image.ColorMap = map)().Build());

        Assert.Equal([Colors.Red, bad, Colors.Blue], Fills(ctx));
        Assert.Equal(3, Cells(ctx).Count);
    }

    [Fact]
    public void AnImageOfNothingButNaN_IsNoCell()
    {
        var ctx = Record(OneImage(new double[,] { { double.NaN, double.NaN } })().Build());

        Assert.Empty(Cells(ctx));
    }

    [Fact]
    public void Alpha_IsTheOpacityOfTheWholeImage_AndIsResetAfterIt()
    {
        var ctx = Record(OneImage(Two, 0, 0, image => image.Alpha = 0.5)().Build());

        int first = ctx.Calls.FindIndex(c => c.Kind == "SetOpacity");
        int last = ctx.Calls.FindLastIndex(c => c.Kind == "SetOpacity");
        Assert.Equal(0.5, (double)Args(ctx.Calls[first]).opacity);
        Assert.Equal(1.0, (double)Args(ctx.Calls[last]).opacity);
        Assert.Equal(2, ctx.CountOf("SetOpacity"));
        Assert.All(CellCalls(ctx), cell =>
        {
            int at = ctx.Calls.IndexOf(cell);
            Assert.InRange(at, first + 1, last - 1);
        });
        Assert.Contains("<g opacity=\"0.5\">", OneImage(Two, 0, 0, image => image.Alpha = 0.5)().Build().ToSvg());
    }

    [Fact]
    public void AnOpaqueImage_NeverTouchesTheOpacity()
    {
        var figure = OneImage(Two)().Build();

        Assert.Equal(0, Record(figure).CountOf("SetOpacity"));
        Assert.DoesNotContain("opacity=\"", figure.ToSvg());
    }

    // ── Under the subplots, on both paths ──────────────────────────────────

    [Fact]
    public void TheImage_IsDrawnAfterTheBackground_AndUnderEverySubplot()
    {
        var figure = Plt.Create().WithSize(800, 600).WithTitle("Super")
            .AddSubPlot(1, 1, 1, ax => ax.WithTitle("Panel").Plot([0.0, 1.0], [0.0, 1.0]))
            .FigImage(Two, 10, 20)
            .Build();

        var ctx = Record(figure);
        int background = ctx.Calls.FindIndex(c => c.Kind == "DrawRectangle");
        int firstCell = ctx.Calls.IndexOf(CellCalls(ctx)[0]);
        int panel = ctx.Calls.FindIndex(c => c.Kind == "DrawText" && (string)Args(c).text == "Panel");
        Assert.True(background < firstCell && firstCell < panel, $"background {background}, cell {firstCell}, panel {panel}");

        string svg = figure.ToSvg();
        int svgBackground = svg.IndexOf("<rect", StringComparison.Ordinal);
        int svgCell = svg.IndexOf("<rect x=\"10\" y=\"578\" width=\"1\" height=\"1\"", StringComparison.Ordinal);
        int svgPanel = svg.IndexOf(">Panel<", StringComparison.Ordinal);
        Assert.True(svgBackground >= 0 && svgBackground < svgCell && svgCell < svgPanel,
            $"background {svgBackground}, cell {svgCell}, panel {svgPanel}");
    }

    [Fact]
    public void TheSvgAndTheRendererPath_DrawTheSameCells_InTheSameColours()
    {
        var map = new LinearColorMap("test", [Colors.Red, Colors.Blue]) { BadColor = Color.FromHex("#13579B") };
        var data = new double[,] { { 0, double.NaN }, { 0.5, 1 } };
        var figure = OneImage(data, 30, 40, image => { image.ColorMap = map; image.Alpha = 0.75; })().Build();

        var recorded = Cells(Record(figure));
        string svg = figure.ToSvg();

        Assert.Equal(recorded, SvgCells(svg));
        foreach (var fill in Fills(Record(figure)))
            Assert.Contains($"fill=\"{fill.ToHex()}\"", svg);
    }

    [Fact]
    public void AFigureWithoutSubplots_StillDrawsItsImage_OnBothPaths()
    {
        var figure = OneImage(Two, 10, 20)().Build();
        Assert.Empty(figure.SubPlots);

        Assert.Equal(6, Cells(Record(figure)).Count);
        Assert.Equal(6, SvgCells(figure.ToSvg()).Count);
    }

    [Fact]
    public void TwoImages_AreDrawnInTheOrderTheyWereAdded_TheLaterOverTheEarlier()
    {
        var figure = Plt.Create().WithSize(800, 600)
            .FigImage(new double[,] { { 1 } }, 0, 0)
            .FigImage(new double[,] { { 1 } }, 5, 5)
            .Build();

        var cells = Cells(Record(figure));

        Assert.Equal([new Rect(0, 599, 1, 1), new Rect(5, 594, 1, 1)], cells);
        Assert.Equal([new Rect(0, 599, 1, 1), new Rect(5, 594, 1, 1)], SvgCells(figure.ToSvg()));
    }

    [Fact]
    public void TheImage_StaysUnderTheTextsAndTheLegend_OfTheFigure()
    {
        var figure = Plt.Create().WithSize(800, 600)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1.0], [0.0, 1.0], s => s.Label = "line"))
            .FigText("Note", 0.5, 0.5)
            .FigLegend()
            .FigImage(Two)
            .Build();

        var ctx = Record(figure);
        int lastCell = ctx.Calls.IndexOf(CellCalls(ctx)[^1]);
        int note = ctx.Calls.FindIndex(c => c.Kind == "DrawText" && (string)Args(c).text == "Note");
        int legend = ctx.Calls.FindLastIndex(c => c.Kind == "BeginGroup" && (string)Args(c).cssClass == "legend");

        Assert.True(lastCell < note && note < legend, $"cell {lastCell}, note {note}, legend {legend}");
    }
}
