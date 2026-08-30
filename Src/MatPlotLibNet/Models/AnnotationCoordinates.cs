// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>Which coordinate system an <see cref="Annotation"/>'s <see cref="Annotation.X"/>/<see cref="Annotation.Y"/>
/// are read in — matplotlib's <c>xycoords</c>. Append-only: the ordinal is serialized.</summary>
public enum AnnotationCoordinates
{
    /// <summary>Data coordinates — the axes' own units (the default).</summary>
    Data = 0,

    /// <summary>Fractions of the plot area: (0, 0) is the bottom-left corner, (1, 1) the top-right. A label
    /// placed this way stays where it is whatever the data limits do — matplotlib's <c>'axes fraction'</c>.</summary>
    AxesFraction = 1,
}
