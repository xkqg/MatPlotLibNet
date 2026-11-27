// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Represents the configuration of a single axis (X or Y) on an axes.</summary>
public class Axis : IChangeTracked
{
    private long _stamp;

    /// <summary>Stores <paramref name="value"/> and marks this axis changed, unless it already holds an equal value.</summary>
    private protected void Change<T>(ref T slot, T value) => ChangeClock.Change(ref slot, value, ref _stamp);

    /// <summary>The mark of the newest change to this axis or to the formatter and locator it holds.</summary>
    internal long LatestChange =>
        ChangeClock.Latest(_stamp, ChangeClock.Latest(ChangeClock.LatestOf(TickFormatter), ChangeClock.LatestOf(TickLocator)));

    long IChangeTracked.LatestChange => LatestChange;

    /// <summary>The axis title drawn beside the axis. <see langword="null"/> (the default) draws none.</summary>
    public string? Label { get; set => Change(ref field, value); }

    /// <summary>Overrides of the axis title's font (size, weight, family, slant, colour) and its extra distance from the
    /// tick labels (<see cref="TextStyle.Pad"/>); whatever it leaves unset follows the theme. <see langword="null"/> (the
    /// default) draws the title in the theme's label font.</summary>
    public TextStyle? LabelStyle { get; set => Change(ref field, value); }

    /// <summary>The lower limit of the axis in data units. <see langword="null"/> (the default) lets the axis scale itself
    /// to the data; a value pins that end exactly, so data past it is clipped.</summary>
    public double? Min { get; set => Change(ref field, value); }

    /// <summary>The upper limit of the axis in data units. <see langword="null"/> (the default) lets the axis scale itself
    /// to the data; a value pins that end exactly, so data past it is clipped.</summary>
    public double? Max { get; set => Change(ref field, value); }

    /// <summary>True when <see cref="Min"/> or <see cref="Max"/> is set: that side is drawn exactly as set.</summary>
    internal bool HasLimit => Min.HasValue || Max.HasValue;

    /// <summary>True when a limit is set and the axis has a range of its own for it to limit: not a linked axis
    /// (<see cref="Link"/>), which shows the primary's range whatever limit it carries. The one rule by which a top X axis or a
    /// Y axis past the primary counts as a limit of its axes (<c>Axes.HasExplicitLimit</c>).</summary>
    internal bool LimitsItsOwnRange => HasLimit && Link is null;

    /// <summary>How values are spread along the axis: linearly by default, or logarithmically, symmetric-logarithmically,
    /// by logit or as dates.</summary>
    public AxisScale Scale { get; set => Change(ref field, value); } = AxisScale.Linear;

    /// <summary>Linear threshold for <see cref="AxisScale.SymLog"/>. Values within [-SymLogLinThresh, SymLogLinThresh] are mapped linearly.
    /// Default <see cref="SymlogTransform.DefaultLinThresh"/>. A threshold set here wins under every theme; an axis given none is drawn
    /// with its theme's (2 under the matplotlib themes, matplotlib's <c>SymmetricalLogScale</c> default;
    /// <see cref="SymlogTransform.DefaultLinThresh"/> under the library's own), which this property, asked without a theme,
    /// reports.</summary>
    public double SymLogLinThresh
    {
        get => double.IsNaN(_symLogLinThresh) ? Rendering.SymlogTransform.DefaultLinThresh : _symLogLinThresh;
        set => Change(ref _symLogLinThresh, value);
    }

    /// <summary>The threshold its author gave; NaN while none is given (no threshold is a NaN), so an axis is no larger than it was.</summary>
    private double _symLogLinThresh = double.NaN;

    /// <summary>The symlog threshold its author gave, <see langword="null"/> when none was given: what the JSON writes and what wins over
    /// the theme's.</summary>
    internal double? GivenSymLogLinThresh => double.IsNaN(_symLogLinThresh) ? null : _symLogLinThresh;

    /// <summary>The main ticks of the axis: the marks that carry labels. Shown by default.</summary>
    public TickConfig MajorTicks { get; set => Change(ref field, value); } = RestingMajorTicks;

    /// <summary>The major ticks every axis starts with. A record no one can change, so every axis shares one: a change is a new
    /// record on that axis alone.</summary>
    private static readonly TickConfig RestingMajorTicks = new();

