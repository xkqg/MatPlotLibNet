// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>One line from a corner of an inset's frame to the same corner of the region it zooms, in pixels.</summary>
/// <param name="OnInset">The corner of the inset's frame.</param>
/// <param name="OnRegion">The same corner of the region, on the parent.</param>
internal readonly record struct ZoomConnector(Point OnInset, Point OnRegion);

/// <summary>The geometry of a zoom indicator (<see cref="ZoomIndicator"/>) in pixels, and its drawing: the region an inset shows,
/// drawn on its parent, and the frame of the inset. The one owner of matplotlib's connector rule, applied where the two stand on
/// the picture, after the parent's transform, so a parent axis that runs backwards, a log axis and an inset whose limits run
/// backwards need no case of their own.</summary>
/// <param name="Region">The rectangle of the region the inset shows, on the parent, in pixels.</param>
/// <param name="Inset">The inset's frame, in pixels.</param>
internal readonly record struct ZoomIndicatorPath(Rect Region, Rect Inset)
{
    /// <summary>The class of the group the indicator is drawn in.</summary>
    internal const string GroupClass = "zoom-indicator";

    /// <summary>The indicator of <paramref name="inset"/>, drawn as <paramref name="insetView"/>, on <paramref name="parent"/>,
    /// drawn as <paramref name="parentView"/>: the region spans the limits the inset drew (<see cref="DataTransform.DrawnRange"/>),
    /// placed by the parent's transform. <see langword="null"/>, so nothing is drawn rather than a wrong or broken shape, when
    /// either axes is not Cartesian (a polar, 3-D or ternary axes has no data rectangle: a ternary frame's transform maps the
    /// triangle, not data), when either render kept no transform, or when a corner of the region is a value the parent's axes
    /// cannot place (below zero on a log axis, inside an axis break).</summary>
    /// <param name="parent">The axes the inset stands in.</param>
    /// <param name="parentView">What the render of the parent drew.</param>
    /// <param name="inset">The inset.</param>
    /// <param name="insetView">What the render of the inset drew: its frame and its transform.</param>
    /// <returns>The geometry, or <see langword="null"/> when there is none to draw.</returns>
    internal static ZoomIndicatorPath? Of(Axes parent, DrawnView parentView, Axes inset, DrawnView insetView)
    {
        if (parent.CoordinateSystem != CoordinateSystem.Cartesian || inset.CoordinateSystem != CoordinateSystem.Cartesian
            || parentView.Transform is not { } onParent || insetView.Transform is not { } view)
        {
            return null;
        }

        // The parent's transform places X and Y apart, so two opposite corners decide the other two.
        var limits = view.DrawnRange;
        var first = onParent.DataToPixel(limits.XMin, limits.YMin);
        var second = onParent.DataToPixel(limits.XMax, limits.YMax);
        if (!double.IsFinite(first.X) || !double.IsFinite(first.Y) || !double.IsFinite(second.X) || !double.IsFinite(second.Y))
            return null;

        return new ZoomIndicatorPath(Rect.FromCorners(first, second), insetView.PlotArea);
    }

    /// <summary>The region's outline, closed: top left, top right, bottom right, bottom left and the top left again.</summary>
    internal Point[] Outline =>
    [
        new(Region.X, Region.Y), new(Region.Right, Region.Y), new(Region.Right, Region.Bottom),
        new(Region.X, Region.Bottom), new(Region.X, Region.Y),
    ];

    /// <summary>The connectors matplotlib's rule keeps, in its order (lower left, upper left, lower right, upper right). With
    /// x0 = the region's left edge left of the inset's, x1 = its right edge left of the inset's, y0 = its lower edge below the
    /// inset's and y1 = its upper edge below the inset's: lower left when x0 xor y0, upper left when x0 equals y1, lower right
    /// when x1 equals y0, upper right when x1 xor y1. Two for an inset beside, above or below its region; none for an inset
    /// wholly inside its region or a region wholly inside its inset.</summary>
    internal IReadOnlyList<ZoomConnector> Connectors
    {
        get
        {
            bool x0 = Region.X < Inset.X;
            bool x1 = Region.Right < Inset.Right;
            bool y0 = Region.Bottom > Inset.Bottom;   // pixel Y runs down: the region's lower edge is the lower one
            bool y1 = Region.Y > Inset.Y;

            var connectors = new List<ZoomConnector>(2);
            if (x0 ^ y0) connectors.Add(new(new(Inset.X, Inset.Bottom), new(Region.X, Region.Bottom)));
            if (x0 == y1) connectors.Add(new(new(Inset.X, Inset.Y), new(Region.X, Region.Y)));
            if (x1 == y0) connectors.Add(new(new(Inset.Right, Inset.Bottom), new(Region.Right, Region.Bottom)));
            if (x1 ^ y1) connectors.Add(new(new(Inset.Right, Inset.Y), new(Region.Right, Region.Y)));
            return connectors;
        }
    }

    /// <summary>Draws the indicator in a group of its own: the region's outline and, when <paramref name="zoom"/> asks for
    /// them, the connectors, all in one stroke (its colour, or the theme's neutral grey, its width and its dash pattern) at its
    /// opacity. A stroke that draws nothing (no width, <see cref="LineStyle.None"/>) draws no group either.</summary>
    /// <param name="ctx">The surface the parent is drawn on.</param>
    /// <param name="zoom">The indicator's settings.</param>
    /// <param name="theme">The theme the figure is drawn under.</param>
    internal void Draw(IRenderContext ctx, ZoomIndicator zoom, Theme theme)
    {
        var stroke = new StrokeStyle(zoom.Color ?? theme.Neutral, zoom.LineWidth, zoom.LineStyle);
        if (!stroke.IsVisible) return;

        ctx.BeginGroup(GroupClass);
        ctx.SetOpacity(zoom.Alpha);
        ctx.DrawLines(Outline, stroke);
        if (zoom.Connectors)
        {
            foreach (var connector in Connectors)
                ctx.DrawLine(connector.OnInset, connector.OnRegion, stroke);
        }

        ctx.SetOpacity(1.0);
        ctx.EndGroup();
    }
}
