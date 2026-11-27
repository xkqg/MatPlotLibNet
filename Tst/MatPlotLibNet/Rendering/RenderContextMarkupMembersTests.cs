// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>What a surface that writes markup for a browser adds around what it draws (accessible groups, series and legend
/// groups, tooltips, the 3-D scene, the view the server script reads) is asked of <see cref="IRenderContext"/>, never of the
/// surface's type: a surface without markup gets the defaults, which draw nothing extra, and any surface that writes markup
/// is handed every one of them, whatever its class.</summary>
public class RenderContextMarkupMembersTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0];
    private static readonly double[] Y = [1.0, 3.0, 2.0];

    /// <summary>A surface without markup opens no group of its own, writes no tooltip, no view and no scene, and hands out no
    /// emit: drawn on it, a figure is drawn exactly as before the members existed.</summary>
    [Fact]
    public void ASurfaceWithoutMarkup_AddsNothingAroundWhatItDraws()
    {
        var plain = new RecordingRenderContext();
        IRenderContext ctx = plain;

        Assert.Equal(default, ctx.Emit);
        Assert.False(ctx.OpenSeriesGroup(new SvgEmit(null, true, true, 0, true), "a", 0, null));
        Assert.False(ctx.BeginLegendItemGroup(0, "a"));
        Assert.False(ctx.OpenPatchGroup(new SvgEmit(null, true, true, 0, true), 3, "a"));
        Assert.False(ctx.Begin3DSceneGroup(30, -60, null, new Rect(0, 0, 10, 10), default));
        ctx.Begin3DSubgroup("mpl-3d-back");
        ctx.End3DSubgroup();
        ctx.BeginTooltipGroup("tip");
        ctx.EndTooltipGroup();
        ctx.SetNextElementClass("mpl-pane");
        ctx.SetNextTextLevel(2);
        ctx.SetNextTextLevel(null);
        ctx.WriteAxesView(0, new Rect(0, 0, 10, 10), null);

        Assert.Empty(plain.Calls);
    }

    /// <summary>An accessible group on a surface without markup is the plain group of the same class, so the legend and the
    /// colour bar open the group they always opened there.</summary>
    [Fact]
    public void ASurfaceWithoutMarkup_OpensAnAccessibleGroupAsAPlainOne()
    {
        var plain = new RecordingRenderContext();

        ((IRenderContext)plain).BeginAccessibleGroup("legend", "Chart legend");

        var call = Assert.IsType<BeginGroupCall>(Assert.Single(plain.Calls));
        Assert.Equal("legend", call.CssClass);
    }

    /// <summary>Asked through the interface, the SVG context writes every markup member: its own members implement the
    /// interface's, so a renderer that holds only an <see cref="IRenderContext"/> gets the markup.</summary>
    [Fact]
    public void TheSvgContext_WritesEveryMarkupMemberAskedThroughTheInterface()
    {
        var svg = new SvgRenderContext { Emit = new SvgEmit(null, true, false, 3, false) };
        IRenderContext ctx = svg;

        Assert.Equal(3, ctx.Emit.ServerViewIndex);
        ctx.BeginAccessibleGroup("legend", "Chart legend");
        Assert.True(ctx.OpenSeriesGroup(new SvgEmit(null, true, false, null, false), "a", 2, null));
        Assert.True(ctx.OpenSeriesGroup(default, "b", 0, null));
        Assert.False(ctx.OpenSeriesGroup(default, null, 0, null));
        Assert.True(ctx.BeginLegendItemGroup(4, "c"));
        Assert.True(ctx.OpenPatchGroup(new SvgEmit(null, true, false, null, false), 7, "d"));
        Assert.False(ctx.OpenPatchGroup(default, 8, "e"));
        ctx.BeginTooltipGroup("tip");
        ctx.EndTooltipGroup();
        ctx.SetNextElementClass("mpl-pane");
        ctx.SetNextTextLevel(3);
        ctx.DrawText("row", new Point(5, 5), new Font(), TextAlignment.Left);
        ctx.DrawRectangle(new Rect(0, 0, 1, 1), new ShapeStyle(Colors.Red, null, 0));
        ctx.WriteAxesView(1, new Rect(0, 0, 10, 10), null);
        Assert.True(ctx.Begin3DSceneGroup(30, -60, null, new Rect(0, 0, 10, 10), default));
        ctx.Begin3DSubgroup("mpl-3d-back");
        ctx.End3DSubgroup();

        string output = svg.GetOutput();
        Assert.Contains("<g class=\"legend\" aria-label=\"Chart legend\">", output);
        Assert.Contains("data-series-index=\"2\" aria-label=\"a\"", output);
        Assert.Contains("<g class=\"series\" aria-label=\"b\">", output);
        Assert.Contains("data-legend-index=\"4\"", output);
        Assert.Contains("<g class=\"patch\" data-series-index=\"7\" aria-label=\"d\">", output);
        Assert.DoesNotContain("aria-label=\"e\"", output);
        Assert.Contains("<g><title>tip</title>", output);
        Assert.Contains("class=\"mpl-pane\"", output);
        Assert.Contains("aria-level=\"3\"", output);
        Assert.Contains("data-axes-index=\"1\"", output);
        Assert.Contains("class=\"mpl-3d-scene\"", output);
        Assert.Contains("<g class=\"mpl-3d-back\">", output);
    }

    /// <summary>The legend and each series are opened through the context, so a context that writes markup gets the accessible
    /// legend group and a series group, whatever its class.</summary>
    [Fact]
    public void TheLegendAndTheSeries_AskTheContextForTheirGroups()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y, s => s.Label = "a").WithLegend()).Build();
        var markup = new MarkupRecordingContext();

        new ChartRenderer().Render(figure, markup);

        Assert.Contains("accessible legend Chart legend", markup.Calls);
        Assert.Contains("series 0 a interactive=False", markup.Calls);
        Assert.DoesNotContain(markup.Calls, c => c.StartsWith("legend item", StringComparison.Ordinal));
    }

    /// <summary>What the figure asked of the context (<see cref="IMarkupRenderContext.Emit"/>) reaches the axes drawn into it: an
    /// interactive draw opens a data group per series and a group per legend entry, and the server view is written.</summary>
    [Fact]
    public void WhatTheContextWasAsked_ReachesTheAxesDrawnIntoIt()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y, s => s.Label = "a").WithLegend()).Build();
        var markup = new MarkupRecordingContext { Emit = new SvgEmit(figure.SubPlots[0], true, false, 0, false) };

        new ChartRenderer().Render(figure, markup);

        Assert.Contains("series 0 a interactive=True", markup.Calls);
        Assert.Contains("legend item 0 a", markup.Calls);
        Assert.Contains("view 0", markup.Calls);
        Assert.Equal(markup.Emit, SvgEmit.Of(figure.SubPlots[0], markup));
    }

    /// <summary>The colour bar opens its accessible group through the context.</summary>
    [Fact]
    public void TheColourBar_AsksTheContextForItsGroup()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } }).WithColorBar()).Build();
        var markup = new MarkupRecordingContext();

        new ChartRenderer().Render(figure, markup);

        Assert.Contains("accessible colorbar Color bar", markup.Calls);
    }

    /// <summary>A 3-D axes asked for its vertex data opens the scene and its three tiers through the context, tags its panes
    /// and writes the vertex data of what it draws.</summary>
    [Fact]
    public void A3DScene_AsksTheContextForItsGroups()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Surface(X, X, new double[,] { { 0, 1, 2 }, { 1, 2, 3 }, { 2, 3, 4 } }))
            .Build();
        var markup = new MarkupRecordingContext { Emit = new SvgEmit(figure.SubPlots[0], false, false, null, true) };

        new ChartRenderer().Render(figure, markup);

        Assert.Contains("scene", markup.Calls);
        string[] tiers = [.. markup.Calls.Where(c => c.StartsWith("tier", StringComparison.Ordinal))];
        Assert.Equal(["tier mpl-3d-back", "tier end", "tier mpl-3d-data", "tier end", "tier mpl-3d-front", "tier end"], tiers);
        Assert.Equal(3, markup.Calls.Count(c => c == "class mpl-pane"));
        Assert.Contains("data v3d", markup.Calls);
    }

    /// <summary>A 3-D axes whose context opens no scene writes no vertex data, even when the axes asks for it.</summary>
    [Fact]
    public void A3DScene_ThatTheContextDoesNotOpen_WritesNoVertexData()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Surface(X, X, new double[,] { { 0, 1, 2 }, { 1, 2, 3 }, { 2, 3, 4 } }))
            .Build();
        var markup = new MarkupRecordingContext { Emit = new SvgEmit(figure.SubPlots[0], false, false, null, true), OpensScene = false };

        new ChartRenderer().Render(figure, markup);

        Assert.DoesNotContain(markup.Calls, c => c.StartsWith("tier", StringComparison.Ordinal));
        Assert.DoesNotContain("data v3d-edge", markup.Calls);
    }

    /// <summary>A series renderer with tooltips on opens its tooltip groups through the context.</summary>
    [Fact]
    public void Tooltips_AskTheContextForTheirGroups()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Scatter(X, Y).WithTooltips()).Build();
        var markup = new MarkupRecordingContext();

        new ChartRenderer().Render(figure, markup);

        int opened = markup.Calls.Count(c => c.StartsWith("tooltip x=", StringComparison.Ordinal));
        Assert.Equal(X.Length, opened);
        Assert.Equal(opened, markup.Calls.Count(c => c == "tooltip end"));
    }

    /// <summary>A word cloud titles every word with its weight through the context.</summary>
    [Fact]
    public void AWordCloud_TitlesItsWordsThroughTheContext()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.WordCloud(["alpha", "beta"], [2.0, 1.0])).Build();
        var markup = new MarkupRecordingContext();

        new ChartRenderer().Render(figure, markup);

        Assert.Contains("tooltip alpha: 2", markup.Calls);
        Assert.Contains("tooltip beta: 1", markup.Calls);
    }

    /// <summary>A context that is not <see cref="SvgRenderContext"/> but writes markup: it records each markup member it is
    /// handed, and draws nothing.</summary>
    private sealed class MarkupRecordingContext : SilentRenderContext, IMarkupRenderContext
    {
        public List<string> Calls { get; } = [];

        public SvgEmit Emit { get; init; }

        /// <summary>Whether the context opens the 3-D scene it is asked for.</summary>
        public bool OpensScene { get; init; } = true;

        SvgEmit IMarkupRenderContext.Emit => Emit;

        public void SetNextElementData(string key, string value) => Calls.Add($"data {key}");

        void IMarkupRenderContext.BeginAccessibleGroup(string cssClass, string ariaLabel) => Calls.Add($"accessible {cssClass} {ariaLabel}");

        bool IMarkupRenderContext.OpenSeriesGroup(SvgEmit emit, string? label, int slotIndex, DataTransform? drawnWith)
        {
            Calls.Add($"series {slotIndex} {label} interactive={emit.InteractiveAttributes}");
            return true;
        }

        void IMarkupRenderContext.OpenPatchGroup(int index, string label) => Calls.Add($"patch {index} {label}");

        bool IMarkupRenderContext.BeginLegendItemGroup(int legendIndex, string? ariaLabel)
        {
            Calls.Add($"legend item {legendIndex} {ariaLabel}");
            return true;
        }

        void IMarkupRenderContext.BeginTooltipGroup(string tooltipText) => Calls.Add($"tooltip {tooltipText}");

        void IMarkupRenderContext.EndTooltipGroup() => Calls.Add("tooltip end");

        void IMarkupRenderContext.SetNextElementClass(string className) => Calls.Add($"class {className}");

        void IMarkupRenderContext.SetNextTextLevel(int? level) => Calls.Add($"level {level}");

        void IMarkupRenderContext.WriteAxesView(int axesIndex, Rect plotArea, DataTransform? drawnWith,
            IReadOnlyList<SecondaryAxisTransform>? secondary) => Calls.Add($"view {axesIndex}");

        bool IMarkupRenderContext.Begin3DSceneGroup(double elevation, double azimuth, double? distance, Rect plotBounds,
            CubeFaceSelection faces)
        {
            if (OpensScene) Calls.Add("scene");
            return OpensScene;
        }

        void IMarkupRenderContext.Begin3DSubgroup(string className) => Calls.Add($"tier {className}");

        void IMarkupRenderContext.End3DSubgroup() => Calls.Add("tier end");
    }
}
