// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>A bar series and an area series can fill with a blend (<c>GradientColor</c>): from the series colour at the foot of
/// a bar, or at the baseline of an area, to the gradient colour at the tip. The fill is a gradient in the pixels of the figure
/// whose line runs from the foot to the end of each bar (the end is the top of a bar that points up and the bottom of one that
/// points down, whichever way the axis runs), and across an area from its baseline to the point of its top edge furthest from
/// it. The alpha of the series applies to both colours, and a hatch is laid over the gradient.</summary>
public class GradientFillSeriesTests
{
    private static readonly Color Foot = Color.FromHex("#1F77B4");
    private static readonly Color Tip = Color.FromHex("#FF7F0E");

    private static string Svg(Action<AxesBuilder> axes, Action<AxesBuilder>? afterwards = null) =>
        Plt.Create().WithSize(600, 400).AddSubPlot(1, 1, 1, ax => { axes(ax); afterwards?.Invoke(ax); }).ToSvg();

    private static string Bars(double[] values, Action<BarSeries>? configure = null, Action<AxesBuilder>? afterwards = null) =>
        Svg(ax => ax.Bar([.. values.Select((_, i) => "c" + i.ToString(CultureInfo.InvariantCulture))], values,
            s => { s.Color = Foot; s.GradientColor = Tip; configure?.Invoke(s); }), afterwards);

    /// <summary>One filled shape and the gradient its fill refers to.</summary>
    private readonly record struct Filled(SvgElement Shape, SvgElement Gradient);

    /// <summary>The shapes (<paramref name="kind"/>) whose fill is a gradient, each with that gradient, in document order.</summary>
    private static List<Filled> GradientShapes(string svg, string kind)
    {
        var gradients = svg.Elements("linearGradient").ToDictionary(g => g["id"]!);
        return [.. svg.Elements(kind)
            .Where(shape => shape["fill"] is { } fill && fill.StartsWith("url(#", StringComparison.Ordinal))
            .Select(shape => new Filled(shape, gradients[shape["fill"]!["url(#".Length..^1]]))];
    }

    private static double[] Line(SvgElement gradient) => [.. new[] { "x1", "y1", "x2", "y2" }.Select(gradient.Number)];

    [Fact]
    public void AnUpwardBar_IsFilledFromItsFootToItsTop_AFallingBarFromItsFootToItsBottom()
    {
        var bars = GradientShapes(Bars([3, -2, 5]), "rect");

        Assert.Equal(3, bars.Count);
        foreach (var (bar, expectsRise) in bars.Zip(new[] { true, false, true }))
        {
            double x = bar.Shape.Number("x"), y = bar.Shape.Number("y"), w = bar.Shape.Number("width"), h = bar.Shape.Number("height");
            double[] line = Line(bar.Gradient);
            Assert.Equal(x + w / 2, line[0], 6);
            Assert.Equal(x + w / 2, line[2], 6);
            Assert.Equal(expectsRise ? y + h : y, line[1], 6);   // the foot: the bottom of a bar that rises, the top of one that falls
            Assert.Equal(expectsRise ? y : y + h, line[3], 6);   // the end
        }
    }

    [Fact]
    public void AHorizontalBar_IsFilledAlongX_FromItsFootToItsEnd()
    {
        var bars = GradientShapes(Bars([3, -2], s => s.Orientation = BarOrientation.Horizontal), "rect");

        Assert.Equal(2, bars.Count);
        foreach (var (bar, expectsRise) in bars.Zip(new[] { true, false }))
        {
            double x = bar.Shape.Number("x"), y = bar.Shape.Number("y"), w = bar.Shape.Number("width"), h = bar.Shape.Number("height");
            double[] line = Line(bar.Gradient);
            Assert.Equal(y + h / 2, line[1], 6);
            Assert.Equal(y + h / 2, line[3], 6);
            Assert.Equal(expectsRise ? x : x + w, line[0], 6);
            Assert.Equal(expectsRise ? x + w : x, line[2], 6);
        }
    }

    [Fact]
    public void OnAnAxisThatRunsBackwards_TheTipIsStillTheEndOfTheBar()
    {
        // Y runs from 10 down to -2: the larger a value, the lower it stands, so the end of a bar of 3 is below its foot.
        var bar = Assert.Single(GradientShapes(Bars([3], afterwards: ax => ax.SetYLim(10, -2)), "rect"));

        double y = bar.Shape.Number("y"), h = bar.Shape.Number("height");
        double[] line = Line(bar.Gradient);
        Assert.Equal(y, line[1], 6);
        Assert.Equal(y + h, line[3], 6);
    }

    [Fact]
    public void TheFootColourIsTheSeriesColour_TheEndColourIsTheGradientColour()
    {
        string svg = Bars([3]);

        Assert.Single(GradientShapes(svg, "rect"));
        Assert.Equal(["#1F77B4", "#FF7F0E"], svg.Elements("stop").Select(stop => stop["stop-color"]!.ToUpperInvariant()));
    }

    [Fact]
    public void TheAlphaOfTheSeries_AppliesToBothColours()
    {
        string svg = Bars([3], s => s.Alpha = 0.5);

        var stops = svg.Elements("stop");
        Assert.Equal(2, stops.Count);
        Assert.All(stops, stop => Assert.Equal(0.5, double.Parse(stop["stop-opacity"]!, CultureInfo.InvariantCulture), 2));
    }

