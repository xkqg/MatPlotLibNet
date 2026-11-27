// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TextMeasurement;

namespace MatPlotLibNet.Rendering;

/// <summary>The four corners of a <see cref="TextBox"/> after its turn, named for where each stands in the text's own
/// frame (the top is the side the letters rise towards).</summary>
internal readonly record struct TextBoxCorners(Point TopLeft, Point TopRight, Point BottomRight, Point BottomLeft);

/// <summary>The box a drawn text occupies, by one rule wherever one is needed: where the one verb draws a text that is centred on,
/// hung from or standing on its anchor, where an axis reserves room for a turned tick label, where an annotation's background stands
/// and where a legend or a moved annotation avoids it. The box runs along the baseline from where the alignment starts it by the
/// measured width, and from the text's ascent above the baseline to its descent below it (<see cref="TextExtent"/>); the whole box is
/// turned counter-clockwise about the point the text is drawn at, the point the renderers rotate about. How far the baseline stands
/// from the anchor is this type's alone: no site writes a drop of its own.</summary>
/// <param name="Placement">Where the text is placed: its anchor, before its vertical alignment moves the baseline, its alignment along
/// the baseline and across it, and its turn.</param>
/// <param name="Extent">The text's measured extent, upright.</param>
internal readonly record struct TextBox(TextPlacement Placement, TextExtent Extent)
{
    /// <summary>How far below the middle of the letters their baseline stands, as a fraction of the ascent: a text
    /// centred on a point has the point at that height above its baseline.</summary>
    private const double CentreDrop = 0.35;

    /// <summary>How far the baseline stands below the anchor: nothing on the baseline, a third of the ascent below a
    /// centre, the ascent below a top and the descent above a bottom.</summary>
    private double BaselineShift => Placement.Vertical switch
    {
        TextVerticalAlignment.Center => CentreDrop * Extent.Ascent,
        TextVerticalAlignment.Top    => Extent.Ascent,
        TextVerticalAlignment.Bottom => Extent.Ascent - Extent.Height,
        _                            => 0,
    };

    /// <summary>The point the text is drawn at, and the point its box turns about: the anchor moved down the picture by the vertical
    /// alignment, whatever the turn (matplotlib's <c>rotation_mode='default'</c>).</summary>
    internal Point Baseline => new(Placement.Anchor.X, Placement.Anchor.Y + BaselineShift);

    /// <summary>The point the text is drawn at when it is aligned in its own frame and then turned about its anchor (matplotlib's
    /// <c>rotation_mode='anchor'</c>, the label set along a contour line): the anchor moved along the text's own down direction.</summary>
    internal Point AnchoredBaseline => Placement.Anchor.BelowTurnedBy(BaselineShift, Placement.Rotation.ToRadians());

    /// <summary>The same text placed at <paramref name="anchor"/> instead: where a layout tries it.</summary>
    /// <param name="anchor">The new anchor.</param>
    /// <returns>The box there.</returns>
    internal TextBox At(Point anchor) => this with { Placement = Placement with { Anchor = anchor } };

    /// <summary>The box's four corners once turned, in the frame of the picture (y downward).</summary>
    /// <returns>The four corners.</returns>
    internal TextBoxCorners Corners()
    {
        double radians = Placement.Rotation.ToRadians();
        double sin = Math.Sin(radians), cos = Math.Cos(radians);
        double left = Placement.Alignment.StartOffset(Extent.Width), right = left + Extent.Width;
        double top = -Extent.Ascent, bottom = Extent.Height - Extent.Ascent;
        var baseline = Baseline;
        return new TextBoxCorners(baseline.InTurnedFrame(left, top, sin, cos), baseline.InTurnedFrame(right, top, sin, cos),
            baseline.InTurnedFrame(right, bottom, sin, cos), baseline.InTurnedFrame(left, bottom, sin, cos));
    }

    /// <summary>The smallest upright rectangle that holds the turned box.</summary>
    internal Rect Bounds()
    {
        if (Placement.Rotation == 0)
        {
            // Upright, the corners are the baseline moved by the start, the end, the ascent and the descent: the same sums the turn
            // makes with a sine of 0 and a cosine of 1, without the trigonometry.
            var at = Baseline;
            double start = Placement.Alignment.StartOffset(Extent.Width);
            double left = at.X + start, right = at.X + (start + Extent.Width);
            double top = at.Y - Extent.Ascent, bottom = at.Y + (Extent.Height - Extent.Ascent);
            double fromX = Math.Min(left, right), fromY = Math.Min(top, bottom);
            return new Rect(fromX, fromY, Math.Max(left, right) - fromX, Math.Max(top, bottom) - fromY);
        }

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
