// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>How far out a series reaches on a polar axes.</summary>
/// <param name="Max">The largest radius the series draws.</param>
/// <param name="Sticky">Whether the series fills out to <paramref name="Max"/> exactly (a polar heatmap, whose cells end
/// there), so the scale reaches at least that far without the margin it leaves beyond a series of marks.</param>
internal readonly record struct RadialExtent(double Max, bool Sticky);
