// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

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

    /// <summary>Creates a new annotation at the given data coordinates.</summary>
    public Annotation(string text, double x, double y)
    {
        Text = text;
        X = x;
        Y = y;
    }
}
