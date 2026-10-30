// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Represents a text annotation at a specific data coordinate, with an optional arrow pointing to a target.</summary>
public sealed class Annotation
{
    /// <summary>The text the annotation shows, drawn as given at the point <see cref="X"/>/<see cref="Y"/> names.</summary>
    public string Text { get; }

    /// <summary>The horizontal position of the text's anchor point: a data value on the X axis, or a fraction of the plot
    /// width (0 at the left edge, 1 at the right) when <see cref="Coordinates"/> is <see cref="AnnotationCoordinates.AxesFraction"/>.</summary>
    public double X { get; }

    /// <summary>The vertical position of the text's anchor point: a data value on the Y axis, or a fraction of the plot
    /// height (0 at the bottom edge, 1 at the top) when <see cref="Coordinates"/> is <see cref="AnnotationCoordinates.AxesFraction"/>.</summary>
    public double Y { get; }

    /// <summary>The coordinate system <see cref="X"/>/<see cref="Y"/> are read in. <see cref="AnnotationCoordinates.Data"/>
    /// by default; <see cref="AnnotationCoordinates.AxesFraction"/> pins the text to the panel instead of the data.
    /// The arrow target, when set, is always in data coordinates — it points AT something.</summary>
    public AnnotationCoordinates Coordinates { get; set; } = AnnotationCoordinates.Data;

    /// <summary>The X data coordinate the arrow points at. The arrow is drawn only when both this and
    /// <see cref="ArrowTargetY"/> are set and <see cref="ArrowStyle"/> is not <see cref="Models.ArrowStyle.None"/>;
    /// <see langword="null"/> (the default) draws no arrow.</summary>
    public double? ArrowTargetX { get; set; }

    /// <summary>The Y data coordinate the arrow points at; see <see cref="ArrowTargetX"/>.</summary>
    public double? ArrowTargetY { get; set; }

    /// <summary>The font the text is drawn in. <see langword="null"/> (the default) takes the theme's data-text font; a
    /// font that carries no colour of its own takes <see cref="Color"/>.</summary>
    public Font? Font { get; set; }

    /// <summary>The colour of the text, and the fallback colour of the arrow and of the line that joins a moved text to its
    /// point. <see langword="null"/> (the default) takes the theme's text colour.</summary>
    public Color? Color { get; set; }

    /// <summary>The colour of the arrow (its line and head). <see langword="null"/> (the default) takes <see cref="Color"/>,
    /// and the theme's text colour when that is not set either.</summary>
    public Color? ArrowColor { get; set; }

    /// <summary>How the text stands against its point horizontally: <see cref="TextAlignment.Left"/> (the default) starts
    /// the text at the point, <see cref="TextAlignment.Center"/> centres it on the point and <see cref="TextAlignment.Right"/>
    /// ends it at the point.</summary>
    public TextAlignment Alignment { get; set; } = TextAlignment.Left;

    /// <summary>How the text stands against its point, vertically: <see cref="TextVerticalAlignment.Baseline"/> (the
    /// default) puts the baseline on the point, <see cref="TextVerticalAlignment.Center"/> centres the text on it,
    /// <see cref="TextVerticalAlignment.Top"/> hangs the text from it and <see cref="TextVerticalAlignment.Bottom"/> stands
    /// the text on it. The size comes from the theme at render time, so a centred text stays centred when the theme changes
    /// its size, which a pixel offset worked out when the chart was built cannot do.</summary>
    public TextVerticalAlignment VerticalAlignment { get; set; } = TextVerticalAlignment.Baseline;

    /// <summary>How many pixels the text stands to the right of its point (to the left when negative). The point stays
    /// where <see cref="X"/> and <see cref="Y"/> put it and an arrow still points at its target; the text, its box and the
    /// start of its arrow move.</summary>
    public double OffsetX { get; set; }

