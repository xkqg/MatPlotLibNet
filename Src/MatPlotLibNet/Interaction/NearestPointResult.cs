// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Result of a nearest-point search: the closest data point to a hover position,
/// together with the series it belongs to and the pixel distance from the cursor.</summary>
public sealed record NearestPointResult(
    string SeriesLabel, int SeriesIndex,
    double DataX, double DataY,
    double PixelDistance);
