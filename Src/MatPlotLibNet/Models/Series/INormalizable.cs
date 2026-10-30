// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Series that supports data normalization before colormap lookup.</summary>
public interface INormalizable
{
    /// <summary>How a value is mapped onto the colour map's 0 to 1 range before the colour lookup (linear, log,
    /// and so on); <see langword="null"/> maps linearly.</summary>
    INormalizer? Normalizer { get; set; }
}
