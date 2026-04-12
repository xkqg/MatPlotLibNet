// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators;

/// <summary>Result for the Ichimoku Cloud indicator containing all five computed lines.</summary>
public sealed record IchimokuResult(
    double[] TenkanSen,
    double[] KijunSen,
    double[] SenkouSpanA,
    double[] SenkouSpanB,
    double[] ChikouSpan) : IIndicatorResult;
