// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Where the label of a contour line stands and how it is turned, as matplotlib's <c>clabel</c> does it: at the
/// middle of the line by arc length, turned to the chord across the label's width there, and folded so it never reads upside
/// down. Worked out in pixels, so an axis drawn upside down or a scale that bends the line turns the label as the line stands
/// on the screen, and the same line traced the other way gets the same label.</summary>
/// <param name="Centre">The point on the line the label is centred on, in pixels.</param>
/// <param name="Rotation">The turn of the label in degrees, counter-clockwise, in [-90, 90).</param>
internal readonly record struct ContourLabelPlacement(Point Centre, double Rotation)
{
    /// <summary>How many label widths long a line must be to carry its label: matplotlib's 1.2.</summary>
    internal const double MinimumLengthRatio = 1.2;

    /// <summary>The room left on each side of the label's text along the line, in pixels, which the box behind it covers:
    /// matplotlib's <c>inline_spacing</c>, 5.</summary>
    internal const double InlineSpacing = 5;

    /// <summary>The label of a line, or none when the line is shorter than <see cref="MinimumLengthRatio"/> times the
    /// label's width or has no length at all.</summary>
    /// <param name="line">The line's points in pixels, in the order it was traced; two or more.</param>
    /// <param name="labelWidth">The measured width of the label's text, in pixels.</param>
    /// <returns>Where the label stands and how it is turned; <see langword="null"/> when the line cannot carry it.</returns>
    internal static ContourLabelPlacement? Along(IReadOnlyList<Point> line, double labelWidth)
    {
        var at = new double[line.Count];
        for (int i = 1; i < line.Count; i++)
        {
            double dx = line[i].X - line[i - 1].X, dy = line[i].Y - line[i - 1].Y;
            at[i] = at[i - 1] + Math.Sqrt(dx * dx + dy * dy);
        }

        double length = at[^1];
        if (length == 0 || length < MinimumLengthRatio * labelWidth) return null;

        // Each end of the chord lies at least a tenth of the label's width inside the line (the line is 1.2 widths or
        // longer), so both stand on it and the segment each falls in has a length.
        double middle = length / 2;
        var from = PointAt(line, at, middle - labelWidth / 2);
        var to = PointAt(line, at, middle + labelWidth / 2);
        double angle = Math.Atan2(-(to.Y - from.Y), to.X - from.X) * 180 / Math.PI;
        return new ContourLabelPlacement(PointAt(line, at, middle), Upright(angle));
    }

    /// <summary>A turn folded into [-90, 90), so a text turned by it never reads upside down: matplotlib's
    /// <c>(angle + 90) % 180 - 90</c>.</summary>
    /// <param name="degrees">The turn in degrees, counter-clockwise.</param>
    /// <returns>The same direction, read left to right, in [-90, 90).</returns>
    internal static double Upright(double degrees)
    {
        double turned = (degrees + 90) % 180;
        if (turned < 0) turned += 180;
        return turned - 90;
    }

    /// <summary>The point <paramref name="distance"/> pixels along the line from its start.</summary>
    private static Point PointAt(IReadOnlyList<Point> line, double[] at, double distance)
    {
        int i = 1;
        while (i < line.Count - 1 && at[i] < distance) i++;
        double t = (distance - at[i - 1]) / (at[i] - at[i - 1]);
        var a = line[i - 1];
        var b = line[i];
        return new Point(a.X + t * (b.X - a.X), a.Y + t * (b.Y - a.Y));
    }
}
