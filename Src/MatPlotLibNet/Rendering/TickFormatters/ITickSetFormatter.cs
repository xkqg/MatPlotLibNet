// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.TickFormatters;

/// <summary>A formatter whose labels depend on the other ticks of the axis or on the view it shows, as a matplotlib formatter reads them in
/// <c>set_locs</c> before it labels any: the axis hands it the ticks and the view once a draw and labels with what it returns.</summary>
/// <remarks>A separate internal interface, not a member of the public <see cref="ITickFormatter"/>, so that an application can still
/// write its own formatter with the one method.</remarks>
internal interface ITickSetFormatter : ITickFormatter
{
    /// <summary>The formatter for the labels of <paramref name="ticks"/> on an axis that shows <paramref name="view"/>.</summary>
    /// <param name="ticks">Every tick of the axis, ascending.</param>
    /// <param name="view">The ends of the view the axis shows, in its data units; <see langword="null"/> when none is known.</param>
    /// <param name="linThresh">The linear threshold of the symmetric-log scale the axis is drawn on; <see langword="null"/> for any other.</param>
    /// <param name="theme">The theme the axis is drawn under.</param>
    /// <returns>A formatter that labels each of them.</returns>
    ITickFormatter ForTicks(double[] ticks, Range1D? view, double? linThresh, Theme theme);
}
