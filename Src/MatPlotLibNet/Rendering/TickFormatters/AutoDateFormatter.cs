// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Rendering.TickFormatters;

/// <summary>Formats date tick values using a format string chosen automatically from the
/// <see cref="AutoDateLocator.ChosenInterval"/> of a paired <see cref="AutoDateLocator"/>.</summary>
/// <remarks>
/// Format selection:
/// <list type="table">
///   <listheader><term>Interval</term><description>Format</description></listheader>
///   <item><term>Years</term><description><c>"yyyy"</c></description></item>
///   <item><term>Months</term><description><c>"MMM yyyy"</c></description></item>
///   <item><term>Weeks / Days</term><description><c>"MMM dd"</c></description></item>
///   <item><term>Hours / Minutes</term><description><c>"HH:mm"</c></description></item>
///   <item><term>Seconds</term><description><c>"HH:mm:ss"</c></description></item>
/// </list>
/// Must be used with the same <see cref="AutoDateLocator"/> instance that produces the tick positions,
/// so that <see cref="AutoDateLocator.ChosenInterval"/> is already set when <see cref="Format"/> is called.
/// </remarks>
public sealed class AutoDateFormatter : ITickFormatter
{
    private readonly AutoDateLocator _locator;

    /// <summary>Creates an <see cref="AutoDateFormatter"/> that reads the interval from <paramref name="locator"/>.</summary>
    /// <param name="locator">The <see cref="AutoDateLocator"/> paired with this formatter.</param>
    public AutoDateFormatter(AutoDateLocator locator) => _locator = locator;

    /// <inheritdoc />
    public string Format(double value)
    {
        var dt = DateTime.FromOADate(value);
        return _locator.ChosenInterval switch
        {
            DateInterval.Years   => dt.ToString("yyyy",       CultureInfo.InvariantCulture),
            DateInterval.Months  => dt.ToString("MMM yyyy",   CultureInfo.InvariantCulture),
            DateInterval.Weeks   => dt.ToString("MMM dd",     CultureInfo.InvariantCulture),
            DateInterval.Days    => dt.ToString("MMM dd",     CultureInfo.InvariantCulture),
            DateInterval.Hours   => dt.ToString("HH:mm",      CultureInfo.InvariantCulture),
            DateInterval.Minutes => dt.ToString("HH:mm",      CultureInfo.InvariantCulture),
            DateInterval.Seconds => dt.ToString("HH:mm:ss",   CultureInfo.InvariantCulture),
            _                    => dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        };
    }
}
