// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Models.Patches;

/// <summary>A polygon through vertices in data coordinates, matplotlib's <c>Polygon</c>: closed back to its first vertex by
/// default, or an open line through them.</summary>
/// <remarks>A polygon of fewer than three vertices, or with a vertex that is not a finite number, has no outline and draws
/// nothing. The polygon keeps its own copy of the vertices, so changing the list it was given afterwards changes nothing.</remarks>
/// <example>
/// <code>
/// ax.AddPatch(new Polygon([new(0, 0), new(2, 0), new(1, 1.5)]) { FaceColor = Colors.Tab10Green });
/// </code>
/// </example>
public sealed class Polygon : FilledPatch
{
    private DataPoint[] _vertices;

    /// <summary>Creates a closed polygon through <paramref name="vertices"/>.</summary>
    /// <param name="vertices">The vertices in order, in data units.</param>
    /// <exception cref="ArgumentNullException"><paramref name="vertices"/> is null.</exception>
    public Polygon(IReadOnlyList<DataPoint> vertices)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        _vertices = [.. vertices];
    }

    /// <summary>The vertices in order, in data units (matplotlib's <c>xy</c>). Setting it keeps a copy of the list given.</summary>
    /// <exception cref="ArgumentNullException">The value set is null.</exception>
    public IReadOnlyList<DataPoint> Vertices
    {
        get => _vertices;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _vertices = [.. value];
        }
    }

    /// <summary>Whether the outline closes back from the last vertex to the first. <see langword="true"/> by default, as in
    /// matplotlib; <see langword="false"/> draws the line through the vertices and leaves it open.</summary>
    public bool Closed { get; set; } = true;

    /// <inheritdoc />
    internal override IReadOnlyList<PathSegment> Outline() => _vertices.Length < 3 ? [] : OutlineThrough(_vertices, Closed);

    /// <inheritdoc />
    internal override PatchDto Describe() => new PolygonDto(this);
}
