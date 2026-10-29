// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>One entry a series has in its legend: a series is one entry, except the one that colours and names its own parts
/// (a stacked area with named layers), which is an entry for each part. Every part is the same series: it carries the
/// series' index, so a click on any of them reaches the series.</summary>
/// <param name="Label">What the legend prints; an empty label draws no entry.</param>
/// <param name="Swatch">How the part is drawn.</param>
/// <param name="ColorSlotOffset">How far past the series' first colour slot the part's colour stands (the part's position).</param>
internal readonly record struct LegendPart(string Label, LegendSwatch Swatch, int ColorSlotOffset = 0);
