// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Represents a text annotation at a specific data coordinate, with an optional arrow pointing to a target.</summary>
public sealed class Annotation
{
    public string Text { get; }

    public double X { get; }

    public double Y { get; }

    public double? ArrowTargetX { get; set; }

    public double? ArrowTargetY { get; set; }

    public Font? Font { get; set; }

    public Color? Color { get; set; }

    public Color? ArrowColor { get; set; }

    public TextAlignment Alignment { get; set; } = TextAlignment.Left;

    public double Rotation { get; set; }

    public ArrowStyle ArrowStyle { get; set; } = ArrowStyle.Simple;

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
