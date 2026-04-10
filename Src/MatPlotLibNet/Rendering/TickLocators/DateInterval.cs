// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.TickLocators;

/// <summary>Granularity level chosen by <see cref="AutoDateLocator"/> based on the visible date range.</summary>
public enum DateInterval
{
    /// <summary>Tick at the start of each year (January 1st).</summary>
    Years,

    /// <summary>Tick at the start of each month (1st day).</summary>
    Months,

    /// <summary>Tick at the start of each week (Monday midnight).</summary>
    Weeks,

    /// <summary>Tick at midnight of each day.</summary>
    Days,

    /// <summary>Tick at the start of each hour.</summary>
    Hours,

    /// <summary>Tick at the start of each minute.</summary>
    Minutes,

    /// <summary>Tick at the start of each second.</summary>
    Seconds
}
