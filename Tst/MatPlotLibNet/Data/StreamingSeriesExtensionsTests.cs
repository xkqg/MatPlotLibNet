// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Tests.Data;

public sealed class StreamingSeriesExtensionsTests
{
    [Fact]
    public void SubscribeTo_XYObservable_AppendsPoints()
    {
        var series = new StreamingLineSeries(100);
        var subject = new TestSubject<StreamingPoint>();
        using var sub = series.SubscribeTo(subject);

        subject.OnNext(new(1.0, 2.0));
        subject.OnNext(new(3.0, 4.0));

        Assert.Equal(2, series.Count);
        var snap = series.CreateSnapshot();
        Assert.Equal([1.0, 3.0], snap.XData);
        Assert.Equal([2.0, 4.0], snap.YData);
    }

    [Fact]
    public void SubscribeTo_OhlcObservable_AppendsBars()
    {
        var series = new StreamingCandlestickSeries(100);
        var subject = new TestSubject<OhlcBar>();
        using var sub = series.SubscribeTo(subject);

        subject.OnNext(new OhlcBar(100, 110, 95, 105));
        Assert.Equal(1, series.Count);
    }

    [Fact]
    public void SubscribeTo_SignalObservable_AppendsSamples()
    {
        var series = new StreamingSignalSeries(100);
        var subject = new TestSubject<double>();
        using var sub = series.SubscribeTo(subject);

        subject.OnNext(1.5);
        subject.OnNext(2.5);
        Assert.Equal(2, series.Count);
    }

    [Fact]
    public void Dispose_Unsubscribes()
    {
        var series = new StreamingLineSeries(100);
        var subject = new TestSubject<StreamingPoint>();
        var sub = series.SubscribeTo(subject);

        subject.OnNext(new(1.0, 2.0));
        Assert.Equal(1, series.Count);

        sub.Dispose();
        subject.OnNext(new(3.0, 4.0)); // should not reach series
        Assert.Equal(1, series.Count);
    }

    /// <summary>Each observer a series subscribes with takes an error or a completion as no work: it does not throw, and nothing is
    /// appended. A row subscribes a series of one kind to a subject, ends the subject (with an error when told to, else by completing it)
    /// and says how many points the series holds.</summary>
    public static TheoryData<Func<bool, int>> EndedSubscriptions => new()
    {
        failed =>
        {
            var series = new StreamingLineSeries(100);
            var subject = new TestSubject<StreamingPoint>();
            using var sub = series.SubscribeTo(subject);
            if (failed) subject.OnError(new InvalidOperationException("test")); else subject.OnCompleted();
            return series.Count;
        },
        failed =>
        {
            var series = new StreamingCandlestickSeries(100);
            var subject = new TestSubject<OhlcBar>();
            using var sub = series.SubscribeTo(subject);
            if (failed) subject.OnError(new InvalidOperationException("test")); else subject.OnCompleted();
            return series.Count;
        },
        failed =>
        {
            var series = new StreamingSignalSeries(100);
            var subject = new TestSubject<double>();
            using var sub = series.SubscribeTo(subject);
            if (failed) subject.OnError(new InvalidOperationException("test")); else subject.OnCompleted();
            return series.Count;
        },
    };

    [Theory]
    [MemberData(nameof(EndedSubscriptions))]
    public void AnObserver_TakesAnErrorAndACompletion_AsNoWork(Func<bool, int> end)
    {
        Assert.Equal(0, end(true));
        Assert.Equal(0, end(false));
    }

    /// <summary>Minimal IObservable implementation for testing without System.Reactive.</summary>
    private sealed class TestSubject<T> : IObservable<T>
    {
        private readonly List<IObserver<T>> _observers = [];

        public IDisposable Subscribe(IObserver<T> observer)
        {
            _observers.Add(observer);
            return new Unsubscriber(_observers, observer);
        }

        public void OnNext(T value)
        {
            foreach (var o in _observers.ToArray()) o.OnNext(value);
        }

        public void OnError(Exception error)
        {
            foreach (var o in _observers.ToArray()) o.OnError(error);
        }

        public void OnCompleted()
        {
            foreach (var o in _observers.ToArray()) o.OnCompleted();
        }

        private sealed class Unsubscriber(List<IObserver<T>> observers, IObserver<T> observer) : IDisposable
        {
            public void Dispose() => observers.Remove(observer);
        }
    }
}
