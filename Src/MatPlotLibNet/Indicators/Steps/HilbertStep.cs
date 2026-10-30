// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Steps;

/// <summary>What the Hilbert discriminator knows about a bar once it has taken it: the dominant cycle period, the phase of the cycle,
/// and the in-phase and quadrature components. All four are 0 for the first six bars, which the discriminator only feeds.</summary>
/// <param name="Period">The dominant cycle period in bars (between 6 and 50 once it has settled).</param>
/// <param name="Phase">The phase of the cycle, in degrees.</param>
/// <param name="I1">The in-phase component.</param>
/// <param name="Q1">The quadrature component.</param>
internal readonly record struct HilbertRow(double Period, double Phase, double I1, double Q1);

/// <summary>One step of the Ehlers homodyne discriminator: the recursion that <c>HilbertDiscriminate</c> folds over a series and the
/// streaming forms of MAMA, the instantaneous trend, the sine wave and the adaptive stochastic take one price at a time, so the
/// arithmetic exists once. It keeps seven values of each of the smoothed price, the detrender, the in-phase and the quadrature
/// component (the filter reaches back six bars) and the last value of each smoothing recursion.</summary>
internal struct HilbertStep
{
    private const int Lags = 7;

    private readonly double[] _smooth = new double[Lags];
    private readonly double[] _detrender = new double[Lags];
    private readonly double[] _i1 = new double[Lags];
    private readonly double[] _q1 = new double[Lags];
    private double _price1, _price2, _price3;
    private double _i2, _q2, _re, _im, _period, _phase;
    private int _count;

    /// <summary>A step before any price.</summary>
    public HilbertStep()
    {
    }

    /// <summary>Takes one price and returns what the discriminator knows about the bar.</summary>
    /// <param name="price">The price (a close, or the typical price of a bar).</param>
    internal HilbertRow Next(double price)
    {
        int i = _count++;
        double smooth = i >= 3
            ? (4 * price + 3 * _price1 + 2 * _price2 + _price3) / 10.0
            : price;
        _price3 = _price2;
        _price2 = _price1;
        _price1 = price;
        _smooth[i % Lags] = smooth;
        if (i < 6) return new HilbertRow(0, 0, 0, 0);

        double adjPrev = 0.075 * _period + 0.54;
        double detrender = (0.0962 * smooth + 0.5769 * _smooth[(i - 2) % Lags]
                          - 0.5769 * _smooth[(i - 4) % Lags] - 0.0962 * _smooth[(i - 6) % Lags]) * adjPrev;
        _detrender[i % Lags] = detrender;
        double q1 = (0.0962 * detrender + 0.5769 * _detrender[(i - 2) % Lags]
                   - 0.5769 * _detrender[(i - 4) % Lags] - 0.0962 * _detrender[(i - 6) % Lags]) * adjPrev;
        _q1[i % Lags] = q1;
        double i1 = _detrender[(i - 3) % Lags];
        _i1[i % Lags] = i1;

        double jI = (0.0962 * i1 + 0.5769 * _i1[(i - 2) % Lags]
                   - 0.5769 * _i1[(i - 4) % Lags] - 0.0962 * _i1[(i - 6) % Lags]) * adjPrev;
        double jQ = (0.0962 * q1 + 0.5769 * _q1[(i - 2) % Lags]
                   - 0.5769 * _q1[(i - 4) % Lags] - 0.0962 * _q1[(i - 6) % Lags]) * adjPrev;

        double i2Raw = i1 - jQ;
        double q2Raw = q1 + jI;
        double i2 = 0.2 * i2Raw + 0.8 * _i2;
        double q2 = 0.2 * q2Raw + 0.8 * _q2;

        double reRaw = i2 * _i2 + q2 * _q2;
        double imRaw = i2 * _q2 - q2 * _i2;
        _i2 = i2;
        _q2 = q2;
        _re = 0.2 * reRaw + 0.8 * _re;
        _im = 0.2 * imRaw + 0.8 * _im;

        double pNew;
        if (_im != 0 && _re != 0)
            pNew = 2.0 * Math.PI / Math.Atan(_im / _re);
        else
            pNew = _period;
        if (pNew > 1.5 * _period) pNew = 1.5 * _period;
        if (pNew < 0.67 * _period) pNew = 0.67 * _period;
        if (pNew < 6) pNew = 6;
        if (pNew > 50) pNew = 50;
        _period = 0.2 * pNew + 0.8 * _period;

        if (q1 != 0)
            _phase = (180.0 / Math.PI) * Math.Atan(i1 / q1);

        return new HilbertRow(_period, _phase, i1, q1);
    }
}
