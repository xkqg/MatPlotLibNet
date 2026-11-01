// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>A line drawn in a translucent colour is as translucent in the SVG as in the PNG of the same figure. Skia paints a
/// stroke in its colour's alpha; the SVG's hex colour carries none, so the SVG writes it beside the colour as
/// <c>stroke-opacity</c>. Before, a grid line in a translucent colour, the minor grid (drawn at half the grid's alpha) and a
/// translucent line series were opaque in the SVG and translucent in the PNG. Each probe reads the colour the SVG gives a line,
/// at the opacity the SVG gives it, over the white plot, and the pixel of the PNG at the middle of that line.</summary>
public class LineStrokeRasterParityTests
{
    private const int Tolerance = 3;

    /// <summary>A stroke as the SVG writes it: its colour and the opacity written beside it (1 when none is).</summary>
    private readonly record struct Stroked(string Hex, double Opacity)
    {
        public static Stroked Of(SvgElement element) =>
            new(element["stroke"]!, element["stroke-opacity"] is { } value ? double.Parse(value, CultureInfo.InvariantCulture) : 1.0);

        /// <summary>The colour on a white page at its opacity.</summary>
        public SKColor OnWhite()
        {
            var colour = Color.FromHex(Hex);
            double opacity = Opacity;
            byte Blend(byte channel) => (byte)Math.Round(channel * opacity + 255 * (1 - opacity));
            return new SKColor(Blend(colour.R), Blend(colour.G), Blend(colour.B));
        }
    }

    /// <summary>An axes from 0 to 10 on both axes whose grid lines stand on the X ticks only, as vertical lines, so the middle
    /// of a grid line crosses nothing; its one series runs along the bottom edge.</summary>
    private static Figure GridOnly(Func<GridStyle, GridStyle> grid) => Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax => ax
        .Plot([0.0, 10.0], [0.0, 0.0], s => s.Color = Colors.Black)
        .SetXLim(0, 10).SetYLim(0, 10)
        .WithGrid(g => grid(g) with { Visible = true, Axis = GridAxis.X }))
        .Build();

    /// <summary>The vertical line in <paramref name="colour"/> nearest the middle of the figure.</summary>
    private static SvgElement MiddleLine(string svg, string colour, Figure figure) =>
        svg.Elements("line")
            .Where(line => line["stroke"] == colour && line.Number("x1") == line.Number("x2"))
            .MinBy(line => Math.Abs(line.Number("x1") - figure.Width / 2));

    private static void AssertPixel(SKBitmap picture, Figure figure, double x, double y, SKColor expected, string part)
    {
        var found = picture.GetPixel((int)Math.Floor(x * picture.Width / figure.Width), (int)Math.Floor(y * picture.Height / figure.Height));
        Assert.True(Math.Abs(found.Red - expected.Red) <= Tolerance && Math.Abs(found.Green - expected.Green) <= Tolerance && Math.Abs(found.Blue - expected.Blue) <= Tolerance,
            $"{part}: the SVG draws #{expected.Red:X2}{expected.Green:X2}{expected.Blue:X2} there, the PNG has #{found.Red:X2}{found.Green:X2}{found.Blue:X2}");
    }

    [Fact]
    public void ATranslucentGridLine_IsTheSameInThePngAsInTheSvg()
    {
        var figure = GridOnly(g => g with { Color = Colors.Blue.WithAlpha(128), LineWidth = 6 });
        var line = MiddleLine(figure.ToSvg(), "#0000FF", figure);
        using var picture = SKBitmap.Decode(figure.ToPng());

        var stroked = Stroked.Of(line);

        Assert.Equal(128 / 255.0, stroked.Opacity, 6);
        AssertPixel(picture, figure, line.Number("x1"), (line.Number("y1") + line.Number("y2")) / 2, stroked.OnWhite(), "the grid line");
    }

    /// <summary>The minor grid is drawn at half the alpha of the grid's colour, so an opaque grid has a translucent minor grid,
    /// as matplotlib draws it.</summary>
    [Fact]
    public void AMinorGridLine_IsTheSameInThePngAsInTheSvg()
    {
        var figure = GridOnly(g => g with { Color = Colors.Blue, LineWidth = 12, Which = GridWhich.Minor });
        var line = MiddleLine(figure.ToSvg(), "#0000FF", figure);
        using var picture = SKBitmap.Decode(figure.ToPng());

        var stroked = Stroked.Of(line);

        Assert.Equal(127 / 255.0, stroked.Opacity, 6);
        AssertPixel(picture, figure, line.Number("x1"), (line.Number("y1") + line.Number("y2")) / 2, stroked.OnWhite(), "the minor grid line");
    }

    /// <summary>A line series in a translucent colour is a polyline, written by the same rule as a line.</summary>
    [Fact]
    public void ATranslucentLineSeries_IsTheSameInThePngAsInTheSvg()
    {
        var figure = Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax => ax
            .ShowGrid(false)
            .Plot([0.0, 10.0], [5.0, 5.0], s => { s.Color = Colors.Red.WithAlpha(128); s.LineWidth = 8; })
            .SetXLim(0, 10).SetYLim(0, 10))
            .Build();
        var polyline = figure.ToSvg().Elements("polyline").Single(line => line["stroke"] == "#FF0000");
        using var picture = SKBitmap.Decode(figure.ToPng());

        double[] ends = [.. polyline["points"]!.Split(' ', ',').Select(number => double.Parse(number, CultureInfo.InvariantCulture))];
        var stroked = Stroked.Of(polyline);

        Assert.Equal(128 / 255.0, stroked.Opacity, 6);
        AssertPixel(picture, figure, (ends[0] + ends[^2]) / 2, (ends[1] + ends[^1]) / 2, stroked.OnWhite(), "the line");
    }
}
