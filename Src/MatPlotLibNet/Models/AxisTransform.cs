// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Models;

/// <summary>How a linked axis writes the primary axis' values in its own unit (<see cref="Axis.Link"/>): matplotlib's
/// <c>functions=(forward, inverse)</c> of <c>secondary_xaxis</c> and <c>secondary_yaxis</c>. <see cref="Forward"/> maps a value of
/// the primary axis to the value the linked axis shows, <see cref="Inverse"/> maps it back, so a tick of the linked axis stands
/// where the primary axis has the value it was mapped from.
/// <para>Make one with a factory: <see cref="Linear"/> (a scale and an offset: Celsius and Fahrenheit, degrees and radians),
/// <see cref="Reciprocal"/> (frequency and period, wavelength and frequency), <see cref="Logarithm"/>, <see cref="Power"/>, or
/// <see cref="Of"/> for a pair of functions of your own. Each refuses, as it is made, a number it cannot map with.</para></summary>
/// <remarks>A transform must run one way over the range it is given, as matplotlib requires. The named transforms say where
/// they do: a reciprocal on either side of 0, a logarithm above 0, a power on 0 and above (above 0 for a negative exponent).
/// When the primary axis shows a range a transform cannot place (a reciprocal across 0, a logarithm reaching 0 or below, a
/// function of your own that is not a number at an end of the range or maps both ends to one value), the linked axis draws no
/// ticks and says why in the figure's diagnostics; the library itself never throws while drawing. An exception thrown by a
/// function you pass to <see cref="Of"/> is yours and travels as it is. matplotlib checks none of this and draws whatever the
/// functions return.</remarks>
/// <example>
/// Fahrenheit on the right of a plot in Celsius, and the period along the top of a plot over frequency:
/// <code>
/// Plt.Create().AddSubPlot(1, 1, 1, ax => ax
///     .Plot(hours, celsius).SetYLabel("°C")
///     .WithAddRightYAxis(right => right.SetYLabel("°F").Link(AxisTransform.Linear(1.8, 32))));
///
/// Plt.Create().AddSubPlot(1, 1, 1, ax => ax
///     .Plot(frequency, power).SetXScale(AxisScale.Log)
///     .WithSecondaryXAxis(top => top.SetXLabel("period (s)").Link(AxisTransform.Reciprocal())));
/// </code>
/// </example>
public abstract class AxisTransform
{
    /// <summary>Only the library derives a transform: a kind of transform is placed, drawn and written by the library, so it is
    /// one of the kinds its factories make.</summary>
    internal AxisTransform() { }