    // matplotlib minor tick defaults: xtick.minor.size = 2.0 pt, xtick.minor.width = 0.6 pt,
    // and minor ticks are OFF by default (ax.minorticks_on() / WithMinorTicks() enables them).
    /// <summary>The minor ticks of the axis: the short unlabelled marks between the major ones. Hidden by default (under the
    /// matplotlib themes a log or a logit axis shows them unless they are switched off), and shorter and thinner than the major
    /// ticks; unless <see cref="TickConfig.Spacing"/> is set they divide each major interval on a linear axis (into five, or under the
    /// matplotlib themes into four unless the interval is 1, 2.5 or 5 times a power of ten) and stand at 2 to 9 times each power of
    /// ten on a log one.</summary>
    public TickConfig MinorTicks { get; set => Change(ref field, value); } = RestingMinorTicks;

    /// <summary>The minor ticks every axis starts with, shared as <see cref="RestingMajorTicks"/> is.</summary>
    private static readonly TickConfig RestingMinorTicks = new()
    {
        RestsAsMinor = true,   // off, 2.0 pt long and 0.6 pt wide (~2.78 px and ~0.83 px at 100 DPI), none of it given
    };

    /// <summary>The ticks held, the theme and the ticks as that theme draws them: the one answer <see cref="MajorTicksUnder"/> and
    /// <see cref="MinorTicksDrawnUnder"/> keep. One reference is the whole of it, so a reader never sees half of a pair.</summary>
    private sealed class TickLook(TickConfig held, Theme theme, TickConfig drawn)
    {
        internal TickConfig Drawn { get; } = drawn;
        internal bool Answers(TickConfig ticks, Theme under) => ReferenceEquals(held, ticks) && ReferenceEquals(theme, under);
    }

    /// <summary>The look kept for each kind of tick. An axis holds one reference to it, and only an axis whose theme names a look for its
    /// ticks ever makes one.</summary>
    private sealed class TickLooks
    {
        internal TickLook? Major;
        internal TickLook? Minor;
    }

    private TickLooks? _drawnTicks;

    /// <summary><see cref="MajorTicks"/> as <paramref name="theme"/> draws them (<see cref="TickConfig.DrawnUnder"/>), worked out once for
    /// the ticks held and the theme and kept: a theme that names a look for the ticks gives a copy of them, and a draw asks for the copy
    /// several times for every axis. Every 2-D axis draws and measures its major ticks by it.</summary>
    /// <param name="theme">The theme the axis is drawn under.</param>
    /// <returns>The major ticks as the theme draws them.</returns>
    internal TickConfig MajorTicksUnder(Theme theme)
    {
        var held = MajorTicks;
        if (_drawnTicks?.Major is { } look && look.Answers(held, theme)) return look.Drawn;
        var drawn = held.DrawnUnder(theme, major: true);
        // A theme that names nothing for these ticks leaves them as they are, which is nothing to keep.
        if (!ReferenceEquals(drawn, held)) (_drawnTicks ??= new()).Major = new TickLook(held, theme, drawn);
        return drawn;
    }

    /// <summary><see cref="MinorTicks"/> as <paramref name="theme"/> draws them on an axis drawn on <paramref name="scale"/>: as drawn
    /// (<see cref="MinorTicksDrawnUnder"/>) where they stand (the theme's <see cref="AxisTickLook.PlacesMinors"/>); minor ticks that do
    /// not stand draw nothing and are as they are held, which saves working out a look nobody reads.</summary>
    /// <param name="theme">The theme the axis is drawn under.</param>
    /// <param name="scale">The scale the axis' ticks are drawn on.</param>
    /// <returns>The minor ticks as drawn.</returns>
    internal TickConfig MinorTicksUnder(Theme theme, ScaleRule scale) =>
        theme.Looks.Ticks.PlacesMinors(MinorTicks, scale) ? MinorTicksDrawnUnder(theme) : MinorTicks;

