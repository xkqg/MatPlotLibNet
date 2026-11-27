// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Buffers;

namespace MatPlotLibNet.Rendering;

/// <summary>
/// Painter's-algorithm depth sink of a 3-D scene: the one place its marks are put back to front. Every 3-D series hands it its marks,
/// each with its depth; <see cref="Flush()"/> sorts them ascending (lowest depth = farthest from camera) and draws them in that order,
/// so front marks paint over back ones across series, not just within one.
/// </summary>
/// <remarks>
/// matplotlib orders the artists of a 3-D axes by one depth each and draws the faces of one collection back to front, so the bars of
/// two <c>ax.bar3d()</c> calls overlap in the order they were added unless the user adds the rows back to front. The shared queue lifts
/// that restriction, so series can be added in any order. A 3-D series drawn off a 3-D axes is a scene of its own, with a queue of its
/// own.
/// </remarks>
public sealed class DepthQueue3D
{
    /// <summary>One queued mark: its depth, the index of the source that draws it among the queue's sources, and its index there. It
    /// holds no reference, so filling the queue and sorting it write no reference into the heap, and it orders itself by depth, so the
    /// sort calls no delegate per comparison.</summary>
    /// <param name="Depth">The depth of the mark; lower is farther from the camera.</param>
    /// <param name="Source">The index of the source the mark belongs to.</param>
    /// <param name="Index">The index of the mark in its source.</param>
    internal readonly record struct DepthItem(double Depth, int Source, int Index) : IComparable<DepthItem>
    {
        /// <summary>Orders marks by depth, the farthest first.</summary>
        /// <param name="other">The mark to compare with.</param>
        /// <returns>Below zero when this mark stands farther than <paramref name="other"/>.</returns>
        public int CompareTo(DepthItem other) => Depth.CompareTo(other.Depth);
    }

    /// <summary>The group of a series that handed the queue no mark (nothing it could draw), written where the series stands among the
    /// sources: before the first run of a source enlisted after it.</summary>
    /// <param name="Before">The number of sources enlisted before the series.</param>
    /// <param name="Group">The series' group.</param>
    private readonly record struct HeldGroup(int Before, SeriesGroup Group);

    // The queued marks, in an array rented from the shared pool for the queue's draw and handed back by the flush, so a scene drawn
    // again allocates no list of its marks; the sources they belong to; the drawing actions handed to Add, one source among them from
    // the first action on; the groups of the series that handed no mark.
    private DepthItem[] _items = [];
    private int _count;
    private readonly List<DepthMarks> _sources = [];
    private readonly List<ActionMark> _actionMarks = [];
    private readonly DepthMarks<ActionMark, EachMark<ActionMark>> _actions;
    private int _actionsSource = -1;
    private readonly List<HeldGroup> _held = [];

    /// <summary>Makes an empty queue.</summary>
    public DepthQueue3D() => _actions = new(_actionMarks, new EachMark<ActionMark>(static mark => mark.Draw()), null);

    /// <summary>Queues a drawing action to be executed during <see cref="Flush()"/>.</summary>
    /// <param name="depth">Centroid depth of the primitive; lower = farther from camera.</param>
    /// <param name="draw">Closure that performs the actual draw when flushed.</param>
    public void Add(double depth, Action draw)
    {
        if (_actionsSource < 0) _actionsSource = Enlist(_actions);
        _actionMarks.Add(new(depth, draw));
        Add(depth, _actionsSource, _actionMarks.Count - 1);
    }

    /// <summary>How many marks are queued: a series that leaves it where it was handed the queue nothing to draw.</summary>
    internal int MarkCount => _count;

    /// <summary>Takes <paramref name="source"/> among the sources of the queue's marks.</summary>
    /// <param name="source">The marks of one series.</param>
    /// <returns>The index the marks of the source are queued with.</returns>
    internal int Enlist(DepthMarks source)
    {
        _sources.Add(source);
        return _sources.Count - 1;
    }

    /// <summary>Holds the group of a series that handed the queue no mark, so the flush writes it where the series stands among the
    /// others, as a series drawn at its turn writes its group whether or not it draws anything.</summary>
    /// <param name="group">The series' group.</param>
    internal void HoldGroup(SeriesGroup group) => _held.Add(new(_sources.Count, group));

    /// <summary>Queues mark <paramref name="index"/> of source <paramref name="source"/> at <paramref name="depth"/>.</summary>
    /// <param name="depth">The depth of the mark; lower is farther from the camera.</param>
    /// <param name="source">The index of the source the mark belongs to (<see cref="Enlist"/>).</param>
    /// <param name="index">The index of the mark in its source.</param>
    internal void Add(double depth, int source, int index)
    {
        if (_count == _items.Length) Reserve(1);
        _items[_count++] = new(depth, source, index);
    }

