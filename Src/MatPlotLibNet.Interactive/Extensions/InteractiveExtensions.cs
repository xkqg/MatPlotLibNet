// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Interactive;

/// <summary>Extension methods for displaying figures interactively in a browser.</summary>
public static class InteractiveExtensions
{
    private static volatile IBrowserLauncher _browserLauncher = new BrowserLauncher();

    /// <summary>Gets or sets the browser launcher used by Show/ShowAsync. Replace for testing or custom behavior.</summary>
    public static IBrowserLauncher Browser
    {
        get => _browserLauncher;
        set => _browserLauncher = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Opens the figure in the default browser asynchronously and returns a handle for pushing updates.</summary>
    public static async Task<InteractiveFigure> ShowAsync(this Figure figure, CancellationToken ct = default)
    {
        var server = ChartServer.Instance;
        await server.EnsureStartedAsync(ct);

        var chartId = server.RegisterFigure(figure);
        var url = server.GetFigureUrl(chartId);

        await _browserLauncher.OpenAsync(url);

        return new InteractiveFigure(chartId, figure);
    }

    /// <summary>Opens the figure in the default browser and returns a handle for pushing updates.</summary>
    public static InteractiveFigure Show(this Figure figure) =>
        ShowAsync(figure).GetAwaiter().GetResult();
}
