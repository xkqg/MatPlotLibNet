// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>The words a count of data is refused in when it does not hold what a parameter needs (<see cref="DataCountExtensions"/>):
/// <c>The signal holds 3 samples; a segment of SegmentLength needs at least 4; set a shorter SegmentLength.</c></summary>
/// <param name="Holder">What holds the items, as the sentence opens: <c>The signal</c>, <c>A row</c>, <c>Group 'North'</c>.</param>
/// <param name="Unit">One item, in the singular: <c>sample</c>, <c>signal</c>, <c>observation</c>, <c>stop</c>.</param>
/// <param name="Taker">What needs the items, named by the parameter that asks: <c>a segment of SegmentLength</c>.</param>
/// <param name="Remedy">What the caller can do, the end of the sentence; <see langword="null"/> ends it at the count.</param>
internal readonly record struct DataNeed(string Holder, string Unit, string Taker, string? Remedy = null);
