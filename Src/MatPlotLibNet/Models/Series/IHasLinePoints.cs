// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>A series whose label can be drawn at the end of its line because the library can read the line: a line series and a
/// streaming line series. An end label is drawn, and the series leaves the legend for it, only for such a series, so a series of
/// the caller's that asks for an end label (<see cref="IHasLabelPlacement"/>) keeps its name in the legend. It lives here and not on
/// <see cref="IHasLabelPlacement"/> so that an application can still implement, proxy or mock the public interface.</summary>
internal interface IHasLinePoints : IHasLabelPlacement
{
    /// <summary>The points of the line as the end label reads them: one consistent snapshot of the x and y values,
    /// so the label is placed at the end of the line that is drawn.</summary>
    internal LinePoints LinePoints();
}
