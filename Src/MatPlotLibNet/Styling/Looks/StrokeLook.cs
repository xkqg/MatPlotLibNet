// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling;

/// <summary>How a stroke is drawn where its author names nothing: its dashes and the join of its corners. A backend reads it from the surface the drawing sequence
/// told the theme to (<see cref="Rendering.IThemedRenderContext"/>).</summary>
internal sealed record StrokeLook : IThemeKeys<StrokeLook>
{
    /// <summary>The library's own strokes.</summary>
    internal static StrokeLook Library { get; } = new();

    /// <inheritdoc />
    public void WriteKeys(ThemeDto keys)
    {
        Dashes.WriteKeys(keys);
        keys.StrokeJoin = Join.OrNullAtRest(Library.Join);
    }

    /// <inheritdoc />
    public static StrokeLook ReadKeys(ThemeDto keys) => new() { Dashes = DashLook.ReadKeys(keys), Join = keys.StrokeJoin ?? Library.Join };

    /// <summary>The lengths a broken line is dashed at.</summary>
    public DashLook Dashes { get; init; } = DashLook.Library;

    /// <summary>How the segments of a line, and of the edge of what stands for a matplotlib collection (a barb, a scatter marker, the edge of
    /// a filled band), meet at a corner: matplotlib's <c>lines.solid_joinstyle</c>, <c>lines.dash_joinstyle</c> and a collection's default,
    /// 'round' under both its styles. A patch (a bar, a box, a wedge) is 'miter' in matplotlib and keeps the mitre. The library's own look
    /// mitres.</summary>
    public JoinStyle Join { get; init; }

    /// <summary>The join a stroke is drawn with: the one it names, else this look's. The one place a line, whatever draws it, takes its
    /// corner from the theme: the stroke owner of each backend asks it, so a line kind needs no code of its own to be joined as the theme
    /// says.</summary>
    /// <param name="stroke">The stroke of a line.</param>
    /// <returns>The join of its corners.</returns>
    internal JoinStyle JoinOf(StrokeStyle stroke) => stroke.Join ?? Join;

    /// <summary>The style of a shape joined at its corners as <paramref name="artist"/> is to matplotlib: one that stands for a collection
    /// (<see cref="Models.Series.MatplotlibArtist.Collection"/>: a scatter's markers, a filled band, a mesh, a 3-D face) stroked with the join
    /// of this look, a patch left with the mitre it has (<see cref="ShapeStyle.StrokeJoin"/>). The one place the join of a shape is read
    /// from what its artist is; the artist is the one place a kind says what it is.</summary>
    /// <param name="artist">What the series the shape belongs to is to matplotlib.</param>
    /// <param name="shape">The style of the shape.</param>
    /// <returns>The style, its stroke joined as the artist's is.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ShapeStyle JoinedAs(in Models.Series.MatplotlibArtist artist, ShapeStyle shape) =>
        artist.Collection ? shape with { StrokeJoin = ShapeJoinOf(artist) } : shape;

    /// <summary>The join <see cref="JoinedAs"/> gives a fresh shape of <paramref name="artist"/>: this look's for a collection, the mitre a
    /// patch has. For a renderer that makes every face of a series in one loop, asked once and laid on each face as it is made
    /// (<see cref="ShapeStyle.Edged(Color?, Color?, double, JoinStyle)"/>).</summary>
    /// <param name="artist">What the series is to matplotlib.</param>
    /// <returns>The join of its shapes under this look.</returns>
    internal JoinStyle ShapeJoinOf(in Models.Series.MatplotlibArtist artist) => artist.Collection ? Join : JoinStyle.Miter;
}