    /// <summary><see cref="MinorTicks"/> as <paramref name="theme"/> draws them when they stand, shown (a log axis' minor ticks stand
    /// under the matplotlib themes with no call), kept as <see cref="MajorTicksUnder"/> keeps the major ones.</summary>
    /// <param name="theme">The theme the axis is drawn under.</param>
    /// <returns>The minor ticks as the theme draws them.</returns>
    internal TickConfig MinorTicksDrawnUnder(Theme theme)
    {
        var held = MinorTicks;
        if (_drawnTicks?.Minor is { } look && look.Answers(held, theme)) return look.Drawn;
        var drawn = held.DrawnUnder(theme, major: false);
        if (!drawn.Visible) drawn = drawn with { Visible = true };
        if (!ReferenceEquals(drawn, held)) (_drawnTicks ??= new()).Minor = new TickLook(held, theme, drawn);
        return drawn;
    }

    /// <summary>Draws the axis the other way round: an X axis from right to left, a Y axis from top to bottom
    /// (matplotlib's <c>Axis.set_inverted(True)</c>, the state <c>invert_xaxis</c> and <c>invert_yaxis</c> switch). The
    /// ticks are those of the same range, each drawn where its value is, by the rule a limit set high to low follows
    /// (<c>SetYLim(5.5, 0.5)</c>).</summary>
    /// <remarks>Inverted and limits set high to low ask for the same thing and are one state, not two turns: an axis whose
    /// limits are already set high to low is drawn reversed once when it is also inverted, not turned back to forward, as
    /// matplotlib's <c>set_inverted(True)</c> leaves an inverted axis inverted. Switching it on and off again returns the
    /// axis to the way its limits draw it (matplotlib's <c>invert_yaxis</c> called twice). A zoom or a pan writes the
    /// limits the way the axis is drawn, so an inverted axis stays reversed through them. Read by Cartesian axes, their
    /// secondary axes and insets; polar and 3-D axes do not read it.</remarks>
    public bool Inverted { get; set => Change(ref field, value); }

    /// <summary>
    /// Data padding as a fraction of the data range (matplotlib <c>axes.xmargin</c>/<c>ymargin</c>).
    /// When <see langword="null"/> (the default), the renderer inherits the value from
    /// <see cref="Styling.Theme.AxisXMargin"/> / <see cref="Styling.Theme.AxisYMargin"/>:
    /// <c>MatplotlibClassic</c> → 0.0 (data touches spines), <c>MatplotlibV2</c>/<c>Default</c> → 0.05.
    /// </summary>
    public double? Margin { get; set => Change(ref field, value); }

    /// <summary>Writes the tick labels; <see langword="null"/> for the plain numeric labels.</summary>
    /// <remarks>A figure's JSON (<c>ToJson</c>, the MCP server, the transports) carries the formatters the library
    /// ships, with what they were made with. A formatter written outside the library is code, which a JSON document
    /// cannot carry: it is left out, and the axis reads back with the plain labels.</remarks>
    public ITickFormatter? TickFormatter { get; set => Change(ref field, value); }

    /// <summary>Places the ticks; <see langword="null"/> for the default nice-number ticks.</summary>
    /// <remarks>A figure's JSON carries the locators the library ships, with what they were made with. A locator
    /// written outside the library is code, which a JSON document cannot carry: it is left out, and the axis reads
    /// back with the default ticks.</remarks>
    public ITickLocator? TickLocator { get; set => Change(ref field, value); }

    /// <summary>Links this axis to the primary axis of its direction (matplotlib's <c>secondary_xaxis</c> and
    /// <c>secondary_yaxis</c>): the axis has no range and no series of its own, and shows the range the primary axis shows, each
    /// value written through the transform, so a tick for a value stands where the primary axis has the value it maps back to.
    /// <see langword="null"/> (the default) is an axis with a range of its own.</summary>
    /// <remarks>Set it through the builder of the axis: <c>SecondaryXAxisBuilder.Link</c> for the top X axis,
    /// <c>YAxisSlotBuilder.Link</c> for a Y axis stacked past the primary on either side, <c>XAxisSlotBuilder.Link</c> for an X axis
    /// below the plot. Those are the axes that draw a link; a primary axis has nothing to link to, and a link set on one is reported
    /// once a draw in the figure's diagnostics. The builders refuse a link on an axis that holds a series and a series on an axis
    /// that is linked; set here, the link is taken as it is.
    /// <para>A figure's JSON (<c>ToJson</c>, the MCP server, the transports) carries a link made with <see cref="AxisTransform.Linear"/>,
    /// <see cref="AxisTransform.Reciprocal"/>, <see cref="AxisTransform.Logarithm"/> or <see cref="AxisTransform.Power"/>, with what it
    /// was made with. A link made with <see cref="AxisTransform.Of"/> is a pair of functions, which is code and which a JSON document
    /// cannot carry: it is left out, and the axis reads back unlinked.</para></remarks>
    /// <example>
    /// Through the builder, the period along the top of a plot over frequency:
    /// <code>
    /// Plt.Create().AddSubPlot(1, 1, 1, ax => ax
    ///     .Plot(frequency, power)
    ///     .WithSecondaryXAxis(top => top.Link(AxisTransform.Reciprocal())));
    /// </code>
    /// </example>
    public AxisTransform? Link { get; set => Change(ref field, value); }

