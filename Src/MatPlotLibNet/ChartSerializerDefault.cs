// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet;

/// <summary>The default <see cref="IChartSerializer"/> for non-DI scenarios (console apps, scripts). One
/// replaceable slot, separated from every other default — the condition a process-global static is held to:
/// it holds the ONE thing it exists to provide, and nothing else is hung off it.</summary>
public static class ChartSerializerDefault
{
    private static volatile IChartSerializer _current = new ChartSerializer();

    /// <summary>Gets or sets the default chart serializer.</summary>
    public static IChartSerializer Current
    {
        get => _current;
        set => _current = value ?? throw new ArgumentNullException(nameof(value));
    }
}
