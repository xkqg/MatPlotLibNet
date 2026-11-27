// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Frames;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Frames;

/// <summary>The one drawing sequence (<see cref="ChartRenderer"/>) draws onto a <see cref="FrameTarget"/>; a target that keeps
/// what it drew (<see cref="KeepingFrameTarget{TLayer}"/>) hands an axes out again when nothing it was drawn from changed, keeps a
/// layer only from the second draw under one key, releases a layer it replaces, and gives the layout of the draw — the legend
/// bounds included — without building a second renderer for an axes it handed out. The SVG writer and the native painter are
/// two leaves of it; this test leaf records into <see cref="RecordingRenderContext"/>s.</summary>
[Collection("ChartServicesGlobalState")]
public sealed class KeepingFrameTargetTests
{
    /// <summary>A keeping target whose layer is a recording of the axes' calls, placed by copying them onto the frame's recording.</summary>
    private sealed class RecordingFrameTarget(KeptAxesDraw<RecordingRenderContext>[]? kept) : KeepingFrameTarget<RecordingRenderContext>(kept)
    {
        public RecordingRenderContext Frame { get; } = new();
        public List<int> Drawn { get; } = [];
        public List<bool> Keeps { get; } = [];
        public List<int> Placed { get; } = [];
        public List<RecordingRenderContext> Released { get; } = [];

        internal override IRenderContext Background => Frame;
        internal override IRenderContext Overlay => Frame;
        protected override object? LayerKey => null;

        protected override LayerDraw<RecordingRenderContext> DrawLayer(in AxesDrawRequest request, bool keep,
            Func<IRenderContext, IReadOnlyList<LegendItemBounds>> draw)
        {
            Drawn.Add(request.Index);
            Keeps.Add(keep);
            var layer = new RecordingRenderContext();
            var legend = draw(layer);
            Frame.Calls.AddRange(layer.Calls);
            return new LayerDraw<RecordingRenderContext>(keep ? layer : null, legend);
        }

        protected override void Place(int index, RecordingRenderContext layer)
        {
            Placed.Add(index);
            Frame.Calls.AddRange(layer.Calls);
        }

        protected override void Release(RecordingRenderContext layer) => Released.Add(layer);
    }

    private static Figure TwoPanels() =>
        Plt.Create().WithSize(500, 300)
            .AddSubPlot(1, 2, 1, ax => ax.Plot([0.0, 1, 2], [1.0, 3, 2], s => s.Label = "a").WithLegend())
            .AddSubPlot(1, 2, 2, ax => ax.Plot([0.0, 1, 2], [2.0, 1, 3], s => s.Label = "b").WithLegend())
            .Build();

    private static RecordingFrameTarget Paint(Figure figure, ref KeptAxesDraw<RecordingRenderContext>[]? kept, out LayoutResult layout)
    {
        var target = new RecordingFrameTarget(kept);
        layout = new ChartRenderer().RenderAndLayOut(figure, target);
        kept = target.ToKeep();
        return target;
    }

    [Fact]
    public void AnUnchangedFigure_IsDrawnTwice_ThenHandedOut_AndKeepsOnlyFromTheSecondDraw()
    {
        var figure = TwoPanels();
        KeptAxesDraw<RecordingRenderContext>[]? kept = null;

        var first = Paint(figure, ref kept, out _);
        var second = Paint(figure, ref kept, out _);
        var third = Paint(figure, ref kept, out _);

        Assert.Equal([0, 1], first.Drawn);
        Assert.Equal([false, false], first.Keeps);
        Assert.Equal([0, 1], second.Drawn);
        Assert.Equal([true, true], second.Keeps);
        Assert.Empty(third.Drawn);
        Assert.Equal([0, 1], third.Placed);
    }

    [Fact]
    public void AHandedOutFrame_IsTheFrameADrawMakes()
    {
        var figure = TwoPanels();
        KeptAxesDraw<RecordingRenderContext>[]? kept = null;
        _ = Paint(figure, ref kept, out _);
        var drawnTarget = Paint(figure, ref kept, out _);

        var placedTarget = Paint(figure, ref kept, out _);

        Assert.Equal(drawnTarget.Frame.Calls.Select(c => c.Kind), placedTarget.Frame.Calls.Select(c => c.Kind));
    }

