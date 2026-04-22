// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators;

/// <summary>Result of <see cref="EhlersSineWave"/>: paired sine / lead-sine waves (cycle-mode
/// position) plus a per-bar cycle/trend flag.</summary>
public sealed record SineWaveResult(
    double[] SineWave,
    double[] LeadSine,
    bool[] IsCyclic) : IIndicatorResult;
