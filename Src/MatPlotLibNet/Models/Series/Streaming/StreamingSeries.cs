// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Models.Series.Streaming;

/// <summary>Abstract base for XY streaming series: ONE <see cref="RingBuffer{T}"/> of
/// <see cref="StreamingPoint"/>, because a point is what this series is a sequence of.
///
/// <para>It used to be two buffers, one for x and one for y. Each was thread-safe and the only thing that
/// mattered — that an x and a y belong to the SAME sample — was guarded by nothing: an append was two separate
/// acts and a snapshot two separate reads, so a reader landing between them got arrays of different lengths,
/// or worse, equal lengths whose pairs came from different samples. Measured, not theorised: a reader beside a
/// writer saw <c>x=41895, y=83826</c> within milliseconds. No lock on those buffers could have fixed it; only
/// storing the value that must stay whole AS one value can.</para>
///
/// <para>Subclasses provide visual properties and visitor dispatch.</para></summary>
public abstract class StreamingSeries : ChartSeries, IStreamingSeries
{
    private readonly RingBuffer<StreamingPoint> _points;
    private long _version;

    /// <inheritdoc />
    public long Version => Interlocked.Read(ref _version);

    /// <inheritdoc />
    public int Count => _points.Count;

    /// <inheritdoc />
    public int Capacity { get; }

    /// <summary>Initializes a new streaming series with the specified buffer capacity.</summary>
    /// <param name="capacity">Maximum number of data points retained. Oldest are evicted when exceeded.</param>
    protected StreamingSeries(int capacity)
    {
        Capacity = capacity;
        _points = new RingBuffer<StreamingPoint>(capacity);
    }

    /// <inheritdoc />
    public void AppendPoint(double x, double y)
    {
        _points.Append(new StreamingPoint(x, y));
        Interlocked.Increment(ref _version);
    }

    /// <inheritdoc />
    public void AppendPoints(ReadOnlySpan<double> x, ReadOnlySpan<double> y)
    {
        if (x.Length != y.Length)
            throw new ArgumentException("X and Y spans must have equal length.");

        // Paired up here, once, so the buffer never holds half a batch.
        var points = new StreamingPoint[x.Length];
        for (int i = 0; i < x.Length; i++)
        {
            points[i] = new StreamingPoint(x[i], y[i]);
        }

        _points.AppendRange(points);
        Interlocked.Increment(ref _version);
    }

    /// <inheritdoc />
    public void Clear()
    {
        _points.Clear();
        Interlocked.Increment(ref _version);
    }

    /// <inheritdoc />
    public StreamingSnapshot CreateSnapshot()
    {
        // ONE read of ONE buffer. The split into two arrays happens afterwards, on a value nothing can change
        // any more, so the shape consumers expect costs nothing in consistency.
        long version = Version;
        var points = _points.ToArray();
        var x = new double[points.Length];
        var y = new double[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            x[i] = points[i].X;
            y[i] = points[i].Y;
        }

        return new StreamingSnapshot(x, y, version);
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        // One read, one state. Four separate Min/Max calls could each land on a different append, and a range
        // whose corners come from different moments describes a window that never existed.
        var points = _points.ToArray();
        if (points.Length == 0)
        {
            return new(null, null, null, null);
        }

        double xMin = points[0].X, xMax = points[0].X, yMin = points[0].Y, yMax = points[0].Y;
        for (int i = 1; i < points.Length; i++)
        {
            var point = points[i];
            xMin = Math.Min(xMin, point.X);
            xMax = Math.Max(xMax, point.X);
            yMin = Math.Min(yMin, point.Y);
            yMax = Math.Max(yMax, point.Y);
        }

        return new(xMin, xMax, yMin, yMax);
    }

    /// <inheritdoc />
    public override Serialization.SeriesDto ToSeriesDto() => new()
    {
        Type = "streaming",
        Label = Label
    };

    /// <inheritdoc />
    /// <remarks>The ring buffer's current snapshot — what the series holds at the moment it is asked, which is
    /// the same thing the next render draws.</remarks>
    public override Models.ChartDataTable? ToDataTable()
    {
        var snapshot = CreateSnapshot();
        return Models.ChartDataTable.FromNumberColumns(
            [new("x"), new(Label ?? "y")], [snapshot.XData, snapshot.YData]);
    }
}
