// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling;

/// <summary>Where the ticks of a 2-D and a 3-D axis stand and how their marks are drawn where their author says nothing. A
/// value left unset keeps the library's own rule, which the reader of that value holds.</summary>
internal sealed record AxisTickLook : IThemeKeys<AxisTickLook>
{
    /// <summary>The library's own ticks.</summary>
    internal static AxisTickLook Library { get; } = new();

    /// <inheritdoc />
    public void WriteKeys(ThemeDto keys)
    {
        keys.AxisTickSteps = Ladder?.Steps;
        keys.AxisTickBins = Ladder?.Bins;
        keys.AxisTickMinTicks = Ladder?.MinTicks;
        keys.AxisTickBinsByLength = (Ladder is { BinsByLength: true }).OrNullAtRest(false);
        keys.TickDirection = Direction;
        keys.TickLength = Length;
        keys.TickWidth = Width;
        keys.TickPad = Pad;
        keys.MinorTickWidth = MinorWidth;
        keys.MinorTickPad = MinorPad;
        keys.TicksOnAllSides = OnAllSides.OrNullAtRest(false);
        keys.MinorTicksByLocator = MinorsByLocator.OrNullAtRest(false);
        keys.MinorGridLikeMajor = MinorGridLikeMajor.OrNullAtRest(false);
    }

    /// <inheritdoc />
    public static AxisTickLook ReadKeys(ThemeDto keys) => new()
    {
        Ladder = keys.AxisTickSteps is { } steps && keys.AxisTickBins is { } bins && keys.AxisTickMinTicks is { } minTicks
            ? new TickLadder(steps, bins, minTicks, keys.AxisTickBinsByLength ?? false)
            : null,
        Direction = keys.TickDirection,
        Length = keys.TickLength,
        Width = keys.TickWidth,
        Pad = keys.TickPad,
        MinorWidth = keys.MinorTickWidth,
        MinorPad = keys.MinorTickPad,
        OnAllSides = keys.TicksOnAllSides ?? false,
        MinorsByLocator = keys.MinorTicksByLocator ?? false,
        // The split of a minor step has no key of its own: it has always followed this one (matplotlib's styles split 'auto').
        MinorSplit = keys.MinorTicksByLocator == true ? AutoMinorLocator.Auto : AutoMinorLocator.Library,
        MinorGridLikeMajor = keys.MinorGridLikeMajor ?? false,
    };

    /// <summary>The steps and bins of the locator that places the ticks of an axis with no locator or spacing of its own, and that
    /// an axis rounded out to its ticks rounds by: matplotlib's <c>AutoLocator</c>, under the classic style in classic mode
    /// (<c>nbins = 9</c>, <c>steps = [1, 2, 5, 10]</c>), under the default style as many as the axis' length holds
    /// (<c>steps = [1, 2, 2.5, 5, 10]</c>). Unset: the library's own rule (<see cref="AutoLocator.ForLength"/>).</summary>
    public TickLadder? Ladder { get; init; }

    /// <summary>Which way the ticks of a 2-D axis point when their author gave no direction: matplotlib's <c>xtick.direction</c>,
    /// <c>'in'</c> under the classic style. Unset: out.</summary>
    public TickDirection? Direction { get; init; }

    /// <summary>How long a major tick of a 2-D axis is, in pixels: matplotlib's <c>xtick.major.size</c>, 4 pt under the classic
    /// style. Unset: <see cref="Models.TickConfig.RestingLength"/>.</summary>
    public double? Length { get; init; }

    /// <summary>How wide a major tick of a 2-D axis is, in pixels: matplotlib's <c>xtick.major.width</c>, 0.5 pt under the classic
    /// style. Unset: <see cref="Models.TickConfig.RestingWidth"/>.</summary>
    public double? Width { get; init; }

    /// <summary>How far a tick label of a 2-D axis stands from its mark, in pixels: matplotlib's <c>xtick.major.pad</c>, 4 pt under
    /// the classic style. Unset: <see cref="Models.TickConfig.RestingPad"/>.</summary>
    public double? Pad { get; init; }

    /// <summary>How wide a minor tick of a 2-D axis is, in pixels: matplotlib's <c>xtick.minor.width</c>, 0.6 pt under the default
    /// style and 0.5 pt under the classic one. Unset: <see cref="Models.TickConfig.RestingMinorWidth"/>.</summary>
    public double? MinorWidth { get; init; }

    /// <summary>How far the label of a minor tick stands from its mark, in pixels: matplotlib's <c>xtick.minor.pad</c>, 3.4 pt
    /// under the default style and 4 pt under the classic one. Unset: <see cref="Models.TickConfig.RestingPad"/>.</summary>
    public double? MinorPad { get; init; }

