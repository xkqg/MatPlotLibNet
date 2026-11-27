// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using MatPlotLibNet.Models;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>The publisher a test hands a session or a registry to see what is published: it keeps every SVG publish, and can be made
/// to behave the way a publisher that fails or is cancelled does, so a test sees what the session does about it.</summary>
internal sealed class RecordingChartPublisher : SilentChartPublisher
{
    /// <summary>The SVG publishes, in the order they came.</summary>
    public ConcurrentQueue<PublishedFigure> Calls { get; } = new();

    /// <summary>The failure the publish's task ends with, after the call is kept; <see langword="null"/> for none.</summary>
    public Exception? Throw { get; set; }

    /// <summary>The failure the publish throws before it returns a task at all, after the call is kept; <see langword="null"/> for none.</summary>
    public Exception? ThrowAtOnce { get; set; }

    /// <summary>What the publisher does while it is publishing, after it has kept the call and before it fails: a test reads the state of
    /// the session there, or fails the call of its choosing by throwing.</summary>
    public Action? WhilePublishing { get; set; }

    /// <inheritdoc />
    public override Task PublishSvgAsync(string chartId, Figure figure, CancellationToken ct = default)
    {
        Calls.Enqueue(new PublishedFigure(chartId, figure));
        WhilePublishing?.Invoke();
        if (ThrowAtOnce is not null)
        {
            throw ThrowAtOnce;
        }

        return Throw is null ? Task.CompletedTask : Task.FromException(Throw);
    }

    /// <summary>Returns once at least <paramref name="count"/> publishes have come.</summary>
    /// <param name="count">How many publishes are waited for.</param>
    /// <param name="within">The longest the wait lasts; left out, five seconds.</param>
    /// <returns>A task that ends when the count is reached.</returns>
    /// <exception cref="TimeoutException">Fewer publishes came within <paramref name="within"/>.</exception>
    public Task WaitForAtLeastAsync(int count, TimeSpan? within = null) =>
        Eventually.HoldsAsync(() => Calls.Count >= count, within);

    /// <summary>Returns once no publish has come for <paramref name="quiet"/>.</summary>
    /// <param name="quiet">How long nothing must be published.</param>
    /// <returns>A task that ends when the publisher has been quiet for that long.</returns>
    public async Task WaitForQuiescenceAsync(TimeSpan quiet)
    {
        int last = Calls.Count;
        var stable = DateTime.UtcNow;
        while (DateTime.UtcNow - stable < quiet)
        {
            await Task.Delay(20);
            if (Calls.Count != last)
            {
                last = Calls.Count;
                stable = DateTime.UtcNow;
            }
        }
    }
}

/// <summary>One SVG publish a <see cref="RecordingChartPublisher"/> was told.</summary>
/// <param name="ChartId">The chart the publish was for.</param>
/// <param name="Figure">The figure that was published.</param>
internal readonly record struct PublishedFigure(string ChartId, Figure Figure);
