// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>A writer that also draws a figure as a document in its parts (<see cref="SvgFigureDocument"/>), the form a publisher
/// sends only the parts that changed from: the library's <see cref="Transforms.SvgTransform"/>. Any <see cref="ISvgRenderer"/> is
/// asked through the same name (<see cref="SvgRendererDocumentExtensions"/>). It lives here and not on
/// <see cref="ISvgRenderer"/> so that an application can still implement, proxy or mock the public interface.</summary>
internal interface ISvgDocumentRenderer : ISvgRenderer
{
    /// <summary>The figure drawn as a document in its parts; written plain it is <see cref="ISvgRenderer.Render"/>'s string.</summary>
    /// <param name="figure">The figure to draw.</param>
    internal SvgFigureDocument RenderDocument(Figure figure);
}
