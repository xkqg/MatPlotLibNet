// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="ContourSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class ContourSeriesRenderer : SeriesRenderer<ContourSeries>
{
    /// <inheritdoc />
    public ContourSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(ContourSeries series)
    {
        int rows = series.ZData.GetLength(0);
        int cols = series.ZData.GetLength(1);
        if (rows < 2 || cols < 2) return;

        // Compute the Z range, over the values that are there: a missing value (NaN) and an infinity are left out, and
        // the cells they are a corner of are masked when the lines are traced.
        if (series.ZData.DrawableRange() is not { } z || z.Lo >= z.Hi) return;
        double zMin = z.Lo, zMax = z.Hi;

        // The levels the user named, or else matplotlib's round levels for the count (the ones a filled contour bands at); a level
        // outside the range of the values crosses no cell, so only the levels inside it are traced.
        if (series.LevelsUnder(Context.Theme) is not { } levels) return;

        // Extract iso-lines via MarchingSquares
        var contours = MarchingSquares.Extract(series.XData, series.YData, series.ZData, [.. levels.Where(level => level >= zMin && level <= zMax)]);


        // The colours run over the levels, the first to the last (ContourSeries.LineColourSpan), and a line takes its colour by the one
        // rule its colour bar's line takes it by (ContourSeries.LineColorOf).
        var span = ContourSeries.LineColourSpan(levels, z);

        // A filled contour fills the bands between the levels the lines are traced at, through the one fill a Contourf uses (so the
        // fill stands where the grid stands and its edges are the lines): from the lowest value to the highest for levels the user
        // named, and over the levels laid for the count as a Contourf does; its colours run over the same span as its lines.
        if (series.Filled)
        {
            bool named = series.LevelValues.NamesLevels;
            double[] thresholds = named ? [zMin, .. levels.Where(level => level > zMin && level < zMax), zMax] : levels;
            Context.FillContourBands(series.XData, series.YData, series.ZData,
                MarchingSquares.ExtractBands(series.XData, series.YData, series.ZData, thresholds), series.LinearMappingUnder(Context.Theme),
                Context.Theme.PatchEdgesOf(series), colourSpan: span);
        }

        double lineWidth = series.LineWidthUnder(Context.Theme);

        // Every line first, then every label: a line traced after a label would run across the label's box.
        var drawn = new List<DrawnContour>(contours.Length);
        foreach (var contour in contours)
        {
            if (contour.Points.Length < 2) continue;

            var color = series.LineColorOf(contour.Level, Context.Theme, span);

            // Convert data points to pixel points
            var pixelPoints = new List<Point>(contour.Points.Length);
            foreach (var pt in contour.Points)
                pixelPoints.Add(Transform.DataToPixel(pt.X, pt.Y));

            Ctx.DrawLines(pixelPoints, StrokeStyle.Solid(color, lineWidth));
            drawn.Add(new DrawnContour(contour.Level, pixelPoints, color));
        }

        if (series.ShowLabels)
        {
            // matplotlib cuts a contour's labels at the axes as it cuts its lines (clabel gives each label the axes' box as its clip
            // box, under every style): the labels are drawn under the clip the lines are drawn under, where a text otherwise stands whole.
            using var cut = Ctx.CuttingTexts();
            foreach (var line in drawn) DrawLabel(series, line);
        }
    }

    /// <summary>A contour line as drawn: its level, its points in pixels and its colour.</summary>
    private readonly record struct DrawnContour(double Level, List<Point> Pixels, Color Color);

    /// <summary>Draws the level of <paramref name="line"/> on it, as matplotlib's <c>clabel</c> does: at the middle of the
    /// line, turned along it (<see cref="LabelAlong"/>), in the line's colour, on a box in the plot's background
    /// that covers the line under the text and <see cref="Rendering.SeriesRenderers.ContourSeriesRenderer.InlineSpacing"/> on each side. A line too
    /// short for its label gets none.</summary>
    private void DrawLabel(ContourSeries series, DrawnContour line)
    {
        var font = Context.Theme.DataTextFont(series.TextSizeUnder(Context.Theme), line.Color);
        string text = series.LabelOf(line.Level);
        var size = Ctx.ExtentOf(TextLabel.Of(text), font);
        if (LabelAlong(line.Pixels, size.Width) is not { } label) return;

        // The text is centred on the line: its baseline stands below the centre in the text's own frame, turned with it.
        var onBaseline = new TextPlacement(label.Centre, TextAlignment.Center) { Rotation = label.Rotation };
        var baseline = new TextBox(onBaseline with { Vertical = TextVerticalAlignment.Center }, size).AnchoredBaseline;
        var box = new TextBox(onBaseline with { Anchor = baseline },
            size with { Width = size.Width + 2 * InlineSpacing }).Corners();

        Ctx.DrawPolygon([box.TopLeft, box.TopRight, box.BottomRight, box.BottomLeft], ShapeStyle.Filled(Context.Theme.AxesBackground));
        Ctx.Draw(TextLabel.Of(text), font, onBaseline with { Anchor = baseline });
    }

    /// <summary>Where the label of a contour line stands and how it is turned, as matplotlib's <c>clabel</c> does it: at the
    /// middle of the line by arc length, turned to the chord across the label's width there, and folded so it never reads upside
    /// down. Worked out in pixels, so an axis drawn upside down or a scale that bends the line turns the label as the line stands
    /// on the screen, and the same line traced the other way gets the same label.</summary>
    /// <param name="Centre">The point on the line the label is centred on, in pixels.</param>
    /// <param name="Rotation">The turn of the label in degrees, counter-clockwise, in [-90, 90).</param>
    internal readonly record struct ContourLabelPlacement(Point Centre, double Rotation);

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
    internal static ContourLabelPlacement? LabelAlong(IReadOnlyList<Point> line, double labelWidth)
    {
        var at = new double[line.Count];
        for (int i = 1; i < line.Count; i++)
        {
            at[i] = at[i - 1] + line[i - 1].DistanceTo(line[i]);
        }

        double length = at[^1];
        if (length == 0 || length < MinimumLengthRatio * labelWidth) return null;

        // Each end of the chord lies at least a tenth of the label's width inside the line (the line is 1.2 widths or
        // longer), so both stand on it and the segment each falls in has a length.
        double middle = length / 2;
        var chord = Range1D.Around(middle, labelWidth);
        var from = PointAlong(line, at, chord.Lo);
        var to = PointAlong(line, at, chord.Hi);
        double angle = Math.Atan2(-(to.Y - from.Y), to.X - from.X).ToDegrees();
        return new ContourLabelPlacement(PointAlong(line, at, middle), Upright(angle));
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
    private static Point PointAlong(IReadOnlyList<Point> line, double[] at, double distance)
    {
        int i = 1;
        while (i < line.Count - 1 && at[i] < distance) i++;
        double t = (distance - at[i - 1]) / (at[i] - at[i - 1]);
        var a = line[i - 1];
        var b = line[i];
        return new Point(a.X + t * (b.X - a.X), a.Y + t * (b.Y - a.Y));
    }
}
