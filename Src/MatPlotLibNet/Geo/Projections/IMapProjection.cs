// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>Maps geographic coordinates (longitude/latitude in degrees) to
/// normalized screen coordinates in [0, 1]².</summary>
public interface IMapProjection
{
    /// <summary>Projects a geographic point to normalized coordinates.
    /// Returns a <see cref="NormalizedPoint"/> where Nx=0 is left, Nx=1 is right, Ny=0 is top, Ny=1 is bottom.</summary>
    NormalizedPoint Project(double lon, double lat);

    /// <summary>The valid geographic bounding box for this projection in degrees.</summary>
    GeoBounds Bounds { get; }
}
