// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickFormatters;

namespace MatPlotLibNet.Tests;

/// <summary>A tick formatter an application wrote, as a test hands one to an axis: it has no JSON form, writes what
/// <paramref name="format"/> says and counts how often it was asked.</summary>
/// <param name="format">The label of each tick value.</param>
internal sealed class TestTickFormatter(Func<double, string> format) : ITickFormatter
{
    /// <summary>How many labels the formatter wrote.</summary>
    public int Calls { get; private set; }

    public string Format(double value)
    {
        Calls++;
        return format(value);
    }
}
