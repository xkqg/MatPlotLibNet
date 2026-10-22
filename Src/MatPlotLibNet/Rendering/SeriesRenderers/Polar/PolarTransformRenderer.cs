// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Abstract base for polar series renderers that share rMax computation and
/// <see cref="PolarTransform"/> setup.</summary>
/// <typeparam name="TSeries">The concrete series type rendered by the subclass.</typeparam>
internal abstract class PolarTransformRenderer<TSeries> : SeriesRenderer<TSeries>
    where TSeries : ISeries
{
    /// <inheritdoc />
    protected PolarTransformRenderer(SeriesRenderContext context) : base(context) { }

    /// <summary>Creates a <see cref="PolarTransform"/> whose rMax equals the largest radius of
    /// <paramref name="r"/> that can be placed (a missing or infinite radius is left out), or 1 when there is none.</summary>
    internal static PolarTransform PrepareTransform(double[] r, Rect bounds) =>
        new(bounds, r.DrawableRange()?.Hi ?? 1);
}
