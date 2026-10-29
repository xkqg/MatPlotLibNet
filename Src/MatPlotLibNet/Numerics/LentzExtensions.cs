// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>The one guard of the modified Lentz method for continued fractions, which <see cref="Gamma"/> and <see cref="Beta"/>
/// both use: a term that is zero, or so near zero that its reciprocal would overflow, is replaced by a tiny number, so the
/// division that follows is finite (Press et al., "Numerical Recipes", the routines <c>gcf</c> and <c>betacf</c>).</summary>
internal static class LentzExtensions
{
    /// <summary>The number that takes the place of a term below it in magnitude: 1e-300, far under any term a fraction has and
    /// far over the smallest double.</summary>
    internal const double Tiny = 1e-300;

    extension(double term)
    {
        /// <summary>The term itself, or <see cref="Tiny"/> when its magnitude is below it (whatever its sign). A missing number
        /// is none of those and stays missing.</summary>
        internal double AwayFromZero() => Math.Abs(term) < Tiny ? Tiny : term;
    }
}
