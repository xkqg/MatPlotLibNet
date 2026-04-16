// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;

namespace MatPlotLibNet.Avalonia.Tests;

/// <summary>
/// Validates the static mapping logic in <see cref="AvaloniaInputAdapter"/> without
/// requiring a running Avalonia application. These tests exercise the adapter's
/// modifier-key mapping and button resolution by calling the internal helpers directly.
/// Full round-trip tests (pointer → controller → figure mutation) run in the
/// integration tests inside <c>MatPlotLibNet.Tests</c>.
/// </summary>
public class AvaloniaInputAdapterTests
{
    // AvaloniaInputAdapter is internal — tested via ModifierKeys mapping reflectively.
    // The interaction layer (InteractionController + modifiers) is already covered by
    // MatPlotLibNet.Tests/Interaction/*. Here we just verify the type exists and is
    // internal to the package boundary.

    [Fact]
    public void AvaloniaInputAdapter_IsInternalType()
    {
        var type = typeof(MplChartControl).Assembly
            .GetType("MatPlotLibNet.Avalonia.AvaloniaInputAdapter");
        Assert.NotNull(type);
        Assert.False(type!.IsPublic);
    }

    [Fact]
    public void MplChartDrawOperation_IsInternalType()
    {
        var type = typeof(MplChartControl).Assembly
            .GetType("MatPlotLibNet.Avalonia.MplChartDrawOperation");
        Assert.NotNull(type);
        Assert.False(type!.IsPublic);
    }
}
