// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace MatPlotLibNet;

/// <summary>The messages the server sends a viewer about the chart other than a redrawn picture: a tooltip it computed for
/// them, another viewer's pointer, that a viewer moved the view or left. The page's script handles them, and it cannot
/// subscribe to the connection that carries them (the client holds it), so the client hands them over here and the
/// component passes them on to the page.</summary>
public interface IChartMessageSource
{
    /// <summary>Registers a callback for the messages. Several callbacks may be registered; disposing the returned token
    /// removes this one.</summary>
    /// <param name="handler">Called with the name of the client method the server called
    /// (<c>ReceiveTooltipContent</c>, <c>ReceiveCursor</c>, <c>ViewChanged</c>, <c>ViewerLeft</c>) and its arguments, in
    /// order, as a JSON array.</param>
    /// <returns>A token that removes the callback.</returns>
    IDisposable SubscribeMessages(Func<string, JsonElement, Task> handler);
}
