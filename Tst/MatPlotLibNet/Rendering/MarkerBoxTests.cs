// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The box a marker fills. matplotlib 3.11.2, read with <c>MarkerStyle(m).get_path().transformed(get_transform())</c> at a marker size of 1:
/// <c>'^'</c>, <c>'v'</c>, <c>'&lt;'</c> and <c>'&gt;'</c> fill 1.0 x 1.0 (an isosceles triangle on the unit box), <c>'D'</c> 1.4142 x 1.4142 (the unit
/// square turned a quarter), <c>'p'</c> 0.9511 x 0.9045, <c>'h'</c> 0.8660 x 1.0 and <c>'*'</c> 0.9511 x 0.9045 (the regular polygons and the star are
/// inscribed in a circle of diameter 1). The library inscribed every polygon in the circle of the size, so its triangle was 0.866 x 0.75 of the size and its
/// diamond 1.0 across: the matplotlib themes now draw matplotlib's, the library's own themes draw what they always did.</summary>
public class MarkerBoxTests
{
    private static (double Width, double Height) BoxOf(MarkerStyle style, Theme? theme, double size = 100)
    {
        var ctx = new RecordingRenderContext();
        (theme is null ? MarkerShape.Of(style) : MarkerShape.Of(style, theme)).Draw(ctx, new Point(0, 0), size, new ShapeStyle(Colors.Red, null, 0));
        var points = ctx.Of<DrawPolygonCall>().Single().Points;
        return (points.Max(p => p.X) - points.Min(p => p.X), points.Max(p => p.Y) - points.Min(p => p.Y));
    }

    [Theory]
    [MemberData(nameof(ThemeCases.Matplotlib), MemberType = typeof(ThemeCases))]
    public void UnderTheMatplotlibThemes_ATriangleFillsTheUnitBox_InEveryDirection(string themeId)
    {
        var theme = themeId.AsTheme();

        foreach (var style in new[] { MarkerStyle.Triangle, MarkerStyle.TriangleDown, MarkerStyle.TriangleLeft, MarkerStyle.TriangleRight })
        {
            var (w, h) = BoxOf(style, theme);
            Assert.Equal(100.0, w, 6);
            Assert.Equal(100.0, h, 6);
        }
    }

    [Theory]
    [MemberData(nameof(ThemeCases.Matplotlib), MemberType = typeof(ThemeCases))]
    public void UnderTheMatplotlibThemes_ADiamondIsTheUnitSquareTurnedAQuarter(string themeId)
    {
        var (w, h) = BoxOf(MarkerStyle.Diamond, themeId.AsTheme());

        Assert.Equal(141.4214, w, 3);
        Assert.Equal(141.4214, h, 3);
    }

    [Theory]
    [MemberData(nameof(ThemeCases.Matplotlib), MemberType = typeof(ThemeCases))]
    public void UnderTheMatplotlibThemes_ThePentagonTheHexagonAndTheStar_AreMatplotlibsToo(string themeId)
    {
        var theme = themeId.AsTheme();

        var (pw, ph) = BoxOf(MarkerStyle.Pentagon, theme);
        var (hw, hh) = BoxOf(MarkerStyle.Hexagon, theme);
        var (sw, sh) = BoxOf(MarkerStyle.Star, theme);

        Assert.Equal((95.11, 90.45), (Math.Round(pw, 2), Math.Round(ph, 2)));
        Assert.Equal((86.60, 100.0), (Math.Round(hw, 2), Math.Round(hh, 2)));
        Assert.Equal((95.11, 90.45), (Math.Round(sw, 2), Math.Round(sh, 2)));
    }

    [Fact]
    public void UnderTheLibrarysThemes_TheMarkersAreAsTheyAlwaysWere()
    {
        var (tw, th) = BoxOf(MarkerStyle.Triangle, Theme.Default);
        var (dw, dh) = BoxOf(MarkerStyle.Diamond, Theme.Default);

        Assert.Equal((86.6, 75.0), (Math.Round(tw, 1), Math.Round(th, 1)));   // inscribed in the circle of the size
        Assert.Equal((100.0, 100.0), (Math.Round(dw, 1), Math.Round(dh, 1)));
    }

    [Fact]
    public void ATrianglePointsTheWayItsNameSays_UnderTheMatplotlibThemes()
    {
        Point[] Corners(MarkerStyle style)
        {
            var ctx = new RecordingRenderContext();
            MarkerShape.Of(style, Theme.MatplotlibV2).Draw(ctx, new Point(0, 0), 10, new ShapeStyle(Colors.Red, null, 0));
            return ctx.Of<DrawPolygonCall>().Single().Points;
        }

        Assert.Single(Corners(MarkerStyle.Triangle), p => p.Y < -4.99);       // the apex is up (the screen's y grows downward)
        Assert.Single(Corners(MarkerStyle.TriangleDown), p => p.Y > 4.99);
        Assert.Single(Corners(MarkerStyle.TriangleLeft), p => p.X < -4.99);
        Assert.Single(Corners(MarkerStyle.TriangleRight), p => p.X > 4.99);
    }

    [Fact]
    public void AFiguresMarker_FillsTheBoxOfItsSize_UnderV2()
    {
        string svg = Plt.Create().WithTheme(Theme.MatplotlibV2).WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0], [1.0, 2.0], s => { s.Marker = MarkerStyle.Triangle; s.MarkerSize = 10; })).ToSvg();
        var polygon = svg.Each("polygon").First(e => e.Attribute("points") is not null);
        var numbers = polygon.Attribute("points")!.Value.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries)
            .Select(n => double.Parse(n, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        double[] xs = [.. numbers.Where((_, i) => i % 2 == 0)], ys = [.. numbers.Where((_, i) => i % 2 == 1)];

        Assert.Equal(10.0.PointsInPixels, xs.Max() - xs.Min(), 1);
        Assert.Equal(10.0.PointsInPixels, ys.Max() - ys.Min(), 1);
    }

}