    /// <summary>Whether a 2-D axes marks its top and right edges with the ticks of its X and Y axes, without labels, where no other
    /// axis stands there: matplotlib's <c>xtick.top</c> and <c>ytick.right</c>, on under the classic style. Otherwise the bottom and
    /// left edges only.</summary>
    public bool OnAllSides { get; init; }

    /// <summary>Whether an axis' minor ticks are the ones matplotlib's minor locator places: an axis whose scale brings a minor
    /// locator of its own (log, logit) draws its minor marks with no call, a linear, symmetric-log or date axis none until they are
    /// switched on, the minor grid stands only where the minor ticks do, and none of it once the ticks are switched off. Otherwise
    /// the minor marks are drawn only when asked, and the minor grid on the places the minor ticks would stand.</summary>
    public bool MinorsByLocator { get; init; }

    /// <summary>How the step between two major ticks of a linear or date axis is split into minor ticks: matplotlib's
    /// <c>AutoMinorLocator</c> with <c>xtick.minor.ndivs: auto</c> under both its styles (<see cref="AutoMinorLocator.Auto"/>, four or five parts
    /// by the step). Unset: fifths (<see cref="AutoMinorLocator.Library"/>).</summary>
    public AutoMinorLocator MinorSplit { get; init; } = AutoMinorLocator.Library;

    /// <summary>Whether a minor grid line is drawn in the grid's own colour, width and line style, as matplotlib draws it (its
    /// <c>grid.minor.*</c> settings fall back to <c>grid.*</c>). Otherwise half as wide and half as opaque as a major line.</summary>
    public bool MinorGridLikeMajor { get; init; }

    /// <summary>The locator whose named ticks include minor ones with a label (a log or a logit axis', <see cref="ITickSpaceLocator"/>),
    /// under a theme whose minor ticks are matplotlib's minor locator's (<see cref="MinorsByLocator"/>); <see langword="null"/> under the
    /// library's own themes, which draw every named tick as a major one, and for any other locator.</summary>
    /// <param name="locator">The locator that places the axis' ticks, or <see langword="null"/>.</param>
    /// <returns>The locator that tells its labelled minor ticks, or <see langword="null"/>.</returns>
    internal ITickSpaceLocator? MinorNamer(ITickLocator? locator) => MinorsByLocator ? locator as ITickSpaceLocator : null;

    /// <summary>Whether an axis' minor ticks (<paramref name="minor"/>) stand on an axis drawn on <paramref name="scale"/>: the one rule
    /// the minor marks and the minor grid follow. They do when they were switched on; under a theme whose minor ticks are matplotlib's
    /// minor locator's (<see cref="MinorsByLocator"/>) they also do, unless they were switched off, on a scale whose minor locator places
    /// them with no call (a log and a logit scale, <see cref="ScaleRule.PlacesMinorsByItself"/>), as matplotlib's does (a linear,
    /// symmetric-log or date axis places none until <c>minorticks_on()</c>).</summary>
    /// <param name="minor">The axis' minor ticks as given.</param>
    /// <param name="scale">The scale the axis' ticks are drawn on (a linked axis', <see cref="ITickPlacement.ScaleAlong"/>).</param>
    /// <returns><see langword="true"/> when the minor ticks stand.</returns>
    internal bool PlacesMinors(TickConfig minor, ScaleRule scale) =>
        minor.Visible || (minor.GivenVisible is null && MinorsByLocator && scale.PlacesMinorsByItself);

    /// <summary>Whether a minor grid line may stand on an axis: always under a theme that draws a minor grid on every axis that asks for
    /// one (<see cref="MinorsByLocator"/> off); under the matplotlib themes only where the axis' minor ticks stand
    /// (<see cref="PlacesMinors"/>), as matplotlib's grid line is a tick's.</summary>
    /// <param name="minor">The axis' minor ticks as given.</param>
    /// <param name="scale">The scale the axis is drawn on.</param>
    /// <returns><see langword="true"/> when the axis draws its minor grid lines.</returns>
    internal bool CarriesMinorGrid(TickConfig minor, ScaleRule scale) => !MinorsByLocator || PlacesMinors(minor, scale);

    /// <summary>Whether the grid lines of an axis that has no minor grid of its own (the wall lines of a 3-D axis) stand on its minor
    /// values too: under the matplotlib themes, where a grid line is a tick's, wherever the minor ticks stand
    /// (<see cref="PlacesMinors"/>, as <c>axis3d.Axis.draw_grid</c> draws a line for every tick in view); never under the library's own.</summary>
    /// <param name="minor">The axis' minor ticks as given.</param>
    /// <param name="scale">The scale the axis is drawn on.</param>
    /// <returns><see langword="true"/> when the grid stands on the minor values too.</returns>
    internal bool GridStandsOnMinors(TickConfig minor, ScaleRule scale) => MinorsByLocator && PlacesMinors(minor, scale);

