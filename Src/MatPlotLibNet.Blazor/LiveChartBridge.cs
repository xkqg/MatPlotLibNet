// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.JSInterop;

namespace MatPlotLibNet.Blazor;

/// <summary>What the page's script calls back into: a gesture to send through the client's connection, and the moment its
/// chart has gone idle, so a redrawn picture that was held back can be mounted.</summary>
/// <param name="sender">The client the gestures are sent through.</param>
/// <param name="onIdle">Called when the chart has gone idle.</param>
internal sealed class LiveChartBridge(IChartGestureSender sender, Func<Task> onIdle)
{
    /// <summary>Sends one gesture the script wrote.</summary>
    /// <param name="method">The hub method that carries it.</param>
    /// <param name="payload">The event as the script wrote it.</param>
    [JSInvokable]
    public Task Send(string method, JsonElement payload) => sender.SendGestureAsync(method, payload);

    /// <summary>The chart went idle.</summary>
    [JSInvokable]
    public Task OnIdle() => onIdle();
}
