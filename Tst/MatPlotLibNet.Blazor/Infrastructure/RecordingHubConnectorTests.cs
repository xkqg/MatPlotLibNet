// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.SignalR.Client;

namespace MatPlotLibNet.Blazor.Tests.Infrastructure;

/// <summary>The connector a test hands a client: it builds a connection the way the test says (SignalR's way, left out) and keeps it.</summary>
public sealed class RecordingHubConnectorTests
{
    [Fact]
    public async Task AConnector_BuildsTheConnectionItWasToldTo_AndKeepsIt()
    {
        await using var given = new HubConnectionBuilder().WithUrl("http://localhost/given").Build();
        string? asked = null;
        var connector = new RecordingHubConnector(connect: url => { asked = url; return given; });

        var built = connector.Connect("http://localhost/hub");

        Assert.Same(given, built);
        Assert.Equal("http://localhost/hub", asked);
        Assert.Same(given, Assert.Single(connector.Built));
    }
}
