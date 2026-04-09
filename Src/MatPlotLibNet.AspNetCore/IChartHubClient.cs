// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.AspNetCore;

/// <summary>SignalR client interface for receiving real-time chart updates.</summary>
public interface IChartHubClient
{
    /// <summary>Receives a chart update as serialized JSON.</summary>
    /// <param name="chartId">The identifier of the chart being updated.</param>
    /// <param name="figureJson">The JSON-serialized figure data.</param>
    Task UpdateChart(string chartId, string figureJson);

    /// <summary>Receives a chart update as pre-rendered SVG markup.</summary>
    /// <param name="chartId">The identifier of the chart being updated.</param>
    /// <param name="svg">The SVG string for the updated chart.</param>
    Task UpdateChartSvg(string chartId, string svg);
}
