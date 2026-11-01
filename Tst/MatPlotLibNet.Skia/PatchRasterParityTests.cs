// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>A patch's opacity reaches its face, its edge and its hatch in the PNG as it does in the SVG of the same figure. The
/// opacity is applied to each colour (matplotlib's per-colour alpha), which Skia paints per paint; the SVG writes it as
/// <c>fill-opacity</c> on the face, <c>stroke-opacity</c> on the edge and <c>fill-opacity</c> on a hatch tile's face, because its
/// hex colours carry none. Each probe reads the colour the SVG gives a part, at the opacity the SVG gives it, over the white plot,
/// and the pixel of the PNG at a place inside that part.</summary>
public class PatchRasterParityTests
{
    private const int Tolerance = 3;

    /// <summary>A colour as the SVG writes it, with the opacity it writes beside it.</summary>
    private readonly record struct Painted(string Hex, double Opacity)
    {
        public static Painted Of(SvgElement element, string colour, string opacity) =>
            new(element[colour]!, element[opacity] is { } value ? double.Parse(value, CultureInfo.InvariantCulture) : 1.0);

        /// <summary>The colour on a white page at its opacity.</summary>
        public SKColor OnWhite()
        {
            var colour = Color.FromHex(Hex);
            double opacity = Opacity;
            byte Blend(byte channel) => (byte)Math.Round(channel * opacity + 255 * (1 - opacity));
            return new SKColor(Blend(colour.R), Blend(colour.G), Blend(colour.B));
        }
    }

    /// <summary>The box of a patch's path in figure pixels, from the numbers of its <c>d</c> attribute.</summary>
    private readonly record struct Box(double Left, double Top, double Right, double Bottom)
    {
        public static Box Of(SvgElement path)
        {
            double[] numbers = [.. path["d"]!.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(token => token.Length > 0 && !char.IsLetter(token[0]))
                .Select(token => double.Parse(token, CultureInfo.InvariantCulture))];
            double[] xs = [.. numbers.Where((_, i) => i % 2 == 0)], ys = [.. numbers.Where((_, i) => i % 2 == 1)];
            return new Box(xs.Min(), ys.Min(), xs.Max(), ys.Max());
        }

        public double MiddleX => (Left + Right) / 2;

        public double MiddleY => (Top + Bottom) / 2;
    }

    private static Figure ThreePatches() => Plt.Create().WithSize(500, 300).AddSubPlot(1, 1, 1, ax => ax
        .ShowGrid(false)
        .AddPatch(new Rectangle(new(1, 1), 2, 2) { FaceColor = Colors.Red, Alpha = 0.5 })
        .AddPatch(new Rectangle(new(4, 1), 2, 2) { FaceColor = Colors.Transparent, EdgeColor = Colors.Blue, EdgeWidth = 8, Alpha = 0.5 })
        .AddPatch(new Rectangle(new(7, 1), 2, 2) { FaceColor = Colors.Green, Hatch = HatchPattern.Horizontal, HatchColor = Colors.Black, Alpha = 0.5 }))
        .Build();

    private static void AssertPixel(SKBitmap picture, Figure figure, double x, double y, SKColor expected, string part)
    {
        var found = picture.GetPixel((int)Math.Floor(x * picture.Width / figure.Width), (int)Math.Floor(y * picture.Height / figure.Height));
        Assert.True(Math.Abs(found.Red - expected.Red) <= Tolerance && Math.Abs(found.Green - expected.Green) <= Tolerance && Math.Abs(found.Blue - expected.Blue) <= Tolerance,
            $"{part}: the SVG draws #{expected.Red:X2}{expected.Green:X2}{expected.Blue:X2} there, the PNG has #{found.Red:X2}{found.Green:X2}{found.Blue:X2}");
    }

    [Fact]
    public void ATranslucentFace_IsTheSameInThePngAsInTheSvg()
    {
        var figure = ThreePatches();
        var face = figure.ToSvg().Elements("path").Single(path => path["fill"] == "#FF0000");
        using var picture = SKBitmap.Decode(figure.ToPng());

        var painted = Painted.Of(face, "fill", "fill-opacity");
        var box = Box.Of(face);

        Assert.Equal(0.5, painted.Opacity, 2);
        AssertPixel(picture, figure, box.MiddleX, box.MiddleY, painted.OnWhite(), "the face");
    }

    [Fact]
    public void ATranslucentEdge_IsTheSameInThePngAsInTheSvg()
    {
        var figure = ThreePatches();
        var edge = figure.ToSvg().Elements("path").Single(path => path["stroke"] == "#0000FF");
        using var picture = SKBitmap.Decode(figure.ToPng());

        var painted = Painted.Of(edge, "stroke", "stroke-opacity");
        var box = Box.Of(edge);

        Assert.Equal("none", edge["fill"]);
        Assert.Equal(0.5, painted.Opacity, 2);
        AssertPixel(picture, figure, box.Left, box.MiddleY, painted.OnWhite(), "the left edge");
    }

    /// <summary>The face of a hatched patch is in the hatch's tile; read between two lines of a horizontal hatch (the tile is
    /// 6 units high from the page's origin, its line at 3), it is the face at its opacity.</summary>
    [Fact]
    public void ATranslucentHatchedFace_IsTheSameInThePngAsInTheSvg()
    {
        var figure = ThreePatches();
        string svg = figure.ToSvg();
        var hatched = svg.Elements("path").Single(path => path["fill"] is { } fill && fill.StartsWith("url(#", StringComparison.Ordinal));
        var tileFace = svg.Elements("rect").Single(rect => rect["fill"] == "#008000");
        using var picture = SKBitmap.Decode(figure.ToPng());

        var painted = Painted.Of(tileFace, "fill", "fill-opacity");
        var box = Box.Of(hatched);
        double y = Math.Ceiling(box.MiddleY / 6) * 6 + 0.5;

        Assert.Equal(0.5, painted.Opacity, 2);
        AssertPixel(picture, figure, box.MiddleX, y, painted.OnWhite(), "the hatched face between two lines");
    }
}
