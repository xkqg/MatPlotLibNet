// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Blazor.Tests.Infrastructure;
using MatPlotLibNet.Diagnostics;
using Microsoft.AspNetCore.SignalR.Client;

namespace MatPlotLibNet.Blazor.Tests;

/// <summary>A connection that came back is subscribed to its charts again, and when that fails for one of them the failure is
/// reported on the diagnostics channel, where an operator can see it, and does not stop the others from being tried.</summary>
[Collection("ChartDiagnosticsGlobalState")]
public class ChartSubscriptionClientReconnectTests : IClassFixture<StreamingHostFixture>
{
    private readonly StreamingHostFixture _fixture;

    public ChartSubscriptionClientReconnectTests(StreamingHostFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ASubscribeThatFailsAfterAReconnect_IsReportedOnTheDiagnosticsChannel()
    {
        var ct = TestContext.Current.CancellationToken;
        HubConnection? built = null;
        await using var client = new ChartSubscriptionClient(
            url => built = ChartSubscriptionClient.BuildHubConnection(url),
            ChartSubscriptionClient.RegisterHandler);
        await client.ConnectAsync(_fixture.HubUrl, ct);
        await client.SubscribeAsync("live-8", ct);

        var reports = new List<ChartDiagnostic>();
        void Collect(ChartDiagnostic report) => reports.Add(report);
        ChartDiagnostics.Emitted += Collect;
        try
        {
            await built!.StopAsync(ct);   // the connection is gone, so the invocation fails
            await client.HandleReconnectedAsync();
        }
        finally
        {
            ChartDiagnostics.Emitted -= Collect;
        }

        var report = Assert.Single(reports, r => r.Source == nameof(ChartSubscriptionClient));
        Assert.Contains("live-8", report.Message);
        Assert.NotNull(report.Exception);
    }
}
