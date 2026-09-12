// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Models.Series;

/// <summary>Defines the contract for a data series that can be rendered on an axes.</summary>
public interface ISeries
{
    string? Label { get; set; }

    bool Visible { get; set; }

    int ZOrder { get; set; }

    /// <summary>Accepts a visitor for rendering this series within the specified area.</summary>
    /// <param name="visitor">The series visitor that performs the rendering.</param>
    /// <param name="area">The render area to draw into.</param>
    void Accept(ISeriesVisitor visitor, RenderArea area);

    /// <summary>Computes this series' contribution to the axes data range.</summary>
    DataRangeContribution ComputeDataRange(IAxesContext context);

    /// <summary>Creates a serialization DTO representing this series.</summary>
    SeriesDto ToSeriesDto();

    /// <summary>This series' data as a table — its own columns and rows, no caption — or <see langword="null"/>
    /// when the series has no tabular form (an annotation, a label placed in a scene). It is the DATA at full
    /// resolution, never what the renderer drew: a downsampled line and a viewport-sliced signal both draw fewer
    /// points than the series holds, and the table says what the series holds.
    /// <para>The default answers <see langword="null"/>, so a series type that has nothing to say says nothing.
    /// The types that DO are listed in the accessibility cookbook page, and a test pins the list against it.</para></summary>
    Models.ChartDataTable? ToDataTable() => null;
}
