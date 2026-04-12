// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.TickFormatters;

/// <summary>Formats tick values for display on chart axes.</summary>
public interface ITickFormatter
{
    /// <summary>Formats a tick value as a display string.</summary>
    string Format(double value);
}