    /// <summary>A linear transform: the primary value times <paramref name="scale"/>, plus <paramref name="offset"/>
    /// (Fahrenheit from Celsius is <c>Linear(1.8, 32)</c>, radians from degrees <c>Linear(Math.PI / 180)</c>).</summary>
    /// <param name="scale">What a primary value is multiplied by: a finite number other than 0; below 0 the linked axis runs the
    /// other way.</param>
    /// <param name="offset">What is added after the scale: a finite number, 0 by default.</param>
    /// <returns>The transform.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="scale"/> is 0 or not finite (a scale of 0 has no inverse), or
    /// <paramref name="offset"/> is not finite.</exception>
    /// <example>
    /// <code>
    /// var fahrenheit = AxisTransform.Linear(1.8, 32);
    /// double shown = fahrenheit.Forward(100);   // 212
    /// double back = fahrenheit.Inverse(32);     // 0
    /// </code>
    /// </example>
    public static AxisTransform Linear(double scale, double offset = 0)
    {
        if (!double.IsFinite(scale) || scale == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scale), scale, "The scale of a linear transform is a finite number other than 0; a scale of 0 maps every value to one and has no inverse.");
        }

        if (!double.IsFinite(offset))
        {
            throw new ArgumentOutOfRangeException(nameof(offset), offset, "The offset of a linear transform is a finite number.");
        }

        return new LinearAxisTransform(scale, offset);
    }

    /// <summary>A reciprocal: <paramref name="numerator"/> divided by the primary value (a period from a frequency is
    /// <c>Reciprocal()</c>, a wavelength in metres from a frequency in hertz <c>Reciprocal(299_792_458)</c>). It is its own
    /// inverse, and it places a range on either side of 0, never one across it.</summary>
    /// <param name="numerator">What is divided by the primary value: a finite number other than 0, 1 by default.</param>
    /// <returns>The transform.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="numerator"/> is 0 or not finite.</exception>
    /// <example>
    /// <code>
    /// var wavelength = AxisTransform.Reciprocal(299_792_458);   // metres from hertz
    /// double metres = wavelength.Forward(100e6);                  // 2.99792458
    /// </code>
    /// </example>
    public static AxisTransform Reciprocal(double numerator = 1)
    {
        if (!double.IsFinite(numerator) || numerator == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(numerator), numerator, "The numerator of a reciprocal is a finite number other than 0.");
        }

        return new ReciprocalAxisTransform(numerator);
    }

    /// <summary>A logarithm in <paramref name="base"/> of the primary value; its inverse raises <paramref name="base"/> to the
    /// value shown. It places a range above 0.</summary>
    /// <param name="base">The base: a finite number above 0 other than 1, 10 by default; below 1 the linked axis runs the other
    /// way.</param>
    /// <returns>The transform.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="base"/> is not above 0, is 1, or is not finite.</exception>
    /// <example>
    /// <code>
    /// var decades = AxisTransform.Logarithm();
    /// double shown = decades.Forward(1000);   // 3
    /// </code>
    /// </example>
    public static AxisTransform Logarithm(double @base = 10)
    {
        if (!double.IsFinite(@base) || @base <= 0 || @base == 1)
        {
            throw new ArgumentOutOfRangeException(nameof(@base), @base, "The base of a logarithm is a finite number above 0 other than 1.");
        }

        return new LogarithmAxisTransform(@base);
    }

    /// <summary>The primary value raised to <paramref name="exponent"/>; its inverse takes the root. It is defined on 0 and above
    /// (above 0 for a negative exponent), where it runs one way: below 0 it maps to no number.</summary>
    /// <param name="exponent">The exponent: a finite number other than 0; below 0 the linked axis runs the other way.</param>
    /// <returns>The transform.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="exponent"/> is 0 (every value would map to 1) or not
    /// finite.</exception>
    /// <example>
    /// <code>
    /// var area = AxisTransform.Power(2);   // the area of a square from its side
    /// double side = area.Inverse(9);         // 3
    /// </code>
    /// </example>
    public static AxisTransform Power(double exponent)
    {
        if (!double.IsFinite(exponent) || exponent == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(exponent), exponent, "The exponent of a power is a finite number other than 0; an exponent of 0 maps every value to 1 and has no inverse.");
        }

        return new PowerAxisTransform(exponent);
    }

    /// <summary>A transform of your own, given as the pair matplotlib's <c>functions=(forward, inverse)</c> is. A function is code,
    /// which a JSON document cannot carry: a figure written to JSON leaves the link out, and the axis reads back unlinked.</summary>
    /// <param name="forward">From a value of the primary axis to the value the linked axis shows. It must run one way over the
    /// range the primary axis shows.</param>
    /// <param name="inverse">From a value the linked axis shows back to the primary axis' value: the inverse of
    /// <paramref name="forward"/>.</param>
    /// <returns>The transform.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="forward"/> or <paramref name="inverse"/> is
    /// <see langword="null"/>.</exception>
    /// <example>
    /// Kelvin from degrees Celsius, as a pair of functions of your own:
    /// <code>
    /// var kelvin = AxisTransform.Of(celsius => celsius + 273.15, k => k - 273.15);
    /// </code>
    /// </example>
    public static AxisTransform Of(Func<double, double> forward, Func<double, double> inverse)
    {
        ArgumentNullException.ThrowIfNull(forward);
        ArgumentNullException.ThrowIfNull(inverse);
        return new FunctionAxisTransform(forward, inverse);
    }

    /// <summary>The value the linked axis shows for <paramref name="value"/> of the primary axis.</summary>
    /// <param name="value">A value of the primary axis.</param>
    /// <returns>The value in the linked axis' unit; not a number (or an infinity) where the transform is not defined.</returns>
    public abstract double Forward(double value);

    /// <summary>The value of the primary axis that the linked axis shows as <paramref name="value"/>: where a tick of the linked
    /// axis stands.</summary>
    /// <param name="value">A value in the linked axis' unit.</param>
    /// <returns>The value of the primary axis; not a number (or an infinity) where the transform is not defined.</returns>
    public abstract double Inverse(double value);

    /// <summary>The range the linked axis shows when the primary axis shows <paramref name="primary"/>: the images of its two ends,
    /// low to high, and whether the transform runs up or down over it. <see langword="null"/> when the transform cannot place the
    /// range: an end maps to no finite number, both ends map to one value, or the transform breaks between them (a reciprocal
    /// across 0).</summary>
    /// <param name="primary">The primary axis' range, in either order (an axis drawn high to low shows the same values).</param>
    /// <returns>The linked range, or <see langword="null"/>.</returns>
    internal LinkedRange? ImageOf(Range1D primary)
    {
        var interval = primary.Interval;
        double low = Forward(interval.Lo), high = Forward(interval.Hi);
        if (!double.IsFinite(low) || !double.IsFinite(high) || low == high || BreaksBetween(interval.Lo, interval.Hi))
        {
            return null;
        }

        return low < high ? new LinkedRange(new Range1D(low, high), Increasing: true) : new LinkedRange(new Range1D(high, low), Increasing: false);
    }

    /// <summary>Whether the transform breaks between <paramref name="low"/> and <paramref name="high"/>, though both ends map to
    /// finite numbers. None does but a reciprocal, across its pole at 0; a function of your own is taken to run one way, as
    /// matplotlib takes it.</summary>
    /// <param name="low">The low end of the range.</param>
    /// <param name="high">The high end of the range.</param>
    private protected virtual bool BreaksBetween(double low, double high) => false;
}

