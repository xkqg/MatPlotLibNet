// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Models.Streaming;

/// <summary>Wraps a <see cref="Models.Figure"/> and adds streaming lifecycle management:
/// render throttling, data version tracking, and auto-scaling axis limits.
/// Subscribe to <see cref="RenderRequested"/> to know when a re-render is needed.</summary>
/// <remarks>The figure owns one worker, a clock that wakes every <see cref="MinRenderInterval"/> and asks for a render when
/// the data changed. It is one worker in the strict sense: a tick that finds the previous one still running (a render slower
/// than the interval) does nothing, so two renders asked for by the clock never run at once and a slow render never piles up
/// waiting ticks. A render that throws costs that frame: the exception is reported through
/// <see cref="ChartDiagnostics.Emitted"/> (source <c>StreamingFigure</c>) instead of ending the process from the pool
/// thread the clock runs on, and the next change of the data is rendered as usual.</remarks>
public sealed class StreamingFigure : IDisposable
{
    private readonly Timer _timer;
    private TimeSpan _minRenderInterval = TimeSpan.FromMilliseconds(33);
    private long _lastRenderedVersion;
    private int _ticking;
    private volatile bool _disposed;

    /// <summary>The underlying figure containing both static and streaming series.</summary>
    public Figure Figure { get; }

    /// <summary>Per-axes streaming configuration. Indexed by axes position (0-based).</summary>
    public Dictionary<int, StreamingAxesConfig> AxesConfigs { get; } = new();

    /// <summary>Default axis config applied when no per-axes config is set.</summary>
    public StreamingAxesConfig DefaultConfig { get; set; } = StreamingAxesConfig.Default();

    /// <summary>Minimum interval between renders. Default 33ms (~30fps).</summary>
    /// <remarks>Setting this re-arms the clock behind it. It used to be read once, when the figure was
    /// constructed, so a caller who asked for a slower chart — directly or through
    /// <c>BuildStreaming(renderInterval)</c> — kept getting 33 ms and had no way to find out: the property
    /// returned the value it had been given while the cadence ignored it.</remarks>
    public TimeSpan MinRenderInterval
    {
        get => _minRenderInterval;
        set
        {
            _minRenderInterval = value;
            if (!_disposed)
            {
                _timer.Change(value, value);
            }
        }
    }

    /// <summary>Fired when data has changed and a re-render should occur.
    /// Subscribers should marshal to their UI thread before rendering.</summary>
    public event Action? RenderRequested;

    /// <summary>Aggregate version across all streaming series. Changes when any series appends data.</summary>
    /// <remarks>The sum of the version of every streaming series the figure shows, whatever its kind (a line, a scatter, a
    /// candlestick, a sampled signal) and wherever it stands: on any Y axis on either side, on the top X axis, or in an inset. The
    /// clock asks it at every tick, so the walk allocates nothing.</remarks>
    public long DataVersion => VersionOf(Figure.SubPlots);

    // Every seat that holds series: each Y axis of both sides (the primary is left slot 0), the top X axis, and the insets with
    // theirs. The sum does not depend on the order, so the walk takes the seats as they are stored, by index.
    private static long VersionOf(IReadOnlyList<Axes> subplots)
    {
        long version = 0;
        for (int i = 0; i < subplots.Count; i++)
        {
            var axes = subplots[i];
            version += VersionOf(axes.YAxes.Left) + VersionOf(axes.YAxes.Right) + VersionOf(axes.XSecondarySeries) + VersionOf(axes.Insets);
        }

        return version;
    }

    private static long VersionOf(YAxisStack stack)
    {
        long version = 0;
        for (int i = 0; i < stack.Count; i++) version += VersionOf(stack[i].Series);
        return version;
    }

    private static long VersionOf(IReadOnlyList<ISeries> seat)
    {
        long version = 0;
        for (int i = 0; i < seat.Count; i++)
        {
            if (seat[i] is IVersionedSeries streamed) version += streamed.Version;
        }

        return version;
    }

    /// <summary>Initializes a new streaming figure wrapping the specified figure.</summary>
    /// <param name="figure">The figure to manage. May contain both static and streaming series.</param>
    public StreamingFigure(Figure figure)
    {
        Figure = figure;
        _timer = new Timer(OnTimerTick, null, MinRenderInterval, MinRenderInterval);
    }

    /// <summary>Applies axis scaling modes to the figure's axes based on current data ranges.
    /// Call this before each render to update axis limits.</summary>
    /// <remarks>An axes the reader has moved (zoomed, panned or rotated) is left where they put it: its first view is
    /// remembered by the figure (<c>Figure.Home</c>), and a scale mode that rewrote the limits at every frame would
    /// overrule the reader at the next frame. Home returns the axes to their first view and forgets it, so the scale mode
    /// follows the data again from the next frame.</remarks>
    public void ApplyAxisScaling()
    {
        for (int i = 0; i < Figure.SubPlots.Count; i++)
        {
            var axes = Figure.SubPlots[i];
            if (Figure.Home.Remembers(axes)) continue;

            var config = AxesConfigs.TryGetValue(i, out var c) ? c : DefaultConfig;
            ApplyScaleMode(axes, config);
        }
    }

    /// <summary>Forces a render request regardless of version or throttle.</summary>
    public void RequestRender()
    {
        Interlocked.Exchange(ref _lastRenderedVersion, DataVersion);
        RenderRequested?.Invoke();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Dispose();
    }

    private void OnTimerTick(object? state)
    {
        // One tick at a time: the one that finds another still running leaves the frame to it.
        if (Interlocked.Exchange(ref _ticking, 1) == 1) return;
        try
        {
            if (_disposed) return;
            long current = DataVersion;
            if (current == Interlocked.Read(ref _lastRenderedVersion)) return;
            Interlocked.Exchange(ref _lastRenderedVersion, current);
            RenderRequested?.Invoke();
        }
        catch (Exception ex)
        {
            ChartDiagnostics.Emit(new ChartDiagnostic(nameof(StreamingFigure),
                "A render the streaming clock asked for threw; that frame was skipped and the clock goes on.", ex));
        }
        finally
        {
            Volatile.Write(ref _ticking, 0);
        }
    }

    private static void ApplyScaleMode(Axes axes, StreamingAxesConfig config)
    {
        // Compute aggregate data range from all streaming series on this axes. A series is asked through the axes it is on,
        // the context the renderer asks it through: a candlestick or a bar reads the axes for its extent.
        double xMin = double.MaxValue, xMax = double.MinValue;
        double yMin = double.MaxValue, yMax = double.MinValue;
        bool hasData = false;
        var context = new AxesContextAdapter(axes);

        foreach (var series in axes.Series)
        {
            if (!series.Visible) continue;
            var range = series.ComputeDataRange(context);
            if (range.XMin is null) continue;
            hasData = true;
            xMin = Math.Min(xMin, range.XMin.Value);
            xMax = Math.Max(xMax, range.XMax!.Value);
            yMin = Math.Min(yMin, range.YMin!.Value);
            yMax = Math.Max(yMax, range.YMax!.Value);
        }

        if (!hasData) return;

        // Each mode owns its rule for the axis it scales.
        config.XMode.ScaleX(axes.XAxis, xMin, xMax);
        config.YMode.ScaleY(axes.YAxis, yMin, yMax);
    }
}
