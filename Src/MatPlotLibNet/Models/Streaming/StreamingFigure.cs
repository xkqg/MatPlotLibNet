// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Models.Streaming;

/// <summary>Wraps a <see cref="Models.Figure"/> and adds streaming lifecycle management:
/// render throttling, data version tracking, and auto-scaling axis limits.
/// Subscribe to <see cref="RenderRequested"/> to know when a re-render is needed.</summary>
public sealed class StreamingFigure : IDisposable
{
    private readonly Timer _timer;
    private long _lastRenderedVersion;
    private bool _disposed;

    /// <summary>The underlying figure containing both static and streaming series.</summary>
    public Figure Figure { get; }

    /// <summary>Per-axes streaming configuration. Indexed by axes position (0-based).</summary>
    public Dictionary<int, StreamingAxesConfig> AxesConfigs { get; } = new();

    /// <summary>Default axis config applied when no per-axes config is set.</summary>
    public StreamingAxesConfig DefaultConfig { get; set; } = StreamingAxesConfig.Default();

    /// <summary>Minimum interval between renders. Default 33ms (~30fps).</summary>
    public TimeSpan MinRenderInterval { get; set; } = TimeSpan.FromMilliseconds(33);

    /// <summary>Fired when data has changed and a re-render should occur.
    /// Subscribers should marshal to their UI thread before rendering.</summary>
    public event Action? RenderRequested;

    /// <summary>Aggregate version across all streaming series. Changes when any series appends data.</summary>
    public long DataVersion
    {
        get
        {
            long v = 0;
            foreach (var axes in Figure.SubPlots)
                foreach (var series in axes.Series)
                    if (series is IStreamingSeries s)
                        v += s.Version;
                    else if (series is IStreamingOhlcSeries o)
                        v += o.Version;
            return v;
        }
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
    public void ApplyAxisScaling()
    {
        for (int i = 0; i < Figure.SubPlots.Count; i++)
        {
            var axes = Figure.SubPlots[i];
            var config = AxesConfigs.TryGetValue(i, out var c) ? c : DefaultConfig;
            ApplyScaleMode(axes, config);
        }
    }

    /// <summary>Forces a render request regardless of version or throttle.</summary>
    public void RequestRender()
    {
        _lastRenderedVersion = DataVersion;
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
        if (_disposed) return;
        long current = DataVersion;
        if (current != _lastRenderedVersion)
        {
            _lastRenderedVersion = current;
            RenderRequested?.Invoke();
        }
    }

    private static void ApplyScaleMode(Axes axes, StreamingAxesConfig config)
    {
        // Compute aggregate data range from all streaming series on this axes
        double xMin = double.MaxValue, xMax = double.MinValue;
        double yMin = double.MaxValue, yMax = double.MinValue;
        bool hasData = false;

        foreach (var series in axes.Series)
        {
            if (!series.Visible) continue;
            var range = series.ComputeDataRange(null!);
            if (range.XMin is null) continue;
            hasData = true;
            xMin = Math.Min(xMin, range.XMin.Value);
            xMax = Math.Max(xMax, range.XMax!.Value);
            yMin = Math.Min(yMin, range.YMin!.Value);
            yMax = Math.Max(yMax, range.YMax!.Value);
        }

        if (!hasData) return;

        // Apply X mode
        switch (config.XMode)
        {
            case AxisScaleMode.AutoScale:
                axes.XAxis.Min = xMin;
                axes.XAxis.Max = xMax;
                break;
            case AxisScaleMode.SlidingWindow sw:
                axes.XAxis.Min = xMax - sw.WindowSize;
                axes.XAxis.Max = xMax;
                break;
            case AxisScaleMode.StickyRight sr:
                double currentXMax = axes.XAxis.Max ?? xMax;
                if (xMax >= currentXMax - sr.WindowSize * 0.01) // within 1% of edge
                {
                    axes.XAxis.Min = xMax - sr.WindowSize;
                    axes.XAxis.Max = xMax;
                }
                break;
            case AxisScaleMode.Fixed:
                break; // no change
        }

        // Apply Y mode (recompute Y range within the visible X window for sliding modes)
        switch (config.YMode)
        {
            case AxisScaleMode.AutoScale:
                double margin = (yMax - yMin) * 0.05;
                if (margin < 1e-10) margin = 1.0;
                axes.YAxis.Min = yMin - margin;
                axes.YAxis.Max = yMax + margin;
                break;
            case AxisScaleMode.Fixed:
                break;
        }
    }
}
