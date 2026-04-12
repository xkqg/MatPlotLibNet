// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Series that have a separately configurable border/edge color.</summary>
public interface IHasEdgeColor
{
    /// <summary>Border/edge color. When <see langword="null"/> the renderer uses its default stroke.</summary>
    Color? EdgeColor { get; set; }
}
