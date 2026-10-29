// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace MatPlotLibNet;

/// <summary>The distribution a sample is compared with in a probability plot
/// (<see cref="DistributionAxesExtensions.ProbabilityPlot"/>): the one whose quantiles the sorted sample is plotted against.
/// The value you get from <c>default</c> is <see cref="Normal"/>, the distribution a Q-Q plot
/// (<see cref="DistributionAxesExtensions.QQPlot"/>) always compares with.
/// <para>Make one with a factory: <see cref="Normal"/>, <see cref="Exponential"/>, <see cref="Uniform"/>,
/// <see cref="LogNormal"/>, <see cref="FittedLogNormal"/>, <see cref="StudentT"/>, or <see cref="Of"/> for a distribution of
/// your own. A reference is a standard member of its family (location 0 and scale 1 where it has them): the reference line of
/// the plot is a least-squares fit, so a sample that differs from the reference by location and scale still lies on it.</para></summary>
public readonly record struct QQReference
{
    /// <summary>The X axis title of a plot against the normal distribution, and the start of every other.</summary>
    private const string TheoreticalQuantiles = "Theoretical quantiles";

    private readonly string? _name;
    private readonly Func<double, double>? _quantile;

    private QQReference(string name, Func<double, double> quantile)
    {
        _name = name;
        _quantile = quantile;
    }

    /// <summary>The standard normal distribution, the one a Q-Q plot compares with. It is what <c>default</c> means.</summary>
    public static QQReference Normal => default;

    /// <summary>The exponential distribution with rate 1 (scipy's <c>expon</c>): the waiting time to the next event of a
    /// steady process. A sample of waiting times lies on the reference line when the events are memoryless.</summary>
    public static QQReference Exponential => new("exponential", Numerics.Exponential.Quantile);

    /// <summary>The uniform distribution on 0 to 1 (scipy's <c>uniform</c>): its quantile is the probability itself, so the
    /// plot shows the sample against the plotting positions.</summary>
    public static QQReference Uniform => new("uniform", static p => p);

    /// <summary>The log-normal distribution with the given shape (scipy's <c>lognorm(s)</c>): the exponential of a normal
    /// variable with standard deviation <paramref name="sigma"/>. A sample whose logarithms are normal lies on the reference
    /// line when <paramref name="sigma"/> is the standard deviation of those logarithms; <see cref="FittedLogNormal"/> takes
    /// it from the sample.</summary>
    /// <param name="sigma">The standard deviation of the logarithm, a finite number above 0.</param>
    /// <returns>The reference.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="sigma"/> is not a finite number above 0.</exception>
    public static QQReference LogNormal(double sigma)
    {
        if (!double.IsFinite(sigma) || sigma <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sigma), sigma, "The shape of a log-normal distribution is a finite number above 0.");
        }

        return new($"log-normal, σ = {Shown(sigma)}", p => Math.Exp(sigma * Numerics.StandardNormal.Quantile(p)));
    }

    /// <summary>The log-normal distribution whose shape is taken from <paramref name="sample"/>: the standard deviation of the
    /// logarithms of its observations, divided by their number (the maximum-likelihood estimate, scipy's
    /// <c>lognorm.fit(x, floc=0)</c>). A missing observation (NaN) and an infinity are left out.</summary>
    /// <param name="sample">The observations, each above 0, with some spread in their logarithms.</param>
    /// <returns>The reference.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sample"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">An observation is not above 0, or the logarithms of the observations have no spread
    /// (fewer than two different observations).</exception>
    public static QQReference FittedLogNormal(double[] sample)
    {
        ArgumentNullException.ThrowIfNull(sample);
        double[] observations = sample.DrawableValues();
        foreach (double value in observations)
        {
            if (value <= 0)
            {
                throw new ArgumentException(
                    $"The sample has an observation of {value.ToString(CultureInfo.InvariantCulture)} that is not above 0; a log-normal is fitted to the logarithms of the observations.",
                    nameof(sample));
            }
        }

        double[] logs = [.. observations.Select(value => Math.Log(value))];
        double mean = logs.Sum() / logs.Length;
        double sigma = Math.Sqrt(logs.Sum(v => (v - mean) * (v - mean)) / logs.Length);
        if (!(sigma > 0))
        {
            throw new ArgumentException("The sample's logarithms have no spread, so a log-normal cannot be fitted to it.", nameof(sample));
        }

        return LogNormal(sigma);
    }

    /// <summary>Student's t distribution with the given degrees of freedom (scipy's <c>t(df)</c>): the normal distribution with
    /// heavier tails, the heavier the fewer the degrees. With 1 it is the Cauchy distribution. A sample with heavy tails curves
    /// away from a normal reference and lies closer to a t reference with few degrees.</summary>
    /// <param name="degreesOfFreedom">The degrees of freedom, a finite number above 0 (not necessarily whole).</param>
    /// <returns>The reference.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="degreesOfFreedom"/> is not a finite number above 0.</exception>
    public static QQReference StudentT(double degreesOfFreedom)
    {
        if (!double.IsFinite(degreesOfFreedom) || degreesOfFreedom <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(degreesOfFreedom), degreesOfFreedom, "The degrees of freedom of a Student t distribution are a finite number above 0.");
        }

        return new($"Student t, {Shown(degreesOfFreedom)} df", p => Numerics.StudentT.Quantile(p, degreesOfFreedom));
    }

    /// <summary>A distribution of your own, given by its quantile function (the inverse of its cumulative distribution
    /// function).</summary>
    /// <param name="name">What the distribution is called; the plot names it on its X axis.</param>
    /// <param name="quantile">The quantile function: the value below which a fraction <c>p</c> of the distribution lies, for
    /// <c>p</c> between 0 and 1 (never 0 or 1 themselves). It must give a finite number there.</param>
    /// <returns>The reference.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/>, empty or white space.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="quantile"/> is <see langword="null"/>.</exception>
    public static QQReference Of(string name, Func<double, double> quantile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(quantile);
        return new(name, quantile);
    }

    /// <summary>What the distribution is called: <c>normal</c>, <c>exponential</c>, <c>uniform</c>, <c>log-normal, σ = 1</c>,
    /// <c>Student t, 5 df</c>, or the name given to <see cref="Of"/>.</summary>
    public string Name => _name ?? "normal";

    /// <summary>The value below which a fraction <paramref name="p"/> of the distribution lies.</summary>
    internal double Quantile(double p) => _quantile is null ? Numerics.StandardNormal.Quantile(p) : _quantile(p);

    /// <summary>The title of the X axis of a plot against this reference: the normal distribution keeps the plain
    /// "Theoretical quantiles" of a Q-Q plot; every other reference adds its name.</summary>
    internal string XAxisTitle => _quantile is null ? TheoreticalQuantiles : $"{TheoreticalQuantiles} ({Name})";

    private static string Shown(double number) => number.ToString("0.###", CultureInfo.InvariantCulture);
}
