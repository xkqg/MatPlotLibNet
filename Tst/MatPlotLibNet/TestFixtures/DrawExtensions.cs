// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests;

/// <summary>How a test draws what it holds: an axes into a rectangle, a figure's first plot area, the distance of two points, the
/// corners of an outline.</summary>
internal static class DrawExtensions
{
    extension(Axes axes)
    {
        /// <summary>The calls the renderer of the axes makes drawing it into <paramref name="area"/>, as the axes' own coordinate system
        /// picks the renderer.</summary>
        /// <param name="area">The rectangle the axes is drawn in.</param>
        /// <param name="theme">The theme; the library's own default when it is <see langword="null"/>.</param>
        public RecordingRenderContext Drawn(Rect area, Theme? theme = null)
        {
            var ctx = new RecordingRenderContext();
            AxesRenderer.Create(axes, area, ctx, theme ?? Theme.Default).Render();
            return ctx;
        }

        /// <summary>The SVG the renderer of the axes writes drawing it into <paramref name="area"/> under the library's own default theme.</summary>
        /// <param name="area">The rectangle the axes is drawn in.</param>
        public string RenderedSvg(Rect area)
        {
            var ctx = new SvgRenderContext();
            AxesRenderer.Create(axes, area, ctx, Theme.Default).Render();
            var sb = new StringBuilder();
            ctx.WriteTo(sb);
            return sb.ToString();
        }
    }

    extension(Figure figure)
    {
        /// <summary>The calls the chart renderer makes drawing the figure.</summary>
        public RecordingRenderContext Drawn()
        {
            var recording = new RecordingRenderContext();
            new ChartRenderer().Render(figure, recording);
            return recording;
        }
    }

    extension(Point point)
    {
        /// <summary>How far the point lies from <paramref name="other"/>, in pixels.</summary>
        /// <param name="other">The other point.</param>
        public double DistanceTo(Point other) => Math.Sqrt(((point.X - other.X) * (point.X - other.X)) + ((point.Y - other.Y) * (point.Y - other.Y)));
    }

    extension(IEnumerable<Point> outline)
    {
        /// <summary>The corners of an outline: a point that repeats the one before, or lies on the straight line between its neighbours,
        /// draws nothing and is dropped.</summary>
        public List<Point> Corners()
        {
            var points = new List<Point>();
            foreach (var p in outline)
            {
                if (points.Count == 0 || Math.Abs(points[^1].X - p.X) > 1e-9 || Math.Abs(points[^1].Y - p.Y) > 1e-9)
                {
                    points.Add(p);
                }
            }

            for (int i = points.Count - 2; i >= 1; i--)
            {
                Point a = points[i - 1], b = points[i], c = points[i + 1];
                if (Math.Abs(((b.X - a.X) * (c.Y - a.Y)) - ((b.Y - a.Y) * (c.X - a.X))) < 1e-9)
                {
                    points.RemoveAt(i);
                }
            }

            return points;
        }
    }
}
