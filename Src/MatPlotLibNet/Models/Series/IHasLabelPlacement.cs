// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Series whose label can be drawn at the end of the line instead of in the legend: a line series and a
/// streaming line series.</summary>
/// <remarks>The placement only has meaning for a series that carries a <see cref="ISeries.Label"/>. Set it per series
/// (<c>s.LabelPlacement = SeriesLabelPlacement.LineEnd</c>) or with <c>WithDirectLabel()</c> on the axes builder or the
/// secondary-axis builder, which marks the series added last.</remarks>
public interface IHasLabelPlacement
{
    /// <summary>Where the series shows its name. Default <see cref="SeriesLabelPlacement.Legend"/>.</summary>
    SeriesLabelPlacement LabelPlacement { get; set; }

    /// <summary>The points of the line as the end label reads them: one consistent snapshot of the x and y values,
    /// so the label is placed at the end of the line that is drawn.</summary>
    internal LinePoints LinePoints();
}
