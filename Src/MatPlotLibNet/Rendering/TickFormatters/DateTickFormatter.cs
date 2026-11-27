// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Rendering.TickFormatters;

/// <summary>Formats tick values as dates by interpreting them as OLE Automation dates.</summary>
public sealed class DateTickFormatter : ITickFormatter, IJsonDescribed<TickFormatterDto>, IChangeTracked
{
    private long _stamp;

    /// <summary>Stores <paramref name="value"/> and marks this formatter changed, unless it already holds an equal value.</summary>
    private void Change<T>(ref T slot, T value) => ChangeClock.Change(ref slot, value, ref _stamp);

    long IChangeTracked.LatestChange => _stamp;

    /// <summary>The .NET date-and-time format string each tick is written with, using the invariant culture. Default
    /// <see cref="DateTimeTickFormatter.DefaultFormat"/>.</summary>
    public string DateFormat { get; set => Change(ref field, value); } = DateTimeTickFormatter.DefaultFormat;

    /// <inheritdoc />
    TickFormatterDto IJsonDescribed<TickFormatterDto>.Describe() => new DateTickFormatterDto(DateFormat);

    /// <inheritdoc />
    /// <remarks>A value no date can be made of is written as the number.</remarks>
    public string Format(double value) => value.ToOleDateText(DateFormat);
}
