// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>What an SVG draw writes for the browser into one subplot, decided from the figure as it is when it is drawn: the
/// data attributes an interaction script finds a series by, the mapping a data cursor converts a click with, the view the
/// server-interaction script reads, and the vertex data a 3-D axes is turned with in the browser. It travels with the context
/// the subplot is drawn into and is never written into the model, so a figure serialises the same before and after a draw and
/// an interaction switched off is gone from the next draw.</summary>
/// <param name="Subject">The subplot these apply to; an inset drawn into the same context is not given them.</param>
/// <param name="InteractiveAttributes">Whether the series and the legend entries carry the attributes the scripts read.</param>
/// <param name="DataCursorMapping">Whether an interactive series group carries the mapping it was drawn with.</param>
/// <param name="ServerViewIndex">The index the subplot writes its drawn view under, or <see langword="null"/> for none.</param>
/// <param name="VertexData3D">Whether a 3-D axes writes the vertex data its faces are turned with.</param>
internal readonly record struct SvgEmit(Axes? Subject, bool InteractiveAttributes, bool DataCursorMapping, int? ServerViewIndex,
    bool VertexData3D)
{
    /// <summary>What a draw of <paramref name="figure"/> writes into its subplot at <paramref name="index"/>.</summary>
    internal static SvgEmit Of(Figure figure, int index)
    {
        var axes = figure.SubPlots[index];
        return new SvgEmit(axes, figure.HasInteractivity, figure.ServerInteraction && figure.EnableDataCursor,
            figure.ServerInteraction ? index : null,
            figure.Enable3DRotation && axes.CoordinateSystem == CoordinateSystem.ThreeD);
    }

    /// <summary>What <paramref name="axes"/> writes for the browser when it is drawn into <paramref name="ctx"/>, the one rule for it:
    /// what the figure asked of the context for this subplot (<see cref="IMarkupRenderContext.Emit"/>), together with the axes' own
    /// <see cref="Axes.EnableInteractiveAttributes"/> and <see cref="Axes.Emit3DVertexData"/>. An axes drawn into a context made for
    /// another subplot (an inset), or into a context without markup, writes only what it asked for itself.</summary>
    /// <param name="axes">The axes that is drawn.</param>
    /// <param name="ctx">The context the axes is drawn into.</param>
    internal static SvgEmit Of(Axes axes, IRenderContext ctx)
    {
        var emit = ctx.Emit;
        var asked = ReferenceEquals(emit.Subject, axes) ? emit : default;
        return asked with
        {
            Subject = axes,
            InteractiveAttributes = asked.InteractiveAttributes || axes.EnableInteractiveAttributes,
            VertexData3D = asked.VertexData3D || axes.Emit3DVertexData,
        };
    }
}
