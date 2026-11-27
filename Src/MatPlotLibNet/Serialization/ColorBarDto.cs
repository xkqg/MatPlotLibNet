// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Serialization;

/// <summary>The figure JSON's form of a <see cref="ColorBar"/> (an axes' or the figure's): each property is written only
/// when it differs from a new colour bar's, the thickness and the gap only when they were given (a theme draws a bar given
/// neither at its own). The colour map travels as a series' does: by its name when the library ships
/// it, as what it is made of when it was built in code (<see cref="ColorMapDescriptionExtensions"/>); a map that cannot be
/// written reads back as none, and the bar then takes the map of the series it describes.</summary>
internal sealed record ColorBarDto
{
    public bool? Visible { get; init; }
    public string? ColorMapName { get; init; }

    /// <summary>The bar's colour map when it was built in code, described by what it is made of.</summary>
    public ColorMapDto? ColorMap { get; init; }

    /// <summary>The bar's colour map as the one value that sits in the two fields that carry it (<see cref="ColorMapName"/>,
    /// <see cref="ColorMap"/>). No field of its own: it is not written itself.</summary>
    internal ColorMapWire MapOnTheWire
    {
        get => new(ColorMapName, ColorMap);
        init
        {
            ColorMapName = value.Name;
            ColorMap = value.Description;
        }
    }
    public double? Min { get; init; }
    public double? Max { get; init; }
    public string? Label { get; init; }
    public double? Width { get; init; }
    public double? Padding { get; init; }
    public ColorBarExtend? Extend { get; init; }
    public ColorBarOrientation? Orientation { get; init; }
    public double? Shrink { get; init; }
    public bool? DrawEdges { get; init; }
    public double? Aspect { get; init; }

    /// <summary>The bar's tick locator when it is one of the library's; absent for the ticks the bar chooses itself, and for a
    /// locator written outside the library (code the JSON cannot carry). A 1.29.0 reader skips it.</summary>
    public TickLocatorDto? Locator { get; init; }

    private static readonly ColorBar Rest = new();

    /// <summary>The JSON's form of <paramref name="colorBar"/>, or <see langword="null"/> when there is none.</summary>
    /// <param name="colorBar">The colour bar; <see langword="null"/> for none.</param>
    /// <returns>The record of the colour bar.</returns>
    internal static ColorBarDto? From(ColorBar? colorBar) => colorBar is null ? null : new()
    {
        Visible = colorBar.Visible.OrNullAtRest(Rest.Visible),
        MapOnTheWire = colorBar.ColorMap.ColorMapOnTheWire(),
        Min = colorBar.Min.OrNullAtRest(Rest.Min),
        Max = colorBar.Max.OrNullAtRest(Rest.Max),
        Label = colorBar.Label,
        Width = colorBar.GivenWidth,
        Padding = colorBar.GivenPadding,
        Extend = colorBar.Extend.OrNullAtRest(Rest.Extend),
        Orientation = colorBar.Orientation.OrNullAtRest(Rest.Orientation),
        Shrink = colorBar.Shrink.OrNullAtRest(Rest.Shrink),
        DrawEdges = colorBar.DrawEdges.OrNullAtRest(Rest.DrawEdges),
        Aspect = colorBar.Aspect.OrNullAtRest(Rest.Aspect),
        Locator = (colorBar.Locator as IJsonDescribed<TickLocatorDto>)?.Describe(),
    };

    /// <summary>The colour bar this record describes; a thickness or a gap the record does not hold stays not given.</summary>
    /// <returns>The colour bar of the record.</returns>
    /// <exception cref="ArgumentException">The record's colour map description cannot make a map; the exception names the field.</exception>
    internal ColorBar ToColorBar()
    {
        var bar = new ColorBar
        {
            Visible = Visible ?? Rest.Visible,
            ColorMap = MapOnTheWire.Read(),
            Min = Min ?? Rest.Min,
            Max = Max ?? Rest.Max,
            Label = Label,
            Extend = Extend ?? Rest.Extend,
            Orientation = Orientation ?? Rest.Orientation,
            Shrink = Shrink ?? Rest.Shrink,
            DrawEdges = DrawEdges ?? Rest.DrawEdges,
            Aspect = Aspect ?? Rest.Aspect,
            Locator = Locator?.Restore(),
        };
        if (Width is { } width) bar = bar with { Width = width };
        return Padding is { } padding ? bar with { Padding = padding } : bar;
    }
}
