// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Represents a text annotation at a specific data coordinate, with an optional arrow pointing to a target.</summary>
public sealed class Annotation
{
    public string Text { get; }

    public double X { get; }

    public double Y { get; }

    /// <summary>The coordinate system <see cref="X"/>/<see cref="Y"/> are read in. <see cref="AnnotationCoordinates.Data"/>
    /// by default; <see cref="AnnotationCoordinates.AxesFraction"/> pins the text to the panel instead of the data.
    /// The arrow target, when set, is always in data coordinates — it points AT something.</summary>
    public AnnotationCoordinates Coordinates { get; set; } = AnnotationCoordinates.Data;

    public double? ArrowTargetX { get; set; }

    public double? ArrowTargetY { get; set; }

    public Font? Font { get; set; }

    public Color? Color { get; set; }

    public Color? ArrowColor { get; set; }

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

    public double Rotation { get; set; }

    public ArrowStyle ArrowStyle { get; set; } = ArrowStyle.Simple;

    public Color? BackgroundColor { get; set; }

    public ConnectionStyle ConnectionStyle { get; set; } = ConnectionStyle.Straight;

    public double ConnectionRad { get; set; } = 0.3;

    public double ArrowHeadSize { get; set; } = 8;

    public BoxStyle BoxStyle { get; set; } = BoxStyle.None;

    public double BoxPadding { get; set; } = 4;

    public double BoxCornerRadius { get; set; } = 5;

    public Color? BoxFaceColor { get; set; }

    public Color? BoxEdgeColor { get; set; }

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
