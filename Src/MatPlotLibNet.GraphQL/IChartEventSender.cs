// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.GraphQL;

/// <summary>Sends chart update events to the GraphQL subscription system.</summary>
public interface IChartEventSender
{
    /// <summary>Sends an SVG update event for the specified chart.</summary>
    Task SendSvgAsync(string chartId, string svg, CancellationToken ct = default);

    /// <summary>Sends a JSON update event for the specified chart.</summary>
    Task SendJsonAsync(string chartId, string json, CancellationToken ct = default);
}
