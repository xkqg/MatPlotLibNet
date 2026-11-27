// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>
/// Bundles the stroke properties — colour, thickness, and dash style — that travel together
/// through every line-drawing operation on <c>IRenderContext</c>.
/// </summary>
/// <param name="Color">The stroke colour.</param>
/// <param name="Thickness">The stroke width in pixels.</param>
/// <param name="Style">The dash pattern to apply.</param>
public readonly record struct StrokeStyle(Color Color, double Thickness, LineStyle Style)
{
    /// <summary>A continuous line: <paramref name="color"/> at <paramref name="thickness"/>, with no dash. The one spelling of a solid
    /// line, so a line does not restate its style. A join is named with a <c>with</c> expression, as it is on any stroke.</summary>
    /// <param name="color">The colour of the line.</param>
    /// <param name="thickness">The width of the line, in pixels.</param>
    /// <returns>The solid stroke.</returns>
    public static StrokeStyle Solid(Color color, double thickness) => Of(color, thickness, LineStyle.Solid);

    /// <summary>The one place a stroke is put together: <paramref name="color"/> at <paramref name="thickness"/> in <paramref name="style"/>,
    /// joined as <paramref name="join"/> says. Every source of a line hands its values here (a tool, a spine, a grid, a shape's outline, a
    /// series' slot, a box's look), so no site assembles a stroke of its own.</summary>
    /// <param name="color">The colour of the line.</param>
    /// <param name="thickness">The width of the line, in pixels.</param>
    /// <param name="style">The dash pattern of the line.</param>
    /// <param name="join">How two segments meet at a corner; <see langword="null"/> (the default) leaves it to the theme the line is drawn under.</param>
    /// <returns>The stroke.</returns>
    internal static StrokeStyle Of(Color color, double thickness, LineStyle style, JoinStyle? join = null) =>
        new StrokeStyle(color, thickness, style) { Join = join };

    /// <summary>This stroke at a share of its opacity and of its width, its dash pattern and join kept: the look a minor grid line has
    /// next to a major one (<c>Faded(0.5)</c>). The opacity is scaled as any colour's own opacity is, to the nearest step.</summary>
    /// <param name="factor">The share of the colour's opacity and of the width that is kept, from 0 to 1.</param>
    /// <returns>The faded stroke.</returns>
    public StrokeStyle Faded(double factor) => this with { Color = Color.ScaleAlpha(factor), Thickness = Thickness * factor };

    /// <summary>How two segments of the stroke meet at a corner, or <see langword="null"/> (the default) for a stroke that names none: the
    /// surface then draws it with the join of the theme it draws under, round under the matplotlib themes
    /// where matplotlib joins a line and a collection so, the mitre under a library theme. A join named here wins over the theme's: a
    /// patch's outline asks for <see cref="JoinStyle.Miter"/>. An <c>init</c> property for the reason <see cref="ShapeStyle.Hatch"/> is
    /// one: a positional addition would break every construction site of this record struct.</summary>
    public JoinStyle? Join { get; init; }

    /// <summary>
    /// Gets a value indicating whether this stroke produces visible output: it has a positive
    /// thickness and a drawable dash style (i.e. not <see cref="LineStyle.None"/>).
    /// </summary>
    public bool IsVisible => Thickness > 0 && Style != LineStyle.None;

    /// <summary>Whether the stroke has no dash: a solid line, which a surface may draw as the outline of its shape (a ring as a circle, a
    /// frame as four bars) where a dashed one has to be stroked as a path. The one place a stroke is asked whether it is solid.</summary>
    internal bool IsSolid => Style == LineStyle.Solid;
}
