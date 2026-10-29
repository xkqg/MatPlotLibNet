// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The flow-family verb of <see cref="Axes"/> (<c>Sankey</c>), constructing the series and handing it to
/// <see cref="Axes.AddSeries{T}"/>.</summary>
public static class AxesFlowVerbs
{
    extension(Axes axes)
    {
        /// <summary>Adds a Sankey diagram series to the axes.</summary>
        public SankeySeries Sankey(SankeyNode[] nodes, SankeyLink[] links)
        {
            links.ThrowIfPointingOutside(nodes.Length);
            return axes.AddSeries(new SankeySeries(nodes, links));
        }
    }
}
