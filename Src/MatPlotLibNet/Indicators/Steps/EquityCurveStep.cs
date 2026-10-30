// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Steps;

/// <summary>One step of the equity curve: the running total that the batch <see cref="EquityCurve"/> folds over its trade returns and
/// <see cref="Streaming.StreamingEquityCurve"/> takes one return at a time. The equity after a trade is the sum of all the returns so
/// far plus the starting capital (the sum is kept apart and the capital added to it, as the batch does).</summary>
internal struct EquityCurveStep
{
    private readonly double _startingCapital;
    private double _sum;

    /// <summary>A step from the given capital.</summary>
    /// <param name="startingCapital">The equity before the first trade.</param>
    internal EquityCurveStep(double startingCapital) => _startingCapital = startingCapital;

    /// <summary>Takes the return of one trade and returns the equity after it.</summary>
    /// <param name="tradeReturn">The profit or loss of the trade.</param>
    internal double Next(double tradeReturn)
    {
        _sum += tradeReturn;
        return _sum + _startingCapital;
    }
}
