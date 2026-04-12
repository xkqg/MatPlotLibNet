// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>Reproducible pseudo-random number generator with NumPy-compatible distribution samplers.
/// Instance-based: construct with a fixed <paramref name="seed"/> for reproducible output.</summary>
public sealed class NpRandom(int seed)
{
    private readonly Random _rng = new(seed);

    /// <summary>Draws <paramref name="n"/> samples from N(<paramref name="mu"/>, <paramref name="sigma"/>²)
    /// using the Box-Muller transform.</summary>
    /// <param name="mu">Mean of the normal distribution.</param>
    /// <param name="sigma">Standard deviation of the normal distribution.</param>
    /// <param name="n">Number of samples to draw.</param>
    /// <returns>Array of <paramref name="n"/> normally distributed values.</returns>
    public double[] Normal(double mu, double sigma, int n)
    {
        var result = new double[n];
        int i = 0;
        while (i < n)
        {
            // Box-Muller: generate a pair (z0, z1) from two uniform samples
            double u1 = 1.0 - _rng.NextDouble();   // avoid log(0)
            double u2 = _rng.NextDouble();
            double mag = sigma * Math.Sqrt(-2.0 * Math.Log(u1));
            result[i++] = mag * Math.Cos(2.0 * Math.PI * u2) + mu;
            if (i < n)
                result[i++] = mag * Math.Sin(2.0 * Math.PI * u2) + mu;
        }
        return result;
    }

    /// <summary>Draws <paramref name="n"/> samples from Uniform[<paramref name="low"/>, <paramref name="high"/>].</summary>
    /// <param name="low">Lower bound of the uniform distribution (inclusive).</param>
    /// <param name="high">Upper bound of the uniform distribution (inclusive).</param>
    /// <param name="n">Number of samples to draw.</param>
    /// <returns>Array of <paramref name="n"/> uniformly distributed values.</returns>
    public double[] Uniform(double low, double high, int n)
    {
        var result = new double[n];
        double range = high - low;
        for (int i = 0; i < n; i++)
            result[i] = low + _rng.NextDouble() * range;
        return result;
    }

    /// <summary>Draws <paramref name="n"/> samples from LogNormal(mu, sigma) = exp(Normal(mu, sigma)).</summary>
    /// <param name="mu">Mean of the underlying normal distribution.</param>
    /// <param name="sigma">Standard deviation of the underlying normal distribution.</param>
    /// <param name="n">Number of samples to draw.</param>
    /// <returns>Array of <paramref name="n"/> log-normally distributed positive values.</returns>
    public double[] Lognormal(double mu, double sigma, int n)
    {
        double[] normal = Normal(mu, sigma, n);
        for (int i = 0; i < n; i++)
            normal[i] = Math.Exp(normal[i]);
        return normal;
    }

    /// <summary>Draws <paramref name="n"/> integers uniformly from [<paramref name="low"/>, <paramref name="high"/>).</summary>
    /// <param name="low">Inclusive lower bound.</param>
    /// <param name="high">Exclusive upper bound.</param>
    /// <param name="n">Number of samples to draw.</param>
    /// <returns>Array of <paramref name="n"/> uniformly distributed integer values.</returns>
    public int[] Integers(int low, int high, int n)
    {
        var result = new int[n];
        for (int i = 0; i < n; i++)
            result[i] = _rng.Next(low, high);
        return result;
    }
}
