// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.GraphQL;

/// <summary>Payload delivered to GraphQL subscription clients when a chart is updated.</summary>
/// <param name="ChartId">The identifier of the chart that was updated.</param>
/// <param name="Payload">The SVG or JSON string payload.</param>
public sealed record ChartEventMessage(string ChartId, string Payload);
