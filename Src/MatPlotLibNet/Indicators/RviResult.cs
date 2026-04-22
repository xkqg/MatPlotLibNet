// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators;

/// <summary>Result of <see cref="RelativeVigorIndex"/>: the RVI line and its 4-bar weighted
/// signal line. A cross of RVI over Signal flags a momentum shift.</summary>
public sealed record RviResult(double[] Rvi, double[] Signal) : IIndicatorResult;
