// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>The caller publisher a test hands a session to see which viewer was sent which tooltip. A test that does not care what is
/// sent to a caller hands the library's own <c>NullCallerPublisher</c> instead.</summary>
internal sealed class RecordingCallerPublisher : ICallerPublisher
{
    /// <summary>The tooltips sent, in the order they were sent.</summary>
    public ConcurrentQueue<TooltipSent> Calls { get; } = new();

    /// <inheritdoc />
    public Task SendTooltipAsync(string connectionId, string chartId, string html, CancellationToken ct = default)
    {
        Calls.Enqueue(new TooltipSent(connectionId, chartId, html));
        return Task.CompletedTask;
    }

    /// <summary>Returns once at least <paramref name="count"/> tooltips have been sent.</summary>
    /// <param name="count">How many tooltips are waited for.</param>
    /// <param name="within">The longest the wait lasts; left out, five seconds.</param>
    /// <returns>A task that ends when the count is reached.</returns>
    /// <exception cref="TimeoutException">Fewer tooltips were sent within <paramref name="within"/>.</exception>
    public Task WaitForAtLeastAsync(int count, TimeSpan? within = null) =>
        Eventually.HoldsAsync(() => Calls.Count >= count, within);
}

/// <summary>One tooltip a <see cref="RecordingCallerPublisher"/> was told to send.</summary>
/// <param name="ConnectionId">The connection of the viewer it was sent to.</param>
/// <param name="ChartId">The chart it is for.</param>
/// <param name="Html">The tooltip's markup.</param>
internal readonly record struct TooltipSent(string ConnectionId, string ChartId, string Html);
