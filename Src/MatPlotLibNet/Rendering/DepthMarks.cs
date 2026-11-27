// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace MatPlotLibNet.Rendering;

/// <summary>A mark of a 3-D scene (a face, a segment, a point, an arrow, a label, a whole drawing): what it is drawn as, and where it
/// stands in depth, lower farther from the camera (<see cref="Projection3D.Depth"/>).</summary>
internal interface IDepthMark
{
    /// <summary>Where the mark stands in depth: lower is farther from the camera.</summary>
    double Depth { get; }
}

/// <summary>The whole drawing of a series that draws its marks in an order of its own (a wireframe's lines, a 3-D contour's levels, the
/// stems and the baseline of a 3-D stem plot) as one mark of the scene, standing at its nearest point, as matplotlib orders a collection
/// among the artists of a 3-D axes by its nearest point.</summary>
/// <param name="Depth">The depth of the drawing's nearest point.</param>
internal readonly record struct WholeDrawing(double Depth) : IDepthMark;

/// <summary>A drawing action handed to a depth queue through its public <see cref="DepthQueue3D.Add(double, Action)"/> (a patch on its
/// plane, a caller's own drawing), as a mark of the scene.</summary>
/// <param name="Depth">The depth the action is drawn at.</param>
/// <param name="Draw">The action.</param>
internal readonly record struct ActionMark(double Depth, Action Draw) : IDepthMark;

/// <summary>The rule that draws a run of a series' marks: the marks of one series that stand next to each other back to front, drawn by
/// one call, so what the rule resolves before its loop is resolved once a run, not once a mark.</summary>
/// <typeparam name="TMark">The shape of a mark.</typeparam>
internal interface IMarkRunDrawer<TMark> where TMark : struct, IDepthMark
{
    /// <summary>Draws the marks of <paramref name="run"/>, in the order it hands them (the farthest first).</summary>
    /// <param name="run">The marks, back to front.</param>
    void DrawRun(MarkRun<TMark> run);
}

/// <summary>The rule of a series that draws each mark by one action: the run's marks drawn one by one through it.</summary>
/// <typeparam name="TMark">The shape of a mark.</typeparam>
/// <param name="draw">The action that draws one mark.</param>
internal readonly struct EachMark<TMark>(Action<TMark> draw) : IMarkRunDrawer<TMark> where TMark : struct, IDepthMark
{
    /// <inheritdoc />
    public void DrawRun(MarkRun<TMark> run)
    {
        foreach (var mark in run) draw(mark);
    }
}

/// <summary>A run of one series' marks, back to front: the marks the depth queue put next to each other, read through the indices the
/// queue sorted, so the series' marks are never moved.</summary>
/// <typeparam name="TMark">The shape of a mark.</typeparam>
/// <param name="items">The sorted items of the run, each naming its mark by its index.</param>
/// <param name="marks">The marks of the series, in the order the series made them.</param>
internal readonly ref struct MarkRun<TMark>(ReadOnlySpan<DepthQueue3D.DepthItem> items, ReadOnlySpan<TMark> marks) where TMark : struct, IDepthMark
{
    private readonly ReadOnlySpan<DepthQueue3D.DepthItem> _items = items;
    private readonly ReadOnlySpan<TMark> _marks = marks;

    /// <summary>How many marks the run holds.</summary>
    internal int Count => _items.Length;

    /// <summary>Walks the run's marks, back to front.</summary>
    /// <returns>The walk.</returns>
    public Enumerator GetEnumerator() => new(this);

    /// <summary>A walk over the marks of a run, back to front.</summary>
    public ref struct Enumerator
    {
        private readonly MarkRun<TMark> _run;
        private int _at;

        internal Enumerator(MarkRun<TMark> run)
        {
            _run = run;
            _at = -1;
        }

        /// <summary>Steps to the next mark.</summary>
        /// <returns><see langword="false"/> past the last mark.</returns>
        public bool MoveNext() => ++_at < _run.Count;

        /// <summary>The mark the walk stands at.</summary>
        public readonly ref readonly TMark Current => ref _run._marks[_run._items[_at].Index];
    }
}

