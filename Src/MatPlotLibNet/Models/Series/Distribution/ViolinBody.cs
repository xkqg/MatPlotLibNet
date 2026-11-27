// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.SeriesRenderers;

namespace MatPlotLibNet.Models.Series;

/// <summary>One height of a violin's outline.</summary>
/// <param name="Y">The value the outline stands at.</param>
/// <param name="HalfWidth">How far the body reaches to either side of its position at that value, in data units, before the
/// series' <see cref="ViolinSeries.Side"/> decides which sides are drawn.</param>
internal readonly record struct ViolinSlice(double Y, double HalfWidth);

/// <summary>One violin as its data makes it, before it is placed on the plot.</summary>
/// <param name="Sample">The observations it is drawn from: the drawable values of its dataset, ascending; empty when none can
/// be drawn.</param>
/// <param name="Outline">The outline of its body at the series' width (<see cref="Models.Series.ViolinSeries"/>); empty when the
/// sample is.</param>
internal readonly record struct ViolinBody(double[] Sample, ViolinSlice[] Outline);
