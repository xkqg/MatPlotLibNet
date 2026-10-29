// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Serialization;

/// <summary>The figure JSON's form of a <see cref="Theme"/>. A theme is data (colours, a font, a grid, a cycle, a
/// spacing, margins, an alarm palette), so it is written property by property and a theme built with
/// <see cref="ThemeBuilder"/> comes back whole, not only the themes the library ships.
/// <para>Every property is measured against the plain theme, <see cref="Theme.Default"/>: it is written only where it
/// differs from that theme's, and read back over that theme. The plain theme leaves every optional property unset, so a
/// property that is unset is never written and always reads back unset. The name is a label like any other property:
/// it does not look a built-in theme up.</para></summary>
internal sealed record ThemeDto
{
    public string? Name { get; init; }
    public Color? Background { get; init; }
    public Color? ForegroundText { get; init; }
    public Color? AxesBackground { get; init; }
    public Color[]? CycleColors { get; init; }
    public FontDto? DefaultFont { get; init; }
    public GridDto? DefaultGrid { get; init; }
    public PropCyclerDto? PropCycler { get; init; }
    public SubPlotSpacingDto? DefaultSpacing { get; init; }
    public Color? PatchEdgeColor { get; init; }
    public Color? ViolinBodyColor { get; init; }
    public Color? ViolinStatsColor { get; init; }
    public double? AxisXMargin { get; init; }
    public double? AxisYMargin { get; init; }
    public LegendPosition? DefaultLegendPosition { get; init; }
    public Color? Pane3DColor { get; init; }
    public AlarmPaletteDto? Alarm { get; init; }

    /// <summary>The theme every property is measured against and read back over.</summary>
    private static Theme Plain => Theme.Default;

    /// <summary>The JSON's form of <paramref name="theme"/>.</summary>
    internal static ThemeDto From(Theme theme) => new()
    {
        Name = theme.Name.OrNullAtRest(Plain.Name),
        Background = theme.Background.OrNullAtRest(Plain.Background),
        ForegroundText = theme.ForegroundText.OrNullAtRest(Plain.ForegroundText),
        AxesBackground = theme.AxesBackground.OrNullAtRest(Plain.AxesBackground),
        CycleColors = theme.CycleColors.SequenceEqual(Plain.CycleColors) ? null : theme.CycleColors,
        DefaultFont = theme.DefaultFont == Plain.DefaultFont ? null : FontDto.From(theme.DefaultFont),
        DefaultGrid = theme.DefaultGrid == Plain.DefaultGrid ? null : GridDto.From(theme.DefaultGrid),
        PropCycler = PropCyclerDto.From(theme.PropCycler),
        DefaultSpacing = theme.DefaultSpacing is { } spacing ? SubPlotSpacingDto.From(spacing) : null,
        PatchEdgeColor = theme.PatchEdgeColor,
        ViolinBodyColor = theme.ViolinBodyColor,
        ViolinStatsColor = theme.ViolinStatsColor,
        AxisXMargin = theme.AxisXMargin.OrNullAtRest(Plain.AxisXMargin),
        AxisYMargin = theme.AxisYMargin.OrNullAtRest(Plain.AxisYMargin),
        DefaultLegendPosition = theme.DefaultLegendPosition.OrNullAtRest(Plain.DefaultLegendPosition),
        Pane3DColor = theme.Pane3DColor,
        Alarm = theme.Alarm == Plain.Alarm ? null : AlarmPaletteDto.From(theme.Alarm, Plain.Alarm),
    };

    /// <summary>The theme this record describes: the plain theme, renamed, with every property this record holds.</summary>
    internal Theme ToTheme() => new(Name ?? Plain.Name, Plain)
    {
        Background = Background ?? Plain.Background,
        ForegroundText = ForegroundText ?? Plain.ForegroundText,
        AxesBackground = AxesBackground ?? Plain.AxesBackground,
        CycleColors = CycleColors ?? Plain.CycleColors,
        DefaultFont = DefaultFont?.ToFont() ?? Plain.DefaultFont,
        DefaultGrid = DefaultGrid?.ToGridStyle() ?? Plain.DefaultGrid,
        PropCycler = PropCycler?.ToPropCycler(),
        DefaultSpacing = DefaultSpacing?.ToSpacing(),
        PatchEdgeColor = PatchEdgeColor,
        ViolinBodyColor = ViolinBodyColor,
        ViolinStatsColor = ViolinStatsColor,
        AxisXMargin = AxisXMargin ?? Plain.AxisXMargin,
        AxisYMargin = AxisYMargin ?? Plain.AxisYMargin,
        DefaultLegendPosition = DefaultLegendPosition ?? Plain.DefaultLegendPosition,
        Pane3DColor = Pane3DColor,
        Alarm = Alarm?.ToAlarmPalette(Plain.Alarm) ?? Plain.Alarm,
    };
}

/// <summary>The figure JSON's form of a <see cref="Styling.PropCycler"/>: the four lists it steps through, as it was built
/// with them. A list left out reads as the cycler's own single default.</summary>
internal sealed record PropCyclerDto
{
    public Color[]? Colors { get; init; }
    public LineStyle[]? LineStyles { get; init; }
    public MarkerStyle[]? MarkerStyles { get; init; }
    public double[]? LineWidths { get; init; }

    /// <summary>The JSON's form of <paramref name="cycler"/>, or <see langword="null"/> when there is none.</summary>
    internal static PropCyclerDto? From(PropCycler? cycler) => cycler is null ? null : new()
    {
        Colors = [.. cycler.ColorCycle],
        LineStyles = [.. cycler.LineStyleCycle],
        MarkerStyles = [.. cycler.MarkerStyleCycle],
        LineWidths = [.. cycler.LineWidthCycle],
    };

    /// <summary>The cycler this record describes.</summary>
    internal PropCycler ToPropCycler() => new(Colors ?? [], LineStyles ?? [], MarkerStyles ?? [], LineWidths ?? []);
}

/// <summary>The figure JSON's form of a theme's <see cref="AlarmPalette"/>: each colour written only where it differs
/// from the palette it is measured against.</summary>
internal sealed record AlarmPaletteDto
{
    public Color? Resting { get; init; }
    public Color? Warning { get; init; }
    public Color? Critical { get; init; }
    public Color? Unknown { get; init; }

    /// <summary>The JSON's form of <paramref name="palette"/>, measured against <paramref name="resting"/>.</summary>
    internal static AlarmPaletteDto From(AlarmPalette palette, AlarmPalette resting) => new()
    {
        Resting = palette.Resting.OrNullAtRest(resting.Resting),
        Warning = palette.Warning.OrNullAtRest(resting.Warning),
        Critical = palette.Critical.OrNullAtRest(resting.Critical),
        Unknown = palette.Unknown.OrNullAtRest(resting.Unknown),
    };

    /// <summary>The palette this record describes, over <paramref name="resting"/>.</summary>
    internal AlarmPalette ToAlarmPalette(AlarmPalette resting) => new(
        Resting ?? resting.Resting,
        Warning ?? resting.Warning,
        Critical ?? resting.Critical,
        Unknown ?? resting.Unknown);
}
