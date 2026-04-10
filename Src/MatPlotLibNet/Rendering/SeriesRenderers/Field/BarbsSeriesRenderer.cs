// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="BarbsSeries"/> using meteorological wind barb notation.</summary>
internal sealed class BarbsSeriesRenderer : SeriesRenderer<BarbsSeries>
{
    public BarbsSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(BarbsSeries series)
    {
        if (series.X.Length == 0) return;

        var color = ResolveColor(series.Color);
        double staffLen = series.BarbLength;

        for (int i = 0; i < series.X.Length; i++)
        {
            double speed = i < series.Speed.Length ? series.Speed[i] : 0;
            double dir = i < series.Direction.Length ? series.Direction[i] : 0;

            // Convert meteorological direction to math angle: 0°N clockwise → radians from East CCW
            double radians = -(dir - 90) * Math.PI / 180.0;
            double dx = Math.Cos(radians), dy = Math.Sin(radians);

            var origin = Transform.DataToPixel(series.X[i], series.Y[i]);
            var tip = new Point(origin.X + dx * staffLen, origin.Y - dy * staffLen);

            // Draw staff
            Ctx.DrawLine(origin, tip, color, 1.5, Styling.LineStyle.Solid);

            // Draw barbs along the staff from tip toward origin
            double remaining = speed;
            double offset = 0;
            double barbSpacing = staffLen / 5.0;

            // Flags (50 kt triangles)
            while (remaining >= 50)
            {
                DrawFlag(origin, dx, dy, staffLen, offset, barbSpacing, color);
                remaining -= 50;
                offset += barbSpacing * 1.5;
            }
            // Full barbs (10 kt lines)
            while (remaining >= 10)
            {
                DrawBarb(origin, dx, dy, staffLen, offset, staffLen * 0.35, color);
                remaining -= 10;
                offset += barbSpacing;
            }
            // Half barb (5 kt)
            if (remaining >= 5)
                DrawBarb(origin, dx, dy, staffLen, offset, staffLen * 0.2, color);
        }
    }

    private void DrawBarb(Point origin, double dx, double dy, double staffLen, double offset,
        double barbLen, Styling.Color color)
    {
        double bx = origin.X + dx * (staffLen - offset);
        double by = origin.Y - dy * (staffLen - offset);
        // Perpendicular direction
        var barbTip = new Point(bx + dy * barbLen, by + dx * barbLen);
        Ctx.DrawLine(new Point(bx, by), barbTip, color, 1.5, Styling.LineStyle.Solid);
    }

    private void DrawFlag(Point origin, double dx, double dy, double staffLen, double offset,
        double size, Styling.Color color)
    {
        double bx = origin.X + dx * (staffLen - offset);
        double by = origin.Y - dy * (staffLen - offset);
        double bx2 = origin.X + dx * (staffLen - offset - size);
        double by2 = origin.Y - dy * (staffLen - offset - size);
        var flagTip = new Point(bx + dy * size, by + dx * size);
        Ctx.DrawPolygon([new(bx, by), new(bx2, by2), flagTip], color, null, 0);
    }
}
