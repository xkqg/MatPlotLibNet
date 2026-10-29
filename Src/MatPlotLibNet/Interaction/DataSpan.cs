// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>One axis' range as a pair of data values: the low end and the high end. Used both as the range a pan
/// starts from and as the range it moves to, so <see cref="PanEvent"/> passes one value instead of two loose
/// doubles and keeps its arithmetic within the four-parameter limit.</summary>
/// <param name="Min">The low end of the range, in data units.</param>
/// <param name="Max">The high end of the range, in data units.</param>
internal readonly record struct DataSpan(double Min, double Max);