    /// <summary>Whether this axis, standing past the primary (a Y axis stacked past it, the top X axis), is drawn with
    /// <paramref name="series"/> as its own: it has series, or it is linked (<see cref="Link"/>) and draws the primary's range in its own
    /// unit without any. One with neither is placed nowhere it is seen and draws nothing.</summary>
    /// <param name="series">The series plotted against this axis.</param>
    /// <returns><see langword="true"/> when the axis is drawn.</returns>
    internal bool IsDrawnWith(IReadOnlyList<Series.ISeries> series) => series.Count > 0 || Link is not null;

    /// <summary>Shorthand for <see cref="TickConfig.LabelRotation"/> on <see cref="MajorTicks"/>.
    /// Degrees; negative = clockwise (matplotlib convention: <c>-45</c> = schuin rechts-onder).
    /// Default 0 (horizontal).</summary>
    public double LabelRotation
    {
        get => MajorTicks.LabelRotation;
        set => MajorTicks = MajorTicks with { LabelRotation = value };
    }
}

/// <summary>Represents the configuration of the Z axis on a 3D axes. Inherits all standard axis
/// properties (<see cref="Axis.Label"/>, <see cref="Axis.Min"/>/<see cref="Axis.Max"/>,
/// <see cref="Axis.MajorTicks"/>, etc.) and may be extended with 3D-specific properties.</summary>
public sealed class Axis3D : Axis { }

/// <summary>Specifies the scale type used for an axis.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder,
/// remove, or renumber.</remarks>
public enum AxisScale
{
    /// <summary>Linear scale.</summary>
    Linear = 0,

    /// <summary>Logarithmic scale.</summary>
    Log = 1,

    /// <summary>Symmetric logarithmic scale that handles values near zero.</summary>
    SymLog = 2,

    /// <summary>Logit scale for probability data in the range (0, 1).</summary>
    Logit = 3,

    /// <summary>Date scale that interprets values as OLE Automation dates.</summary>
    Date = 4,
}

/// <summary>Configures the appearance and spacing of axis tick marks.</summary>
public sealed record TickConfig
{
    /// <summary>Whether these ticks are drawn, marks and labels both. On by default; off draws nothing and reserves no room
    /// for them. An axis' minor ticks read off until switched on; under the matplotlib themes a log or a logit axis draws them
    /// unless they were switched off.</summary>
    public bool Visible { get => GivenVisible ?? !RestsAsMinor; init => GivenVisible = value; }

    /// <summary>The visibility that was given, <see langword="null"/> when none was: the ticks are then shown as their kind and
    /// the axis' scale say (an axis' minor ticks are off, except on a log or a logit axis under the matplotlib themes).</summary>
    internal bool? GivenVisible { get; private init; }

    /// <summary>The distance between neighbouring ticks, in data units. <see langword="null"/> (the default) places the
    /// ticks automatically; for minor ticks it replaces the split of each major interval and the minor ticks a log scale
    /// places.</summary>
    public double? Spacing { get; init; }

    /// <summary>A format string for the tick labels, kept with the tick settings and written to the JSON. The labels drawn
    /// come from the axis' <see cref="Axis.TickFormatter"/>, or are plain numbers when it is <see langword="null"/>.</summary>
    public string? Format { get; init; }

