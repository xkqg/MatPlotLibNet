// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>One bar of a waterfall as the series lays it out: the level it stands on, the level it reaches, and whether
/// it is a total. The series works this out once (<see cref="WaterfallSeries.Bars"/>), and its data range and its
/// renderer both read it, so what a bar is cannot be decided twice.</summary>
/// <param name="Foot">The level the bar stands on: the running total before a step, zero for a total.</param>
/// <param name="Top">The level the bar reaches, and the running total after it: the previous total plus the step, or the
/// level a total holds. Not finite when it cannot be drawn: a missing step is not known, and neither is any total after
/// it until the next total states a level.</param>
/// <param name="IsTotal">Whether the bar is a total (<see cref="WaterfallSeries.TotalIndices"/>).</param>
/// <param name="Rises">Whether the bar's value is zero or more, so the bar runs up from <paramref name="Foot"/>; a bar
/// with a negative or missing value runs down.</param>
internal readonly record struct WaterfallBar(double Foot, double Top, bool IsTotal, bool Rises);
