// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.SignalR.Client;

namespace MatPlotLibNet.Blazor;

/// <summary>The two calls a <see cref="ChartSubscriptionClient"/> makes to SignalR when it connects: build a connection to a hub,
/// and listen to one of the hub's methods. SignalR's own (<see cref="SignalRHubConnector"/>) is the one a client is made with; a
/// test hands it another to watch, or to stand in for, a connection.</summary>
internal interface IHubConnector
{
    /// <summary>A connection to the hub at <paramref name="hubUrl"/>, not started yet.</summary>
    /// <param name="hubUrl">The hub's URL.</param>
    HubConnection Connect(string hubUrl);

    /// <summary>Has <paramref name="handler"/> called with the two arguments of every call of <paramref name="methodName"/> the
    /// hub makes on <paramref name="hub"/>.</summary>
    /// <param name="hub">The connection.</param>
    /// <param name="methodName">The client method the hub calls.</param>
    /// <param name="handler">What is called with its arguments.</param>
    /// <returns>The token that stops the listening when it is let go of.</returns>
    IDisposable Listen(HubConnection hub, string methodName, Func<string, string, Task> handler);
}

/// <summary>SignalR's connector: a connection that reconnects by itself after it is lost, and SignalR's own listening.</summary>
internal sealed class SignalRHubConnector : IHubConnector
{
    /// <inheritdoc />
    public HubConnection Connect(string hubUrl) =>
        new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

    /// <inheritdoc />
    public IDisposable Listen(HubConnection hub, string methodName, Func<string, string, Task> handler) =>
        hub.On(methodName, handler);
}