    /// <summary>Which way the tick marks point: out of the plot (the default), into it, or across the axis line. Ticks given no
    /// direction read <see cref="RestingDirection"/> and point as their theme says (the matplotlib classic theme points them
    /// in); a direction that was given, out included, is drawn as it is under every theme.</summary>
    public TickDirection Direction { get => GivenDirection ?? RestingDirection; init => GivenDirection = value; }

    // matplotlib `xtick.major.size = 3.5` POINTS → at 100 DPI = 4.861 px.
    // We store in pixels (the unit Skia and SVG render in), pre-converted.
    /// <summary>The length of a tick mark, in pixels. Default 4.86 (matplotlib's 3.5 points at 100 dpi); ticks given no length
    /// are drawn as long as their theme says, a length that was given as it is.</summary>
    public double Length { get => GivenLength ?? (RestsAsMinor ? RestingMinorLength : RestingLength); init => GivenLength = value; }

    // matplotlib `xtick.major.width = 0.8` POINTS → at 100 DPI = 1.111 px.
    /// <summary>The line width of a tick mark, in pixels. Default 1.11 (matplotlib's 0.8 points at 100 dpi); ticks given no
    /// width are drawn as wide as their theme says, a width that was given as it is.</summary>
    public double Width { get => GivenWidth ?? (RestsAsMinor ? RestingMinorWidth : RestingWidth); init => GivenWidth = value; }

    /// <summary>The direction, length, width and pad a tick reads when none was given (matplotlib's default style).</summary>
    private const TickDirection RestingDirection = TickDirection.Out;
    private static readonly double RestingLength = 3.5.PointsInPixels;
    private static readonly double RestingWidth = 0.8.PointsInPixels;
    private static readonly double RestingPad = 3.5.PointsInPixels;

    /// <summary>The length and the width a minor tick reads when none was given (matplotlib's <c>xtick.minor.size</c> and
    /// <c>xtick.minor.width</c> in the default style).</summary>
    private static readonly double RestingMinorLength = 2.0.PointsInPixels;
    private static readonly double RestingMinorWidth = 0.6.PointsInPixels;

    /// <summary>Whether these are the minor ticks an axis starts with, which read the length and the width of a minor tick where none
    /// was given: set on that one record, and carried by every change of it (<c>with</c>), so a minor tick the author gave a visibility
    /// still reads a minor tick's size and one the author gave a width is not mistaken for the resting one.</summary>
    internal bool RestsAsMinor { get; init; }

    /// <summary>The direction that was given, <see langword="null"/> when none was and the ticks point as their theme says.</summary>
    internal TickDirection? GivenDirection { get; private init; }

    /// <summary>The length that was given, <see langword="null"/> when none was.</summary>
    internal double? GivenLength { get; private init; }

    /// <summary>The width that was given, <see langword="null"/> when none was.</summary>
    internal double? GivenWidth { get; private init; }

    /// <summary>The pad that was given, <see langword="null"/> when none was.</summary>
    internal double? GivenPad { get; private init; }

    /// <summary>These ticks as they are drawn under <paramref name="theme"/>: every setting that was given as it is, and the
    /// direction, length, width and pad that were not as the theme draws them (<see cref="AxisTickLook.Direction"/>,
    /// <see cref="AxisTickLook.Length"/>, <see cref="AxisTickLook.Width"/>, <see cref="AxisTickLook.Pad"/>; the length of major ticks only,
    /// minor ticks keeping their own, and their width and pad the theme's minor ones, <see cref="AxisTickLook.MinorWidth"/> and
    /// <see cref="AxisTickLook.MinorPad"/>, the major pad where it names no minor one). The same ticks when the theme names none of them.</summary>
    /// <param name="theme">The theme the axis is drawn under.</param>
    /// <param name="major">Whether these are the major ticks.</param>
    /// <returns>The ticks as drawn.</returns>
    internal TickConfig DrawnUnder(Theme theme, bool major)
    {
        // A kind the theme names no setting for is not changed: the same ticks.
        var look = theme.Looks.Ticks;
        if (look.NamesNothingFor(major)) return this;
        return this with
        {
            Direction = look.DirectionOver(GivenDirection, RestingDirection),
            Length = look.LengthOver(GivenLength, major, Length),
            Width = look.WidthOver(GivenWidth, major, Width),
            Pad = look.PadOver(GivenPad, major, Pad),
        };
    }

