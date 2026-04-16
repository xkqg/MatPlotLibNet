// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;

namespace MatPlotLibNet.Uno;

/// <summary>Fluent extension for wiring an <see cref="MplChartElement"/> to a server-side
/// event sink (e.g. SignalR). Interaction events are dispatched via the supplied async send
/// delegate instead of being applied locally to the figure.</summary>
public static class ServerInteractionExtensions
{
    /// <summary>Configures this element to route interaction events through the supplied
    /// <paramref name="sendAsync"/> delegate (typically <c>hubConnection.SendAsync</c>)
    /// instead of applying them locally.</summary>
    /// <returns>The same element instance for fluent chaining.</returns>
    public static MplChartElement WithServerInteraction(
        this MplChartElement element,
        Func<string, object, Task> sendAsync)
    {
        element.IsInteractive = true;
        element.ServerEventSink = SignalREventSink.Create(sendAsync);
        return element;
    }
}
