// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>What the width of a stroke says about the room it covers: the one rule an axes' spine (<see cref="Models.SpineConfig.HalfWidthIn"/>),
/// a colour bar's frame (which matplotlib draws as a spine), a raster's line on the pixel grid and an arrow head drawn at the end of a
/// stroked line read.</summary>
internal static class StrokeWidthExtensions
{
    extension(double strokeWidth)
    {
        /// <summary>How far a stroke of this width reaches past the line it is drawn along: half its width, on either side across it and past
        /// an end that projects. The room a tick mark keeps clear of a spine, the band a line covers, the length a projecting cap adds.</summary>
        internal double Reach => strokeWidth / 2.0;
    }
}
