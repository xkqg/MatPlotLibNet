// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Diagnostics;

namespace MatPlotLibNet.Rendering;

/// <summary>The top X axis as ONE render places it (<see cref="CartesianAxesRenderer.PlaceTopAxis"/>): the range it is drawn
/// over, the transform its series are drawn with, where its ticks stand, its tick values and where they come from, and why a
/// linked one draws no ticks. The part that draws it (<c>CartesianSecondaryXAxisPart</c>), the band the layout reserves for it and
/// the marks a legend keeps clear of all read this one value, so none of them works the top axis' range, transform or ticks out a
/// second way.</summary>
/// <remarks>A top axis with a range of its own is drawn over that range through its own scale, each tick at its own value. A linked
/// one (<see cref="Models.Axis.Link"/>) has no range of its own: it takes the primary's range and transform, and its ticks stand
/// through <see cref="Placement"/>, where the primary shows the value each maps back to.</remarks>
internal readonly record struct PlacedTopAxis
{
    /// <summary>The range the top axis is drawn over: its own X range beside the primary's Y range; for a linked top axis the
    /// primary's range whole.</summary>
    internal required DataRange Range { get; init; }

    /// <summary>The data-to-pixel transform the top axis' series are drawn with: through its own X scale over its own range; for a
    /// linked top axis the primary's.</summary>
    internal required DataTransform Transform { get; init; }

    /// <summary>Where the ticks stand: <see cref="Transform"/>, a tick at its own value, unless set; for a linked top axis a
    /// <see cref="LinkedTickPlacement"/>, a tick of value v where the primary shows the value v maps back to.</summary>
    internal ITickPlacement Placement
    {
        get => _placement ?? Transform;
        init => _placement = value;
    }
    private readonly ITickPlacement? _placement;

    /// <summary>The major tick values the top axis draws, in its own unit.</summary>
    internal required double[] Ticks { get; init; }

    /// <summary>Where the ticks come from and how their labels are written.</summary>
    internal required TickSource Source { get; init; }

    /// <summary>What the drawing of the top axis reports through <see cref="ChartDiagnostics"/>: why a linked top axis draws no
    /// ticks, when its link cannot place the primary's range; <see langword="null"/> otherwise. Only the drawing reports it, so a
    /// render says it once, however often the layout and the legend place the axis.</summary>
    internal ChartDiagnostic? Diagnostic { get; init; }
}
