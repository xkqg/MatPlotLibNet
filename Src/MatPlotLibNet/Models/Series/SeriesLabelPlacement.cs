// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Where a labelled series shows its name.</summary>
/// <remarks>The ordinals are part of the public contract: members are only ever appended.</remarks>
public enum SeriesLabelPlacement
{
    /// <summary>The name is an entry in the axes legend. This is the default.</summary>
    Legend = 0,

    /// <summary>The name is drawn at the end of the line, in the line's colour, and the series has no legend entry
    /// on axes that draw such labels (Cartesian axes). Polar and 3D axes draw no end labels, so there the series
    /// keeps its legend entry.</summary>
    LineEnd = 1,
}
