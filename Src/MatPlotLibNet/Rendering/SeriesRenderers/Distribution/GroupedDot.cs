// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>One observation of a strip plot or a swarm plot as it stands on the plot, and the group it belongs to.</summary>
/// <param name="Pixel">Where the dot stands.</param>
/// <param name="Group">The position of its group among the series' datasets.</param>
internal readonly record struct GroupedDot(Point Pixel, int Group);
