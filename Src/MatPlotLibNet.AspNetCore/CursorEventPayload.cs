// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.AspNetCore;

/// <summary>Wire DTO for <see cref="ChartHub.OnCursor"/>: where a viewer's pointer is, in the data units of the axes it is
/// over. The viewer's identity is not part of it: the hub knows which connection the message came in on.</summary>
/// <param name="ChartId">The chart the pointer is over.</param>
/// <param name="AxesIndex">The axes of the chart the pointer is over.</param>
/// <param name="X">The pointer's X in data units.</param>
/// <param name="Y">The pointer's Y in data units.</param>
public sealed record CursorEventPayload(string ChartId, int AxesIndex, double X, double Y);
