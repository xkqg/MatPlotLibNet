// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Bars that float, collected one by one and made into one bar series: each stands at a place of its own along X, on a
/// foot of its own, as tall as its height, and is named by a text (the label of its category). The bars are one width and one
/// colour. A Renko brick and the mark of a calendar event are both such a bar; this is the one place that makes the series.</summary>
internal sealed class FloatingBars
{
    private readonly List<string> _names = [];
    private readonly List<double> _centres = [];
    private readonly List<double> _feet = [];
    private readonly List<double> _heights = [];
    private readonly double _width;
    private readonly Color? _color;

    /// <summary>Starts a set of bars of one width and one colour.</summary>
    /// <param name="width">The width of every bar, in data units.</param>
    /// <param name="color">The colour of every bar, or <see langword="null"/> to take the next colour of the axes.</param>
    internal FloatingBars(double width, Color? color)
    {
        _width = width;
        _color = color;
    }

    /// <summary>Adds a bar.</summary>
    /// <param name="name">The text that names the bar.</param>
    /// <param name="centre">Where the bar's middle lies along X.</param>
    /// <param name="foot">Where the bar begins along Y.</param>
    /// <param name="height">How tall the bar is.</param>
    internal void Add(string name, double centre, double foot, double height)
    {
        _names.Add(name);
        _centres.Add(centre);
        _feet.Add(foot);
        _heights.Add(height);
    }

    /// <summary>The bars as one series.</summary>
    /// <param name="label">The series' legend name, or <see langword="null"/> for none.</param>
    /// <returns>A bar series that holds its own copy of the bars so far.</returns>
    internal BarSeries ToSeries(string? label) => new([.. _names], [.. _heights])
    {
        XCoordinate = [.. _centres],
        StackBaseline = [.. _feet],
        BarWidth = _width,
        Color = _color,
        Label = label,
    };
}
