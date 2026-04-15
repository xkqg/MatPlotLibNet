// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>A client-initiated brush selection — the user Shift+dragged a rubber-band rectangle
/// over the plot area. Carries the data-space rectangle <c>(X1, Y1) → (X2, Y2)</c>. Non-mutating:
/// the server observes the selection and routes it to a user-registered handler (typically to
/// log, filter, or trigger downstream work). The figure is not re-rendered.</summary>
public sealed record BrushSelectEvent(
    string ChartId,
    int AxesIndex,
    double X1,
    double Y1,
    double X2,
    double Y2) : FigureNotificationEvent(ChartId, AxesIndex);
