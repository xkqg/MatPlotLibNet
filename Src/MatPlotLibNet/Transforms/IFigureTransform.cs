// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Transforms;

/// <summary>Transforms a <see cref="Figure"/> into a specific output format written to a stream.</summary>
public interface IFigureTransform
{
    /// <summary>Transforms the figure and writes the result to the output stream.</summary>
    /// <param name="figure">The figure to transform.</param>
    /// <param name="output">The stream to write the transformed output to.</param>
    void Transform(Figure figure, Stream output);
}
