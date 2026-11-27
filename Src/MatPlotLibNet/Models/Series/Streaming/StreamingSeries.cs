// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series.Streaming;

/// <summary>Abstract base for XY streaming series: ONE ring of
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
public abstract class StreamingSeries : ChartSeries, IStreamingSeries, IVersionedSeries
{
    private readonly StreamRing<StreamingPoint> _points;

    /// <inheritdoc cref="IStreamingSeries.Version" />
    public long Version => _points.Version;

    /// <inheritdoc />
    public int Count => _points.Count;

    /// <inheritdoc />
    public int Capacity => _points.Capacity;

    /// <summary>How many points a streaming line or scatter keeps when it is told no capacity.</summary>
    internal const int RestingCapacity = 10_000;

    /// <inheritdoc />
    internal override long LatestChange => ChangeClock.Latest(base.LatestChange, _points.LatestChange);

    /// <summary>Initializes a new streaming series with the specified buffer capacity.</summary>
    /// <param name="capacity">Maximum number of data points retained. Oldest are evicted when exceeded.</param>
    protected StreamingSeries(int capacity) => _points = new StreamRing<StreamingPoint>(capacity);

    /// <inheritdoc />
    public void AppendPoint(double x, double y) => _points.Append(new StreamingPoint(x, y));

    /// <inheritdoc />
    public void AppendPoints(ReadOnlySpan<double> x, ReadOnlySpan<double> y)
    {
        x.Length.ThrowIfMisaligned(y.Length);

        // Paired up here, once, so the buffer never holds half a batch.
        var points = new StreamingPoint[x.Length];
        for (int i = 0; i < x.Length; i++)
        {
            points[i] = new StreamingPoint(x[i], y[i]);
        }

        _points.AppendRange(points);
    }

    /// <inheritdoc />
    public void Clear() => _points.Clear();

    /// <inheritdoc />
    public StreamingSnapshot CreateSnapshot()
    {
        // ONE read of ONE buffer. The split into two arrays happens afterwards, on a value nothing can change
        // any more, so the shape consumers expect costs nothing in consistency.
        var (points, version) = _points.Read();
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
        // One WALK, one state: the four corners come from the same moment or from none — and a render that
        // asks for the range no longer copies the whole window to find out.
        var box = _points.Aggregate(Box.Empty, static (box, point) => box.Extend(point.X, point.Y));
        return DataRangeContribution.Spanning(box.X.OrNone(), box.Y.OrNone());
    }

    /// <summary>The bounding box of what has been walked so far, and how many items were walked — the accumulator a range is folded
    /// into, so the four corners and the count are one value and cannot come from different moments. Each side takes in only the values
    /// an axis can place (a missing value, NaN, and an infinity are left out), the rule every series ranges by; the count takes in every
    /// item. A stream of points folds its X and Y, a stream of bars its lows and highs, a stream of samples the ordinals and the values.</summary>
    /// <param name="X">The extent folded along the first coordinate; <see cref="Range1D.Empty"/> until a value is taken in.</param>
    /// <param name="Y">The extent folded along the second coordinate.</param>
    /// <param name="Count">How many items were walked.</param>
    internal readonly record struct Box(Range1D X, Range1D Y, int Count)
    {
        public static Box Empty { get; } = new(Range1D.Empty, Range1D.Empty, 0);

        public Box Extend(double x, double y) => new(X.Including(x), Y.Including(y), Count + 1);
    }

    /// <summary>The record of the stream every streaming line and scatter writes: its kind, the x and y of one snapshot (never x
    /// and y from two moments), its capacity and its colour. A kind adds the fields of its own look with <c>with</c>.</summary>
    /// <param name="kind">The name the kind is written under.</param>
    /// <param name="color">The colour the series was given.</param>
    /// <returns>The record.</returns>
    private protected SeriesDto StreamDto(string kind, Color? color)
    {
        var snapshot = CreateSnapshot();
        return new() { Type = kind, XData = snapshot.XData, YData = snapshot.YData, Capacity = Capacity, Color = color };
    }

    /// <summary>A stream read back from its record: made at the capacity it was written with (the resting one when it names none),
    /// added to the axes, filled with the points of its snapshot and given its colour. A kind then reads the fields of its own look.</summary>
    /// <typeparam name="TSeries">The kind of stream.</typeparam>
    /// <param name="axes">The axes the stream is added to.</param>
    /// <param name="dto">The record.</param>
    /// <param name="make">Makes an empty stream of the kind at a capacity.</param>
    /// <returns>The stream.</returns>
    private protected static TSeries FromStreamDto<TSeries>(Axes axes, SeriesDto dto, Func<int, TSeries> make)
        where TSeries : StreamingSeries, IHasColor
    {
        var series = axes.AddSeries(make(dto.Capacity ?? RestingCapacity));
        series.AppendPoints(dto.XData ?? [], dto.YData ?? []);
        if (dto.Color is { } color) series.Color = color;
        return series;
    }

    // ToSeriesDto is left ABSTRACT here on purpose (inherited from ChartSeries, not overridden): a single shared
    // implementation once wrote "streaming" for both StreamingLineSeries and StreamingScatterSeries, which
    // collided on read — the registry could restore only one of the two (JSON round-trip). Each concrete
    // subclass now writes its own discriminator; leaving this abstract makes the compiler refuse a future
    // subclass that forgets to.

    /// <inheritdoc />
    /// <remarks>The ring buffer's current snapshot — what the series holds at the moment it is asked, which is
    /// the same thing the next render draws.</remarks>
    public override Models.ChartDataTable? ToDataTable()
    {
        var snapshot = CreateSnapshot();
        return Models.ChartDataTable.XY(Label, snapshot.XData, snapshot.YData);
    }
}
