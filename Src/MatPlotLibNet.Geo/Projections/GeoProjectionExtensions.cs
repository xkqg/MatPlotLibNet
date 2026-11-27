// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

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

        /// <summary>Where <paramref name="place"/> stands on the map. A place a projection cannot place (the far side of a globe) is
        /// off its domain (<see cref="ProjectedPointExtensions"/>' <c>IsOffDomain</c>) and has no point.</summary>
        /// <param name="place">The place on the globe.</param>
        /// <returns>The projected point, or <see langword="null"/> when the projection cannot place the place.</returns>
        internal ProjectedPoint? Place(GeoCoordinate place)
        {
            var at = projection.Forward(place.Latitude, place.Longitude);
            return at.IsOffDomain ? null : at;
        }

        /// <summary>The projection as a message names it: <c>"Orthographic(lat_0=10, lon_0=20)"</c>.</summary>
        internal string Described()
        {
            var parameters = projection.ParametersOf();
            return parameters.Count == 0
                ? projection.Name
                : $"{projection.Name}({string.Join(", ", parameters.Select(pair => $"{pair.Key}={pair.Value.InvariantText("R")}"))})";
        }
    }

    /// <summary>The projection of the given name made with the given parameters: a parameter that is left out takes the
    /// projection's own default.</summary>
    /// <param name="name">The name (<see cref="IGeoProjection.Name"/>).</param>
    /// <param name="parameters">The parameters by their PROJ names.</param>
    /// <returns>The projection; <see langword="null"/> when the name is none of the 13 this package ships, or a parameter is one the
    /// projection does not take (a misspelt name would otherwise be taken as no number and draw the default without a word).</returns>
    internal static IGeoProjection? Make(string name, IReadOnlyDictionary<string, double> parameters) =>
        !Kinds.TryGetValue(name, out var kind) || !parameters.Keys.All(kind.ParametersOf().ContainsKey) ? null
            : kind is IProjectionParameters parametric ? parametric.Made(parameters)
            : kind;

    /// <summary>The projections a map's JSON may name, in words: each by its name, followed in brackets by the PROJ names of the
    /// numbers it takes when it takes any (<c>Orthographic (lat_0, lon_0)</c>).</summary>
    internal static string KindsDescribed { get; } = string.Join(", ", Kinds.Values.Select(kind =>
        kind.ParametersOf().Count == 0 ? kind.Name : $"{kind.Name} ({string.Join(", ", kind.ParametersOf().Keys)})"));
}
