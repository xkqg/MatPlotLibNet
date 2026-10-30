// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Creates a platform-neutral event sink <c>Action&lt;FigureInteractionEvent&gt;</c>
/// that dispatches each interaction event type to the hub method that carries it (<c>HubMethodNames</c>, the one place
/// the names are written; an event that is not sent over a wire is dropped) on a generic async send
/// delegate — typically backed by a SignalR <c>HubCallerClients.SendAsync</c>. Lives in the
/// core library so both Avalonia and Uno controls can use it without taking a SignalR
/// dependency.</summary>
public static class SignalREventSink
{
    /// <summary>Creates an event sink that routes each <see cref="FigureInteractionEvent"/>
    /// subtype to the corresponding <paramref name="sendAsync"/> method name.</summary>
    /// <param name="sendAsync">A delegate that sends the event object to a named endpoint
    /// (e.g. <c>(method, payload) => hubConnection.SendAsync(method, payload)</c>).</param>
    public static Action<FigureInteractionEvent> Create(Func<string, object, Task> sendAsync)
    {
        return evt => _ = HubMethodNames.For(evt) is { } name ? sendAsync(name, evt) : Task.CompletedTask;
    }
}