    /// <summary>The minor values between <paramref name="majors"/> by an axis' minor tick settings (<paramref name="minor"/>), the one rule
    /// the minor tick marks, the minor grid lines and the minor marks and wall lines of a 3-D axis stand on: the settings' own
    /// <see cref="TickConfig.Spacing"/> when they set a positive one (a non-positive spacing is a computed one that came out wrong, never
    /// an instruction); else the minor ticks the scale's minor locator places (<paramref name="scaleMinors"/>, a log or logit axis'); else
    /// the step between the first two majors split as this look says (<see cref="MinorSplit"/>: the library's fifths, or matplotlib's
    /// <c>AutoMinorLocator</c>).</summary>
    /// <param name="minor">The axis' minor tick settings.</param>
    /// <param name="majors">The major tick values, low to high.</param>
    /// <param name="scaleMinors">The minor ticks the scale's minor locator places, or <see langword="null"/>.</param>
    /// <returns>The walk; <c>foreach</c> over it allocates nothing.</returns>
    internal MinorWalk MinorValuesBetween(TickConfig minor, double[] majors, double[]? scaleMinors = null)
    {
        if (minor.Spacing is { } spacing && spacing > 0) return new(majors, spacing);
        if (scaleMinors is not null) return MinorWalk.Over(scaleMinors);
        return MinorSplit.Between(majors);
    }

    /// <summary>The line a minor grid line is drawn with, from the major line <paramref name="major"/>: the same line where the theme
    /// draws the minor grid like the major (<see cref="MinorGridLikeMajor"/>, matplotlib's <c>grid.minor.*</c> falling back to
    /// <c>grid.*</c>), half as wide and half as opaque otherwise.</summary>
    /// <param name="major">The major grid line.</param>
    /// <returns>The minor grid line.</returns>
    internal StrokeStyle MinorGridStroke(StrokeStyle major) => MinorGridLikeMajor ? major : major.Faded(0.5);

    /// <summary>Whether this look names nothing for a kind of tick, so the ticks are drawn as they are.</summary>
    /// <param name="major">Whether the ticks are major.</param>
    /// <returns>Whether it names none of direction, length, width and pad.</returns>
    internal bool NamesNothingFor(bool major) => Direction is null && Pad is null && (major ? Length is null && Width is null : MinorWidth is null && MinorPad is null);

    /// <summary>The direction a tick points: the one given, else this look's, else <paramref name="resting"/>.</summary>
    /// <param name="given">The value its author gave, <see langword="null"/> when none.</param>
    /// <param name="resting">The value its owner rests at where the theme names none.</param>
    /// <returns>The value drawn.</returns>
    internal TickDirection DirectionOver(TickDirection? given, TickDirection resting) => given ?? Direction ?? resting;

    /// <summary>The length of a tick: the one given, else this look's for a major tick (a minor tick keeps its own), else <paramref name="resting"/>.</summary>
    /// <param name="given">The value its author gave, <see langword="null"/> when none.</param>
    /// <param name="major">Whether the tick is major.</param>
    /// <param name="resting">The value its owner rests at where the theme names none.</param>
    /// <returns>The value drawn.</returns>
    internal double LengthOver(double? given, bool major, double resting) => given ?? (major ? Length : null) ?? resting;

    /// <summary>The width of a tick: the one given, else this look's for its kind, else <paramref name="resting"/>.</summary>
    /// <param name="given">The value its author gave, <see langword="null"/> when none.</param>
    /// <param name="major">Whether the tick is major.</param>
    /// <param name="resting">The value its owner rests at where the theme names none.</param>
    /// <returns>The value drawn.</returns>
    internal double WidthOver(double? given, bool major, double resting) => given ?? (major ? Width : MinorWidth) ?? resting;

    /// <summary>The pad of a tick's label: the one given, else this look's for its kind (the major pad where it names no minor one), else <paramref name="resting"/>.</summary>
    /// <param name="given">The value its author gave, <see langword="null"/> when none.</param>
    /// <param name="major">Whether the tick is major.</param>
    /// <param name="resting">The value its owner rests at where the theme names none.</param>
    /// <returns>The value drawn.</returns>
    internal double PadOver(double? given, bool major, double resting) => given ?? (major ? Pad : MinorPad ?? Pad) ?? resting;
}
