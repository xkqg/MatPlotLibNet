// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.Frames;

/// <summary>Everything a draw of one subplot depends on that a mark or a reference can say: its place and plot area, the newest
/// change mark of the figure (its size, theme, spacing, interaction) and of the axes with every axes it shares a scale with, the
/// axes renderers registered in the process (<see cref="AxesRenderer.Registrations"/>), the text measurer of the process, the
/// theme, and what the target's kind of layer depends on besides (<see cref="KeepingFrameTarget{TLayer}"/>: the SVG glyph
/// writer, the native canvas' matrix). The <see cref="ChartRenderer"/> itself holds no state, so which one draws says nothing.</summary>
internal readonly record struct AxesDrawKey(int Index, Rect PlotArea, long FigureChange, long AxesChange, long RendererRegistrations,
    IFontMetrics FontMetrics, Theme Theme, object? LayerKey)
{
    /// <summary>The key of a draw of the subplot <paramref name="request"/> names, taken before it is drawn.</summary>
    internal static AxesDrawKey Of(in AxesDrawRequest request, object? layerKey) =>
        new(request.Index, request.PlotArea, request.Figure.LatestChange, request.Axes.LatestChangeWithShared(),
            AxesRenderer.Registrations, FontMetricsDefault.Current, request.Figure.Theme, layerKey);
}

/// <summary>One subplot as a target drew it, kept with what it was drawn from, so the next draw of the figure can hand it out
/// again instead of drawing it: only when nothing it was drawn from changed, compared by its key and by the copy of its data.</summary>
/// <typeparam name="TLayer">What the target keeps of a drawn subplot: its SVG markup, a native recording.</typeparam>
/// <remarks>The copy of the data is taken only when the subplot is drawn under the same key as the draw before, and before it is
/// drawn: a subplot that changes on every draw (a stream), or a figure built anew for every draw, never pays for a copy it would
/// never use, an unchanged one is handed out from its third draw on, and data that changes while the subplot is drawn is seen by
/// the next draw.</remarks>
internal sealed class KeptAxesDraw<TLayer> where TLayer : class
{
    private readonly AxesDrawKey _key;
    private readonly DrawnFrom? _from;

    private KeptAxesDraw(AxesDrawKey key, TLayer? layer, DrawnFrom? from, IReadOnlyList<LegendItemBounds> legend)
    {
        _key = key;
        Layer = layer;
        _from = from;
        Legend = legend;
    }

    /// <summary>What the target kept of the drawn subplot; <see langword="null"/> when it kept nothing (a draw that kept no copy of
    /// its data is never handed out, so it holds no layer).</summary>
    internal TLayer? Layer { get; }

    /// <summary>The bounds of the subplot's legend items as drawn, which a handed-out subplot gives again.</summary>
    internal IReadOnlyList<LegendItemBounds> Legend { get; }

    /// <summary>Whether this draw kept a copy of its data, so a later draw can be handed it.</summary>
    internal bool KeptData => _from is not null;

    /// <summary>A kept draw of what was just drawn under <paramref name="key"/> into <paramref name="layer"/>, from the data
    /// <paramref name="from"/> copied before the draw.</summary>
    internal static KeptAxesDraw<TLayer> Of(AxesDrawKey key, TLayer? layer, DrawnFrom? from, IReadOnlyList<LegendItemBounds> legend) =>
        new(key, layer, from, legend);

    /// <summary>The copy of the data a draw of <paramref name="axes"/> under <paramref name="key"/> is drawn from, taken before it
    /// is drawn; <see langword="null"/> when the draw before it, <paramref name="previous"/>, was not made under the same key.</summary>
    internal static DrawnFrom? CopyBeforeDraw(AxesDrawKey key, Axes axes, KeptAxesDraw<TLayer>? previous) =>
        previous is not null && previous._key == key ? DrawnFrom.Of(axes) : null;

    /// <summary>Whether this kept draw draws what a draw of <paramref name="axes"/> under <paramref name="key"/> would draw.</summary>
    internal bool Fits(AxesDrawKey key, Axes axes) => Layer is not null && key == _key && _from is { } from && from.Holds(axes);

    /// <summary>What the owner keeps of this draw until the next one: all of it when it kept a copy of the data, so the next
    /// draw can hand it out; otherwise only the key, which is all the next draw reads of it (whether to keep a copy then), so what
    /// an axes that changes on every draw drew is not held on to for nothing.</summary>
    internal KeptAxesDraw<TLayer> ToKeep() => KeptData ? this : new KeptAxesDraw<TLayer>(_key, null, null, []);

    /// <summary>What a draw was drawn from beyond its key: the objects that hold the axes' data and a copy of what each held.</summary>
    /// <param name="Holders">The data holders of the axes, in the order <see cref="AxesDrawnDataExtensions.DataHolders"/> gives them.</param>
    /// <param name="Data">A copy of what each holder held, in the same order.</param>
    internal readonly record struct DrawnFrom(object[] Holders, DataSnapshot[] Data)
    {
        /// <summary>The copy of the data a draw of <paramref name="axes"/> is drawn from, taken before it is drawn.</summary>
        internal static DrawnFrom Of(Axes axes)
        {
            object[] holders = [.. axes.DataHolders()];
            var data = new DataSnapshot[holders.Length];
            for (int i = 0; i < holders.Length; i++) data[i] = DataSnapshot.Of(holders[i]);
            return new DrawnFrom(holders, data);
        }

        /// <summary>Whether <paramref name="axes"/> holds what it held when this copy was taken: the same holders, each holding the
        /// same values.</summary>
        internal bool Holds(Axes axes)
        {
            int i = 0;
            foreach (var holder in axes.DataHolders())
            {
                if (i >= Holders.Length || !ReferenceEquals(holder, Holders[i]) || !Data[i].Matches(holder)) return false;
                i++;
            }

            return i == Holders.Length;
        }
    }
}
