// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>Any SVG writer asked for a figure's document in its parts.</summary>
internal static class SvgRendererDocumentExtensions
{
    extension(ISvgRenderer writer)
    {
        /// <summary>The figure drawn as a document in its parts (<see cref="SvgFigureDocument"/>); <see langword="null"/> for a
        /// writer that draws the document whole only, which is every writer but the library's own
        /// (<see cref="ISvgDocumentRenderer"/>), so a publisher sends such a writer's document whole.</summary>
        /// <param name="figure">The figure to draw.</param>
        internal SvgFigureDocument? RenderDocument(Figure figure) =>
            writer is ISvgDocumentRenderer parts ? parts.RenderDocument(figure) : null;
    }
}
