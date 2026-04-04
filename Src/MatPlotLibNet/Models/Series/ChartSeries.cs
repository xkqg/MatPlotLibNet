// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Models.Series;

/// <summary>Base class providing common <see cref="ISeries"/> properties. Subclasses must implement <see cref="Accept"/>.</summary>
public abstract class ChartSeries : ISeries
{
    /// <inheritdoc />
    public string? Label { get; set; }

    /// <inheritdoc />
    public bool Visible { get; set; } = true;

    /// <inheritdoc />
    public int ZOrder { get; set; }

    /// <inheritdoc />
    public abstract void Accept(ISeriesVisitor visitor, RenderArea area);
}