    /// <summary>How far the mark reaches outside the axes, which is where its label's pad starts (matplotlib's
    /// <c>Tick.get_tick_padding</c>: the size times 0 pointing in, a half across the axis, 1 pointing out).</summary>
    internal double OutsideLength => TickMarkSpan.Of(Direction, Length, 0).Outer;

    /// <summary>From an edge to the far side of the tick labels drawn on it: the part of the mark outside the axes
    /// (<see cref="OutsideLength"/>: none for a mark pointing in, half for one across the axis, all of one pointing out, as matplotlib's
    /// <c>Tick.get_tick_padding</c>), the pad and the label extent (the widest label on a left or right edge, the tallest on a top or
    /// bottom edge). The layout reserves this band with the same rule the drawing places the labels by.</summary>
    /// <param name="labelExtent">The widest or tallest label drawn on that edge, 0 when none was drawn.</param>
    /// <returns>The band in pixels; 0 when no label was drawn.</returns>
    internal double TickLabelBand(double labelExtent) => labelExtent > 0 ? OutsideLength + Pad + labelExtent : 0;

    /// <summary>The colour of the tick marks. <see langword="null"/> (the default) takes the theme's foreground colour.</summary>
    public Color? Color { get; init; }

    /// <summary>The size of the tick labels, in the units of the theme's font sizes. <see langword="null"/> (the default)
    /// takes the theme's tick-label size.</summary>
    public double? LabelSize { get; init; }

    /// <summary>The colour of the tick labels. <see langword="null"/> (the default) takes the theme's tick-label colour.</summary>
    public Color? LabelColor { get; init; }

    // matplotlib `xtick.major.pad = 3.5` POINTS → at 100 DPI = 4.861 px.
    /// <summary>The gap between the part of a tick mark outside the axes and its label, in pixels. Default 4.86 (matplotlib's
    /// 3.5 points at 100 dpi); ticks given no pad stand their labels as far as their theme says, a pad that was given as it
    /// is.</summary>
    public double Pad { get => GivenPad ?? RestingPad; init => GivenPad = value; }

    /// <summary>When <c>true</c>, ticks and labels are drawn on both sides of the axes
    /// (e.g. Y ticks on both left and right spines). Equivalent to matplotlib's
    /// <c>ax.tick_params(right=True, labelright=True)</c>. Default <c>false</c>.</summary>
    public bool Mirror { get; init; } = false;

    /// <summary>Whether the tick labels stand on the axis' own side: the bottom of an X axis, the left of a Y axis, the side of a Y axis
    /// stacked beside the plot. Equivalent to matplotlib's <c>tick_params(labelbottom=...)</c> and <c>labelleft</c>; off, the marks stay on
    /// that side and the layout keeps no room for labels there. With <see cref="Mirror"/> the labels then stand on the mirrored side only,
    /// as matplotlib's <c>matshow</c> numbers its columns along the top. A Cartesian axes only. Default <c>true</c>.</summary>
    public bool LabelsOnOwnSide { get; init; } = true;

    /// <summary>What of <paramref name="ink"/> is drawn on the axis' own side: all of it, or the marks alone when the labels stand off
    /// it (<see cref="LabelsOnOwnSide"/>). The one rule the drawing of every Cartesian axis reads on its own side.</summary>
    /// <param name="ink">What the pass draws.</param>
    /// <returns><paramref name="ink"/> itself, or without its labels when they stand on the other side.</returns>
    internal TickInk OnOwnSide(TickInk ink) => LabelsOnOwnSide ? ink : ink.WithoutLabels;

    /// <summary>Whether tick labels are drawn on the axis' own side: the ticks are visible and labelled there. What the layout reserves a
    /// label band, or estimates one, by.</summary>
    internal bool LabelsOwnSide => Visible && LabelsOnOwnSide;

    /// <summary>Tick-label rotation in degrees (matplotlib's <c>ax.tick_params(labelrotation=...)</c>).
    /// Zero means horizontal labels; positive tilts counter-clockwise. When 0, the renderer
    /// may still auto-rotate X-axis labels to 30° if adjacent labels would overlap
    /// (matches matplotlib <c>Figure.autofmt_xdate</c>). Default 0.</summary>
    public double LabelRotation { get; init; } = 0;
}
