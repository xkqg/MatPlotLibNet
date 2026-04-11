// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.TickFormatters;

/// <summary>
/// Formats tick values for bar/candlestick charts where ticks are placed at bar centres (i+0.5).
/// Maps 0.5 → "0", 1.5 → "1", 5.5 → "5", etc.
/// </summary>
public sealed class BarCenterFormatter : ITickFormatter
{
    /// <inheritdoc />
    public string Format(double value) => ((int)Math.Floor(value)).ToString();
}
