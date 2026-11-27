// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Frames;
using MatPlotLibNet.Rendering.TextMeasurement;

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>The target an SVG draw of a figure goes onto: a context for what lies under the subplots, one per subplot and one for
/// what lies above them, so the document can be written whole or in its parts (<see cref="SvgFigureDocument"/>). A kept subplot is
/// the context it was drawn into; one handed out again is that same context, so a publisher sees it is unchanged.</summary>
internal sealed class SvgFrameTarget : KeepingFrameTarget<SvgRenderContext>
{
    private readonly Figure _figure;
    private readonly SvgRenderContext _background = new();
    private readonly SvgRenderContext _overlay = new();
    private readonly SvgRenderContext[] _axes;

    /// <summary>A target for a draw of <paramref name="figure"/> after the one that left <paramref name="kept"/>.</summary>
    internal SvgFrameTarget(Figure figure, KeptAxesDraw<SvgRenderContext>[]? kept) : base(kept)
    {
        _figure = figure;
        _axes = new SvgRenderContext[figure.SubPlots.Count];
    }

    /// <inheritdoc />
    internal override IRenderContext Background => _background;

    /// <inheritdoc />
    internal override IRenderContext Overlay => _overlay;

    /// <summary>The glyph writer of the process: with one installed, the SVG's text is written as outlines.</summary>
    protected override object? LayerKey => GlyphPathProviderDefault.Current;

    /// <summary>Draws the subplot into a context of its own, whose ids carry the subplot's prefix and which carries what the
    /// browser needs of it (<see cref="SvgEmit"/>); the context is the layer, kept or not.</summary>
    protected override LayerDraw<SvgRenderContext> DrawLayer(in AxesDrawRequest request, bool keep,
        Func<IRenderContext, IReadOnlyList<LegendItemBounds>> draw)
    {
        var ctx = new SvgRenderContext(SvgIdScope.ForSubplot(request.Index)) { Emit = SvgEmit.Of(_figure, request.Index) };
        var legend = draw(ctx);
        _axes[request.Index] = ctx;
        return new LayerDraw<SvgRenderContext>(ctx, legend);
    }

    /// <inheritdoc />
    protected override void Place(int index, SvgRenderContext layer) => _axes[index] = layer;

    /// <summary>The document of this draw, in its parts. A subplot the renderer never drew onto a context of its own (a renderer
    /// of the caller's draws the whole figure onto the background) is an empty part.</summary>
    internal SvgFigureDocument ToDocument(string rootTag, string headBody, IReadOnlyList<string> scripts)
    {
        for (int i = 0; i < _axes.Length; i++) _axes[i] ??= new SvgRenderContext(SvgIdScope.ForSubplot(i));
        return new(rootTag, headBody, _background, _axes, _overlay, scripts);
    }
}
