// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>What the opacity of a legend's frame (<see cref="Models.Legend.FrameAlpha"/>) applies to, a value of
/// <see cref="LegendLook.FrameOpacity"/>.</summary>
internal enum LegendFrameOpacity
{
    /// <summary>The fill alone; the outline stays opaque: the library's own themes.</summary>
    Fill,

    /// <summary>The whole frame, its fill and its outline, as matplotlib's <c>legend.framealpha</c> is the alpha of the frame patch,
    /// which a patch gives its edge too: the matplotlib themes.</summary>
    Patch,
}
