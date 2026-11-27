// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet;

/// <summary>The default <see cref="IChartRenderer"/> for non-DI scenarios (console apps, scripts, and every
/// native UI control -- Avalonia/Maui/Uno/Wpf -- that has no DI container reachable from its own render
/// callback). One replaceable slot, separated from every other default.</summary>
public static class ChartRendererDefault
{
    private static volatile IChartRenderer _current = new ChartRenderer();

    /// <summary>Gets or sets the default chart renderer.</summary>
    public static IChartRenderer Current
    {
        get => _current;
        set => _current = value ?? throw new ArgumentNullException(nameof(value));
    }
}
