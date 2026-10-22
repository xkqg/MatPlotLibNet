// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Layout;

/// <summary>Holds the minimum pixel margin requirements computed for a single subplot.</summary>
/// <param name="LeftNeeded">Room the left edge needs.</param>
/// <param name="BottomNeeded">Room the bottom edge needs.</param>
/// <param name="TopNeeded">Room the top edge needs.</param>
/// <param name="RightNeeded">Room the right edge needs: the right band, or a colorbar or outside legend when wider.</param>
/// <param name="RightBand">The measured right band alone (<see cref="RightBand.Width"/>); it lifts the right-margin
/// and gutter caps, since measured content must never be cut.</param>
/// <param name="TopBand">The measured top band with the axes title above it (<see cref="EdgeBands.Top"/> stacked under
/// the title) when a top band is drawn, 0 otherwise; it lifts the top-margin and row-gap caps the way
/// <paramref name="RightBand"/> lifts the right ones.</param>
/// <param name="LeftBand">The left need when the Y tick labels were measured (category names on the Y axis,
/// <see cref="EdgeBands.Left"/>, with the Y axis title past them), 0 otherwise; it lifts the left-margin and gutter caps
/// the way <paramref name="RightBand"/> lifts the right ones.</param>
/// <param name="BottomBand">The bottom need when the renderer measured what it draws below the bottom spine (the X tick
/// labels, <see cref="EdgeBands.Bottom"/>, with the X axis title past them, <see cref="EdgeBands.BottomTitle"/>), 0
/// otherwise; it lifts the bottom-margin and row-gap caps the way <paramref name="LeftBand"/> lifts the left ones.</param>
internal sealed record LayoutMetrics(
    double LeftNeeded,
    double BottomNeeded,
    double TopNeeded,
    double RightNeeded,
    double RightBand,
    double TopBand,
    double LeftBand,
    double BottomBand);
