// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Geo.Projections;

/// <summary>What the package reads of a point in the plane of a projection: whether it is off the projection's domain, the point a
/// projection answers for a place off it, and the step from one point to another. The one place the convention
/// <see cref="ProjectedPoint"/> states (a missing coordinate, <c>(NaN, NaN)</c>) is spelled: no projection builds the pair and no
/// reader tests it itself.</summary>
internal static class ProjectedPointExtensions
{
    extension(ProjectedPoint)
    {
        /// <summary>The point a projection answers for a place off its domain (the far side of a globe, the singularity of a
        /// transverse Mercator map): both coordinates missing, which every reader leaves out as a gap.</summary>
        internal static ProjectedPoint OffDomain => new(double.NaN, double.NaN);
    }

    extension(ProjectedPoint point)
    {
        /// <summary>Whether the point is off the projection's domain: a coordinate is missing (not a number).</summary>
        internal bool IsOffDomain => double.IsNaN(point.X) || double.IsNaN(point.Y);

        /// <summary>The step from this point to <paramref name="to"/> in the plane of the projection, as a vector whose length and its
        /// square are the library's (<see cref="PointExtensions"/>' <c>Length</c> and <c>SquaredLength</c>).</summary>
        /// <param name="to">The point the step runs to.</param>
        /// <returns>The step: the difference along each axis.</returns>
        internal Point StepTo(ProjectedPoint to) => new(to.X - point.X, to.Y - point.Y);
    }
}
