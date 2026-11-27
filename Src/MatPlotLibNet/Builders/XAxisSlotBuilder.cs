// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet;

/// <summary>Fluent builder for ONE linked X axis below the plot, the slot <see cref="AxesBuilderLinkedAxesVerbs.WithAddBottomXAxis"/> just
/// stacked: matplotlib's <c>ax.secondary_xaxis('bottom', functions=(forward, inverse))</c>. The axis shows the range the primary X
/// axis shows, each value written through its link, and holds no series of its own.</summary>
/// <remarks>Every verb acts on this builder's own axis and no other.</remarks>
/// <example>
/// The period below a plot over frequency, a fifth of the plot's height under it:
/// <code>
/// Plt.Create().AddSubPlot(1, 1, 1, ax => ax
///     .Plot(frequency, power).SetXLabel("frequency (Hz)")
///     .WithAddBottomXAxis(period => period
///         .SetXLabel("period (s)").Link(AxisTransform.Reciprocal()).SetLocation(-0.2)));
/// </code>
/// </example>
public sealed class XAxisSlotBuilder
{
    private readonly AxisVerbs _verbs;

    /// <summary>Initializes a builder over <paramref name="slot"/> of <paramref name="axes"/>.</summary>
    /// <param name="axes">The axes the slot belongs to.</param>
    /// <param name="slot">The slot this builder configures.</param>
    internal XAxisSlotBuilder(Axes axes, YAxisSlot slot)
    {
        _verbs = AxisVerbs.Of(axes, slot);
    }

    /// <summary>Sets the title drawn below this axis' tick labels, with an optional text style override.</summary>
    /// <param name="label">The axis title.</param>
    /// <param name="configure">Function that receives the current <see cref="TextStyle"/> of the title and returns a modified copy;
    /// <see langword="null"/> leaves the style unchanged.</param>
    /// <returns>This builder, for chaining.</returns>
    public XAxisSlotBuilder SetXLabel(string label, Func<TextStyle, TextStyle>? configure = null)
    {
        _verbs.Label(label, configure);
        return this;
    }

    /// <summary>Colours this axis in one call: its line, its tick marks, its tick labels and its title.</summary>
    /// <param name="color">The colour all of them take.</param>
    /// <returns>This builder, for chaining.</returns>
    public XAxisSlotBuilder SetXAxisColor(Color color)
    {
        _verbs.Color(color);
        return this;
    }

    /// <summary>Links this axis to the primary X axis: it shows the range the primary axis shows, each value written through
    /// <paramref name="transform"/>, so a tick for a value stands below the place where the primary axis has the value it maps back
    /// to. Called again, the new transform replaces the first.</summary>
    /// <param name="transform">From the primary axis' values to this axis' values: <see cref="AxisTransform.Linear"/>,
    /// <see cref="AxisTransform.Reciprocal"/>, <see cref="AxisTransform.Logarithm"/>, <see cref="AxisTransform.Power"/>, or
    /// <see cref="AxisTransform.Of"/> for a pair of functions of your own.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="transform"/> is <see langword="null"/>.</exception>
    public XAxisSlotBuilder Link(AxisTransform transform)
    {
        _verbs.Link(transform);
        return this;
    }

    /// <summary>Stands this axis at <paramref name="location"/>, a fraction of the plot's height up from its bottom spine, as
    /// matplotlib's <c>secondary_xaxis(location)</c> takes a number: −0.2 a fifth of the plot's height below the bottom spine, 0 on
    /// it. Without a location the axis stands below everything above it on its side: the primary X axis' tick labels and title,
    /// and every linked X axis stacked before it.</summary>
    /// <param name="location">The fraction of the plot's height; a finite number.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="location"/> is NaN or infinite.</exception>
    public XAxisSlotBuilder SetLocation(double location)
    {
        _verbs.Slot!.Location = location.ThrowIfNotFinite(nameof(location));
        return this;
    }
}
