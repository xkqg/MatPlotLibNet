// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Frames;

/// <summary>A target that keeps what it drew of each subplot, so an unchanged subplot is handed out again on the next draw
/// instead of being drawn: the rule for when (<see cref="KeptAxesDraw{TLayer}"/>) is here, once, and a leaf says only what a kept
/// subplot is (its <typeparamref name="TLayer"/>), how one is made and placed, and what else the layer depends on.</summary>
/// <typeparam name="TLayer">What the leaf keeps of a drawn subplot.</typeparam>
internal abstract class KeepingFrameTarget<TLayer> : FrameTarget where TLayer : class
{
    private readonly KeptAxesDraw<TLayer>[]? _kept;
    private readonly List<KeptAxesDraw<TLayer>> _drawn = [];

    /// <summary>A target for a draw after the one that left <paramref name="kept"/> (<see cref="ToKeep"/>); <see langword="null"/>
    /// for the first draw.</summary>
    protected KeepingFrameTarget(KeptAxesDraw<TLayer>[]? kept) => _kept = kept;

    /// <summary>What a layer of this leaf depends on besides the key every kept subplot has; <see langword="null"/> for nothing.</summary>
    protected abstract object? LayerKey { get; }

    /// <summary>Draws the subplot with <paramref name="draw"/> where this leaf draws it, keeping a layer of it when
    /// <paramref name="keep"/> is set.</summary>
    /// <param name="request">The subplot.</param>
    /// <param name="keep">Whether the draw is kept: a layer is made only from the second draw under one key.</param>
    /// <param name="draw">Draws the subplot onto the context it is given.</param>
    /// <returns>The layer (required when <paramref name="keep"/> is set) and the bounds of the subplot's legend items.</returns>
    protected abstract LayerDraw<TLayer> DrawLayer(in AxesDrawRequest request, bool keep,
        Func<IRenderContext, IReadOnlyList<LegendItemBounds>> draw);

    /// <summary>Puts a kept layer where the subplot at <paramref name="index"/> goes in this draw.</summary>
    protected abstract void Place(int index, TLayer layer);

    /// <summary>Lets go of a layer a new draw replaced. Nothing here; a leaf whose layer holds native memory frees it.</summary>
    protected virtual void Release(TLayer layer) { }

    /// <inheritdoc />
    internal sealed override IReadOnlyList<LegendItemBounds> DrawAxes(in AxesDrawRequest request,
        Func<IRenderContext, IReadOnlyList<LegendItemBounds>> draw)
    {
        var axes = request.Axes;
        var key = AxesDrawKey.Of(request, LayerKey);
        var previous = _kept.ElementOr(request.Index, (KeptAxesDraw<TLayer>?)null);
        if (previous is not null && previous.Fits(key, axes))
        {
            Place(request.Index, previous.Layer!);
            _drawn.Add(previous);
            return previous.Legend;
        }

        var from = KeptAxesDraw<TLayer>.CopyBeforeDraw(key, axes, previous);
        var drawn = DrawLayer(request, keep: from is not null, draw);
        if (previous?.Layer is { } replaced && !ReferenceEquals(replaced, drawn.Layer)) Release(replaced);
        _drawn.Add(KeptAxesDraw<TLayer>.Of(key, drawn.Layer, from, drawn.Legend));
        return drawn.Legend;
    }

    /// <summary>What the owner of the target keeps until the next draw, one entry per subplot drawn. A layer kept from before that
    /// this draw never came to (a renderer of the caller's draws without the target's subplots) is let go of.</summary>
    internal KeptAxesDraw<TLayer>[] ToKeep()
    {
        if (_kept is not null)
            for (int i = _drawn.Count; i < _kept.Length; i++)
                if (_kept[i].Layer is { } unvisited) Release(unvisited);

        var keep = new KeptAxesDraw<TLayer>[_drawn.Count];
        for (int i = 0; i < keep.Length; i++) keep[i] = _drawn[i].ToKeep();
        return keep;
    }
}

/// <summary>What a leaf's draw of one subplot gave: the layer it kept, if any, and the bounds of the subplot's legend items.</summary>
internal readonly record struct LayerDraw<TLayer>(TLayer? Layer, IReadOnlyList<LegendItemBounds> Legend) where TLayer : class;
