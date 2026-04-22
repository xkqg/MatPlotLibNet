// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>Dimensions of a two-dimensional grid or matrix <c>(Rows × Cols)</c>. Returned by
/// <see cref="Mat.Shape"/> and <see cref="Builders.SubplotMosaicParser.GetDimensions"/>.</summary>
/// <param name="Rows">Number of rows.</param>
/// <param name="Cols">Number of columns.</param>
public readonly record struct MatShape(int Rows, int Cols);
