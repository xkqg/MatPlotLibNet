// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Serialization;

/// <summary>The figure JSON's form of a <see cref="ColorBar"/> (an axes' or the figure's): each property is written only
/// when it differs from a new colour bar's. The colour map travels as a series' does: by its name when the library ships
/// it, as what it is made of when it was built in code (<see cref="ColorMapDescriptionExtensions"/>); a map that cannot be
/// written reads back as none, and the bar then takes the map of the series it describes.</summary>
internal sealed record ColorBarDto
{
    public bool? Visible { get; init; }
    public string? ColorMapName { get; init; }

    /// <summary>The bar's colour map when it was built in code, described by what it is made of.</summary>
    public ColorMapDto? ColorMap { get; init; }
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

    private static readonly ColorBar Rest = new();

    /// <summary>The JSON's form of <paramref name="colorBar"/>, or <see langword="null"/> when there is none.</summary>
    internal static ColorBarDto? From(ColorBar? colorBar) => colorBar is null ? null : new()
    {
        Visible = colorBar.Visible.OrNullAtRest(Rest.Visible),
        ColorMapName = colorBar.ColorMap.NameForJson(),
        ColorMap = colorBar.ColorMap.DescriptionForJson(),
        Min = colorBar.Min.OrNullAtRest(Rest.Min),
        Max = colorBar.Max.OrNullAtRest(Rest.Max),
        Label = colorBar.Label,
        Width = colorBar.Width.OrNullAtRest(Rest.Width),
        Padding = colorBar.Padding.OrNullAtRest(Rest.Padding),
        Extend = colorBar.Extend.OrNullAtRest(Rest.Extend),
        Orientation = colorBar.Orientation.OrNullAtRest(Rest.Orientation),
        Shrink = colorBar.Shrink.OrNullAtRest(Rest.Shrink),
        DrawEdges = colorBar.DrawEdges.OrNullAtRest(Rest.DrawEdges),
        Aspect = colorBar.Aspect.OrNullAtRest(Rest.Aspect),
    };

    /// <summary>The colour bar this record describes.</summary>
    internal ColorBar ToColorBar() => new()
    {
        Visible = Visible ?? Rest.Visible,
        ColorMap = ColorMap.ReadColorMap(ColorMapName),
        Min = Min ?? Rest.Min,
        Max = Max ?? Rest.Max,
        Label = Label,
        Width = Width ?? Rest.Width,
        Padding = Padding ?? Rest.Padding,
        Extend = Extend ?? Rest.Extend,
        Orientation = Orientation ?? Rest.Orientation,
        Shrink = Shrink ?? Rest.Shrink,
        DrawEdges = DrawEdges ?? Rest.DrawEdges,
        Aspect = Aspect ?? Rest.Aspect,
    };
}
