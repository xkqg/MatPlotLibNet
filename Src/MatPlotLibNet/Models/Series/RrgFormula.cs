// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>RS-Ratio / RS-Momentum calculation model for <see cref="RelativeRotationSeries"/>.</summary>
/// <remarks>Ordinals are explicit and append-only — never reorder or remove values.
/// See <c>EnumOrdinalContractTests</c>.</remarks>
public enum RrgFormula
{
    /// <summary>Canonical JdK dual-EMA reconstruction (default).
    /// <c>RsRatio = EMA(RS, short) / EMA(RS, long) × 100</c>;
    /// <c>RsMomentum = EMA(RsRatio, short) / EMA(RsRatio, long) × 100</c>.
    /// No mean-reversion assumption — suitable for trending assets such as crypto.</summary>
    DualEma = 0,

    /// <summary>Z-score normalization around 100.
    /// <c>RsRatio = 100 + (RS − SMA_w) / StdDev_w</c>;
    /// momentum uses ROC then the same z-score wrap. Better for mean-stationary regimes.</summary>
    ZScore = 1,

    /// <summary>Log-return momentum with z-score wrap.
    /// <c>RsMomentum = ln(1 + r_long) − ln(1 + r_short)</c> where <c>r_k = pct_change(RS, k)</c>,
    /// then z-score normalized. RsRatio uses the same z-score as <see cref="ZScore"/>.</summary>
    LogReturn = 2,
}
