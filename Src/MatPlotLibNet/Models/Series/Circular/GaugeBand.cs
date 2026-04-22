// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>One coloured band of a <see cref="GaugeSeries"/> dial. The band covers the
/// dial arc up to <paramref name="Threshold"/> (in the gauge's <c>[Min, Max]</c> value
/// space) in the given <paramref name="Color"/>. Adjacent bands share the previous band's
/// upper bound as their lower bound.</summary>
/// <param name="Threshold">Upper bound (in gauge value units) of the band's arc.</param>
/// <param name="Color">Fill colour painted onto the band's arc.</param>
public readonly record struct GaugeBand(double Threshold, Color Color);
