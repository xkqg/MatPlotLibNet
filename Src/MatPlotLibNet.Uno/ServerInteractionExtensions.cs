// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;

namespace MatPlotLibNet.Uno;

/// <summary>Fluent extension for wiring an <see cref="MplChartElement"/> or an <see cref="MplStreamingChartElement"/> to a
/// server-side event sink (e.g. SignalR). Interaction events are dispatched via the supplied async send
/// delegate instead of being applied locally to the figure.</summary>
public static class ServerInteractionExtensions
{
    /// <param name="element">The element to wire.</param>
    /// <typeparam name="TControl">The element's own type, so the call hands it back for chaining.</typeparam>
    extension<TControl>(TControl element) where TControl : MplChartSurface
    {
        /// <summary>Configures this element to route interaction events through the supplied
        /// <paramref name="sendAsync"/> delegate (typically <c>hubConnection.SendAsync</c>)
        /// instead of applying them locally.</summary>
        /// <param name="sendAsync">Sends a named event to the server.</param>
        /// <returns>The same element instance for fluent chaining.</returns>
        public TControl WithServerInteraction(Func<string, object, Task> sendAsync) =>
            element.RouteToServer(sendAsync);
    }
}
