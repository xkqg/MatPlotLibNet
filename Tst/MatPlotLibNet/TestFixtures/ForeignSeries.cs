// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests;

/// <summary>A series written outside the library: it implements <see cref="ISeries"/> and derives from none of its series types, draws
/// nothing and holds no data. A test hands one to the axes to see what the library does with a series it does not know.</summary>
/// <param name="reported">The range the series reports; left out, it reports none.</param>
internal sealed class ForeignSeries(DataRangeContribution reported = default) : ISeries
{
    /// <inheritdoc />
    public string? Label { get; set; }

    /// <inheritdoc />
    public bool Visible { get; set; } = true;

    /// <inheritdoc />
    public int ZOrder { get; set; }

    /// <inheritdoc />
    public void Accept(ISeriesVisitor visitor, RenderArea area) { }

    /// <inheritdoc />
    public DataRangeContribution ComputeDataRange(IAxesContext context) => reported;

    /// <inheritdoc />
    public SeriesDto ToSeriesDto() => new() { Type = "foreign" };
}
