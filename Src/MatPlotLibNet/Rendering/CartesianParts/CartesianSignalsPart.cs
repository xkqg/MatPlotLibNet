// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.CartesianParts;

/// <summary>
/// Renders buy/sell signal markers as triangles on a Cartesian axes.
/// Draws each SignalMarker's triangle (pointing up
/// for Buy, down for Sell) in the signal's color.
/// </summary>
internal sealed class CartesianSignalsPart : CartesianAxesPart
{
    /// <inheritdoc />
    public CartesianSignalsPart(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme, DataTransform transform)
        : base(axes, plotArea, ctx, theme, transform) { }

    /// <inheritdoc />
    public override void Render()
    {
        foreach (var signal in Axes.Signals)
        {
            var pt = Transform.DataToPixel(signal.X, signal.Y);
            // A signal at a point the axes cannot place is skipped.
            if (!pt.IsDrawable()) continue;
            double s = signal.Size;
            var signalColor = signal.Color ?? (signal.Direction == SignalDirection.Buy ? Colors.Green : Colors.Red);

            Point[] triangle = signal.Direction == SignalDirection.Buy
                ? [new(pt.X, pt.Y + s), new(pt.X - s / 2, pt.Y + s * 2), new(pt.X + s / 2, pt.Y + s * 2)]
                : [new(pt.X, pt.Y - s), new(pt.X - s / 2, pt.Y - s * 2), new(pt.X + s / 2, pt.Y - s * 2)];
            Ctx.DrawPolygon(triangle, new ShapeStyle(signalColor, null, 0));
        }
    }
}
