// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>A technical indicator that can be applied to an <see cref="Axes"/> to add computed series or decorations.</summary>
/// <remarks>Indicators compute derived data (moving averages, oscillators, etc.) from price arrays
/// and inject the result as standard series into the axes. Overlay indicators add to the same axes;
/// panel indicators are placed in separate subplots by the caller.</remarks>
public interface IIndicator
{
    /// <summary>Computes the indicator and adds the resulting series or decorations to the axes.</summary>
    /// <param name="axes">The axes to add computed series to.</param>
    void Apply(Axes axes);
}
