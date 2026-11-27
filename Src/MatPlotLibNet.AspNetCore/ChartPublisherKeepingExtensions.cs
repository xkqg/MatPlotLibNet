// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.AspNetCore;

/// <summary>Any publisher asked what it drew and told to let go of a chart. The library's publisher answers
/// (<see cref="IKeepingChartPublisher"/>); a publisher of the caller's keeps nothing and says nothing of what it drew.</summary>
internal static class ChartPublisherKeepingExtensions
{
    extension(IChartPublisher publisher)
    {
        /// <summary>Publishes the figure as SVG and hands back the plain SVG it published, so a decorator that sends the same
        /// picture elsewhere (the GraphQL subscriptions) draws nothing a second time. A publisher of the caller's publishes with
        /// <see cref="IChartPublisher.PublishSvgAsync"/> and hands back nothing: the decorator then draws the picture itself.</summary>
        /// <param name="chartId">The identifier of the chart to update.</param>
        /// <param name="figure">The figure to render and publish.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The plain SVG that was published, or <see langword="null"/> when this publisher does not say.</returns>
        internal async Task<string?> PublishSvgAndHandOnAsync(string chartId, Figure figure, CancellationToken ct)
        {
            if (publisher is IKeepingChartPublisher keeping) return await keeping.PublishSvgAndHandOnAsync(chartId, figure, ct);
            await publisher.PublishSvgAsync(chartId, figure, ct);
            return null;
        }

        /// <summary>Lets go of whatever the publisher keeps for a chart that ended or that nobody views any more. A publisher of
        /// the caller's keeps nothing, so nothing is done.</summary>
        /// <param name="chartId">The identifier of the chart.</param>
        internal Task ForgetAsync(string chartId) =>
            publisher is IKeepingChartPublisher keeping ? keeping.ForgetAsync(chartId) : Task.CompletedTask;
    }
}
