// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.AspNetCore;

/// <summary>The parts of a chart's SVG that changed since the document a viewer holds, sent to a viewer that patches
/// (<see cref="ChartHub.SubscribeToParts"/>). Every part is the whole element to put in place of the one of the same
/// <c>data-mpl-part</c> name: <c>&lt;g data-mpl-part="axes-1"&gt;…&lt;/g&gt;</c>. A part that did not change is
/// <see langword="null"/> (or absent from <see cref="Axes"/>) and stays as it is.</summary>
/// <param name="RootTag">The root element's new opening tag, whose attributes replace the ones the root has (a zoom changes
/// the view a figure with server interaction carries there); <see langword="null"/> when it did not change.</param>
/// <param name="Background">The new background part; <see langword="null"/> when it did not change.</param>
/// <param name="Overlay">The new part the figure draws above its subplots; <see langword="null"/> when it did not change.</param>
/// <param name="Axes">The subplots that changed, each with its index and its new part.</param>
public sealed record ChartSvgParts(string? RootTag, string? Background, string? Overlay, IReadOnlyList<ChartSvgAxesPart> Axes);

/// <summary>One subplot that changed: its index among the figure's subplots and its new part, the whole
/// <c>&lt;g data-mpl-part="axes-<paramref name="Index"/>"&gt;</c> element.</summary>
/// <param name="Index">The subplot's index.</param>
/// <param name="Markup">The new part.</param>
public readonly record struct ChartSvgAxesPart(int Index, string Markup);
