// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Interaction;

/// <summary>Notification event carrying a pinned data point annotation.
/// Non-mutating — routes to a user handler, does not change the figure.</summary>
public sealed record DataCursorEvent(
    string ChartId,
    int AxesIndex,
    PinnedAnnotation Annotation) : FigureNotificationEvent(ChartId, AxesIndex);
