// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Interaction;

/// <summary>Notification event carrying the X-range selected by an Alt+drag span gesture.
/// Non-mutating — routes to a user handler, does not change axis limits.</summary>
public sealed record SpanSelectEvent(
    string ChartId,
    int AxesIndex,
    double XMin,
    double XMax) : FigureNotificationEvent(ChartId, AxesIndex);
