// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Models.Series;

/// <summary>Base class providing common <see cref="ISeries"/> properties. All series must implement
/// <see cref="Accept"/>, <see cref="ComputeDataRange"/>, and <see cref="ToSeriesDto"/>.</summary>
public abstract class ChartSeries : ISeries, IHasDataRange, ISeriesSerializable
{
    public string? Label { get; set; }

    public bool Visible { get; set; } = true;

    public int ZOrder { get; set; }

    /// <inheritdoc />
    public abstract void Accept(ISeriesVisitor visitor, RenderArea area);

    /// <inheritdoc />
    public abstract DataRangeContribution ComputeDataRange(IAxesContext context);

    /// <inheritdoc />
    public abstract SeriesDto ToSeriesDto();
}
