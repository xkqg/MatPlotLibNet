// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="Text3DSeries"/> as depth-sorted text labels projected from 3D to 2D.</summary>
internal sealed class Text3DSeriesRenderer : SeriesRenderer<Text3DSeries>
{
    /// <inheritdoc />
    public Text3DSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(Text3DSeries series)
    {
        if (series.Annotations.Count == 0) return;

        var bounds = Area.PlotBounds;
        var color = ResolveColor(series.Color);

        double xMin = double.MaxValue, xMax = double.MinValue;
        double yMin = double.MaxValue, yMax = double.MinValue;
        double zMin = double.MaxValue, zMax = double.MinValue;

        foreach (var a in series.Annotations)
        {
            if (a.X < xMin) xMin = a.X; if (a.X > xMax) xMax = a.X;
            if (a.Y < yMin) yMin = a.Y; if (a.Y > yMax) yMax = a.Y;
            if (a.Z < zMin) zMin = a.Z; if (a.Z > zMax) zMax = a.Z;
        }

        var proj = Context.Projection3D
            ?? new Projection3D(30, -60, bounds, xMin, xMax, yMin, yMax, zMin, zMax);

        // Build indexed list for depth sorting
        var items = new List<(double Depth, Point Pt, string Text)>(series.Annotations.Count);
        foreach (var a in series.Annotations)
        {
            var pt = proj.Project(a.X, a.Y, a.Z);
            double depth = proj.Depth(a.X, a.Y, a.Z);
            items.Add((depth, pt, a.Text));
        }

        // Sort back-to-front (painter's algorithm)
        items.Sort((a, b) => a.Depth.CompareTo(b.Depth));

        var font = new Font { Size = series.FontSize, Color = color };

        foreach (var (_, pt, text) in items)
            Ctx.DrawText(text, pt, font, TextAlignment.Center);
    }
}
