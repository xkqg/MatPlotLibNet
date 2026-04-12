// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>Represents the computed data range for a set of axes.</summary>
/// <param name="XMin">Minimum X value.</param>
/// <param name="XMax">Maximum X value.</param>
/// <param name="YMin">Minimum Y value.</param>
/// <param name="YMax">Maximum Y value.</param>
public readonly record struct DataRange(double XMin, double XMax, double YMin, double YMax);
