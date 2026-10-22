// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Rendering.TickFormatters;

/// <summary>Formats tick values as dates by interpreting them as OLE Automation dates.</summary>
public sealed class DateTickFormatter : ITickFormatter, IJsonDescribed<TickFormatterDto>
{
    public string DateFormat { get; set; } = "yyyy-MM-dd";

    /// <inheritdoc />
    TickFormatterDto IJsonDescribed<TickFormatterDto>.Describe() => new DateTickFormatterDto(DateFormat);

    /// <inheritdoc />
    public string Format(double value) =>
        DateTime.FromOADate(value).ToString(DateFormat, CultureInfo.InvariantCulture);
}
