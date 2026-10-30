// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Steps;

/// <summary>One step of the drawdown: the running total that the batch <see cref="DrawDown"/> folds over its equity curve and
/// <see cref="Streaming.StreamingDrawDown"/> takes one equity value at a time. It keeps the highest equity so far (the first value
/// is the first peak) and says how far the equity is below it, in percent of the peak (0 when the peak is not above 0).</summary>
internal struct DrawDownStep
{
    private double _peak;
    private bool _started;

    /// <summary>Takes one equity value and returns the drawdown in percent.</summary>
    /// <param name="equity">The equity.</param>
    internal double Next(double equity)
    {
        if (!_started)
        {
            _started = true;
            _peak = equity;
        }

        if (equity > _peak) _peak = equity;
        return _peak > 0 ? (_peak - equity) / _peak * 100 : 0;
    }
}