/// <summary><c>scale × value + offset</c> (<see cref="AxisTransform.Linear"/>).</summary>
/// <param name="scale">A finite number other than 0.</param>
/// <param name="offset">A finite number.</param>
internal sealed class LinearAxisTransform(double scale, double offset) : AxisTransform, IJsonDescribed<AxisTransformDto>
{
    /// <inheritdoc/>
    public override double Forward(double value) => scale * value + offset;

    /// <inheritdoc/>
    public override double Inverse(double value) => (value - offset) / scale;

    /// <inheritdoc/>
    AxisTransformDto IJsonDescribed<AxisTransformDto>.Describe() => new LinearAxisTransformDto(scale, offset);
}

/// <summary><c>numerator / value</c>, its own inverse (<see cref="AxisTransform.Reciprocal"/>).</summary>
/// <param name="numerator">A finite number other than 0.</param>
internal sealed class ReciprocalAxisTransform(double numerator) : AxisTransform, IJsonDescribed<AxisTransformDto>
{
    /// <inheritdoc/>
    public override double Forward(double value) => numerator / value;

    /// <inheritdoc/>
    public override double Inverse(double value) => numerator / value;

    /// <summary>The pole at 0 lies strictly between the ends: each end maps to a finite number, and the image between them runs
    /// through both infinities. An end at 0 maps to an infinity itself.</summary>
    private protected override bool BreaksBetween(double low, double high) => low < 0 && high > 0;

    /// <inheritdoc/>
    AxisTransformDto IJsonDescribed<AxisTransformDto>.Describe() => new ReciprocalAxisTransformDto(numerator);
}

/// <summary>The logarithm in a base (<see cref="AxisTransform.Logarithm"/>): the decimal logarithm of the value divided by that of
/// the base, so a decimal logarithm of a power of ten is exact.</summary>
/// <param name="base">A finite number above 0 other than 1.</param>
internal sealed class LogarithmAxisTransform(double @base) : AxisTransform, IJsonDescribed<AxisTransformDto>
{
    private readonly double _log10Base = Math.Log10(@base);

    /// <inheritdoc/>
    public override double Forward(double value) => Math.Log10(value) / _log10Base;

    /// <inheritdoc/>
    public override double Inverse(double value) => Math.Pow(@base, value);

    /// <inheritdoc/>
    AxisTransformDto IJsonDescribed<AxisTransformDto>.Describe() => new LogarithmAxisTransformDto(@base);
}

/// <summary>The value raised to an exponent (<see cref="AxisTransform.Power"/>), on 0 and above only, where it runs one way: below
/// 0 neither way maps to a number.</summary>
/// <param name="exponent">A finite number other than 0.</param>
internal sealed class PowerAxisTransform(double exponent) : AxisTransform, IJsonDescribed<AxisTransformDto>
{
    /// <inheritdoc/>
    public override double Forward(double value) => value < 0 ? double.NaN : Math.Pow(value, exponent);

    /// <inheritdoc/>
    public override double Inverse(double value) => value < 0 ? double.NaN : Math.Pow(value, 1 / exponent);

    /// <inheritdoc/>
    AxisTransformDto IJsonDescribed<AxisTransformDto>.Describe() => new PowerAxisTransformDto(exponent);
}

/// <summary>A pair of functions of the caller's own (<see cref="AxisTransform.Of"/>). It does not implement
/// <see cref="IJsonDescribed{TDescription}"/>: a function is code, which a JSON document cannot carry, so the link is left out of the
/// JSON and the axis reads back unlinked.</summary>
/// <param name="forward">From a primary value to the value shown.</param>
/// <param name="inverse">Back from the value shown to the primary value.</param>
internal sealed class FunctionAxisTransform(Func<double, double> forward, Func<double, double> inverse) : AxisTransform
{
    /// <inheritdoc/>
    public override double Forward(double value) => forward(value);

    /// <inheritdoc/>
    public override double Inverse(double value) => inverse(value);
}
