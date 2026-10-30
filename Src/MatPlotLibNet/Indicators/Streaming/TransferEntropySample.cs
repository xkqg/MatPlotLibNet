// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>One sample of the series a <see cref="StreamingTransferEntropy"/> reads: the value of the source, the value of the target
/// and, for a form that conditions on a third series, the value of that series, all at the same bar.</summary>
/// <param name="Source">The value of the source series (the one whose influence is measured).</param>
/// <param name="Target">The value of the target series (the one that is influenced).</param>
/// <param name="Condition">The value of the series conditioned on, or NaN when the form conditions on none.</param>
public readonly record struct TransferEntropySample(double Source, double Target, double Condition = double.NaN);
