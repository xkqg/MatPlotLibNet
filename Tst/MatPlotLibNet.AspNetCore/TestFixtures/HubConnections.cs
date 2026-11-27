// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace MatPlotLibNet.Tests;

/// <summary>How a server test reaches the chart hub of its in-memory host.</summary>
public static class HubConnections
{
    extension(IHost host)
    {
        /// <summary>A connection to the chart hub of the host, not yet started.</summary>
        public HubConnection ConnectToTheHub() => new HubConnectionBuilder()
            .WithUrl("http://localhost/charts-hub", options => options.HttpMessageHandlerFactory = _ => host.GetTestServer().CreateHandler())
            .Build();
    }
}
