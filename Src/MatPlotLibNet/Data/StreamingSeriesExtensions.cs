// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Data;

/// <summary>Extension methods for connecting <see cref="IObservable{T}"/> data sources to
/// streaming series. Enables Rx-style reactive pipelines without adding a core dependency
/// on System.Reactive — only <see cref="IObservable{T}"/> (built into .NET) is used.</summary>
public static class StreamingSeriesExtensions
{
    /// <summary>Subscribes a streaming XY series to an observable source of
    /// <see cref="StreamingPoint"/> samples. Each emitted point is appended to the series via
    /// <see cref="IStreamingSeries.AppendPoint"/>. Errors and completion are silently absorbed.</summary>
    /// <param name="series">Target streaming series whose ring buffer receives the samples.</param>
    /// <param name="source">Observable source of <see cref="StreamingPoint"/> values.</param>
    /// <returns>A disposable subscription handle — dispose to stop forwarding samples.</returns>
    public static IDisposable SubscribeTo(this IStreamingSeries series, IObservable<StreamingPoint> source)
    {
        return source.Subscribe(new XYObserver(series));
    }

    /// <summary>Subscribes a streaming OHLC series to an observable source of
    /// <see cref="OhlcBar"/> values. Errors and completion are silently absorbed.</summary>
    /// <param name="series">Target streaming OHLC series whose ring buffer receives the bars.</param>
    /// <param name="source">Observable source of <see cref="OhlcBar"/> values.</param>
    /// <returns>A disposable subscription handle — dispose to stop forwarding bars.</returns>
    public static IDisposable SubscribeTo(this IStreamingOhlcSeries series, IObservable<OhlcBar> source)
    {
        return source.Subscribe(new OhlcObserver(series));
    }

    /// <summary>Subscribes a streaming signal series to an observable source of scalar Y
    /// samples. Errors and completion are silently absorbed.</summary>
    /// <param name="series">Target streaming signal series whose ring buffer receives the samples.</param>
    /// <param name="source">Observable source of scalar Y values.</param>
    /// <returns>A disposable subscription handle — dispose to stop forwarding samples.</returns>
    public static IDisposable SubscribeTo(this StreamingSignalSeries series, IObservable<double> source)
    {
        return source.Subscribe(new SignalObserver(series));
    }

    private sealed class XYObserver(IStreamingSeries series) : IObserver<StreamingPoint>
    {
        public void OnNext(StreamingPoint value) => series.AppendPoint(value.X, value.Y);
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
