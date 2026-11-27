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

/// <summary>Serializes the test classes that mutate the process-global
/// <see cref="InteractiveExtensions.Browser"/> and/or the <see cref="ChartServer"/>
/// singleton, so xunit never runs them in parallel (they share mutable static state).
/// Fixes a pre-existing latent race between the two InteractiveExtensions test classes.</summary>
[CollectionDefinition("InteractiveDisplayGlobalState", DisableParallelization = true)]
public sealed class InteractiveDisplayGlobalStateCollection { }

/// <summary>B2 / F7 (v1.13.0, 2026-07-04) — pins the async-all-the-way fix for the
/// sync-over-async deadlock on the Interactive public surface. The whole Interactive
/// await chain now uses <c>ConfigureAwait(false)</c>, the synchronous <c>Show()</c> /
/// <c>EnsureStarted()</c> facades were removed (async-only surface), and
/// <see cref="ChartServer.EnsureStartedAsync"/> carries a disposed guard.</summary>
[Collection("InteractiveDisplayGlobalState")]
public class ChartServerAsyncSafetyTests
{
    /// <summary>Regression guard for the F7/B2 sync-over-async deadlock. A caller that
    /// blocks on <c>ShowAsync</c> from a thread owning a single-threaded, unpumped
    /// <see cref="SynchronizationContext"/> (the WinForms/WPF/legacy-ASP.NET/notebook-host
    /// shape) must complete, not deadlock. The injected <see cref="YieldingBrowserLauncher"/>
    /// is a genuinely-async launcher, giving ShowAsync a deterministic suspension point:
    /// pre-fix the resuming continuation is posted back to the blocked context and strands
    /// (RED — bounded 30 s join times out), post-fix ConfigureAwait(false) resumes it off
    /// the context (GREEN — returns in well under a second).</summary>
    [Fact]
    public void ShowAsync_BlockedUnderCapturedSyncContext_NoDeadlock()
    {
        var originalBrowser = InteractiveExtensions.Browser;
        InteractiveExtensions.Browser = new YieldingBrowserLauncher();
        try
        {
            var figure = Plt.Create().WithTitle("Deadlock probe").Plot([1.0], [2.0]).Build();
            Exception? failure = null;

            var thread = new Thread(() =>
            {
                SynchronizationContext.SetSynchronizationContext(new SingleThreadSynchronizationContext());
                try
                {
                    // Sync-over-async on a captured, unpumped context: this thread blocks
                    // here. ShowAsync awaits a genuinely-async browser launcher; if that
                    // await captures this context (missing ConfigureAwait(false)), its
                    // continuation is posted back here and can never run — deadlock.
                    _ = figure.ShowAsync().GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            })
            {
                IsBackground = true,
                Name = nameof(ShowAsync_BlockedUnderCapturedSyncContext_NoDeadlock),
            };

            thread.Start();
            var completed = thread.Join(TimeSpan.FromSeconds(30));

            Assert.True(completed,
                "ShowAsync deadlocked under a captured SynchronizationContext (sync-over-async); " +
                "the Interactive await chain must use ConfigureAwait(false).");
            Assert.Null(failure);
        }
        finally
        {
            InteractiveExtensions.Browser = originalBrowser;
        }
    }

    /// <summary>M14 disposed guard: a started-then-disposed server keeps a non-null
    /// <c>_app</c>, so without the guard EnsureStartedAsync would silently no-op via the
    /// fast path. It must instead throw <see cref="ObjectDisposedException"/>.</summary>
    [Fact]
    public async Task EnsureStartedAsync_AfterDispose_ThrowsObjectDisposed()
    {
        var server = new ChartServer();
        await server.EnsureStartedAsync(TestContext.Current.CancellationToken);
        await server.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => server.EnsureStartedAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>Pins the B2 public-surface decision: the Interactive display API is
    /// async-only. The pre-1.13.0 synchronous <c>Show()</c> extension and the internal
    /// <c>EnsureStarted()</c> sync-over-async wrappers were removed; <c>ShowAsync</c> and
    /// <c>EnsureStartedAsync</c> are the sole entry points.</summary>
    [Fact]
    public void InteractiveDisplayApi_IsAsyncOnly_NoSyncFacade()
    {
        var extensions = typeof(InteractiveExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static);
        Assert.DoesNotContain(extensions, m => m.Name == "Show");
        Assert.Contains(extensions, m => m.Name == "ShowAsync");

        var serverMethods = typeof(ChartServer)
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.DoesNotContain(serverMethods, m => m.Name == "EnsureStarted");
        Assert.Contains(serverMethods, m => m.Name == "EnsureStartedAsync");
    }

    /// <summary>A single-threaded <see cref="SynchronizationContext"/> that queues posted
    /// continuations but never pumps them — reproducing a UI/host message loop blocked
    /// inside a synchronous wait. Any continuation posted here (an await that captured the
    /// context) is stranded, which is exactly the deadlock under test.</summary>
    private sealed class SingleThreadSynchronizationContext : SynchronizationContext
    {
        private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = new();

        public override void Post(SendOrPostCallback d, object? state) => _queue.Add((d, state));
    }

    /// <summary>A browser launcher that genuinely completes asynchronously on a pool
    /// thread (as a real launcher doing I/O would), giving <c>ShowAsync</c> a deterministic
    /// suspension point. It uses ConfigureAwait(false) internally so the launcher itself
    /// never strands on the caller's context — the deadlock under test is ShowAsync's own
    /// await of this method, not the launcher's internals.</summary>
    private sealed class YieldingBrowserLauncher : IBrowserLauncher
    {
        public async Task OpenAsync(string url) => await Task.Delay(50).ConfigureAwait(false);
    }
}
