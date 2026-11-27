// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.CartesianParts;

/// <summary>What one pass over an axis' ticks drew (<see cref="CartesianAxisPart.RenderTicks"/>): how far the labels on its own side reach
/// past the mark and the pad, and the band the labels on the opposite edge take.</summary>
/// <param name="OwnReach">How far the tick labels drawn on the axis' own edge reach past the mark and the pad; 0 when none was drawn.</param>
/// <param name="OppositeBand">From the opposite edge to the far side of the tick labels drawn there (<see cref="Models.TickConfig.TickLabelBand"/>);
/// 0 when none was drawn.</param>
internal readonly record struct TicksDrawn(double OwnReach, double OppositeBand);