    /// <summary>Makes room for <paramref name="more"/> marks past those queued, in one rented array.</summary>
    /// <param name="more">How many marks are about to be queued.</param>
    internal void Reserve(int more)
    {
        int needed = _count + more;
        if (needed <= _items.Length) return;

        var larger = ArrayPool<DepthItem>.Shared.Rent(Math.Max(needed, _items.Length * 2));
        _items.AsSpan(0, _count).CopyTo(larger);
        ReturnItems();
        _items = larger;
    }

    /// <summary>Sorts queued items ascending by depth and invokes each draw closure.</summary>
    public void Flush() => Flush(null);

    /// <summary>Sorts the queued marks ascending by depth and draws them on <paramref name="ctx"/>, each run of one source's marks by one
    /// call of its rule (<see cref="DepthMarks.DrawRun"/>): inside the group of its series, opened around the run, and at the opacity of
    /// its series, set only where it changes from one run to the next. The surface is left at the opacity it was found at, and every
    /// group is closed.</summary>
    /// <param name="ctx">The surface the marks draw on; <see langword="null"/> draws them with no group and no opacity of their own.</param>
    internal void Flush(IRenderContext? ctx)
    {
        var items = _items.AsSpan(0, _count);
        items.Sort();
        if (ctx is null)
        {
            for (int start = 0; start < items.Length;)
            {
                int end = RunEnd(items, start);
                _sources[items[start].Source].DrawRun(items[start..end]);
                start = end;
            }
        }
        else
        {
            DrawIn(ctx, items);
        }

        ReturnItems();
        _items = [];
        _count = 0;
        _sources.Clear();
        _actionMarks.Clear();
        _actionsSource = -1;
        _held.Clear();
    }

    /// <summary>Hands the rented array back to the pool (its items hold no reference, so nothing is kept alive by it).</summary>
    private void ReturnItems()
    {
        if (_items.Length != 0) ArrayPool<DepthItem>.Shared.Return(_items);
    }

    /// <summary>Where the run that starts at <paramref name="start"/> ends: the first mark of another source, or the end.</summary>
    private static int RunEnd(ReadOnlySpan<DepthItem> items, int start)
    {
        int source = items[start].Source;
        int end = start + 1;
        while (end < items.Length && items[end].Source == source) end++;
        return end;
    }

    /// <summary>Draws the sorted marks on <paramref name="ctx"/> run by run, in the groups and at the opacities of their series, the
    /// groups of the series that handed no mark each where its series stands.</summary>
    private void DrawIn(IRenderContext ctx, ReadOnlySpan<DepthItem> items)
    {
        double found = ctx.OpacityTold;
        double current = found;
        DepthMarks? previous = null;
        bool grouped = false;
        int held = 0;
        for (int start = 0; start < items.Length;)
        {
            int end = RunEnd(items, start);
            int index = items[start].Source;
            var source = _sources[index];
            if (!source.SharesGroupWith(previous))
            {
                if (grouped)
                {
                    current = Restore(ctx, current, found);
                    ctx.EndGroup();
                }

                held = WriteHeldGroups(ctx, held, index);
                grouped = source.Group is { } group && group.OpenOn(ctx);
            }

            double opacity = source.Opacity ?? found;
            if (opacity != current)
            {
                ctx.SetOpacity(opacity);
                current = opacity;
            }

            source.DrawRun(items[start..end]);
            previous = source;
            start = end;
        }

        Restore(ctx, current, found);
        if (grouped) ctx.EndGroup();
        WriteHeldGroups(ctx, held, int.MaxValue);
    }

    /// <summary>Writes the held groups from <paramref name="from"/> on whose series stand before source <paramref name="before"/>.</summary>
    /// <returns>The index of the first held group not written.</returns>
    private int WriteHeldGroups(IRenderContext ctx, int from, int before)
    {
        for (; from < _held.Count && _held[from].Before <= before; from++)
        {
            if (_held[from].Group.OpenOn(ctx)) ctx.EndGroup();
        }

        return from;
    }

    /// <summary>Puts <paramref name="ctx"/> back at <paramref name="found"/> when it stands at another opacity.</summary>
    /// <returns>The opacity the surface stands at.</returns>
    private static double Restore(IRenderContext ctx, double current, double found)
    {
        if (current != found) ctx.SetOpacity(found);
        return found;
    }
}
