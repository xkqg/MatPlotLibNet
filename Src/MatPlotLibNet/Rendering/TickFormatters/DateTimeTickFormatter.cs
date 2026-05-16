// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace MatPlotLibNet.Rendering.TickFormatters;

/// <summary>Tick formatter for DateTime axes. Use <see cref="FromArray"/> when tick values
/// are 0-based integer indices into a <c>DateTime[]</c> array (Surface, Bar, Heatmap), or
/// <see cref="FromEpochMs"/> when tick values are Unix milliseconds (LineSeries).</summary>
public sealed class DateTimeTickFormatter : ITickFormatter
{
    private readonly DateTime[]? _timestamps;   // null → epoch-ms mode
    private readonly string      _format;

    private DateTimeTickFormatter(DateTime[]? timestamps, string format)
    {
        _timestamps = timestamps;
        _format     = format;
    }

    /// <summary>Index-based factory: the tick value is rounded to the nearest integer and
    /// used as an index into <paramref name="timestamps"/>. Out-of-range → empty string.</summary>
    /// <param name="timestamps">DateTime array; index 0 corresponds to tick value 0.</param>
    /// <param name="format">.NET date format string (default <c>"yyyy-MM-dd"</c>).</param>
    public static DateTimeTickFormatter FromArray(
        DateTime[] timestamps, string format = "yyyy-MM-dd")
    {
        ArgumentNullException.ThrowIfNull(timestamps);
        return new(timestamps, format);
    }

    /// <summary>Epoch-milliseconds factory: the tick value is interpreted as
    /// Unix milliseconds since 1970-01-01 UTC.</summary>
    /// <param name="format">.NET date format string (default <c>"yyyy-MM-dd"</c>).</param>
    public static DateTimeTickFormatter FromEpochMs(string format = "yyyy-MM-dd") =>
        new(null, format);

    /// <inheritdoc />
    public string Format(double value)
    {
        if (_timestamps is not null)
        {
            int idx = (int)Math.Round(value);
            return idx >= 0 && idx < _timestamps.Length
                ? _timestamps[idx].ToString(_format, CultureInfo.InvariantCulture)
                : string.Empty;
        }
        return DateTimeOffset.FromUnixTimeMilliseconds((long)value)
            .ToString(_format, CultureInfo.InvariantCulture);
    }
}
