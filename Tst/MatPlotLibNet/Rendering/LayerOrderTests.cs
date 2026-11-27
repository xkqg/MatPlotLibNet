// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The pieces of the one z-order: the sort of the steps (<see cref="Rendering.LayerOrder.SortByZ"/>), where each thing
/// stands under a theme (<see cref="ZOrderExtensions"/>), the one orderer (<see cref="LayerOrder"/>), the order of a series pass
/// (<see cref="SeriesPass.Order"/>) and the list of a Cartesian axes' data layer (<see cref="CartesianAxesRenderer.DataLayers"/>).</summary>
public class LayerOrderTests
{
    private static LayerStep Step(double z, int place) => new(z, place, DataLayer.Series, place);

    private static int[] Places(Span<LayerStep> steps)
    {
        var places = new int[steps.Length];
        for (int i = 0; i < steps.Length; i++) places[i] = steps[i].Place;
        return places;
    }

    // ---- SortByZ ------------------------------------------------------------------------------------------------------------

    [Fact]
    public void StepsAlreadyInOrder_AreLeftAsTheyAre()
    {
        LayerStep[] steps = [Step(double.NegativeInfinity, 0), Step(1, 1), Step(1, 2), Step(2, 3)];

        LayerOrder.SortByZ(steps.AsSpan());

        Assert.Equal([0, 1, 2, 3], Places(steps));
    }

    [Fact]
    public void StepsOutOfOrder_AreSortedByZ_ATieKeepingThePlaceTheyWereListedIn()
    {
        LayerStep[] steps = [Step(2, 0), Step(1, 1), Step(2, 2), Step(1, 3), Step(0, 4)];

        LayerOrder.SortByZ(steps.AsSpan());

        Assert.Equal([4, 1, 3, 0, 2], Places(steps));
    }

    [Fact]
    public void NoStepAndOneStep_AreLeftAsTheyAre()
    {
        LayerStep[] one = [Step(5, 0)];

        LayerOrder.SortByZ(Span<LayerStep>.Empty);
        LayerOrder.SortByZ(one.AsSpan());

        Assert.Equal([0], Places(one));
    }

    // ---- where each thing stands under a theme -------------------------------------------------------------------------------

    [Fact]
    public void UnderALibraryTheme_TheGridAndTheOverlaysStandUnderEverySeries_AndASeriesAtItsZOrder()
    {
        var theme = Theme.Default;

        Assert.Equal(double.NegativeInfinity, theme.GridZ);
        Assert.Equal(double.NegativeInfinity, theme.SpansZ);
        Assert.Equal(double.NegativeInfinity, theme.OverlayLinesZ);
        Assert.Equal(double.NegativeInfinity, theme.ZOf(new Rectangle(new(0, 0), 1, 1)));
        Assert.Equal(5, theme.ZOf(new Rectangle(new(0, 0), 1, 1) { ZOrder = 5 }));
        Assert.Equal(-1, theme.ZOf(new AreaSeries([0.0, 1.0], [1.0, 2.0])));
        Assert.Equal(0, theme.ZOf(new LineSeries([0.0, 1.0], [1.0, 2.0])));
        Assert.Equal(4, theme.ZOf(new LineSeries([0.0, 1.0], [1.0, 2.0]) { ZOrder = 4 }));
    }

    [Fact]
    public void UnderTheMatplotlibThemes_EachStandsAtMatplotlibsZOrder()
    {
        var v2 = Theme.MatplotlibV2;

        Assert.Equal(1.5, v2.GridZ);
        Assert.Equal(2.5, Theme.MatplotlibClassic.GridZ);
        Assert.Equal(1, v2.SpansZ);
        Assert.Equal(2, v2.OverlayLinesZ);
        Assert.Equal(1, v2.ZOf(new Rectangle(new(0, 0), 1, 1)));
        Assert.Equal(3, v2.ZOf(new Rectangle(new(0, 0), 1, 1) { ZOrder = 3 }));
        Assert.Equal(2, v2.ZOf(new LineSeries([0.0, 1.0], [1.0, 2.0])));
        Assert.Equal(1, v2.ZOf(new AreaSeries([0.0, 1.0], [1.0, 2.0])));
        Assert.Equal(0, v2.ZOf(new LineSeries([0.0, 1.0], [1.0, 2.0]) { ZOrder = 0 }));
    }

    [Fact]
    public void ASeriesTheLibraryDoesNotKnow_StandsAtItsZOrder_UnderEveryTheme()
    {
        var foreign = new ForeignSeries { ZOrder = 7 };

        Assert.Equal(7, Theme.MatplotlibV2.ZOf(foreign));
        Assert.Equal(7, Theme.Default.ZOf(foreign));
    }

    // ---- the order of one list of series -------------------------------------------------------------------------------------

    private static readonly Rect Plot = new(0, 0, 100, 100);

