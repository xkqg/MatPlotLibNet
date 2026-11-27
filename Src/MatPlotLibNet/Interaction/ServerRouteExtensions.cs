// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Sends the gestures of a native control to a server instead of applying them to the figure in place: the one
/// wiring behind every package's public <c>WithServerInteraction</c>.</summary>
internal static class ServerRouteExtensions
{
    /// <param name="control">The control to wire.</param>
    /// <typeparam name="TControl">The control's own type, so the call returns it for chaining.</typeparam>
    extension<TControl>(TControl control) where TControl : IServerInteractive
    {
        /// <summary>Makes the control interactive and sends each of its gestures through <paramref name="sendAsync"/> (typically a
        /// hub connection's <c>SendAsync</c>), where whoever receives it applies it.</summary>
        /// <param name="sendAsync">Sends a named event to the server.</param>
        /// <returns>The same control, for chaining.</returns>
        public TControl RouteToServer(Func<string, object, Task> sendAsync)
        {
            control.IsInteractive = true;
            control.Host.ServerEventSink = SignalREventSink.Create(sendAsync);
            return control;
        }
    }
}