    [Fact]
    public void AChangedAxes_IsDrawnAgain_AndTheLayerItReplacesIsReleased_TheOtherHandedOut()
    {
        var figure = TwoPanels();
        KeptAxesDraw<RecordingRenderContext>[]? kept = null;
        _ = Paint(figure, ref kept, out _);
        _ = Paint(figure, ref kept, out _);
        var replaced = kept![1].Layer;

        // A value inside the range the axes already shows: its ticks, and so the layout of the axes beside it, stay as they are.
        ((MatPlotLibNet.Models.Series.LineSeries)figure.SubPlots[1].Series[0]).YData[0] = 2.5;
        var next = Paint(figure, ref kept, out _);

        Assert.Equal([1], next.Drawn);
        Assert.Equal([0], next.Placed);
        Assert.Same(replaced, Assert.Single(next.Released));
    }

    [Fact]
    public void ADrawThatKeepsNoCopy_HoldsNoLayer()
    {
        var figure = TwoPanels();
        KeptAxesDraw<RecordingRenderContext>[]? kept = null;

        _ = Paint(figure, ref kept, out _);

        Assert.All(kept!, draw =>
        {
            Assert.False(draw.KeptData);
            Assert.Null(draw.Layer);
        });
    }

    [Fact]
    public void ThePaintsLayout_IsTheLayoutComputeLayoutGives_AlsoWhenEveryAxesIsHandedOut()
    {
        var figure = TwoPanels();
        var computed = new ChartRenderer().ComputeLayout(figure, new RecordingRenderContext());
        KeptAxesDraw<RecordingRenderContext>[]? kept = null;

        for (int draw = 0; draw < 3; draw++)
        {
            _ = Paint(figure, ref kept, out var layout);
            Assert.Equal(computed.PlotAreas, layout.PlotAreas);
            for (int i = 0; i < computed.LegendItems.Count; i++) Assert.Equal(computed.LegendItems[i], layout.LegendItems[i]);
        }
    }

    /// <summary>Counts the renderers made for a polar axes.</summary>
    private static int s_made;

    [Fact]
    public void AHandedOutAxes_BuildsNoRenderer_NotEvenForItsLayout()
    {
        AxesRenderer.RegisterRenderer(CoordinateSystem.Polar, (a, p, c, t) => { s_made++; return new PolarAxesRenderer(a, p, c, t); });
        try
        {
            var figure = Plt.Create().WithSize(300, 300).Plot([0.0, 1, 2], [1.0, 2, 1]).Build();
            figure.SubPlots[0].CoordinateSystem = CoordinateSystem.Polar;
            KeptAxesDraw<RecordingRenderContext>[]? kept = null;
            _ = Paint(figure, ref kept, out _);
            _ = Paint(figure, ref kept, out _);
            // Laying the frame out measures what each axes draws past its spines, with a renderer of its own: that much a
            // paint costs whatever it hands out.
            s_made = 0;
            _ = new ChartRenderer().FrameOf(figure, new RecordingRenderContext());
            int forTheFrame = s_made;
            s_made = 0;

            _ = Paint(figure, ref kept, out _);

            Assert.Equal(forTheFrame, s_made);
        }
        finally
        {
            AxesRenderer.RegisterRenderer(CoordinateSystem.Polar, (a, p, c, t) => new PolarAxesRenderer(a, p, c, t));
        }
    }

    [Fact]
    public void ADirectTarget_DrawsWhatRenderDraws()
    {
        var figure = TwoPanels();
        var rendered = new RecordingRenderContext();
        new ChartRenderer().Render(figure, rendered);
        var direct = new RecordingRenderContext();

        _ = new ChartRenderer().RenderAndLayOut(figure, new DirectFrameTarget(direct));

        Assert.Equal(rendered.Calls.Select(c => c.Kind), direct.Calls.Select(c => c.Kind));
    }
}
