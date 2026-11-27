// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Serialization;

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
public sealed class AutoDateFormatter : ITickFormatter, IJsonDescribed<TickFormatterDto>
{
    /// <summary>The locator whose interval the formats follow; <see langword="null"/> over a locator that chooses none.</summary>
    private readonly AutoDateLocator? _locator;

    /// <summary>Creates an <see cref="AutoDateFormatter"/> that reads the interval from <paramref name="locator"/>.</summary>
    /// <param name="locator">The <see cref="AutoDateLocator"/> paired with this formatter.</param>
    public AutoDateFormatter(AutoDateLocator locator) => _locator = locator;

    /// <summary>The formatter of a date axis whose ticks <paramref name="locator"/> places: one that reads its interval when it is an
    /// <see cref="AutoDateLocator"/>, otherwise one that writes every tick as a day, as matplotlib's <c>AutoDateFormatter</c> does once
    /// <c>set_major_locator</c> points it at a locator that chooses no interval (no <c>_get_unit</c>: the scale of a day).</summary>
    /// <param name="locator">The locator that places the axis' ticks.</param>
    /// <returns>The formatter.</returns>
    internal static AutoDateFormatter Over(ITickLocator locator) => locator is AutoDateLocator date ? new(date) : new();

    /// <summary>Creates a formatter that writes every tick as a day (<see cref="Over"/>).</summary>
    private AutoDateFormatter() { }

    /// <inheritdoc />
    TickFormatterDto IJsonDescribed<TickFormatterDto>.Describe() => new AutoDateTickFormatterDto();

    /// <inheritdoc />
    /// <remarks>A value no date can be made of is written as the number.</remarks>
    public string Format(double value) => value.ToOleDateText((_locator?.ChosenInterval ?? DateInterval.Days) switch
    {
        DateInterval.Years   => "yyyy",
        DateInterval.Months  => "MMM yyyy",
        DateInterval.Weeks   => "MMM dd",
        DateInterval.Days    => "MMM dd",
        DateInterval.Hours   => "HH:mm",
        DateInterval.Minutes => "HH:mm",
        _                    => "HH:mm:ss" // Seconds: the finest, what no coarser member claims
    });
}
