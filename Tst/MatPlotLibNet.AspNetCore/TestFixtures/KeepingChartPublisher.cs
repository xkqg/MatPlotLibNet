// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>The publisher a test hands a registry to see which charts it is told to forget and how many SVGs it is asked to
/// publish: a <see cref="SilentChartPublisher"/> that also says what it keeps per chart, so the two members of the interface with no
/// default body are written once, there.</summary>
internal sealed class KeepingChartPublisher : SilentChartPublisher, IKeepingChartPublisher
{
    private int _svgs;

    /// <summary>How many SVGs were published, whether by <see cref="PublishSvgAsync"/> or by the hand-on form.</summary>
    public int Svgs => Volatile.Read(ref _svgs);

    /// <summary>The charts it was told to forget, in the order it was told.</summary>
    public ConcurrentQueue<string> Forgotten { get; } = new();

    /// <inheritdoc />
    public override Task PublishSvgAsync(string chartId, Figure figure, CancellationToken ct = default)
    {
        Interlocked.Increment(ref _svgs);
        return Task.CompletedTask;
    }

    async Task<string?> IKeepingChartPublisher.PublishSvgAndHandOnAsync(string chartId, Figure figure, CancellationToken ct)
    {
        await PublishSvgAsync(chartId, figure, ct);
        return null;
    }

    Task IKeepingChartPublisher.ForgetAsync(string chartId)
    {
        Forgotten.Enqueue(chartId);
        return Task.CompletedTask;
    }
}
