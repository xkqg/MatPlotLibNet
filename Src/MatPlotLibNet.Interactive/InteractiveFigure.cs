// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Interactive;

/// <summary>Handle returned by <see cref="InteractiveExtensions.Show"/> for pushing updates to a displayed chart.</summary>
public sealed class InteractiveFigure
{
    /// <summary>Gets the chart identifier used for SignalR subscription.</summary>
    public string ChartId { get; }

    /// <summary>Gets the figure being displayed.</summary>
    public Figure Figure { get; }

    internal InteractiveFigure(string chartId, Figure figure)
    {
        ChartId = chartId;
        Figure = figure;
    }

    /// <summary>Pushes the current state of the figure to the browser via SignalR.</summary>
    public async Task UpdateAsync()
    {
        await ChartServer.Instance.UpdateFigureAsync(ChartId, Figure);
    }
}
