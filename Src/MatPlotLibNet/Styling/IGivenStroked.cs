// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>A kind whose line keeps the width and the dash pattern its author gave as one value (<see cref="GivenStroke"/>): a line, a step,
/// a signal, an ECDF, a density, a regression line, an area's edge, a polar, ternary, 3-D or streaming line, an indicator's lines. Its given
/// width is the stroke's.</summary>
internal interface IGivenStroked : IWidthGiven
{
    /// <summary>The stroke its author gave, each part <see langword="null"/> while not given.</summary>
    GivenStroke Stroke { get; }

    double? IWidthGiven.GivenLineWidth => Stroke.Width;
}
