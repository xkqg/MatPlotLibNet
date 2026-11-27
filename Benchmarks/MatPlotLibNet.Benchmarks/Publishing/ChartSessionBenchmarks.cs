// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using MatPlotLibNet.AspNetCore;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Benchmarks.Publishing;

/// <summary>What the reader of a chart session costs when a viewer drags: twenty pan events written to the channel at once, taken
/// off it, applied to the figure and answered with the publish that coalesces them. With <see cref="RealPublisher"/> off the
/// publish is a count, so the row is the session's own work; with it on, the publish is the server package's, over a hub with no
/// connection behind it.</summary>
[MemoryDiagnoser]
public class ChartSessionBenchmarks
{
    private const string Chart = "drag";
    private const int BatchSize = 20;
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
    private readonly DrainProbe _probe = new();
    private ChartSession _session = default!;
    private PanEvent _pan = default!;

    /// <summary>Whether the session publishes through the server package's publisher over a stand-in hub, or only counts the call.</summary>
    [Params(false, true)]
    public bool RealPublisher;

    [GlobalSetup]
    public void Setup()
    {
        var t = Enumerable.Range(0, 360).Select(i => (double)i).ToArray();
        var figure = Plt.Create().WithSize(1000, 400)
            .AddSubPlot(1, 1, 1, ax =>
            {
                for (var line = 0; line < 3; line++)
                {
                    var shift = line;
                    ax.Plot(t, t.Select(v => Math.Sin((v / 20) + shift)).ToArray());
                }
            })
            .Build();

        IChartPublisher? inner = null;
        if (RealPublisher)
        {
            var subscriptions = new ChartSubscriptions();
            subscriptions.Subscribe(Chart, "plain-viewer");
            subscriptions.SubscribeToParts(Chart, "patching-viewer");
            inner = new ChartPublisher(new StandInHub(new CountingClient()), ChartSerializerDefault.Current, new SvgTransform(), subscriptions);
        }

        _session = new ChartSession(Chart, figure, new ProbePublisher(_probe, inner), new SilentCaller(), new ChartSessionOptions().Handle(_probe));
        _pan = new PanEvent(Chart, 0, 0.25, 0.01) { InView = new DataRange(0, 359, -1.5, 1.5) };
    }

    [GlobalCleanup]
    public async Task Cleanup() => await _session.DisposeAsync();

    [Benchmark]
    public async Task DrainTwentyPans()
    {
        var done = _probe.Arm(BatchSize);
        for (var i = 0; i < BatchSize; i++) _session.Events.Writer.TryWrite(_pan);
        await done.WaitAsync(Deadline);
    }

    /// <summary>Counts the events the session handled and says when the publish that follows the last of a batch has run.</summary>
    private sealed class DrainProbe : IChartEventHandler
    {
        private TaskCompletionSource _done = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _expected;
        private int _handled;

        public Task Arm(int events)
        {
            _done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _handled = 0;
            _expected = events;
            return _done.Task;
        }

        public ValueTask OnEventAsync(FigureInteractionEvent evt, ChartEventContext context)
        {
            Interlocked.Increment(ref _handled);
            return ValueTask.CompletedTask;
        }

        /// <summary>The publish of a batch has run; when every event is handled, that batch was the last.</summary>
        public void Published()
        {
            if (Volatile.Read(ref _handled) >= _expected) _done.TrySetResult();
        }
    }

    private sealed class ProbePublisher(DrainProbe probe, IChartPublisher? inner) : IChartPublisher
    {
        public Task PublishAsync(string chartId, Figure figure, CancellationToken ct = default) => Task.CompletedTask;

        public async Task PublishSvgAsync(string chartId, Figure figure, CancellationToken ct = default)
        {
            if (inner is not null) await inner.PublishSvgAsync(chartId, figure, ct);
            probe.Published();
        }
    }

    private sealed class SilentCaller : ICallerPublisher
    {
        public Task SendTooltipAsync(string connectionId, string chartId, string html, CancellationToken ct = default) => Task.CompletedTask;
    }
}
