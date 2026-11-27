// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Serialization;

/// <summary>The figure JSON's form of a theme's <see cref="BoxLook"/>, the one key the look writes and reads itself
/// (<see cref="BoxLook.WriteKeys"/>, <see cref="BoxLook.ReadKeys"/>): a look is written whole or not at all, so every part is written.</summary>
internal sealed record BoxLookDto
{
    /// <summary>The colour of the box.</summary>
    public Color Box { get; init; }
    /// <summary>The colour of the median line.</summary>
    public Color Median { get; init; }
    /// <summary>The colour of the whiskers.</summary>
    public Color Whisker { get; init; }
    /// <summary>The line style of the whiskers.</summary>
    public LineStyle WhiskerStyle { get; init; }
    /// <summary>The colour of the caps.</summary>
    public Color Cap { get; init; }
    /// <summary>The width of a cap as a share of the box's width.</summary>
    public double CapShare { get; init; }
    /// <summary>The width of the box's lines, in pixels.</summary>
    public double LineWidth { get; init; }
    /// <summary>The marker of a flier.</summary>
    public MarkerStyle FlierMarker { get; init; }
    /// <summary>The face colour of a flier; <see langword="null"/> for none.</summary>
    public Color? FlierFace { get; init; }
    /// <summary>The edge colour of a flier.</summary>
    public Color FlierEdge { get; init; }
    /// <summary>The marker of the mean.</summary>
    public MarkerStyle MeanMarker { get; init; }
    /// <summary>The face colour of the mean marker.</summary>
    public Color MeanFace { get; init; }
    /// <summary>The edge colour of the mean marker.</summary>
    public Color MeanEdge { get; init; }
    /// <summary>The size of the fliers and of the mean marker, in points across.</summary>
    public double MarkerSize { get; init; }
}
