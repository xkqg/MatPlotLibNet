// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Transforms;

/// <summary>Base class for figure transforms that convert a <see cref="Figure"/> into a specific output format.</summary>
/// <remarks>Concrete implementations (<see cref="SvgTransform"/>, <c>PngTransform</c>, <c>PdfTransform</c>) each
/// create a format-specific <see cref="IRenderContext"/>, draw the figure onto it with the <see cref="IChartRenderer"/> the
/// transform was made with (<see cref="FigureRenderer"/>), and encode the result to the target format. Any renderer will do: the
/// library's <see cref="ChartRenderer"/>, the default, or one of the caller's.</remarks>
public abstract class FigureTransform : IFigureTransform
{
    /// <summary>The chart renderer that lays out and draws the figure onto the format-specific render context: the one the
    /// transform was made with.</summary>
    protected IChartRenderer FigureRenderer { get; }

    /// <summary>Creates a new figure transform that draws with <paramref name="renderer"/>.</summary>
    /// <param name="renderer">The renderer that draws the figure: the library's or one of the caller's.</param>
    protected FigureTransform(IChartRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        FigureRenderer = renderer;
    }

    /// <summary>Creates a new figure transform with the default chart renderer.</summary>
    protected FigureTransform() : this(new ChartRenderer()) { }

    /// <inheritdoc />
    public abstract void Transform(Figure figure, Stream output);

    /// <summary>Writes the figure using the settings of <paramref name="options"/> the format can use. This base
    /// implementation reads none of them and calls <see cref="Transform(Figure, Stream)"/>; a transform that can use
    /// some overrides it and documents which.</summary>
    public virtual void Transform(Figure figure, Stream output, SaveOptions options) => Transform(figure, output);
}
