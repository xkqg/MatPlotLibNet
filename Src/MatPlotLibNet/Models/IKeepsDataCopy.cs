// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>An object that keeps the last copy of its data it was handed (<see cref="DataSnapshot.Of"/>), because a draw reads a
/// copy of it twice: for the kept drawing of its subplot and for an estimate the object keeps (<see cref="Series.KeptEstimate{T}"/>,
/// which takes only a series that keeps its copy). While the object holds the same data it is handed the same copy, so the two
/// share one. An object nothing else reads a copy of in a draw does not keep one and is copied afresh.</summary>
internal interface IKeepsDataCopy
{
    /// <summary>The field, marked <see cref="RenderCacheAttribute"/>, that keeps the last copy; <see langword="null"/> before the
    /// first.</summary>
    ref DataSnapshot? DataCopy { get; }
}
