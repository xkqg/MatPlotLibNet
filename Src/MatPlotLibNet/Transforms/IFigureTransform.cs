// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Transforms;

/// <summary>Transforms a <see cref="Figure"/> into a specific output format written to a stream.</summary>
public interface IFigureTransform
{
    /// <summary>Transforms the figure and writes the result to the output stream.</summary>
    /// <param name="figure">The figure to transform.</param>
    /// <param name="output">The stream to write the transformed output to.</param>
    void Transform(Figure figure, Stream output);

    /// <summary>Transforms the figure and writes the result to the output stream, using the settings in
    /// <paramref name="options"/> that the format can use. <c>Figure.Save(path, options)</c> calls this one when it is
    /// given options.</summary>
    /// <param name="figure">The figure to transform.</param>
    /// <param name="output">The stream to write the transformed output to.</param>
    /// <param name="options">The save settings. Which of them a transform reads is stated by the transform;
    /// <see cref="SvgTransform"/>, the PNG transform and the PDF transform each document theirs.</param>
    /// <remarks>The default implementation reads none of the settings and calls
    /// <see cref="Transform(Figure, Stream)"/>, so a transform written before the save settings existed keeps working
    /// and writes the same file with or without options. A transform that can use some of them overrides this method
    /// and says in its documentation which.</remarks>
    void Transform(Figure figure, Stream output, SaveOptions options) => Transform(figure, output);
}