    private static RenderPass Render(Theme theme) => new(Plot, new RecordingRenderContext(), theme);

    private static int[] Indices(LayerOrder order)
    {
        var steps = order.Steps;
        var indices = new int[steps.Length];
        for (int i = 0; i < steps.Length; i++) indices[i] = steps[i].Index;
        order.Return();
        return indices;
    }

    /// <summary>A pass that orders its series by their own z-order (<see cref="SeriesPass.OnUnitSquare"/>) over <paramref name="series"/>.</summary>
    private static LayerOrder OwnOrder(IReadOnlyList<ISeries> series, IReadOnlyList<Patch>? patches = null) =>
        (SeriesPass.OnUnitSquare(new Axes(), Render(Theme.MatplotlibV2), patches) with { Series = series }).Order();

    /// <summary>A pass over a Cartesian plot, which orders its series where the theme places them (<see cref="SeriesPass.OverPlot"/>).</summary>
    private static LayerOrder ThemeOrder(IReadOnlyList<ISeries> series, Theme theme) =>
        (SeriesPass.OverPlot(new Axes(), Render(theme), new DataTransform(0, 1, 0, 1, Plot)) with { Series = series }).Order();

    [Fact]
    public void NoSeriesAndOneSeries_AreTheirOwnOrder()
    {
        Assert.Empty(Indices(ThemeOrder([], Theme.MatplotlibV2)));
        Assert.Equal([0], Indices(OwnOrder([new LineSeries([0.0], [1.0])])));
    }

    /// <summary>Without a theme the series stand by their <see cref="ISeries.ZOrder"/>; under the default style by matplotlib's
    /// z-order, so a scatter (1) comes before a line (2) added ahead of it.</summary>
    [Fact]
    public void SeveralSeries_StandByZOrder_OrWhereTheThemePlacesThem()
    {
        ISeries[] series = [new LineSeries([0.0], [1.0]), new ScatterSeries([0.0], [1.0]), new AreaSeries([0.0], [1.0])];

        Assert.Equal([2, 0, 1], Indices(OwnOrder(series)));
        Assert.Equal([1, 2, 0], Indices(ThemeOrder(series, Theme.MatplotlibV2)));
    }

    /// <summary>A pass that orders by its own z-order puts a patch given none under every series, and one given a z-order among them.</summary>
    [Fact]
    public void APatch_StandsAmongTheSeries_ByItsZOrder_OrUnderThemAll()
    {
        ISeries[] series = [new LineSeries([0.0], [1.0]), new LineSeries([0.0], [1.0]) { ZOrder = 4 }];
        Patch[] patches = [new Rectangle(new(0, 0), 1, 1), new Rectangle(new(0, 0), 1, 1) { ZOrder = 2 }];

        var order = OwnOrder(series, patches);
        var steps = order.Steps;
        string drawn = string.Join(", ", steps.ToArray().Select(s => $"{s.Layer} {s.Index}"));
        order.Return();

        Assert.Equal("Patch 0, Series 0, Patch 1, Series 1", drawn);
    }

    /// <summary>A layered pass (<see cref="SeriesPass.Layered"/>, polar and ternary) lists its grid first among its equals, at the theme's
    /// grid z-order: under v2 over a patch and under a line.</summary>
    [Fact]
    public void ALayeredPass_ListsItsGrid_AtTheThemesGridZOrder()
    {
        var axes = new Axes();
        axes.AddSeries(new LineSeries([0.0], [1.0]));
        var order = SeriesPass.Layered(axes, Render(Theme.MatplotlibV2), [new Rectangle(new(0, 0), 1, 1)]).Order();
        string drawn = string.Join(", ", order.Steps.ToArray().Select(s => s.Layer));
        order.Return();

        Assert.Equal("Patch, Grid, Series", drawn);
    }

    /// <summary>A list of one step takes no array and is handed back as it is; a longer one is sorted in a rented array.</summary>
    [Fact]
    public void AListOfOne_TakesNoArray_AndALongerOneIsSorted()
    {
        var one = new LayerOrder(1);
        one.Add(9, DataLayer.Patch, 0);
        one.Sort();
        Assert.Equal([new LayerStep(0, 0, DataLayer.Patch, 0)], one.Steps.ToArray());
        one.Return();

        var three = new LayerOrder(3);
        three.Add(2, DataLayer.Series, 0);
        three.Add(1, DataLayer.Series, 1);
        three.Add(2, DataLayer.Series, 2);
        three.Sort();
        Assert.Equal([1, 0, 2], Indices(three));
    }

    // ---- the list of a Cartesian data layer ----------------------------------------------------------------------------------

    private static LayerOrder DataLayers(Axes axes, Theme theme, bool drawsGrid, bool drawsTicks) =>
        new CartesianAxesRenderer(axes, Plot, new RecordingRenderContext(), theme).DataLayers(drawsGrid, drawsTicks);

