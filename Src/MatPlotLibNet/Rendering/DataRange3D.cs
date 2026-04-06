// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>Represents the 3D data range bounds.</summary>
public readonly record struct DataRange3D(double XMin, double XMax, double YMin, double YMax, double ZMin, double ZMax);
