// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering;

/// <summary>Adapts an <see cref="Axes"/> instance to the <see cref="IAxesContext"/> interface, the context a draw asks every
/// series' range through. It keeps the stack of the bars the first time a bar asks for it, so the bars of one range read one
/// stack.</summary>
internal class AxesContextAdapter : IKeepsBarStack
{
    private readonly Axes _axes;
    private BarStack? _stack;

    /// <summary>Initializes a new adapter wrapping the given <see cref="Axes"/> instance.</summary>
    /// <param name="axes">The subplot axes to expose through the <see cref="IAxesContext"/> interface.</param>
    public AxesContextAdapter(Axes axes) => _axes = axes;

    /// <summary>The context the series of an axis past the primary (a Y axis slot, or the top X axis) are ranged under, as an axes of
    /// its own to matplotlib (<c>twinx</c>, <c>twiny</c>): that axis' scale and its own limits along its own direction, so its series
    /// leave out what its scale has no place for and stop where it is pinned, and its own series, so its bars are stacked among
    /// themselves; the primary's along the other direction. A context of its own type, so the one every axes makes for each draw holds
    /// no more than it did.</summary>
    /// <param name="axes">The axes the axis stands beside.</param>
    /// <param name="axis">The axis past the primary: the axes' top axis (<see cref="Axes.SecondaryXAxis"/>), else a Y axis slot's.</param>
    /// <param name="series">The axis' own series: the top axis' (<see cref="Axes.XSecondarySeries"/>), else the slot's.</param>
    /// <param name="isX">Whether the axis runs along X (the top axis); a Y slot otherwise.</param>
    /// <returns>The context.</returns>
    internal static AxesContextAdapter PastPrimary(Axes axes, Axis axis, IReadOnlyList<ISeries> series, bool isX) =>
        new PastPrimaryContext(axes, axes.PrimaryAxes.With(isX, axis), series);

    /// <inheritdoc />
    public virtual double? XAxisMin => _axes.XAxis.Min;
    /// <inheritdoc />
    public virtual double? XAxisMax => _axes.XAxis.Max;
    /// <inheritdoc />
    public virtual double? YAxisMin => _axes.YAxis.Min;
    /// <inheritdoc />
    public virtual double? YAxisMax => _axes.YAxis.Max;
    /// <inheritdoc />
    public BarMode BarMode => _axes.BarMode;
    /// <inheritdoc />
    public virtual IReadOnlyList<ISeries> AllSeries => _axes.Series;

    /// <inheritdoc />
    public virtual AxisScale XScale => _axes.XAxis.Scale;

    /// <inheritdoc />
    public virtual AxisScale YScale => _axes.YAxis.Scale;

    /// <inheritdoc />
    /// <remarks>Stacked over <see cref="AllSeries"/>, the set a context that keeps no stack is stacked over.</remarks>
    public BarStack BarStack => _stack ??= AllSeries.StackLayers().Stack();

    /// <summary>The context of an axis past the primary (<see cref="PastPrimary"/>): its scale and its limits along its own direction,
    /// which is X for the top axis (the axes' <see cref="Axes.SecondaryXAxis"/>) and Y for a slot, the primary's along the other, and its
    /// own series. The two axes are held as one pair, the axis past the primary in its direction's place, so no member asks which
    /// direction it is; the pair and the series make the context three references larger than the primary's.</summary>
    private sealed class PastPrimaryContext(Axes axes, AxisPair<Axis> axis, IReadOnlyList<ISeries> series) : AxesContextAdapter(axes)
    {
        /// <inheritdoc />
        public override AxisScale XScale => axis.X.Scale;

        /// <inheritdoc />
        public override AxisScale YScale => axis.Y.Scale;

        /// <inheritdoc />
        public override double? XAxisMin => axis.X.Min;

        /// <inheritdoc />
        public override double? XAxisMax => axis.X.Max;

        /// <inheritdoc />
        public override double? YAxisMin => axis.Y.Min;

        /// <inheritdoc />
        public override double? YAxisMax => axis.Y.Max;

        /// <inheritdoc />
        public override IReadOnlyList<ISeries> AllSeries => series;
    }
}