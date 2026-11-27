// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>A hub client that writes only the members of <see cref="IChartHubClient"/> that have no default body and does nothing in
/// them, so a test sees what the interface's own default members do for a client written before they existed.</summary>
internal sealed class BareHubClient : IChartHubClient
{
    /// <inheritdoc />
    public Task UpdateChart(string chartId, string figureJson) => Task.CompletedTask;

    /// <inheritdoc />
    public Task UpdateChartSvg(string chartId, string svg) => Task.CompletedTask;

    /// <inheritdoc />
    public Task ReceiveTooltipContent(string chartId, string html) => Task.CompletedTask;
}
