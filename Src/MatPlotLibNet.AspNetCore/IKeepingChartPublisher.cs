// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.AspNetCore;

/// <summary>A publisher that keeps something per chart and says what it drew: the library's <see cref="ChartPublisher"/>, and the
/// GraphQL decorator that hands both on to the publisher it decorates. Any <see cref="IChartPublisher"/> is asked through the
/// same names (<see cref="ChartPublisherKeepingExtensions"/>). They live here and not on <see cref="IChartPublisher"/> so that
/// an application can still implement, proxy or mock the public interface.</summary>
internal interface IKeepingChartPublisher : IChartPublisher
{
    /// <summary>Publishes the figure as SVG and hands back the plain SVG it published, so a decorator that sends the same picture
    /// elsewhere draws nothing a second time.</summary>
    /// <param name="chartId">The identifier of the chart to update.</param>
    /// <param name="figure">The figure to render and publish.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The plain SVG that was published, or <see langword="null"/> when the publisher does not say.</returns>
    internal Task<string?> PublishSvgAndHandOnAsync(string chartId, Figure figure, CancellationToken ct);

    /// <summary>Lets go of whatever the publisher keeps for a chart that ended (it was unregistered) or that nobody views any
    /// more (its last viewer left). A publish of the chart after it starts afresh: every viewer is sent the whole picture.</summary>
    /// <param name="chartId">The identifier of the chart.</param>
    internal Task ForgetAsync(string chartId);
}