    private static DataLayer[] Layers(LayerOrder layers)
    {
        var steps = layers.Steps;
        var kinds = new DataLayer[steps.Length];
        for (int i = 0; i < steps.Length; i++) kinds[i] = steps[i].Layer;
        return kinds;
    }

    [Fact]
    public void AnAxesWithNothingToDraw_ListsNoStep()
    {
        var layers = DataLayers(new Axes(), Theme.MatplotlibV2, drawsGrid: false, drawsTicks: false);

        Assert.Equal(0, layers.Steps.Length);
        layers.Return();
    }

    [Fact]
    public void ALayerWithNothingToDraw_IsNotListed_AndTheOthersStandInTheOrderTheyWereDrawnIn()
    {
        var axes = new Axes();
        axes.AddSeries(new LineSeries([0.0, 1.0], [1.0, 2.0]));
        axes.AddPatch(new Rectangle(new(0, 0), 1, 1));
        axes.AxHLine(1);

        var layers = DataLayers(axes, Theme.Default, drawsGrid: true, drawsTicks: false);

        Assert.Equal([DataLayer.Grid, DataLayer.Patch, DataLayer.ReferenceLines, DataLayer.Series], Layers(layers));
        layers.Return();
    }

    /// <summary>Any one of the trendlines, the price levels and the Fibonacci retracements lists the tools.</summary>
    [Theory]
    [InlineData("trendline")]
    [InlineData("level")]
    [InlineData("fibonacci")]
    public void AnyDrawingTool_ListsTheTools(string tool)
    {
        var axes = new Axes();
        switch (tool)
        {
            case "trendline": axes.AddTrendline(0, 1, 1, 2); break;
            case "level": axes.AddLevel(1.5); break;
            default: axes.AddFibonacci(2, 1); break;
        }

        var layers = DataLayers(axes, Theme.MatplotlibV2, drawsGrid: false, drawsTicks: false);

        Assert.Equal([DataLayer.Tools], Layers(layers));
        layers.Return();
    }

    [Fact]
    public void UnderV2_TheSpansAndAPatchStandUnderTheGrid_AndTheGridUnderALine()
    {
        var axes = new Axes();
        axes.AddSeries(new LineSeries([0.0, 1.0], [1.0, 2.0]));
        axes.AddPatch(new Rectangle(new(0, 0), 1, 1));
        axes.AxHSpan(0, 1);

        var layers = DataLayers(axes, Theme.MatplotlibV2, drawsGrid: true, drawsTicks: false);

        Assert.Equal([DataLayer.Spans, DataLayer.Patch, DataLayer.Grid, DataLayer.Series], Layers(layers));
        layers.Return();
    }

    /// <summary>The tick marks are listed right after the grid, so a v2 axes of a grid and lines over them is listed in the order it is
    /// drawn in and is not sorted: every step stands at the place it was listed at.</summary>
    [Fact]
    public void UnderV2_AGridTickMarksAndALine_AreListedInTheOrderTheyAreDrawn()
    {
        var axes = new Axes();
        axes.AddSeries(new LineSeries([0.0, 1.0], [1.0, 2.0]));

        var layers = DataLayers(axes, Theme.MatplotlibV2, drawsGrid: true, drawsTicks: true);

        var steps = layers.Steps;
        Assert.Equal(3, steps.Length);
        for (int i = 0; i < steps.Length; i++) Assert.Equal(i, steps[i].Place);
        layers.Return();
    }

    /// <summary>Under the matplotlib themes the tick marks are a step at the axis' z-order (1.5 under v2, 2.5 under classic), after
    /// the grid at the same z-order, as matplotlib draws a tick's grid line before its marks; a series given a higher z-order stands
    /// over them. Under the library's own themes, or for an axes that draws no ticks, they are no step (the marks are drawn with
    /// their labels).</summary>
    [Theory]
    [InlineData("v2", true, "Patch, Grid, TickMarks, Series, Series")]
    [InlineData("classic", true, "Patch, Series, Grid, TickMarks, Series")]
    [InlineData("default", true, "Grid, Patch, Series, Series")]
    [InlineData("v2", false, "Patch, Grid, Series, Series")]
    public void TheTickMarks_AreAStepAtTheAxisZOrder_OnlyUnderTheMatplotlibThemes(string theme, bool drawsTicks, string expected)
    {
        var axes = new Axes();
        axes.AddSeries(new LineSeries([0.0, 1.0], [1.0, 2.0]));
        axes.AddSeries(new LineSeries([0.0, 1.0], [1.0, 2.0]) { ZOrder = 3 });
        axes.AddPatch(new Rectangle(new(0, 0), 1, 1));
        var look = theme.AsTheme();

        var layers = DataLayers(axes, look, drawsGrid: true, drawsTicks: drawsTicks);

        Assert.Equal(expected, string.Join(", ", Layers(layers)));
        layers.Return();
    }
}
