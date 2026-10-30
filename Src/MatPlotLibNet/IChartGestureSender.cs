// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace MatPlotLibNet;

/// <summary>Sends a gesture a viewer made to the server that holds the chart. A live chart whose SVG is drawn by a script
/// in the page has no connection of its own to the server: the connection belongs to the client the component holds, and
/// the script reaches it through this interface, so the page and the component share one connection per viewer instead of
/// each opening one.
/// <para>Separate from <see cref="IChartSubscriptionClient"/> because most clients only receive: a class that
/// subscribes needs nothing here, and one that also forwards gestures opts in by implementing this too.</para></summary>
public interface IChartGestureSender
{
    /// <summary>Sends one gesture. The payload is the JSON the page's script wrote; the server binds it to the event the
    /// hub method takes and stamps who sent it.</summary>
    /// <param name="method">The hub method that carries the event (<c>OnZoom</c>, <c>OnPan</c>, <c>OnCursor</c>, …).</param>
    /// <param name="payload">The event as the script wrote it.</param>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>A task that completes when the message has been handed to the connection; a connection that is not up
    /// completes it without sending.</returns>
    Task SendGestureAsync(string method, JsonElement payload, CancellationToken ct = default);
}
