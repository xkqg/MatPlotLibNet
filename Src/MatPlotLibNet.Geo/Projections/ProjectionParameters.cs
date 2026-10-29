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

/// <summary>What the package knows of a projection beyond <see cref="IGeoProjection"/>: its parameters, whether another is the
/// same projection, how it reads in a message, and the projection a name and its parameters make again.</summary>
internal static class GeoProjectionExtensions
{
    private static readonly IReadOnlyDictionary<string, double> NoParameters = new Dictionary<string, double>();

    /// <summary>The 13 projections of the package by their names, each as it is made with no numbers: the table a name is looked
    /// up in, and the projection a parametric kind is made again from.</summary>
    private static readonly Dictionary<string, IGeoProjection> Kinds = KindsOf(
        new PlateCarree(), new Mercator(), new Robinson(), new Mollweide(), new Sinusoidal(), new NaturalEarthProjection(),
        new EqualEarth(), new Orthographic(), new LambertConformal(), new AlbersEqualArea(), new AzimuthalEquidistant(),
        new Stereographic(), new TransverseMercator());

    private static Dictionary<string, IGeoProjection> KindsOf(params IGeoProjection[] projections)
    {
        var kinds = new Dictionary<string, IGeoProjection>();
        foreach (var projection in projections) kinds[projection.Name] = projection;
        return kinds;
    }

    extension(IGeoProjection projection)
    {
        /// <summary>The parameters the projection was made with; none for one that takes none, and for one this package does not
        /// know.</summary>
        internal IReadOnlyDictionary<string, double> ParametersOf() =>
            projection is IProjectionParameters described ? described.Parameters : NoParameters;

        /// <summary>Whether <paramref name="other"/> is the same projection: the same kind with the same parameters. A projection
        /// this package does not know is the same as itself and as nothing else.</summary>
        internal bool IsSameProjectionAs(IGeoProjection other) =>
            ReferenceEquals(projection, other)
            || (projection.GetType() == other.GetType()
                && projection.GetType().Assembly == typeof(IGeoProjection).Assembly
                && projection.ParametersOf().Count == other.ParametersOf().Count
                && projection.ParametersOf().All(pair => other.ParametersOf()[pair.Key] == pair.Value));

        /// <summary>The projection as a message names it: <c>Orthographic(lat_0=10, lon_0=20)</c>.</summary>
        internal string Described()
        {
            var parameters = projection.ParametersOf();
            return parameters.Count == 0
                ? projection.Name
                : $"{projection.Name}({string.Join(", ", parameters.Select(pair => $"{pair.Key}={pair.Value.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}"))})";
        }
    }

    /// <summary>The projection of the given name made with the given parameters: a parameter that is left out takes the
    /// projection's own default.</summary>
    /// <param name="name">The name (<see cref="IGeoProjection.Name"/>).</param>
    /// <param name="parameters">The parameters by their PROJ names.</param>
    /// <returns>The projection; <see langword="null"/> when the name is none of the 13 this package ships.</returns>
    internal static IGeoProjection? Make(string name, IReadOnlyDictionary<string, double> parameters) =>
        !Kinds.TryGetValue(name, out var kind) ? null
            : kind is IProjectionParameters parametric ? parametric.Made(parameters)
            : kind;
}
