// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>The radii a series reaches on a polar axes: the data limits of its radial axis, matplotlib's <c>dataLim.intervaly</c>.</summary>
/// <param name="Min">The smallest radius the series draws.</param>
/// <param name="Max">The largest radius the series draws.</param>
/// <param name="Sticky">Whether the series fills from <paramref name="Min"/> to <paramref name="Max"/> exactly (a polar heatmap, whose
/// cells start at the centre and end there, matplotlib's sticky edges of a <c>pcolormesh</c>), so the scale reaches at least that far
/// without the margin it leaves beyond a series of marks.</param>
internal readonly record struct RadialExtent(double Min, double Max, bool Sticky);
