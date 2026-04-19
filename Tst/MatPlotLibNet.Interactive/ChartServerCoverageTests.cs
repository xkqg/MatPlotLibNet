// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interactive.Tests;

/// <summary>Phase Y.7 (v1.7.2, 2026-04-19) — branch coverage for the
/// <see cref="ChartServer"/> lifecycle methods that the existing
/// <see cref="ChartServerTests"/> harness left at 0% (DisposeAsync, IsRunning
/// when not started, EnsureStartedAsync idempotence). Pre-Y.7: 83.6%L / 50%B.
/// Each fact constructs a FRESH non-singleton ChartServer via the internal
/// constructor (IVT is set in MatPlotLibNet.Interactive.csproj) so the global
/// singleton is never touched.</summary>
public class ChartServerCoverageTests
{
    /// <summary>IsRunning returns false on a fresh, never-started ChartServer
    /// (line 37 — `_app is not null` false arm).</summary>
    [Fact]
    public async Task IsRunning_OnFreshServer_False()
    {
        await using var server = new ChartServer();
        Assert.False(server.IsRunning);
        Assert.Equal(0, server.Port);
    }

    /// <summary>DisposeAsync on a never-started ChartServer (line 124-134) — the
    /// `_app is not null` false arm at line 129. Should dispose the SemaphoreSlim
    /// without trying to stop a non-existent app.</summary>
    [Fact]
    public async Task DisposeAsync_NeverStartedServer_NoOp()
    {
        var server = new ChartServer();
        await server.DisposeAsync();
        // Idempotent: second dispose hits the line-126 _disposed guard.
        await server.DisposeAsync();
    }

    /// <summary>DisposeAsync on a STARTED ChartServer (line 129 true arm).
    /// Verifies the full teardown of the embedded Kestrel host.</summary>
    [Fact]
    public async Task DisposeAsync_StartedServer_StopsKestrel()
    {
        var server = new ChartServer();
        await server.EnsureStartedAsync();
        Assert.True(server.IsRunning);
        Assert.True(server.Port > 0);

        await server.DisposeAsync();
        // After dispose, the second call hits _disposed=true short-circuit (line 126).
        await server.DisposeAsync();
    }

    /// <summary>EnsureStartedAsync called twice on the same instance — the second
    /// call must hit the early-return at line 44 (`_app is not null` true arm).</summary>
    [Fact]
    public async Task EnsureStartedAsync_CalledTwice_SecondCallShortCircuits()
    {
        var server = new ChartServer();
        await server.EnsureStartedAsync();
        var firstPort = server.Port;
        await server.EnsureStartedAsync();
        Assert.Equal(firstPort, server.Port);   // same instance, same port
        await server.DisposeAsync();
    }

    /// <summary>EnsureStarted (synchronous wrapper, line 76) — verify it blocks
    /// until startup completes.</summary>
    [Fact]
    public async Task EnsureStarted_Synchronous_StartsServer()
    {
        var server = new ChartServer();
        server.EnsureStarted();
        Assert.True(server.IsRunning);
        await server.DisposeAsync();
    }
}
