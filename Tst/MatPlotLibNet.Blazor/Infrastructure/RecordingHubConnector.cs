// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.SignalR.Client;

namespace MatPlotLibNet.Blazor.Tests.Infrastructure;

/// <summary>The connector a test hands a <see cref="ChartSubscriptionClient"/>: it connects and listens through another connector
/// (SignalR's by default) and records the connections it built and the listening tokens that were let go of, so a test watches the
/// client's real connect and dispose without changing them.</summary>
/// <param name="inner">The connector that does the work; SignalR's when none is given.</param>
/// <param name="connect">The connection built for a hub address, when the test builds its own (one that reaches an in-process server, one
/// whose dispose fails); left out, the inner connector's.</param>
internal sealed class RecordingHubConnector(IHubConnector? inner = null, Func<string, HubConnection>? connect = null) : IHubConnector
{
    private readonly IHubConnector _inner = inner ?? new SignalRHubConnector();

    /// <summary>Every connection built, in order.</summary>
    public List<HubConnection> Built { get; } = [];

    /// <summary>How many of the tokens handed out were let go of.</summary>
    public int TokensDisposed { get; private set; }

    /// <inheritdoc />
    public HubConnection Connect(string hubUrl)
    {
        var hub = connect?.Invoke(hubUrl) ?? _inner.Connect(hubUrl);
        Built.Add(hub);
        return hub;
    }

    /// <inheritdoc />
    public IDisposable Listen(HubConnection hub, string methodName, Func<string, string, Task> handler) =>
        new CountedToken(_inner.Listen(hub, methodName, handler), this);

    private sealed class CountedToken(IDisposable token, RecordingHubConnector owner) : IDisposable
    {
        public void Dispose()
        {
            token.Dispose();
            owner.TokensDisposed++;
        }
    }
}
