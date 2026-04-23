// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators;

/// <summary>Result of <see cref="KlingerVolumeOscillator"/>: the KVO oscillator (fast EMA −
/// slow EMA of Klinger's volume-force) and its EMA signal line. Crossovers of
/// <see cref="Kvo"/> vs <see cref="Signal"/> are the standard buy / sell signals.</summary>
/// <param name="Kvo">Klinger Volume Oscillator values (fast EMA − slow EMA of volume force).</param>
/// <param name="Signal">EMA of <see cref="Kvo"/>, used for crossover detection.</param>
public readonly record struct KlingerResult(double[] Kvo, double[] Signal) : IIndicatorResult;
