// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Frames;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Frames;

/// <summary>A native control paints through one path (<see cref="FrameTargetPaintExtensions"/>): the process' chart renderer
/// (<see cref="ChartRendererDefault.Current"/>) draws the figure onto the control's target and returns the layout of that draw,
/// then the rubber band of a brush-select drag in progress is drawn over it, by one rule for every native head. A renderer of
/// the caller's, which knows nothing of targets, is honoured: it draws with its own Render and lays out with its own
/// ComputeLayout.</summary>
[Collection("ChartServicesGlobalState")]
public sealed class FramePaintTests
{
    private static Figure OneLine() => Plt.Create().WithSize(300, 200).Plot([0.0, 1, 2], [1.0, 2, 1]).Build();

    /// <summary>A renderer of the caller's: draws one rectangle and lays out one plot area.</summary>
    private static RecordingChartRenderer CallersRenderer() => new(
        (_, ctx) => ctx.DrawRectangle(new Rect(1, 2, 3, 4), new ShapeStyle(Colors.Red, null, 0)),
        () => new LayoutResult([new Rect(5, 6, 7, 8)], [[]]));

    [Fact]
    public void ARendererOfTheCallers_IsHonoured_ItsRenderDrawsAndItsComputeLayoutLaysOut()
    {
        var callers = CallersRenderer();
        using var heldRenderer = ChartRendererDefault.Holding(callers);
        var surface = new RecordingRenderContext();

        var layout = new DirectFrameTarget(surface).Paint(OneLine(), band: null);

        Assert.Equal(1, callers.Renders);
        Assert.Equal(1, callers.Layouts);
        Assert.Equal([new Rect(5, 6, 7, 8)], layout.PlotAreas);
        Assert.Equal(new Rect(1, 2, 3, 4), Assert.Single(surface.Of<DrawRectangleCall>()).Rect);
    }

    [Fact]
    public void TheLibrarysRenderer_PaintsInOnePass_TheLayoutComputeLayoutGives()
    {
        var figure = OneLine();

        var layout = new DirectFrameTarget(new RecordingRenderContext()).Paint(figure, band: null);

        Assert.Equal(new ChartRenderer().ComputeLayout(figure, new RecordingRenderContext()).PlotAreas, layout.PlotAreas);
    }

    [Fact]
    public void ABrushSelectInProgress_IsDrawnOverTheFigure_LastOfAll()
    {
        var surface = new RecordingRenderContext();
        var band = new BrushSelectState(40, 30, 10, 50, 0);

        _ = new DirectFrameTarget(surface).Paint(OneLine(), band);

        var last = Assert.IsType<DrawRectangleCall>(surface.Calls[^1]);
        Assert.Equal(new Rect(10, 30, 30, 20), last.Rect);
    }

    [Fact]
    public void NoBrushSelect_DrawsNoBand()
    {
        var withBand = new RecordingRenderContext();
        var without = new RecordingRenderContext();

        _ = new DirectFrameTarget(withBand).Paint(OneLine(), new BrushSelectState(1, 1, 9, 9, 0));
        _ = new DirectFrameTarget(without).Paint(OneLine(), band: null);

        Assert.Equal(without.Count<DrawRectangleCall>() + 1, withBand.Count<DrawRectangleCall>());
    }

    [Fact]
    public void TheBand_IsATranslucentFillUnderAFirmerOutline_InTheColoursEveryNativeHeadUses()
    {
        var surface = new RecordingRenderContext();

        ((IRenderContext)surface).DrawBrushSelect(new BrushSelectState(5, 5, 25, 15, 0));

        var call = Assert.IsType<DrawRectangleCall>(Assert.Single(surface.Calls));
        Assert.Equal(new Color(30, 144, 255, 50), call.Fill);
        Assert.Equal(new Color(30, 144, 255, 180), call.Stroke);
        Assert.Equal(1.5, call.StrokeThickness);
    }

    /// <summary>A keeping target that records what it releases.</summary>
    private sealed class Releasing(KeptAxesDraw<RecordingRenderContext>[]? kept) : KeepingFrameTarget<RecordingRenderContext>(kept)
    {
        private readonly RecordingRenderContext _frame = new();
        public List<RecordingRenderContext> Released { get; } = [];
        internal override IRenderContext Background => _frame;
        internal override IRenderContext Overlay => _frame;
        protected override object? LayerKey => null;

        protected override LayerDraw<RecordingRenderContext> DrawLayer(in AxesDrawRequest request, bool keep,
            Func<IRenderContext, IReadOnlyList<LegendItemBounds>> draw)
        {
            var layer = new RecordingRenderContext();
            return new LayerDraw<RecordingRenderContext>(keep ? layer : null, draw(layer));
        }

        protected override void Place(int index, RecordingRenderContext layer) { }

        protected override void Release(RecordingRenderContext layer) => Released.Add(layer);
    }

    [Fact]
    public void ADrawThatVisitsNoSubplot_ReleasesEveryLayerItWasHanded()
    {
        var figure = OneLine();
        KeptAxesDraw<RecordingRenderContext>[]? kept = null;
        for (int i = 0; i < 2; i++)
        {
            var settling = new Releasing(kept);
            _ = new ChartRenderer().RenderAndLayOut(figure, settling);
            kept = settling.ToKeep();
        }

        var held = kept![0].Layer;
        Assert.NotNull(held);
        using var heldRenderer = ChartRendererDefault.Holding(CallersRenderer());
        var target = new Releasing(kept);
        _ = target.Paint(figure, band: null);

        Assert.Empty(target.ToKeep());
        Assert.Same(held, Assert.Single(target.Released));
    }
}
