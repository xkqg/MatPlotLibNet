// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Data;

/// <summary>Extension methods for connecting <see cref="IObservable{T}"/> data sources to
/// streaming series. Enables Rx-style reactive pipelines without adding a core dependency
/// on System.Reactive — only <see cref="IObservable{T}"/> (built into .NET) is used.</summary>
public static class StreamingSeriesExtensions
{
    /// <summary>Subscribes a streaming XY series to an observable source of (x, y) tuples.
    /// Each emitted tuple is appended to the series. Dispose the returned handle to unsubscribe.</summary>
    public static IDisposable SubscribeTo(this IStreamingSeries series, IObservable<(double x, double y)> source)
    {
        return source.Subscribe(new XYObserver(series));
    }

    /// <summary>Subscribes a streaming OHLC series to an observable source of <see cref="OhlcBar"/> values.
    /// Dispose the returned handle to unsubscribe.</summary>
    public static IDisposable SubscribeTo(this IStreamingOhlcSeries series, IObservable<OhlcBar> source)
    {
        return source.Subscribe(new OhlcObserver(series));
    }

    /// <summary>Subscribes a streaming signal series to an observable source of Y samples.
    /// Dispose the returned handle to unsubscribe.</summary>
    public static IDisposable SubscribeTo(this StreamingSignalSeries series, IObservable<double> source)
    {
        return source.Subscribe(new SignalObserver(series));
    }

    private sealed class XYObserver(IStreamingSeries series) : IObserver<(double x, double y)>
    {
        public void OnNext((double x, double y) value) => series.AppendPoint(value.x, value.y);
        public void OnError(Exception error) { }
        public void OnCompleted() { }
    }

    private sealed class OhlcObserver(IStreamingOhlcSeries series) : IObserver<OhlcBar>
    {
        public void OnNext(OhlcBar value) => series.AppendBar(value);
        public void OnError(Exception error) { }
        public void OnCompleted() { }
    }

    private sealed class SignalObserver(StreamingSignalSeries series) : IObserver<double>
    {
        public void OnNext(double value) => series.AppendSample(value);
        public void OnError(Exception error) { }
        public void OnCompleted() { }
    }
}
