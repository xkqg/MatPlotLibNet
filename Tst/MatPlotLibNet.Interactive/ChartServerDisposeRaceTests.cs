// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using MatPlotLibNet.AspNetCore;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Interactive;

namespace MatPlotLibNet.Interactive.Tests;

/// <summary>A server disposed while it is starting ends stopped: the start finishes or is refused, and nothing is left
/// listening. Dispose did not wait for the start, so it saw no app yet, and the start then put up a Kestrel nobody stopped and
/// threw when it let go of the lock that dispose had already disposed.</summary>
public class ChartServerDisposeRaceTests
{
    [Fact]
    public async Task DisposeWhileStarting_LeavesNothingListening_AndNoError()
    {
        for (int round = 0; round < 8; round++)
        {
            var server = new ChartServer();
            using var both = new Barrier(2);

            var start = Task.Run(async () =>
            {
                both.SignalAndWait(TestContext.Current.CancellationToken);
                try { await server.EnsureStartedAsync(TestContext.Current.CancellationToken); }
                catch (ObjectDisposedException) { /* refused: the dispose came first */ }
            }, TestContext.Current.CancellationToken);
            var dispose = Task.Run(async () =>
            {
                both.SignalAndWait(TestContext.Current.CancellationToken);
                await Task.Delay(round * 5, TestContext.Current.CancellationToken);
                await server.DisposeAsync();
            }, TestContext.Current.CancellationToken);
            await Task.WhenAll(start, dispose).WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

            Assert.False(server.IsRunning);
            if (server.Port > 0)
            {
                using var client = new System.Net.Sockets.TcpClient();
                await Assert.ThrowsAnyAsync<System.Net.Sockets.SocketException>(
                    () => client.ConnectAsync("127.0.0.1", server.Port, TestContext.Current.CancellationToken).AsTask());
            }
        }
    }

    [Fact]
    public async Task AStartAfterDispose_IsRefused()
    {
        var server = new ChartServer();
        await server.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => server.EnsureStartedAsync(TestContext.Current.CancellationToken));
    }
}
