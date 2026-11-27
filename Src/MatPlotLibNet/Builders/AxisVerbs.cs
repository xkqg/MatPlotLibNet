// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet;

/// <summary>The verbs of one axis, written once for every axis a builder configures: the X, the Y and the Z axis of
/// <see cref="AxesBuilder"/>, each Y axis of <see cref="YAxisSlotBuilder"/>, a linked X axis below the plot
/// (<see cref="XAxisSlotBuilder"/>) and the top X axis (<see cref="SecondaryXAxisBuilder"/>). A builder holds the verbs of its axes
/// and forwards to them, so a setting is made the same way on whichever axis it is made, and no builder writes an axis itself. The
/// verbs of the axis' ticks are <see cref="Ticks"/>.</summary>
/// <param name="Axes">The axes the axis belongs to: the spines a colour reaches are its.</param>
/// <param name="Axis">The axis the verbs set.</param>
/// <param name="Spine">The edge whose spine the axis stands on, which a colour of the axis colours too; <see langword="null"/> for an
/// axis that draws no spine of its own (a Y axis stacked past the first of its side, a linked X axis below the plot, the Z axis).</param>
/// <param name="Held">The series plotted against the axis, which a link refuses.</param>
/// <param name="Slot">The slot of a Y axis stacked on a side, or of a linked X axis below the plot, whose builder reaches its location and its
/// series through it; <see langword="null"/> for an axis that is no slot.</param>
internal readonly record struct AxisVerbs(Axes Axes, Axis Axis, SpineEdge? Spine, IReadOnlyList<ISeries> Held, YAxisSlot? Slot = null)
{
    /// <summary>The verbs of the primary X axis, along the bottom spine.</summary>
    /// <param name="axes">The axes.</param>
    /// <returns>The verbs.</returns>
    internal static AxisVerbs BottomOf(Axes axes) => new(axes, axes.XAxis, SpineEdge.Bottom, []);

    /// <summary>The verbs of the Z axis of a 3-D axes, which stands on no spine.</summary>
    /// <param name="axes">The axes.</param>
    /// <returns>The verbs.</returns>
    internal static AxisVerbs DepthOf(Axes axes) => new(axes, axes.ZAxis, null, []);

    /// <summary>The verbs of the top X axis, which the call makes when the axes has none yet.</summary>
    /// <param name="axes">The axes.</param>
    /// <returns>The verbs.</returns>
    internal static AxisVerbs TopOf(Axes axes) => new(axes, axes.TwinY().SecondaryXAxis!, SpineEdge.Top, axes.XSecondarySeries);

    /// <summary>The verbs of the axis of <paramref name="slot"/>: slot 0 of the left or the right side stands on that side's spine; a
    /// slot stacked past it, and a linked X axis below the plot, on none.</summary>
    /// <param name="axes">The axes the slot belongs to.</param>
    /// <param name="slot">The slot.</param>
    /// <returns>The verbs.</returns>
    internal static AxisVerbs Of(Axes axes, YAxisSlot slot) =>
        new(axes, slot.Axis,
            slot.Side != AxisSide.Bottom && ReferenceEquals(axes.YAxes.StackOn(slot.Side)[0], slot) ? Rendering.EdgeTerms.Of(slot.Side).SpineEdge : null,
            slot.Series, slot);

    /// <summary>The verbs of the axis' ticks.</summary>
    internal TickVerbs Ticks => new(Axis);

    /// <summary>Sets the axis title, and its text style through <paramref name="configure"/> when one is given.</summary>
    internal void Label(string label, Func<TextStyle, TextStyle>? configure = null) => SetLabel(Axis, label, configure);

    /// <summary>Sets the limits of the axis' range.</summary>
    internal void Limits(double min, double max)
    {
        Axis.Min = min;
        Axis.Max = max;
    }

    /// <summary>Colours the axis' tick marks, tick labels and title, and its spine when it stands on one.</summary>
    internal void Color(Color color) => ApplyAxisColor(Axes, Axis, Spine, color);

    /// <summary>Puts a series on the axis, as every series verb of an axis past the primary does (a Y slot's, the top axis'): refused,
    /// with nothing made, while the axis is linked (a linked axis holds no series of its own); otherwise <paramref name="make"/> makes
    /// it on the axis, <paramref name="configure"/> configures it, and it is handed back.</summary>
    /// <typeparam name="TSeries">The kind of series.</typeparam>
    /// <param name="parameter">The parameter the refusal names: the values the series would have measured against the axis.</param>
    /// <param name="make">Makes the series on the axis.</param>
    /// <param name="configure">The caller's configuration of the new series, if any.</param>
    /// <returns>The series.</returns>
    /// <exception cref="ArgumentException">The axis is linked.</exception>
    internal TSeries Seat<TSeries>(string parameter, Func<TSeries> make, Action<TSeries>? configure) where TSeries : ISeries
    {
        RefuseSeriesWhenLinked(Axis, parameter);
        var series = make();
        configure?.Invoke(series);
        return series;
    }

    /// <summary>Links the axis to its primary through <paramref name="transform"/>; refused while the axis holds a series.</summary>
    internal void Link(AxisTransform transform) => LinkThrough(Axis, transform, Held.Count);

    /// <summary>Sets the margin the automatic range keeps on each side of the data.</summary>
    internal void Margin(double margin) => Axis.Margin = margin;

    /// <summary>Sets the scale of the axis.</summary>
    internal void Scale(AxisScale scale) => Axis.Scale = scale;

    /// <summary>Puts the axis on the symmetric-log scale, linear within <c>[-linthresh, linthresh]</c> and logarithmic outside, replacing
    /// a locator the axis had: with a threshold given, the <see cref="SymlogLocator"/> of that threshold, which the axis keeps under every
    /// theme; with none, no locator of the axis' own, so the scale brings the one of the threshold its theme draws it with (matplotlib's 2
    /// under the matplotlib themes). A formatter of the axis' own is kept, else the scale's <see cref="LogTickFormatter"/> writes the
    /// labels, whatever locator places them. The locator is the same on either axis; it is taken from the one place that says which
    /// locator a scale brings (its rule's <c>Locator</c>), so this verb and a bare scale cannot place ticks two ways.</summary>
    /// <param name="linthresh">The linear threshold its author gave, <see langword="null"/> for the theme's.</param>
    internal void Symlog(double? linthresh)
    {
        Axis.Scale = AxisScale.SymLog;
        if (linthresh is { } given)
        {
            Axis.SymLogLinThresh = given;
            Axis.TickLocator = ScaleRule.SymLogAt(given).Locator(isX: false);
        }
        else
        {
            Axis.TickLocator = null;
        }
    }

    /// <summary>Puts the axis on the date scale with an <see cref="AutoDateLocator"/> and an <see cref="AutoDateFormatter"/> that reads
    /// the interval that same locator chose, replacing a locator or formatter the axis had: together they pick years, months, weeks,
    /// days, hours, minutes or seconds from the range the axis shows. The locator is the date rule's own <c>Locator</c>, the one that
    /// also places the ticks of a date axis with no locator of its own. Only an X axis is put on dates.</summary>
    internal void AutoDates()
    {
        Axis.Scale = AxisScale.Date;
        var rule = Axis.Scale.Rule;
        var locator = rule.Locator(isX: true)!;
        Axis.TickLocator = locator;
        Axis.TickFormatter = rule.Formatter(locator, isX: true);
    }

    /// <summary>Puts the axis on the date scale with one fixed format for every tick label, such as <c>yyyy-MM-dd</c>. The locator the
    /// axis had is kept; a formatter it had is replaced.</summary>
    /// <param name="format">The .NET date format every tick label is written with.</param>
    internal void DateFormat(string format)
    {
        Axis.Scale = AxisScale.Date;
        Axis.TickFormatter = new DateTickFormatter { DateFormat = format };
    }

    /// <summary>Turns the axis round: a toggle, as matplotlib's <c>invert_xaxis()</c> is.</summary>
    internal void Invert() => Axis.Inverted = !Axis.Inverted;

    /// <summary>Sets the title of this axis and, when a style edit is given, replaces the title's style with the edit's
    /// result. The edit receives the style the axis already has, or a plain <see cref="TextStyle"/> when it has none.</summary>
    /// <param name="label">The title text.</param>
    /// <param name="configure">Function that receives the current style and returns a modified copy; <see langword="null"/>
    /// leaves the style as it was.</param>
    /// <param name="axis">The <see cref="Axis"/> it works on.</param>
    internal static void SetLabel(Axis axis, string label, Func<TextStyle, TextStyle>? configure)
    {
        axis.Label = label;
        if (configure is not null) axis.LabelStyle = configure(axis.LabelStyle ?? new TextStyle());
    }

    /// <summary>Colours the tick marks, the tick labels and the title of this axis with
    /// <paramref name="color"/>, keeping every other tick and label setting it already carries; a missing
    /// <see cref="Axis.LabelStyle"/> is created.</summary>
    /// <param name="color">The colour all three take.</param>
    /// <param name="axis">The <see cref="Axis"/> it works on.</param>
    internal static void ApplyAxisColor(Axis axis, Color color)
    {
        axis.MajorTicks = axis.MajorTicks with { Color = color, LabelColor = color };
        axis.LabelStyle = (axis.LabelStyle ?? new TextStyle()) with { Color = color };
    }

    /// <summary>Colours <paramref name="axis"/> (its tick marks, tick labels and title) and, when the axis stands on
    /// a spine, that spine, keeping everything else the axis and the spine already carry.</summary>
    /// <param name="axis">The axis to colour; one of this axes' own.</param>
    /// <param name="spine">The edge whose spine takes the colour too, or <see langword="null"/> for an axis that
    /// has no spine of its own: a Y axis past the first on its side draws ticks, tick labels and a title only.</param>
    /// <param name="color">The colour the axis, and the spine when there is one, take.</param>
    /// <param name="axes">The <see cref="Axes"/> it works on.</param>
    internal static void ApplyAxisColor(Axes axes, Axis axis, SpineEdge? spine, Color color)
    {
        ApplyAxisColor(axis, color);
        if (spine is { } edge) axes.Spines = axes.Spines.WithEdge(edge, stroke => stroke with { Color = color });
    }

    /// <summary>Links this axis through <paramref name="transform"/>, replacing a link it had; refused, with nothing changed,
    /// when the axis holds a series.</summary>
    /// <param name="transform">The link.</param>
    /// <param name="seriesHeld">How many series the axis holds.</param>
    /// <exception cref="ArgumentNullException"><paramref name="transform"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The axis holds a series.</exception>
    /// <param name="axis">The <see cref="Axis"/> it works on.</param>
    private static void LinkThrough(Axis axis, AxisTransform transform, int seriesHeld)
    {
        ArgumentNullException.ThrowIfNull(transform);
        if (seriesHeld > 0)
        {
            throw new ArgumentException(
                "This axis holds a series; a linked axis shows the primary axis' values in other units and holds no series of its own.",
                nameof(transform));
        }

        axis.Link = transform;
    }

    /// <summary>Refuses a series on this axis when it is linked, naming <paramref name="parameter"/>: the values the series
    /// would have measured against it.</summary>
    /// <param name="parameter">The name of the parameter the refusal names.</param>
    /// <exception cref="ArgumentException">The axis is linked.</exception>
    /// <param name="axis">The <see cref="Axis"/> it works on.</param>
    private static void RefuseSeriesWhenLinked(Axis axis, string parameter)
    {
        if (axis.Link is not null)
        {
            throw new ArgumentException(
                "This axis is linked to the primary axis; a linked axis shows the primary axis' values in other units and holds no series of its own.",
                parameter);
        }
    }
}
