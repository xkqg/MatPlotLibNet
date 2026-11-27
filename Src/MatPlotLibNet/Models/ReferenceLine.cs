// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Specifies the orientation of a reference line or span region.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder,
/// remove, or renumber.</remarks>
public enum Orientation
{
    /// <summary>Horizontal (constant Y value).</summary>
    Horizontal = 0,

    /// <summary>Vertical (constant X value).</summary>
    Vertical = 1,
}

/// <summary>Specifies which side of a threshold value constitutes a breach zone.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder,
/// remove, or renumber.</remarks>
public enum ThresholdBreach
{
    /// <summary>Breach zone is above (or to the right of) the threshold value.</summary>
    Above = 0,

    /// <summary>Breach zone is below (or to the left of) the threshold value.</summary>
    Below = 1,
}

/// <summary>Represents a horizontal or vertical reference line at a specific data value.</summary>
public sealed class ReferenceLine : IChangeTracked, IStrokedTool, IWidthGiven
{
    private long _stamp;

    /// <summary>Stores <paramref name="value"/> and marks this reference line changed, unless it already holds an equal value.</summary>
    private void Change<T>(ref T slot, T value) => ChangeClock.Change(ref slot, value, ref _stamp);

    long IChangeTracked.LatestChange => _stamp;

    /// <summary>The data value the line sits at. Settable, so a line that tracks something — a playback position,
    /// a live threshold — moves in place instead of the whole figure being rebuilt, which would also throw away the
    /// reader's zoom.</summary>
    public double Value { get; set => Change(ref field, value); }

    /// <summary>Whether the line runs across the plot horizontally, at a constant Y value, or vertically, at a constant X
    /// value. Fixed when the line is created.</summary>
    public Orientation Orientation { get; }

    /// <summary>The colour of the line and its label. <see langword="null"/> (the default) takes the theme's neutral
    /// colour.</summary>
    public Color? Color { get; set => Change(ref field, value); }

    /// <summary>The colour a verb chose for this line where its caller named none (a Fibonacci retracement's level): the library's own
    /// themes draw it, and a theme whose uncoloured lines take the cycle (<see cref="CycleLook.GroupsWearTheCycle"/>, the matplotlib
    /// themes) draws <c>lines.color</c>, the first colour of the cycle, as matplotlib's <c>axhline</c> draws a line that names no colour.
    /// A line that has neither this nor a <see cref="Color"/> takes the theme's neutral colour. A colour the line names itself wins.
    /// Written as <c>verbColor</c>.</summary>
    internal Color? VerbColor { get; set => Change(ref field, value); }

    /// <summary>The dash pattern of the line. Dashed by default.</summary>
    public LineStyle LineStyle { get; set => Change(ref field, value); } = LineStyle.Dashed;

    /// <summary>The width of the line, in pixels, read and drawn as a line's (<see cref="Series.LineSeries.LineWidth"/>) but resting at
    /// the library's one-pixel width of a collection, as matplotlib's <c>axhline</c> and <c>axvline</c> draw one.</summary>
    public double LineWidth { get => this.ReportedLineWidth; set => GivenLineWidth = value; }

    /// <summary>How wide the line is drawn where it was given no width: a line its verb makes itself, so no cycler names its width
    /// (matplotlib's <c>axhline</c> makes a <c>Line2D</c> and takes <c>lines.linewidth</c>).</summary>
    LineWidthRule IWidthGiven.WidthRule => LineWidthRule.OnePixelCollection;

    /// <summary>The width the line was given, <see langword="null"/> while none was and the theme decides. Written as <c>lineWidth</c>.</summary>
    internal double? GivenLineWidth { get; private set => Change(ref field, value); }

    double? IWidthGiven.GivenLineWidth => GivenLineWidth;

    /// <summary>A line that names no colour is drawn in the one its verb chose (<see cref="VerbColor"/>) as a series the verb coloured
    /// is drawn (<see cref="Rendering.SeriesSlot.ChosenOrCycleColor"/>, the first slot of the cycle under the matplotlib themes:
    /// <c>axhline</c> draws in <c>lines.color</c> and asks no cycle), else in the theme's neutral colour.</summary>
    Color IStrokedTool.UnnamedColorUnder(Theme theme) =>
        VerbColor is { } chosen ? theme.SeriesSlot(0).ChosenOrCycleColor(chosen) : theme.Neutral;

    /// <summary>As wide as given, else as the theme's lines (<c>axhline</c>'s <c>lines.linewidth</c>), else at rest: the line's
    /// <see cref="LineWidthRule.OnePixelCollection"/> over the theme's width.</summary>
    double IStrokedTool.WidthUnder(Theme theme) => theme.Looks.Lines.WidthOf(this);

    /// <summary>Text drawn beside the line, at the right end of a horizontal line and the top of a vertical one, in the
    /// line's colour. <see langword="null"/> (the default) draws none.</summary>
    public string? Label { get; set => Change(ref field, value); }

    /// <summary>Creates a new reference line at the given value and orientation.</summary>
    /// <param name="value">The data value the line sits at: a Y value for a horizontal line, an X value for a vertical one.</param>
    /// <param name="orientation">Whether the line is horizontal (constant Y) or vertical (constant X).</param>
    public ReferenceLine(double value, Orientation orientation)
    {
        Value = value;
        Orientation = orientation;
    }
}
