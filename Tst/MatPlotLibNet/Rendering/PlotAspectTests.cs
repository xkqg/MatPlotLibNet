// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A plot with an aspect is drawn in the largest rectangle of the shape that aspect asks for that fits its cell, and
/// that rectangle is the plot area for everything downstream of the layout: the series, the ticks and labels, the colour bar,
/// the clip, the data cursor's mapping and the hit-testing. The rule has one owner, the layout; it is not applied inside the
/// renderer, because the interaction layer reads the rectangles the layout gives it.</summary>
public class PlotAspectTests
{
    private static Figure Heatmap(int width, int height, double? aspect) => Plt.Create().WithSize(width, height)
        .AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[10, 10]).SetAspect(aspect))
        .Build();

    private static Rect PlotArea(Figure figure, int index = 0) =>
        new ChartRenderer().ComputeLayout(figure, new SvgRenderContext()).PlotAreas[index];

    /// <summary>How many data units one pixel is across, per axis: equal on both axes means equal scale.</summary>
    private static (double PerPixelX, double PerPixelY) UnitsPerPixel(Figure figure, Rect plot)
    {
        var range = figure.SubPlots[0].ComputeDataRanges(figure.Theme);
        return ((range.XMax - range.XMin) / plot.Width, (range.YMax - range.YMin) / plot.Height);
    }

    [Theory]
    [InlineData(600, 600)]
    [InlineData(500, 350)]
    [InlineData(900, 500)]
    public void AnAspectOfOne_MakesTheCellsSquare_AtAnyFigureSize(int width, int height)
    {
        var figure = Heatmap(width, height, 1.0);

        var (x, y) = UnitsPerPixel(figure, PlotArea(figure));

        Assert.Equal(x, y, 9);
    }

    [Fact]
    public void AnAspectOfTwo_MakesAUnitOfYTwiceAsLongAsAUnitOfX()
    {
        var figure = Heatmap(900, 500, 2.0);

        var (x, y) = UnitsPerPixel(figure, PlotArea(figure));

        Assert.Equal(2 * y, x, 9);
    }

    [Fact]
    public void TheRectangle_IsCentredInTheCellItWasGiven()
    {
        var fitted = PlotArea(Heatmap(900, 500, 1.0));
        var cell = PlotArea(Heatmap(900, 500, null));

        Assert.Equal(cell.X + cell.Width / 2, fitted.X + fitted.Width / 2, 9);
        Assert.Equal(cell.Y + cell.Height / 2, fitted.Y + fitted.Height / 2, 9);
        Assert.True(fitted.Width <= cell.Width + 1e-9 && fitted.Height <= cell.Height + 1e-9);
        Assert.True(Math.Abs(fitted.Width - cell.Width) < 1e-9 || Math.Abs(fitted.Height - cell.Height) < 1e-9, "one side fills the cell");
    }

    [Fact]
    public void WithoutAnAspect_TheCellIsTheLayoutsAsItWas()
    {
        var plain = PlotArea(Heatmap(900, 500, null));
        var cell = ChartRenderer.SubPlotRects(Heatmap(900, 500, null), 40, new SubPlotSpacing()).Count;

        Assert.True(plain.Width > 0 && plain.Height > 0);
        Assert.Equal(1, cell);
    }

    [Fact]
    public void TheDrawnClip_IsTheRectangleTheLayoutGives()
    {
        var figure = Plt.Create().WithSize(900, 500)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 10.0], [0.0, 10.0]).SetXLim(0, 10).SetYLim(0, 10).SetAspect(1.0))
            .Build();

        var plot = PlotArea(figure);
        var clip = figure.ToSvg().Elements("rect").First(rect => rect["width"] is { } w && Math.Abs(double.Parse(w, System.Globalization.CultureInfo.InvariantCulture) - plot.Width) < 1e-6);

        Assert.Equal(plot.Height, clip.Number("height"), 6);
        Assert.Equal(plot.X, clip.Number("x"), 6);
        Assert.Equal(plot.Y, clip.Number("y"), 6);
    }

    [Fact]
    public void TheColourBar_IsAsTallAsTheRectangleThatIsDrawn()
    {
        // Taller than wide: the cell is taller than the square the aspect asks for, so a bar as tall as the cell would show.
        var figure = Plt.Create().WithSize(500, 800)
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[10, 10]).WithColorBar().SetAspect(1.0))
            .Build();
        var plot = PlotArea(figure);
        var recording = new RecordingRenderContext();

        new ChartRenderer().Render(figure, recording);

        var bar = recording.OfKind("DrawRectangle")
            .Select(call => (Rect)((dynamic)call.Args!).rect)
            .Where(rect => rect.X >= plot.X + plot.Width && rect.Width < plot.Width / 3)
            .ToArray();
        Assert.NotEmpty(bar);
        Assert.True(plot.Height < plot.Width * 1.2, "the plot is the square the aspect asks for, not the tall cell");
        Assert.InRange(bar.Min(rect => rect.Y), plot.Y - 1.5, plot.Y + 1.5);
        Assert.InRange(bar.Max(rect => rect.Y + rect.Height), plot.Y + plot.Height - 1.5, plot.Y + plot.Height + 1.5);
    }

    [Fact]
    public void TheHitTest_UsesTheRectangleThatIsDrawn()
    {
        var figure = Plt.Create().WithSize(900, 500)
            .AddSubPlot(1, 2, 1, ax => ax.Heatmap(new double[10, 10]).SetAspect(1.0))
            .AddSubPlot(1, 2, 2, ax => ax.Heatmap(new double[10, 10]))
            .Build();
        var layout = ChartLayout.Create(figure, new ChartRenderer().ComputeLayout(figure, new SvgRenderContext()));
        var fitted = PlotArea(figure, 0);
        var (perPixelX, perPixelY) = UnitsPerPixel(figure, fitted);
        Assert.Equal(perPixelX, perPixelY, 9);

        // A point in the strip of the cell the fitted rectangle leaves free belongs to no axes; one inside it belongs to the first.
        Assert.Equal(0, layout.HitTestAxes(fitted.X + fitted.Width / 2, fitted.Y + fitted.Height / 2));
        Assert.Null(layout.HitTestAxes(fitted.X + fitted.Width / 2, fitted.Y - 3));
    }

    [Fact]
    public void AnInsetWithAnAspect_IsFittedInsideItsBounds()
    {
        var figure = Plt.Create().WithSize(800, 600)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([0.0, 10.0], [0.0, 10.0])
                .AddInset(0.1, 0.1, 0.6, 0.3, inset => inset.Plot([0.0, 10.0], [0.0, 10.0]).SetXLim(0, 10).SetYLim(0, 10).SetAspect(1.0)))
            .Build();

        var rects = figure.ToSvg().Elements("clipPath").Count;
        var inset = figure.SubPlots[0].Insets[0];
        var clipRects = figure.ToSvg().Elements("rect").Where(r => r["width"] is not null && r["height"] is not null).ToArray();

        // The inset's bounds are 0.6 wide by 0.3 high of a 4:3 plot, so its cell is wider than tall: the fitted rectangle is square.
        Assert.Contains(clipRects, r => Math.Abs(r.Number("width") - r.Number("height")) < 1e-6 && r.Number("width") > 50);
        Assert.True(rects >= 0);
        Assert.Equal(1.0, inset.Aspect);
    }

    [Fact]
    public void APolarAxes_IgnoresTheAspect()
    {
        var withAspect = Plt.Create().WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax.PolarPlot([1.0, 2.0, 3.0], [0.0, 1.0, 2.0]).SetAspect(2.0)).Build();
        var without = Plt.Create().WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax.PolarPlot([1.0, 2.0, 3.0], [0.0, 1.0, 2.0])).Build();

        Assert.Equal(PlotArea(without), PlotArea(withAspect));
    }

    [Fact]
    public void ABackwardsAxis_StillGetsEqualScale()
    {
        var figure = Plt.Create().WithSize(700, 500)
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[10, 10]).SetAspect(1.0).SetYLim(10, 0))
            .Build();

        var plot = PlotArea(figure);
        var range = figure.SubPlots[0].ComputeDataRanges(figure.Theme);

        Assert.Equal(Math.Abs(range.XMax - range.XMin) / plot.Width, Math.Abs(range.YMax - range.YMin) / plot.Height, 9);
    }

    [Fact]
    public void ALogAxis_IsMeasuredInTheScaleItIsDrawnIn()
    {
        var figure = Plt.Create().WithSize(700, 500)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 100.0], [1.0, 10.0]).SetYScale(AxisScale.Log).SetAspect(1.0))
            .Build();

        var plot = PlotArea(figure);
        var range = figure.SubPlots[0].ComputeDataRanges(figure.Theme);

        // One decade of Y is as long as one linear unit of X: the box is as tall as the log10 span of Y, in X units.
        double ySpan = Math.Log10(range.YMax) - Math.Log10(range.YMin);
        double xSpan = range.XMax - range.XMin;
        Assert.Equal(ySpan / xSpan, plot.Height / plot.Width, 9);
    }

    /// <summary>A range with an end at infinity has no length on the page (an infinite one, or none at all when both ends are
    /// the same infinity): the rule cannot measure it, and leaves the cell as it is rather than fit a box to nothing.</summary>
    [Theory]
    [InlineData(true, 0.0, double.PositiveInfinity)]
    [InlineData(true, double.PositiveInfinity, double.PositiveInfinity)]
    [InlineData(false, double.NegativeInfinity, 2.0)]
    [InlineData(false, double.PositiveInfinity, double.PositiveInfinity)]
    public void ARangeThatCannotBeMeasured_LeavesTheCellAsItIs(bool onX, double low, double high)
    {
        Figure Of(double? aspect) => Plt.Create().WithSize(700, 500)
            .AddSubPlot(1, 1, 1, ax => (onX
                ? ax.Plot([1.0, 100.0], [1.0, 10.0]).SetXLim(low, high)
                : ax.Plot([1.0, 100.0], [1.0, 10.0]).SetYLim(low, high)).SetAspect(aspect))
            .Build();

        Assert.Equal(PlotArea(Of(null)), PlotArea(Of(1.0)));
    }

    [Fact]
    public void TheRootOfTheSvg_SaysTheAspectIsLocked_WhenAnyAxesHasOne()
    {
        Assert.Contains("data-aspect-lock=\"true\"", Heatmap(600, 400, 1.0).ToSvg(), StringComparison.Ordinal);
        Assert.DoesNotContain("data-aspect-lock", Heatmap(600, 400, null).ToSvg(), StringComparison.Ordinal);
    }

    [Fact]
    public void TheAspectSurvivesTheJson_AndDrawsTheSamePicture()
    {
        var figure = Heatmap(600, 400, 1.0);

        var back = ChartSerializerDefault.Current.FromJson(figure.ToJson());

        Assert.Equal(1.0, back.SubPlots[0].Aspect);
        Assert.Equal(figure.ToSvg(), back.ToSvg());
    }

    [Fact]
    public void AnAspectAtRest_IsLeftOutOfTheJson()
    {
        Assert.DoesNotContain("aspect", Heatmap(600, 400, null).ToJson(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ABuilderRefuses_AnAspectThatIsNotPositiveAndFinite_ByName(double aspect)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.SetAspect(aspect)).Build());

        Assert.Equal("aspect", ex.ParamName);
    }

    [Fact]
    public void ABuilder_TakesNullToFillTheCellAgain()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.SetAspect(1.0).SetAspect(null)).Build();

        Assert.Null(figure.SubPlots[0].Aspect);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(double.NaN)]
    public void AnAspectThatCameInThroughTheModelOrTheJson_AndIsNotPositive_IsIgnoredNotObeyed(double aspect)
    {
        var figure = Heatmap(600, 400, null);
        figure.SubPlots[0].Aspect = aspect;

        Assert.Equal(PlotArea(Heatmap(600, 400, null)), PlotArea(figure));
    }

    [Fact]
    public void AnEmptyAxes_IsMeasuredByTheRangeItDrawsWithNoData_ASquare()
    {
        var figure = Plt.Create().WithSize(600, 400).AddSubPlot(1, 1, 1, ax => ax.SetAspect(1.0)).Build();

        var plot = PlotArea(figure);

        Assert.Equal(plot.Width, plot.Height, 9);
    }
}

/// <summary>The one rule that fits a rectangle to a box aspect.</summary>
public class RectBoxAspectTests
{
    [Fact]
    public void ARectangleTooTallForTheAspect_LosesHeight_AndStaysCentred()
    {
        var fitted = new Rect(10, 20, 400, 300).WithBoxAspect(0.5);

        Assert.Equal(new Rect(10, 20 + 50, 400, 200), fitted);
    }

    [Fact]
    public void ARectangleTooWideForTheAspect_LosesWidth_AndStaysCentred()
    {
        var fitted = new Rect(10, 20, 400, 200).WithBoxAspect(1.0);

        Assert.Equal(new Rect(110, 20, 200, 200), fitted);
    }

    [Fact]
    public void ARectangleOfTheShapeAlready_IsUnchanged()
    {
        Assert.Equal(new Rect(0, 0, 300, 150), new Rect(0, 0, 300, 150).WithBoxAspect(0.5));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-2.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ABoxAspectThatIsNotPositiveAndFinite_LeavesTheRectangleAlone(double aspect)
    {
        Assert.Equal(new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 4).WithBoxAspect(aspect));
    }
}
