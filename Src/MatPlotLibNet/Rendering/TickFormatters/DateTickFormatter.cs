// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace MatPlotLibNet.Rendering.TickFormatters;

/// <summary>Formats tick values as dates by interpreting them as OLE Automation dates.</summary>
public sealed class DateTickFormatter : ITickFormatter
{
    /// <summary>Gets or sets the date format string (default "yyyy-MM-dd").</summary>
    public string DateFormat { get; set; } = "yyyy-MM-dd";

    /// <inheritdoc />
    public string Format(double value) =>
        DateTime.FromOADate(value).ToString(DateFormat, CultureInfo.InvariantCulture);
}
