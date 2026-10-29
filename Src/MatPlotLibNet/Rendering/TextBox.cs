// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>The four corners of a <see cref="TextBox"/> after its turn, named for where each stands in the text's own
/// frame (the top is the side the letters rise towards).</summary>
internal readonly record struct TextBoxCorners(Point TopLeft, Point TopRight, Point BottomRight, Point BottomLeft);

/// <summary>The box a drawn text occupies, by one rule wherever one is needed: where an axis reserves room for a turned
/// tick label, where an annotation's background stands and where a legend or a moved annotation avoids it. The box runs
/// along the baseline from where the alignment starts it by the measured width, and from the font size above the
/// baseline (the ascent) to the rest of the measured height below it (the descent); the whole box is turned
/// counter-clockwise about the point the text is drawn at, the point the renderers rotate about.</summary>
/// <param name="Anchor">The point the text is placed at, before its vertical alignment moves the baseline.</param>
/// <param name="Size">The text's measured size, upright.</param>
/// <param name="FontSize">The font size in pixels: the ascent above the baseline.</param>
/// <param name="Alignment">How the text starts along its baseline.</param>
/// <param name="Vertical">How the text stands against the anchor, vertically.</param>
/// <param name="Rotation">The turn in degrees, counter-clockwise.</param>
internal readonly record struct TextBox(
    Point Anchor, Size Size, double FontSize, TextAlignment Alignment, TextVerticalAlignment Vertical, double Rotation)
{
    /// <summary>How far below the middle of the letters their baseline stands, as a fraction of the font size: a text
    /// centred on a point has the point at that height above its baseline.</summary>
    internal const double CentreDrop = 0.35;

    /// <summary>How far the baseline stands below the anchor: nothing on the baseline, a third of the size below a
    /// centre, the ascent below a top and the descent above a bottom.</summary>
    private double BaselineShift => Vertical switch
    {
        TextVerticalAlignment.Center => CentreDrop * FontSize,
        TextVerticalAlignment.Top    => FontSize,
        TextVerticalAlignment.Bottom => FontSize - Size.Height,
        _                            => 0,
    };

    /// <summary>The point the text is drawn at, and the point its box turns about.</summary>
    internal Point Baseline => new(Anchor.X, Anchor.Y + BaselineShift);

    /// <summary>The box's four corners once turned, in the frame of the picture (y downward).</summary>
    internal TextBoxCorners Corners()
    {
        double radians = Rotation * Math.PI / 180;
        double sin = Math.Sin(radians), cos = Math.Cos(radians);
        double left = Alignment.StartOffset(Size.Width), right = left + Size.Width;
        double top = -FontSize, bottom = Size.Height - FontSize;
        var baseline = Baseline;
        Point Turn(double x, double y) => new(baseline.X + x * cos + y * sin, baseline.Y - x * sin + y * cos);
        return new TextBoxCorners(Turn(left, top), Turn(right, top), Turn(right, bottom), Turn(left, bottom));
    }

    /// <summary>The smallest upright rectangle that holds the turned box.</summary>
    internal Rect Bounds()
    {
        var c = Corners();
        double minX = Math.Min(Math.Min(c.TopLeft.X, c.TopRight.X), Math.Min(c.BottomRight.X, c.BottomLeft.X));
        double maxX = Math.Max(Math.Max(c.TopLeft.X, c.TopRight.X), Math.Max(c.BottomRight.X, c.BottomLeft.X));
        double minY = Math.Min(Math.Min(c.TopLeft.Y, c.TopRight.Y), Math.Min(c.BottomRight.Y, c.BottomLeft.Y));
        double maxY = Math.Max(Math.Max(c.TopLeft.Y, c.TopRight.Y), Math.Max(c.BottomRight.Y, c.BottomLeft.Y));
        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }
}

/// <summary>What a <see cref="TextAlignment"/> says about where a text starts.</summary>
internal static class TextAlignmentExtensions
{
    extension(TextAlignment alignment)
    {
        /// <summary>Where a text of <paramref name="width"/> starts along its baseline, relative to the point it is placed
        /// at: at the point for left, half the width before it for centre, the whole width before it for right.</summary>
        /// <param name="width">The text's measured width.</param>
        /// <returns>The offset of the text's start from the point, 0 or negative.</returns>
        internal double StartOffset(double width) => alignment switch
        {
            TextAlignment.Right  => -width,
            TextAlignment.Center => -width / 2,
            _                    => 0,
        };
    }
}
