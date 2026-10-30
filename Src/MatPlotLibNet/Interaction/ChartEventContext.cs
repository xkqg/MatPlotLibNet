// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Who did what an <see cref="IChartEventHandler"/> is told about. A chart can be open in several browsers at once
/// and in a native window too; the context says which viewer an event came from.</summary>
/// <param name="ViewerId">The identity of the viewer: for a browser, the connection the server stamped on the event (a
/// client cannot choose it); <see langword="null"/> when the event did not come over a connection, as on a native
/// control.</param>
/// <param name="ViewerName">A short name for the viewer ("Viewer 2"), in the order the viewers joined the chart;
/// <see langword="null"/> when there is no viewer id.</param>
public readonly record struct ChartEventContext(string? ViewerId = null, string? ViewerName = null);
