// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a funnel chart showing progressive reduction through stages (e.g., sales pipeline).</summary>
public sealed class FunnelSeries : ChartSeries
{
    public string[] Labels { get; }
    public double[] Values { get; }
    public Color[]? Colors { get; set; }

    public FunnelSeries(string[] labels, double[] values)
    {
        Labels = labels; Values = values;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
