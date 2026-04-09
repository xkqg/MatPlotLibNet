// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Series that supports data normalization before colormap lookup.</summary>
public interface INormalizable
{
    /// <summary>Gets or sets the normalizer that maps data values to [0, 1], or null for linear normalization.</summary>
    INormalizer? Normalizer { get; set; }
}
