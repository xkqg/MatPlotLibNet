// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>A bar or an area that fills with a blend (<c>GradientColor</c>) shows it in the PNG as it does in the SVG: the colour
/// of the series at the foot of a bar or the baseline of an area, the gradient colour at its tip, the blend between, and the PDF
/// keeps it as a vector shading. The pixels are read inside the shape a little way from each end, so an anti-aliased edge never
/// decides the colour, on a shape tall enough that the blend has barely moved by then.</summary>
public class BarAreaGradientPixelTests
{
    private const int Tolerance = 3;
    private static readonly Color Foot = Color.FromHex("#1F77B4");
    private static readonly Color Tip = Color.FromHex("#FF7F0E");

    private static Figure Bar(double value, Action<AxesBuilder>? afterwards = null, BarOrientation orientation = BarOrientation.Vertical) =>
        Plt.Create().WithSize(600, 600).AddSubPlot(1, 1, 1, ax =>
        {
            ax.Bar(["a"], [value], s => { s.Color = Foot; s.GradientColor = Tip; s.Orientation = orientation; });
            ax.SetYLim(Math.Min(0, value) - 0.5, Math.Max(0, value) + 0.5);
            afterwards?.Invoke(ax);
        }).Build();

    /// <summary>The shape (<paramref name="kind"/>) whose fill is a gradient: its bounds in figure pixels.</summary>
    private static SvgElement Gradiented(Figure figure, string kind) =>
        figure.ToSvg().Elements(kind).Single(shape => shape["fill"] is { } fill && fill.StartsWith("url(#", StringComparison.Ordinal));

    private static SKColor PixelAt(Figure figure, double x, double y)
    {
        using var picture = SKBitmap.Decode(figure.ToPng());
        double scaleX = picture.Width / figure.Width, scaleY = picture.Height / figure.Height;
        return picture.GetPixel((int)Math.Floor(x * scaleX), (int)Math.Floor(y * scaleY));
    }

    private static void AssertNear(Color expected, SKColor actual, string where) =>
        Assert.True(Math.Abs(actual.Red - expected.R) <= Tolerance && Math.Abs(actual.Green - expected.G) <= Tolerance && Math.Abs(actual.Blue - expected.B) <= Tolerance,
            $"{where}: expected {expected.ToHex()}, the PNG has #{actual.Red:X2}{actual.Green:X2}{actual.Blue:X2}");

    [Fact]
    public void AnUpwardBar_IsTheSeriesColourAtItsFoot_AndTheGradientColourAtItsTop()
    {
        var figure = Bar(5);
        var rect = Gradiented(figure, "rect");
        double centre = rect.Number("x") + rect.Number("width") / 2, top = rect.Number("y"), bottom = top + rect.Number("height");

        AssertNear(Foot, PixelAt(figure, centre, bottom - 2), "two pixels above the foot");
        AssertNear(Tip, PixelAt(figure, centre, top + 2), "two pixels below the top");
    }

    [Fact]
    public void AFallingBar_IsTheGradientColourAtItsBottom_TheSeriesColourAtItsFoot()
    {
        var figure = Bar(-5);
        var rect = Gradiented(figure, "rect");
        double centre = rect.Number("x") + rect.Number("width") / 2, top = rect.Number("y"), bottom = top + rect.Number("height");

        AssertNear(Foot, PixelAt(figure, centre, top + 2), "two pixels below the foot");
        AssertNear(Tip, PixelAt(figure, centre, bottom - 2), "two pixels above the bottom");
    }

    [Fact]
    public void OnAnAxisThatRunsBackwards_TheTipIsStillTheEndOfTheBar()
    {
        var figure = Bar(5, ax => ax.SetYLim(5.5, -0.5));
        var rect = Gradiented(figure, "rect");
        double centre = rect.Number("x") + rect.Number("width") / 2, top = rect.Number("y"), bottom = top + rect.Number("height");

        // The larger the value, the lower it stands: the foot is at the top of the rectangle, the end at its bottom.
        AssertNear(Foot, PixelAt(figure, centre, top + 2), "two pixels below the foot");
        AssertNear(Tip, PixelAt(figure, centre, bottom - 2), "two pixels above the end");
    }

    [Fact]
    public void AHorizontalBar_IsTheSeriesColourAtItsFoot_AndTheGradientColourAtItsEnd()
    {
        var figure = Plt.Create().WithSize(600, 600).AddSubPlot(1, 1, 1, ax =>
        {
            ax.Bar(["a"], [5.0], s => { s.Color = Foot; s.GradientColor = Tip; s.Orientation = BarOrientation.Horizontal; });
            ax.SetXLim(-0.5, 5.5);
        }).Build();
        var rect = Gradiented(figure, "rect");
        double centre = rect.Number("y") + rect.Number("height") / 2, left = rect.Number("x"), right = left + rect.Number("width");

        AssertNear(Foot, PixelAt(figure, left + 2, centre), "two pixels right of the foot");
        AssertNear(Tip, PixelAt(figure, right - 2, centre), "two pixels left of the end");
    }

    [Fact]
    public void AnArea_IsTheSeriesColourAtItsBaseline_AndTheGradientColourAtItsHighestPoint()
    {
        var figure = Plt.Create().WithSize(600, 600).AddSubPlot(1, 1, 1, ax =>
        {
            ax.FillBetween([0, 1, 2], [0.0, 10.0, 0.0], null, s => { s.Color = Foot; s.GradientColor = Tip; s.Alpha = 1; });
            ax.SetXLim(0, 2).SetYLim(0, 10);
        }).Build();
        var polygon = Gradiented(figure, "polygon");
        var points = polygon["points"]!.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split(',')).Select(pair => (X: double.Parse(pair[0], System.Globalization.CultureInfo.InvariantCulture), Y: double.Parse(pair[1], System.Globalization.CultureInfo.InvariantCulture))).ToList();
        double peakX = points.OrderBy(p => p.Y).First().X, peak = points.Min(p => p.Y), baseline = points.Max(p => p.Y);

        // Under the peak the area is as tall as the figure's plot, so the two ends are far apart. The line along the top edge keeps
        // the series colour, so the tip is read a few pixels under the peak, clear of the line that runs down both sides of it.
        AssertNear(Foot, PixelAt(figure, peakX, baseline - 2), "two pixels above the baseline");
        AssertNear(Tip, PixelAt(figure, peakX, peak + 5), "five pixels under the peak");
    }

    [Fact]
    public void ThePdf_KeepsTheBlendAsAVectorShading()
    {
        byte[] pdf = Bar(5).ToPdf();

        Assert.Contains("/ShadingType 2", Encoding.Latin1.GetString(pdf), StringComparison.Ordinal);
    }
}
