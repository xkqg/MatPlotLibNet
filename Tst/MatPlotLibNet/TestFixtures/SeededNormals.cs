// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests;

/// <summary>A generator with no libm: a 64-bit linear congruential generator, a uniform from the top 53 bits and a normal as the sum of
/// 12 uniforms less 6, so the same seed gives the same numbers on every platform.</summary>
/// <param name="seed">The seed.</param>
internal sealed class SeededNormals(ulong seed)
{
    private ulong _state = seed;

    /// <summary>The next uniform number in [0, 1).</summary>
    /// <returns>The number.</returns>
    public double Uniform()
    {
        _state = unchecked(_state * 6364136223846793005UL + 1442695040888963407UL);
        return (_state >> 11) / (double)(1UL << 53);
    }

    /// <summary>The next number of a normal with mean 0 and standard deviation 1.</summary>
    /// <returns>The number.</returns>
    public double Normal()
    {
        double sum = 0;
        for (int i = 0; i < 12; i++)
        {
            sum += Uniform();
        }

        return sum - 6;
    }
}