/// <summary>Marks one series hands to the depth queue of its scene (<see cref="DepthQueue3D"/>), each run of them drawn by the series'
/// own rule when the queue reaches it: the opacity they are drawn at and the group of the series they are drawn in belong to all of them.
/// The queue holds an index into its source, so a mark costs the queue no closure of its own.</summary>
internal abstract class DepthMarks
{
    /// <summary>Makes a source of marks drawn at <paramref name="opacity"/> inside <paramref name="group"/>.</summary>
    /// <param name="opacity">The opacity the marks are drawn at, 0 to 1; <see langword="null"/> for a series with no opacity of its
    /// own, whose marks are drawn at the opacity the queue found the surface at.</param>
    /// <param name="group">The group of the series the marks belong to, which the queue opens around each run of them;
    /// <see langword="null"/> for marks drawn in no group of their own.</param>
    private protected DepthMarks(double? opacity, SeriesGroup? group)
    {
        Opacity = opacity;
        Group = group;
    }

    /// <summary>The opacity the marks are drawn at; <see langword="null"/> for the opacity the queue found the surface at.</summary>
    internal double? Opacity { get; }

    /// <summary>The group of the series the marks belong to; <see langword="null"/> for none.</summary>
    internal SeriesGroup? Group { get; }

    /// <summary>Whether the marks of this source are drawn in the same group as those of <paramref name="other"/>: both in none, or
    /// both in the group of one series.</summary>
    /// <param name="other">The source of the marks drawn just before, <see langword="null"/> for none.</param>
    /// <returns><see langword="true"/> when no group has to be closed or opened between them.</returns>
    internal bool SharesGroupWith(DepthMarks? other) =>
        other is not null && Nullable.Equals(Group, other.Group);

    /// <summary>Draws the marks named by <paramref name="run"/>, one run of this source, back to front.</summary>
    /// <param name="run">The sorted items of the run, each naming a mark of this source by the index it was handed with.</param>
    internal abstract void DrawRun(ReadOnlySpan<DepthQueue3D.DepthItem> run);
}

/// <summary>The marks of one series of one shape (<typeparamref name="TMark"/>), each run of them drawn by the series' rule.</summary>
/// <typeparam name="TMark">The shape of a mark: what the series' rule draws it from, with its depth.</typeparam>
/// <typeparam name="TDrawer">The series' rule, a value, so the queue calls it without a delegate.</typeparam>
/// <param name="marks">The marks, in the order the series made them.</param>
/// <param name="drawer">The rule that draws a run of them.</param>
/// <param name="opacity">The opacity the marks are drawn at, 0 to 1; <see langword="null"/> for the opacity the queue found.</param>
/// <param name="group">The group of the series the marks belong to; <see langword="null"/> for none.</param>
internal sealed class DepthMarks<TMark, TDrawer>(List<TMark> marks, TDrawer drawer, double? opacity, SeriesGroup? group = null)
    : DepthMarks(opacity, group) where TMark : struct, IDepthMark where TDrawer : struct, IMarkRunDrawer<TMark>
{
    /// <summary>Hands every mark to <paramref name="queue"/>, at its depth, in the order the series made them.</summary>
    /// <param name="queue">The depth queue of the scene the marks are drawn in.</param>
    internal void HandTo(DepthQueue3D queue)
    {
        int source = queue.Enlist(this);
        queue.Reserve(marks.Count);
        var span = CollectionsMarshal.AsSpan(marks);
        for (int i = 0; i < span.Length; i++)
        {
            queue.Add(span[i].Depth, source, i);
        }
    }

    /// <inheritdoc />
    internal override void DrawRun(ReadOnlySpan<DepthQueue3D.DepthItem> run)
    {
        var rule = drawer;
        rule.DrawRun(new MarkRun<TMark>(run, CollectionsMarshal.AsSpan(marks)));
    }
}
