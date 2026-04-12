// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.TickLocators;

/// <summary>
/// Returns exactly the provided positions that fall within [min, max].
/// Equivalent to matplotlib's <c>FixedLocator</c> — useful when tick positions are known in advance.
/// </summary>
public sealed class FixedLocator : ITickLocator
{
    private readonly double[] _positions;

    /// <summary>Initialises with the fixed tick positions.</summary>
    /// <param name="positions">The explicit tick values to use; positions outside the current axis range are silently ignored.</param>
    public FixedLocator(double[] positions) => _positions = positions;

    /// <inheritdoc />
    public double[] Locate(double min, double max) =>
        _positions.Where(p => p >= min && p <= max).ToArray();
}
