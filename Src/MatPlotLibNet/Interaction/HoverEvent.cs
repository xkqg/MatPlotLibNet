// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>A client-initiated hover — the user's cursor is at data-space <c>(X, Y)</c>.
/// Non-mutating: the server routes the event to a user-registered handler that computes a
/// tooltip HTML fragment and returns it to the <em>originating</em> client only (not
/// broadcast to the SignalR group). This is the first per-caller response pattern in the
/// library — v1.2.0 only had group broadcast.</summary>
/// <param name="ChartId">Chart identifier — matches the figure's registry key.</param>
/// <param name="AxesIndex">Index of the target subplot within the figure.</param>
/// <param name="X">Hover point X in data space.</param>
/// <param name="Y">Hover point Y in data space.</param>
/// <param name="CallerConnectionId">SignalR connection ID of the originating client, stamped
/// server-side by <c>ChartHub.OnHover</c> so the client cannot spoof it. Null on events
/// constructed directly (e.g. in unit tests) before they reach the hub.</param>
public sealed record HoverEvent(
    string ChartId,
    int AxesIndex,
    double X,
    double Y,
    string? CallerConnectionId = null) : FigureNotificationEvent(ChartId, AxesIndex);
