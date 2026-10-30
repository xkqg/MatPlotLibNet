// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Transforms;

/// <summary>Base class for figure transforms that convert a <see cref="Figure"/> into a specific output format.</summary>
/// <remarks>Concrete implementations (<see cref="SvgTransform"/>, <c>PngTransform</c>, <c>PdfTransform</c>) each
/// create a format-specific <see cref="IRenderContext"/>, pass it through the shared <see cref="ChartRenderer"/> pipeline,
/// and encode the result to the target format. The renderer is held as a shared dependency in this base class.</remarks>
public abstract class FigureTransform : IFigureTransform
{
    /// <summary>The shared chart renderer that lays out and draws the figure onto the format-specific render context.</summary>
    protected ChartRenderer Renderer { get; }

    /// <summary>Creates a new figure transform with the specified chart renderer.</summary>
    protected FigureTransform(IChartRenderer renderer) => Renderer = (ChartRenderer)renderer;

    /// <summary>Creates a new figure transform with the default chart renderer.</summary>
    protected FigureTransform() : this(new ChartRenderer()) { }

    /// <inheritdoc />
    public abstract void Transform(Figure figure, Stream output);

    /// <summary>Writes the figure using the settings of <paramref name="options"/> the format can use. This base
    /// implementation reads none of them and calls <see cref="Transform(Figure, Stream)"/>; a transform that can use
    /// some overrides it and documents which.</summary>
    public virtual void Transform(Figure figure, Stream output, SaveOptions options) => Transform(figure, output);
}
