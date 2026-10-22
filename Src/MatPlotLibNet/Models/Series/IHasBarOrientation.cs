// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Series drawn as bars that stand up or lie down (<see cref="BarSeries"/>, <see cref="CountSeries"/>). Their
/// orientation decides which axis carries their categories (<see cref="BarOrientationExtensions"/>).</summary>
internal interface IHasBarOrientation
{
    /// <summary>Whether the bars stand up (categories along X) or lie down (categories along Y).</summary>
    BarOrientation Orientation { get; }
}