    /// <summary>How many pixels the text stands below its point (above when negative), in the picture's own direction
    /// (down is positive), like <see cref="OffsetX"/>.</summary>
    public double OffsetY { get; set; }

    /// <summary>Whether the text moves out of the way of the marks, the legend and the other annotations that ask the same:
    /// when something stands where it was put, it stands at the nearest of a fixed list of places that covers the fewest, joined
    /// to its anchor by a line (an annotation with an arrow needs no line: the arrow leaves the moved text). <see langword="false"/>
    /// by default: an annotation stands where it is put, as it always did, and as matplotlib's annotations do.</summary>
    public bool AvoidOverlap { get; set; }

    /// <summary>The angle the text is turned through, in degrees, counter-clockwise when positive. 0 (the default) leaves
    /// the text horizontal.</summary>
    public double Rotation { get; set; }

    /// <summary>The shape of the arrow's head, or <see cref="Models.ArrowStyle.None"/> for no arrow at all even when a
    /// target is set. <see cref="Models.ArrowStyle.Simple"/> by default.</summary>
    public ArrowStyle ArrowStyle { get; set; } = ArrowStyle.Simple;

    /// <summary>A plain rectangle of this colour filled behind the text. Used only while <see cref="BoxStyle"/> is
    /// <see cref="Models.BoxStyle.None"/>; a box style draws its own fill (<see cref="BoxFaceColor"/>) instead.
    /// <see langword="null"/> (the default) fills nothing.</summary>
    public Color? BackgroundColor { get; set; }

    /// <summary>The path the arrow takes from the text to its target: a straight line by default, or one of the curved and
    /// right-angled paths of <see cref="Models.ConnectionStyle"/>.</summary>
    public ConnectionStyle ConnectionStyle { get; set; } = ConnectionStyle.Straight;

    /// <summary>How strongly a curved connection bends. For <see cref="Models.ConnectionStyle.Arc3"/> it is the sideways bulge
    /// of the arc as a fraction of the distance from the text to the target; for <see cref="Models.ConnectionStyle.Angle3"/>
    /// it is the rounding of the corner as a fraction of the shorter leg (held between 0.05 and 0.5). Default 0.3; the straight
    /// and plain angled styles ignore it.</summary>
    public double ConnectionRad { get; set; } = 0.3;

    /// <summary>The size of the arrow's head, in pixels. Default 8.</summary>
    public double ArrowHeadSize { get; set; } = 8;

    /// <summary>The shape of the box drawn around the text. <see cref="Models.BoxStyle.None"/> (the default) draws no box.</summary>
    public BoxStyle BoxStyle { get; set; } = BoxStyle.None;

    /// <summary>The gap between the text and the edge of its box, in pixels. Default 4; used only when
    /// <see cref="BoxStyle"/> draws a box.</summary>
    public double BoxPadding { get; set; } = 4;

    /// <summary>The radius of the box's rounded corners, in pixels, never more than half the box's shorter side. Default 5;
    /// used by the rounded box styles only.</summary>
    public double BoxCornerRadius { get; set; } = 5;

    /// <summary>The fill colour of the box. <see langword="null"/> (the default) leaves the box unfilled.</summary>
    public Color? BoxFaceColor { get; set; }

    /// <summary>The outline colour of the box. <see langword="null"/> (the default) draws the box with no outline.</summary>
    public Color? BoxEdgeColor { get; set; }

    /// <summary>The width of the box's outline, in pixels. Default 1; it only shows when <see cref="BoxEdgeColor"/> is set.</summary>
    public double BoxLineWidth { get; set; } = 1;

    /// <summary>Creates a new annotation at the given data coordinates.</summary>
    /// <param name="text">The annotation text to display.</param>
    /// <param name="x">The X data coordinate of the annotation.</param>
    /// <param name="y">The Y data coordinate of the annotation.</param>
    public Annotation(string text, double x, double y)
    {
        Text = text;
        X = x;
        Y = y;
    }
}
