// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>A part of a figure that reports its changes: <see cref="LatestChange"/> is the <see cref="ChangeClock"/> mark of the
/// newest change to it or to anything it holds, <see cref="long.MaxValue"/> when it holds something that cannot tell (a series
/// a caller wrote against <see cref="Series.ISeries"/> directly), which therefore always counts as changed.</summary>
internal interface IChangeTracked
{
    /// <summary>The mark of the newest change to this part or to anything it holds.</summary>
    long LatestChange { get; }
}
