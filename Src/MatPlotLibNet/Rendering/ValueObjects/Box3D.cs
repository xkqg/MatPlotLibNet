// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering;

/// <summary>
/// An axis-aligned 3-D bounding box composed of three <see cref="Range1D"/> extents.
/// The 3-D analogue of <see cref="Rect"/> — used when a series needs to report its full
/// <c>(X, Y, Z)</c> footprint as a single structured value rather than six loose locals.
/// </summary>
/// <param name="X">Extent along the X axis.</param>
/// <param name="Y">Extent along the Y axis.</param>
/// <param name="Z">Extent along the Z axis.</param>
public readonly record struct Box3D(Range1D X, Range1D Y, Range1D Z)
{
    /// <summary>Convenience ctor from flat min/max scalars.</summary>
    public Box3D(double xMin, double xMax, double yMin, double yMax, double zMin, double zMax)
        : this(new Range1D(xMin, xMax), new Range1D(yMin, yMax), new Range1D(zMin, zMax)) { }

    /// <summary>Returns a new <see cref="Box3D"/> with <see cref="X"/> replaced.</summary>
    public Box3D WithX(Range1D x) => this with { X = x };

    /// <summary>Returns a new <see cref="Box3D"/> with <see cref="Y"/> replaced.</summary>
    public Box3D WithY(Range1D y) => this with { Y = y };

    /// <summary>Returns a new <see cref="Box3D"/> with <see cref="Z"/> replaced.</summary>
    public Box3D WithZ(Range1D z) => this with { Z = z };

    /// <summary>
    /// Converts this box into a <see cref="DataRangeContribution"/>, optionally attaching
    /// sticky-edge constraints. Lets 3-D series phrase their range computation as
    /// <c>new Box3D(…).ToContribution(stickyZMin: 0)</c> — one structured call instead of
    /// six positional scalars plus named sticky args.
    /// </summary>
    public DataRangeContribution ToContribution(
        double? stickyXMin = null, double? stickyXMax = null,
        double? stickyYMin = null, double? stickyYMax = null,
        double? stickyZMin = null, double? stickyZMax = null) => new(
            X.Lo, X.Hi, Y.Lo, Y.Hi,
            stickyXMin, stickyXMax,
            stickyYMin, stickyYMax,
            stickyZMin, stickyZMax,
            Z.Lo, Z.Hi);
}
