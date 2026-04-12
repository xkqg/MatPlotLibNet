// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Series whose fill or body has a configurable opacity.</summary>
public interface IHasAlpha
{
    /// <summary>Fill/body opacity in the range [0, 1] where 0 is fully transparent and 1 is fully opaque.</summary>
    double Alpha { get; set; }
}
