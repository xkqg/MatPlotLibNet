// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet;

/// <summary>The one check a series whose two data arrays must agree in length makes, worded the same way
/// everywhere. Most XY series are validated by the verb that constructs them (<c>Axes.Plot</c> and its siblings
/// call a private length check before the constructor ever runs); <see cref="Models.Series.HexbinSeries"/>'s verb
/// does not, so it validates itself here instead, holding a caller who constructs it directly to the same
/// invariant. <see cref="Models.Series.RegressionSeries"/> deliberately does NOT use this check — see its
/// constructor's own doc comment for why.</summary>
internal static class ArrayLengthExtensions
{
    extension(double[] x)
    {
        /// <summary>Throws when <paramref name="y"/>'s length differs from this array's.</summary>
        internal void ThrowIfMisaligned(double[] y)
        {
            if (x.Length != y.Length)
                throw new ArgumentException($"Array lengths must match. Got {x.Length} and {y.Length}.");
        }
    }

    extension(int length)
    {
        /// <summary>Throws when <paramref name="other"/> differs from this length. The same check, worded
        /// identically, for the verbs whose two inputs are not both <c>double[]</c> (e.g. <c>string[]</c>
        /// categories against a <c>double[]</c> of values).</summary>
        internal void ThrowIfMisaligned(int other)
        {
            if (length != other)
                throw new ArgumentException($"Array lengths must match. Got {length} and {other}.");
        }
    }
}
