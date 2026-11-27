// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>What a Cartesian draw leaves undone when there is nothing to do: an axes with only its primary Y axis stacks no slots,
/// and an axes without spans, patches, reference lines, drawing tools, annotations or signal markers makes no part for them. The
/// drawing itself is the same either way; these tests hold both halves, the saving and that a decoration that is there is still
/// drawn.</summary>
[Collection("ChartServicesGlobalState")]
public sealed class CartesianDrawTrimTests
{
    private static readonly Rect Plot = new(60, 20, 400, 300);

    private static readonly double[] Ramp = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

    private static Axes LineAxes(Action<AxesBuilder>? configure = null) =>
        Plt.Create().WithSize(500, 350)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot(Ramp, Ramp).SetXLim(0, 10).SetYLim(0, 10);
                configure?.Invoke(ax);
            }).Build().SubPlots[0];

    /// <summary>Bytes the current thread allocated while <paramref name="work"/> ran.</summary>
    private static long Allocated(Action work)
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        work();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    // ---- the primary Y axis alone -------------------------------------------------------------------------------

    private static PlacedAxis Primary(CartesianAxisPlacement placement, Axes axes) =>
        placement.PlaceSlot(axes.YAxes.Left[0], placement.ComputeDataRanges()) with { Pair = SeriesAxisPair.Primary, Band = 40 };

    [Fact]
    public void AnAxesWithOnlyThePrimaryYAxis_IsPlacedAtOffsetZero_WithItsBand()
    {
        var axes = LineAxes();
        var placement = new CartesianAxisPlacement(axes, Plot, new SilentRenderContext(), Theme.Default);
        var primary = Primary(placement, axes);

        var placed = Assert.Single(placement.PlaceYSlots(primary));

        Assert.Equal(0, placed.Offset);
        Assert.Equal(40, placed.Band);
        Assert.Same(primary.Axis, placed.Axis);
    }

    /// <summary>The list that holds the primary is all it makes: no seats are listed, no slot is stacked.</summary>
    [Fact]
    public void AnAxesWithOnlyThePrimaryYAxis_MakesNothingButTheListThatHoldsIt()
    {
        var axes = LineAxes();
        var placement = new CartesianAxisPlacement(axes, Plot, new SilentRenderContext(), Theme.Default);
        var primary = Primary(placement, axes);
        placement.PlaceYSlots(primary);

        long placing = Allocated(() => placement.PlaceYSlots(primary));
        long bareList = Allocated(() => _ = new List<PlacedAxis>(1) { primary });

        Assert.Equal(bareList, placing);
    }

    [Fact]
    public void ASlotPastThePrimaryOnTheRight_StandsAtTheEdge_AndTheNextOneIsStackedPastIt()
    {
        var axes = LineAxes(ax => ax
            .WithAddRightYAxis(s => s.Plot(Ramp, [.. Ramp.Select(v => v * 100)]))
            .WithAddRightYAxis(s => s.Plot(Ramp, [.. Ramp.Select(v => v * 1000)])));
        var placement = new CartesianAxisPlacement(axes, Plot, new SilentRenderContext(), Theme.Default);

        var placed = placement.PlaceYSlots(Primary(placement, axes));

        Assert.Equal(3, placed.Count);
        Assert.Equal(0, placed[0].Offset);
        Assert.Equal(0, placed[1].Offset);
        Assert.Equal(placed[1].Band + axes.YAxes.TitleGap, placed[2].Offset);
    }

    [Fact]
    public void ASlotPastThePrimaryOnTheLeft_IsStackedPastThePrimarysBand()
    {
        var axes = LineAxes(ax => ax.WithAddLeftYAxis(s => s.Plot(Ramp, [.. Ramp.Select(v => v * 100)])));
        var placement = new CartesianAxisPlacement(axes, Plot, new SilentRenderContext(), Theme.Default);

        var placed = placement.PlaceYSlots(Primary(placement, axes));

        Assert.Equal(2, placed.Count);
        Assert.Equal(0, placed[0].Offset);
        Assert.Equal(placed[0].Band + axes.YAxes.TitleGap, placed[1].Offset);
    }

    // ---- the parts that draw what is laid over the data ----------------------------------------------------------

    /// <summary>One decoration of each kind the parts draw, at a place the axes cannot place (not a number), so it adds nothing
    /// to the picture: any bytes it adds to a draw are the part that would draw it.</summary>
    public static TheoryData<string> Decorations() =>
        ["span", "patch", "reference line", "trendline", "level", "fibonacci", "annotation", "signal"];

    private static void Decorate(AxesBuilder ax, string kind)
    {
        switch (kind)
        {
            case "span": ax.AxHSpan(double.NaN, double.NaN); break;
            case "patch": ax.AddPatch(new Rectangle(new DataPoint(double.NaN, double.NaN), 1, 1)); break;
            case "reference line": ax.AxHLine(double.NaN); break;
            case "trendline": ax.AddTrendline(double.NaN, 0, 1, 1); break;
            case "level": ax.AddLevel(double.NaN); break;
            case "fibonacci": ax.AddFibonacci(double.NaN, double.NaN); break;
            case "annotation": ax.Annotate("note", double.NaN, 1); break;
            case "signal": ax.AddSignal(double.NaN, 1, SignalDirection.Buy); break;
            default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }

    private static long RenderAllocated(Axes axes)
    {
        var renderer = new CartesianAxesRenderer(axes, Plot, new SilentRenderContext(), Theme.Default);
        return Allocated(renderer.Render);
    }

    /// <summary>An axes with nothing laid over the data draws its axes, ticks and series and makes no part for the rest, and stacks
    /// no slot past the primary. Eight parts are each an object of a hundred bytes and more, and the slots another three
    /// kilobytes; the ceiling is what the draw costs without them.</summary>
    [Fact]
    public void AnAxesWithoutDecorations_MakesNoPartForThem()
    {
        var axes = LineAxes();
        RenderAllocated(axes);

        long allocated = RenderAllocated(axes);

        Assert.True(allocated <= DrawWithoutParts, $"A draw of a plain line axes allocated {allocated} bytes; the ceiling is {DrawWithoutParts}.");
    }

    /// <summary>Bytes a draw of the plain line axes above may allocate: what it takes with the parts left unmade, and a margin.</summary>
    private const long DrawWithoutParts = 12_800;

    [Theory]
    [MemberData(nameof(Decorations))]
    public void ADecorationThatIsThere_StillMakesItsPart_SoTheDrawCostsMoreThanWithout(string kind)
    {
        var plain = LineAxes();
        var decorated = LineAxes(ax => Decorate(ax, kind));
        RenderAllocated(plain);
        RenderAllocated(decorated);

        Assert.True(RenderAllocated(decorated) > RenderAllocated(plain), kind);
    }

    // ---- the picture does not change ----------------------------------------------------------------------------

    /// <summary>One decoration of each kind at a place that can be placed, and what each draws.</summary>
    public static TheoryData<string, Type> Drawn() => new()
    {
        { "span", typeof(DrawRectangleCall) },
        { "patch", typeof(DrawPathCall) },
        { "reference line", typeof(DrawLineCall) },
        { "trendline", typeof(DrawLineCall) },
        { "level", typeof(DrawLineCall) },
        { "fibonacci", typeof(DrawLineCall) },
        { "annotation", typeof(DrawTextCall) },
        { "signal", typeof(DrawPolygonCall) },
    };

    private static void DecorateInPlace(AxesBuilder ax, string kind)
    {
        switch (kind)
        {
            case "span": ax.AxHSpan(2, 4); break;
            case "patch": ax.AddPatch(new Rectangle(new DataPoint(2, 2), 3, 3)); break;
            case "reference line": ax.AxHLine(5); break;
            case "trendline": ax.AddTrendline(1, 1, 8, 7); break;
            case "level": ax.AddLevel(5); break;
            case "fibonacci": ax.AddFibonacci(9, 1); break;
            case "annotation": ax.Annotate("note", 4, 4); break;
            case "signal": ax.AddSignal(4, 4, SignalDirection.Buy); break;
            default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }

    [Theory]
    [MemberData(nameof(Drawn))]
    public void ADecorationThatCanBePlaced_IsStillDrawn(string kind, Type call)
    {
        var plain = new RecordingRenderContext();
        new CartesianAxesRenderer(LineAxes(), Plot, plain, Theme.Default).Render();
        var decorated = new RecordingRenderContext();
        new CartesianAxesRenderer(LineAxes(ax => DecorateInPlace(ax, kind)), Plot, decorated, Theme.Default).Render();

        Assert.True(decorated.Calls.Count(c => c.GetType() == call) > plain.Calls.Count(c => c.GetType() == call), $"{kind} drew no {call.Name}");
    }

    // ---- the axes background -------------------------------------------------------------------------------------

    /// <summary>Every renderer that fills its plot area with the theme's axes background does it first, before anything else.
    /// A polar axes fills its circle instead (<see cref="PolarBackgroundTests"/>).</summary>
    [Theory]
    [InlineData(CoordinateSystem.Cartesian)]
    [InlineData(CoordinateSystem.ThreeD)]
    public void TheAxesBackground_IsTheFirstThingDrawn_InThePlotArea(CoordinateSystem system)
    {
        var axes = LineAxes();
        axes.CoordinateSystem = system;
        var theme = Theme.CreateFrom(Theme.Default).WithAxesBackground(Colors.Orange).Build();
        var ctx = axes.Drawn(Plot, theme);

        var first = Assert.IsType<DrawRectangleCall>(ctx.Calls[0]);
        Assert.Equal(Plot, first.Rect);
        Assert.Equal(Colors.Orange, first.Fill);
        Assert.Null(first.Stroke);
    }
}
