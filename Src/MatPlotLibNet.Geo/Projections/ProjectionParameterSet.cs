// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>The numbers a parametric projection is made with, by their PROJ names (<c>lat_0</c> the latitude of origin or of the
/// centre, <c>lon_0</c> the central meridian, <c>lat_1</c> and <c>lat_2</c> the standard parallels), in the order the projection names
/// them, which is the order its map's JSON writes them in. The one place the names are spelled: a projection takes its numbers from
/// here by name and is made again with others through <see cref="With"/>.</summary>
internal readonly struct ProjectionParameterSet
{
    private const string Lat0Name = "lat_0", Lon0Name = "lon_0", Lat1Name = "lat_1", Lat2Name = "lat_2";

    private readonly Dictionary<string, double> _numbers;

    private ProjectionParameterSet(Dictionary<string, double> numbers) => _numbers = numbers;

    /// <summary>The numbers of a projection centred on a place (an orthographic, an azimuthal equidistant, a stereographic map):
    /// <c>lat_0</c>, <c>lon_0</c>.</summary>
    /// <param name="latitude">The latitude of the centre, in degrees.</param>
    /// <param name="longitude">The longitude of the centre, in degrees.</param>
    internal static ProjectionParameterSet Centred(double latitude, double longitude) =>
        new(new() { [Lat0Name] = latitude, [Lon0Name] = longitude });

    /// <summary>The numbers of a conic projection (Albers, Lambert): <c>lat_1</c>, <c>lat_2</c>, <c>lon_0</c>, <c>lat_0</c>.</summary>
    /// <param name="firstParallel">The first standard parallel, in degrees.</param>
    /// <param name="secondParallel">The second standard parallel, in degrees.</param>
    /// <param name="centralMeridian">The central meridian, in degrees.</param>
    /// <param name="origin">The latitude of origin, in degrees.</param>
    internal static ProjectionParameterSet Conic(double firstParallel, double secondParallel, double centralMeridian, double origin) =>
        new(new() { [Lat1Name] = firstParallel, [Lat2Name] = secondParallel, [Lon0Name] = centralMeridian, [Lat0Name] = origin });

    /// <summary>The numbers of a projection turned about a central meridian (a transverse Mercator map): <c>lon_0</c>.</summary>
    /// <param name="centralMeridian">The central meridian, in degrees.</param>
    internal static ProjectionParameterSet Meridian(double centralMeridian) => new(new() { [Lon0Name] = centralMeridian });

    /// <summary>The numbers by their PROJ names, in the order the projection names them.</summary>
    internal IReadOnlyDictionary<string, double> Numbers => _numbers;

    /// <summary>The latitude of origin or of the centre (<c>lat_0</c>), in degrees.</summary>
    internal double Lat0 => _numbers[Lat0Name];

    /// <summary>The central meridian (<c>lon_0</c>), in degrees.</summary>
    internal double Lon0 => _numbers[Lon0Name];

    /// <summary>The first standard parallel (<c>lat_1</c>), in degrees.</summary>
    internal double Lat1 => _numbers[Lat1Name];

    /// <summary>The second standard parallel (<c>lat_2</c>), in degrees.</summary>
    internal double Lat2 => _numbers[Lat2Name];

    /// <summary>These numbers with each one <paramref name="given"/> names in its place: a number left out keeps the value it has, and
    /// the order stays the projection's.</summary>
    /// <param name="given">The numbers to take, by their PROJ names.</param>
    /// <returns>The numbers to make the projection again with.</returns>
    internal ProjectionParameterSet With(IReadOnlyDictionary<string, double> given)
    {
        var numbers = new Dictionary<string, double>(_numbers.Count);
        foreach (var (name, value) in _numbers) numbers[name] = given.GetValueOrDefault(name, value);
        return new(numbers);
    }
}
