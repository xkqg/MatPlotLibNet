// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Represents a text annotation at a specific data coordinate, with an optional arrow pointing to a target.</summary>
public sealed class Annotation
{
    /// <summary>Gets the annotation text.</summary>
    public string Text { get; }

    /// <summary>Gets the X data coordinate for the annotation text.</summary>
    public double X { get; }

    /// <summary>Gets the Y data coordinate for the annotation text.</summary>
    public double Y { get; }

    /// <summary>Gets or sets the optional X data coordinate for the arrow target.</summary>
    public double? ArrowTargetX { get; set; }

    /// <summary>Gets or sets the optional Y data coordinate for the arrow target.</summary>
    public double? ArrowTargetY { get; set; }

    /// <summary>Gets or sets the font for the annotation text.</summary>
    public Font? Font { get; set; }

    /// <summary>Gets or sets the text color.</summary>
    public Color? Color { get; set; }

    /// <summary>Gets or sets the arrow color.</summary>
    public Color? ArrowColor { get; set; }

    /// <summary>Gets or sets the horizontal alignment of the annotation text. Default is <see cref="TextAlignment.Left"/>.</summary>
    public TextAlignment Alignment { get; set; } = TextAlignment.Left;

    /// <summary>Gets or sets the rotation angle of the annotation text in degrees. Default is 0 (horizontal).</summary>
    public double Rotation { get; set; }

    /// <summary>Gets or sets the visual style of the arrow. Default is <see cref="ArrowStyle.Simple"/>.</summary>
    public ArrowStyle ArrowStyle { get; set; } = ArrowStyle.Simple;

    /// <summary>Gets or sets an optional background fill color drawn behind the annotation text.</summary>
    public Color? BackgroundColor { get; set; }

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
