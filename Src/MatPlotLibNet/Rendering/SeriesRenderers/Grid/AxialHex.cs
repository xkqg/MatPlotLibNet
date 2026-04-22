// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>Axial (Q, R) coordinate of a flat-top hex cell. <see cref="Q"/> advances across
/// columns (east/west); <see cref="R"/> advances down rows (south) with a skew determined by
/// <see cref="Q"/>. Used as a dictionary key by <c>HexGrid.ComputeHexBins</c> for the hexagonal
/// binning of scatter data in <see cref="Models.Series.HexbinSeries"/>.</summary>
/// <param name="Q">Column axis (flat-top convention).</param>
/// <param name="R">Row axis (flat-top convention).</param>
internal readonly record struct AxialHex(int Q, int R);
