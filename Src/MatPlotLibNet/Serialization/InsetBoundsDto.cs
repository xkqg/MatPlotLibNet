// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Serialization;

/// <summary>Data-transfer object for an inset axes bounding box expressed in figure-normalized coordinates (0–1).</summary>
internal sealed record InsetBoundsDto
{
    public double X { get; init; }
    public double Y { get; init; }
    public double Width { get; init; }
    public double Height { get; init; }

    /// <summary>The JSON's form of <paramref name="bounds"/>.</summary>
    internal static InsetBoundsDto Of(InsetBounds bounds) => new()
    {
        X = bounds.X, Y = bounds.Y, Width = bounds.Width, Height = bounds.Height
    };

    /// <summary>Adds an inset at these bounds to <paramref name="parent"/> and returns it.</summary>
    /// <param name="parent">The axes the inset is drawn in.</param>
    internal Axes AddInsetTo(Axes parent) => parent.AddInset(X, Y, Width, Height);
}
