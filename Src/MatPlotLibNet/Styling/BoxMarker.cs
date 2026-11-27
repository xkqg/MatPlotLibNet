// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>A marker of a box plot (an observation beyond the whiskers, the mean) where its author names nothing: its shape, its fill, its
/// outline, its size and the width of its outline.</summary>
/// <param name="Marker">The shape of the marker (<c>boxplot.flierprops.marker</c>, <c>boxplot.meanprops.marker</c>).</param>
/// <param name="Face">The fill of the marker; <see langword="null"/> leaves it open (<c>markerfacecolor: none</c>).</param>
/// <param name="Edge">The colour of the marker's outline, or of its strokes where the marker is strokes alone; <see langword="null"/>
/// for none.</param>
/// <param name="Size">How large the marker is, in points across (<c>markersize</c>, 6 pt).</param>
/// <param name="EdgeWidth">The width of the marker's outline, in pixels (<c>markeredgewidth</c>).</param>
internal readonly record struct BoxMarker(MarkerStyle Marker, Color? Face, Color? Edge, double Size, double EdgeWidth);
