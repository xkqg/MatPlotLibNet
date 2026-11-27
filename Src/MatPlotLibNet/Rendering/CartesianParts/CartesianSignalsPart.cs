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
    /// <summary>Constructs the part for one render.</summary>
    /// <param name="context">What the render draws with: the axes, the plot area, the surface, the theme and the primary transform.</param>
    internal CartesianSignalsPart(in CartesianPartContext context)
        : base(context) { }

    /// <inheritdoc />
    public override void Render()
    {
        foreach (var signal in Axes.Signals)
        {
            var pt = Transform.DataToPixel(signal.X, signal.Y);
            // A signal at a point the axes cannot place is skipped.
            if (!pt.IsDrawable()) continue;
            double s = signal.Size;
            var signalColor = signal.Color ?? (signal.Direction == SignalDirection.Buy ? UpDownColors.Standard.Up : UpDownColors.Standard.Down);

            // The triangle's foot, s wide, is centred under (or over) its tip.
            var foot = Range1D.Around(pt.X, s);
            Point[] triangle = signal.Direction == SignalDirection.Buy
                ? [new(pt.X, pt.Y + s), new(foot.Lo, pt.Y + s * 2), new(foot.Hi, pt.Y + s * 2)]
                : [new(pt.X, pt.Y - s), new(foot.Lo, pt.Y - s * 2), new(foot.Hi, pt.Y - s * 2)];
            Ctx.DrawPolygon(triangle, ShapeStyle.Filled(signalColor));
        }
    }
}
