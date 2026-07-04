// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Interactive;

/// <summary>Extension methods for displaying figures interactively in a browser.</summary>
public static class InteractiveExtensions
{
    private static volatile IBrowserLauncher _browserLauncher = new BrowserLauncher();

    /// <summary>Gets or sets the browser launcher used by <see cref="ShowAsync"/>. Replace for testing or custom behavior.</summary>
    public static IBrowserLauncher Browser
    {
        get => _browserLauncher;
        set => _browserLauncher = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Opens the figure in the default browser asynchronously and returns a handle for pushing updates.</summary>
    /// <remarks>The embedded server is the process-lifetime <see cref="ChartServer.Instance"/> singleton;
    /// it is intentionally not disposed here and outlives any single call (the returned
    /// <see cref="InteractiveFigure"/> does not own it). The awaited chain uses
    /// <c>ConfigureAwait(false)</c> throughout, so this method is safe to block on from a captured
    /// <see cref="SynchronizationContext"/> — provided any custom <see cref="Browser"/> launcher does
    /// the same. Prefer awaiting it (<c>await figure.ShowAsync()</c>).</remarks>
    public static async Task<InteractiveFigure> ShowAsync(this Figure figure, CancellationToken ct = default)
    {
        var server = ChartServer.Instance;
        await server.EnsureStartedAsync(ct).ConfigureAwait(false);

        var chartId = server.RegisterFigure(figure);
        var url = server.GetFigureUrl(chartId);

        await _browserLauncher.OpenAsync(url).ConfigureAwait(false);

        return new InteractiveFigure(chartId, figure);
    }
}
