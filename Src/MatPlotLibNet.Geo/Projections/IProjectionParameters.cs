// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>The numbers a projection was made with, by their PROJ names (<c>lon_0</c> the central meridian, <c>lat_0</c> the
/// latitude of origin or of the centre, <c>lat_1</c> and <c>lat_2</c> the standard parallels). A projection that takes none
/// does not implement this. It is what two projections are compared by, and what a map's JSON carries so the projection can be
/// made again; internal, because it is the package's own description of its projections and not a member of
/// <see cref="IGeoProjection"/> (which another assembly implements).</summary>
internal interface IProjectionParameters
{
    /// <summary>The parameters, every one the projection takes, in degrees.</summary>
    IReadOnlyDictionary<string, double> Parameters { get; }

    /// <summary>A projection of the same kind made with <paramref name="parameters"/>, by their PROJ names: a parameter that is
    /// left out keeps the value this projection has. The projection that takes the numbers is the one that knows their names, so
    /// it is the one that is made again from them.</summary>
    /// <param name="parameters">The parameters to make the projection with.</param>
    IGeoProjection Made(IReadOnlyDictionary<string, double> parameters);
}