    [Fact]
    public void WithoutAGradientColour_TheFillIsFlat_AndNoGradientIsDefined()
    {
        string svg = Svg(ax => ax.Bar(["a", "b"], [3.0, 2.0], s => s.Color = Foot));

        Assert.Empty(svg.Elements("linearGradient"));
        Assert.DoesNotContain("url(#grad", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void AHatchTogetherWithAGradient_IsLaidOverTheGradient()
    {
        string svg = Bars([3], s => s.Hatch = HatchPattern.ForwardDiagonal);

        var gradient = svg.Elements("linearGradient");
        var patterns = svg.Elements("pattern");
        Assert.Single(gradient);
        Assert.NotEmpty(patterns);
        int gradientAt = svg.IndexOf("url(#grad-0)", StringComparison.Ordinal);
        int hatchAt = svg.IndexOf("url(#" + patterns[0]["id"] + ")", StringComparison.Ordinal);
        Assert.True(gradientAt >= 0 && hatchAt > gradientAt, "the hatch is drawn after the gradient fill, over it");
    }

    [Fact]
    public void AnUpwardArea_IsFilledFromItsBaseline_ToTheHighestPointOfItsTopEdge()
    {
        string svg = Svg(ax => ax.FillBetween([0, 1, 2], [1.0, 4.0, 2.0], null, s => { s.Color = Foot; s.GradientColor = Tip; }));

        var area = Assert.Single(GradientShapes(svg, "polygon"));

        var points = Points(area.Shape);
        double baseline = points[^1].Y, highest = points.Min(p => p.Y);
        double[] line = Line(area.Gradient);
        Assert.Equal(baseline, line[1], 6);
        Assert.Equal(highest, line[3], 6);
        Assert.Equal(line[0], line[2], 6);
        Assert.True(baseline > highest);
    }

    [Fact]
    public void ADownwardArea_IsFilledFromItsBaseline_ToTheLowestPointOfItsTopEdge()
    {
        string svg = Svg(ax => ax.FillBetween([0, 1, 2], [-1.0, -4.0, -2.0], null, s => { s.Color = Foot; s.GradientColor = Tip; }));

        var area = Assert.Single(GradientShapes(svg, "polygon"));

        var points = Points(area.Shape);
        double[] line = Line(area.Gradient);
        Assert.Equal(points[^1].Y, line[1], 6);
        Assert.Equal(points.Max(p => p.Y), line[3], 6);
        Assert.True(line[3] > line[1]);
    }

    [Fact]
    public void AnAreaBetweenTwoCurves_StartsAtTheMeanHeightOfTheLowerOne()
    {
        string svg = Svg(ax => ax.FillBetween([0, 1, 2], [4.0, 5.0, 6.0], [1.0, 2.0, 3.0], s => { s.Color = Foot; s.GradientColor = Tip; }));

        var area = Assert.Single(GradientShapes(svg, "polygon"));

        var points = Points(area.Shape);
        double lowerMean = points.Skip(3).Average(p => p.Y);
        double[] line = Line(area.Gradient);
        Assert.Equal(lowerMean, line[1], 6);
        Assert.Equal(points.Take(3).Min(p => p.Y), line[3], 6);
    }

    [Fact]
    public void AnAreaThatLiesDown_IsFilledAlongX_FromItsBaselineToTheFurthestPointOfItsEdge()
    {
        string svg = Svg(ax => ax.FillBetweenX([0, 1, 2], [1.0, 4.0, 2.0], null, s => { s.Color = Foot; s.GradientColor = Tip; }));

        var area = Assert.Single(GradientShapes(svg, "polygon"));

        var points = Points(area.Shape);
        double[] line = Line(area.Gradient);
        Assert.Equal(points[^1].X, line[0], 6);
        Assert.Equal(points.Max(p => p.X), line[2], 6);
        Assert.Equal(line[1], line[3], 6);
    }

    [Fact]
    public void AFillColourOfTheAreaIsTheBaselineColour_AndTheGradientColourIsTakenAsGiven()
    {
        string svg = Svg(ax => ax.FillBetween([0, 1, 2], [1.0, 4.0, 2.0], null, s =>
        {
            s.Color = Foot; s.FillColor = Color.FromHex("#00FF00"); s.GradientColor = Tip; s.Alpha = 0.5;
        }));

        var stops = svg.Elements("stop");
        Assert.Equal(["#00FF00", "#FF7F0E"], stops.Select(stop => stop["stop-color"]!.ToUpperInvariant()));
        Assert.Null(stops[1]["stop-opacity"]);
    }

    [Fact]
    public void TheGradientColour_SurvivesTheJson_AndDrawsTheSamePicture()
    {
        var figure = Plt.Create().WithSize(600, 400).AddSubPlot(1, 1, 1, ax => ax
            .Bar(["a", "b"], [3.0, -2.0], s => { s.Color = Foot; s.GradientColor = Tip; })
            .FillBetween([0, 1, 2], [1.0, 4.0, 2.0], null, s => { s.Color = Foot; s.GradientColor = Tip; })).Build();

        var back = ChartSerializerDefault.Current.FromJson(figure.ToJson());

        Assert.Equal(Tip, Assert.IsType<BarSeries>(back.SubPlots[0].Series[0]).GradientColor);
        Assert.Equal(Tip, Assert.IsType<AreaSeries>(back.SubPlots[0].Series[1]).GradientColor);
        Assert.Equal(figure.ToSvg(), back.ToSvg());
    }

    [Fact]
    public void AFlatFill_LeavesTheGradientColourOutOfTheJson()
    {
        string json = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Bar(["a"], [1.0])).ToJson();

        Assert.DoesNotContain("gradientColor", json, StringComparison.OrdinalIgnoreCase);
    }

    private static List<Point> Points(SvgElement polygon) =>
    [
        .. polygon["points"]!.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split(','))
            .Select(pair => new Point(double.Parse(pair[0], CultureInfo.InvariantCulture), double.Parse(pair[1], CultureInfo.InvariantCulture))),
    ];
}
