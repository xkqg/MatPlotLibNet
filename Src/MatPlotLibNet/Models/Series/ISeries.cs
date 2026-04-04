// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Models.Series;

/// <summary>Defines the contract for a data series that can be rendered on an axes.</summary>
public interface ISeries
{
    /// <summary>Gets or sets the legend label for this series.</summary>
    string? Label { get; set; }

    /// <summary>Gets or sets whether this series is visible.</summary>
    bool Visible { get; set; }

    /// <summary>Gets or sets the drawing order of this series.</summary>
    int ZOrder { get; set; }

    /// <summary>Accepts a visitor for rendering this series within the specified area.</summary>
    /// <param name="visitor">The series visitor that performs the rendering.</param>
    /// <param name="area">The render area to draw into.</param>
    void Accept(ISeriesVisitor visitor, RenderArea area);
}
