// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging.OwnedRules;

/// <summary>How a series is spelled on the wire is written once per rule: the name of a kind is a member of the series class that
/// the writer and the registry both read, an optional setting is read back as <c>is { } value</c>, a flag that is false at rest is
/// left out by <c>OrNullAtRest</c>, the name of an enum on the wire is <c>ToWireName</c>, and the colour scale of a series (its map
/// and its normaliser) is written and read as one.</summary>
internal static class SerializationRules
{
    private static IEnumerable<OwnedRule> PerFolder(string rule, string pattern, string[] finds, params string[] folders) =>
        folders.Select(folder => OwnedRule.Nowhere($"{rule} ({folder})", pattern, under: folder) with { Finds = finds });

    /// <summary>The rules of the area, in the order the scan reads them.</summary>
    internal static OwnedRule[] All { get; } =
    [
        new("a grid of values becomes the rows of the JSON through Grid2DSerializationExtensions (To2DList, JaggedTo2DList): no series copies a grid into lists itself",
            @"\bnew List<List<double>>\(",
            "Src/MatPlotLibNet/Serialization/Grid2DSerializationExtensions.cs") { Under = "Src/MatPlotLibNet/", Finds = [
            "            var result = new List<List<double>>(rows);"] },
        OwnedRule.Nowhere("a series hands its given stroke to its wire form as one value, SeriesDto.Stroking (width and dash name, each only while given): no series writes the dash name and the width of its stroke field by field",
            @"\bLineStyle\s*=\s*[\w.]*\bStroke\.StyleName\b|\bLineWidth\s*=\s*[\w.]*\bStroke\.Width\b", under: "Src/") with { Finds = [
            "        LineStyle = Stroke.StyleName,",
            "        LineWidth = Stroke.Width,"] },
        OwnedRule.Nowhere("the name a series kind is written under is the Kind of its class (ISeriesKind), read by its writer through WireKind and by the registry: no writer types the name as a literal",
            @"\bType\s*=\s*""[A-Za-z0-9_-]+""", under: "Src/") with { Finds = [
            "        Type = \"line\",",
            "        Type    = \"line\","] },
        OwnedRule.Nowhere("a series reader is registered under the Kind of the class it returns, never under a name typed beside it",
            @"\bRegister(?:Reader)?(?:<\w+>)?\(\s*""", under: "Src/") with { Finds = [
            "        Register(\"line\", LineSeries.FromSeriesDto);"] },
        OwnedRule.Nowhere("a package's series names its kind through ISeriesKind too: no constant of its own holds the discriminator",
            @"\bconst string Discriminator\b", under: "Src/") with { Finds = [
            "    private const string Discriminator = \"geo_polygon\";"] },
        .. PerFolder("a setting a reader takes from the document is applied by `if (dto.X is { } x) target.X = x;` (a name, a value a document carries only when it was given): no reader tests HasValue",
            @"\bif \(\s*[\w.]+\s*\.HasValue\)",
            [
            "        if (dto.Alpha.HasValue) s.Alpha = dto.Alpha.Value;",
            "        if (Elevation.HasValue) axes.Elevation = Elevation.Value;",
            "        else if (x.HasValue)",
            "        if (dto.Values.HasValue) series.Values = dto.Values.Value;"], "Src/MatPlotLibNet/Models/", "Src/MatPlotLibNet/Serialization/", "Src/MatPlotLibNet/Builders/", "Src/MatPlotLibNet.Geo/"),
        OwnedRule.Nowhere("a setting a series reader takes from the document is applied by `is { } x`, whatever its type: no reader tests a field against null",
            @"\bif \(\s*dto\.\w+\s+is not null\s*\)", under: "Src/") with { Finds = [
            "        if (dto.Categories is not null)"] },
        OwnedRule.Nowhere("a setting a DTO applies to its model is applied by `is { } x`, whatever its type: no DTO tests its own field against null",
            @"\bif \(\s*[A-Z]\w*\s+is not null\s*\)", under: "Src/MatPlotLibNet/Serialization/") with { Finds = [
            "        if (Color is not null) line.Color = Styling.Color.FromHex(Color);"] },
        OwnedRule.Nowhere("a setting a series reader takes from the document is applied by `if (dto.X is { } x) s.X = x;` and in no other way: no reader assigns a fallback (a resting value, the target's own value, `== true`, `!= false`, `?? false`), which states again what the new series already holds",
            @"^\s*(?:\w+\.)?\w+\s*=\s*dto\.\w+\s*(?:\?\?[^;]*|==\s*true|!=\s*false)\s*;", under: "Src/") with { Finds = [
            "        s.Alpha = dto.Alpha ?? RestingAlpha;",
            "        SmoothResolution = dto.SmoothResolution ?? SmoothResolution;",
            "        s.ShowMeans = dto.ShowMeans == true;",
            "        s.Vert = dto.Vert != false;",
            "        s.Density = dto.Density ?? false;"] },
        OwnedRule.Nowhere("a setting a DTO applies to the object it has just made is applied by `if (X is { } x) target.X = x;`: no DTO assigns a fallback that states again what the new object holds",
            @"^\s*\w+\.\w+\s*=\s*[A-Z]\w*\s*(?:\?\?[^;]*|==\s*true|!=\s*false)\s*;", under: "Src/MatPlotLibNet/Serialization/") with { Finds = [
            "        image.Alpha = Alpha ?? Rest.Alpha;",
            "        patch.EdgeLineStyle = EdgeLineStyle ?? patch.EdgeLineStyle;"] },
        OwnedRule.Nowhere("a flag that is at rest is left out of the document by OrNullAtRest(resting): no site spells the choice by hand",
            @"\?\s*(?:true\s*:\s*null|false\s*:\s*null|null\s*:\s*(?:true|false))\b", under: "Src/") with { Finds = [
            "        Smooth = Smooth ? true : null,",
            "        ShowZeroLine = ShowZeroLine ? null : false,",
            "        Visible = !patch.Visible ? false : null;"] },
        new("the name an enum is written under in a document, or named by in a message, is value.ToWireName(), the lower-case name of the member: no writer in any package spells it",
            @"\.ToString\(\)\.ToLowerInvariant\(\)",
            "Src/MatPlotLibNet/Serialization/EnumParsingExtensions.cs") { Under = "Src/", Finds = [
            "        Orientation = Orientation.ToString().ToLowerInvariant(),",
            "        Style = stroke.Style?.ToString().ToLowerInvariant(),",
            "            $\"The format is {format.ToString().ToLowerInvariant()} but the path ends in '{extension}'. \""] },
        OwnedRule.Nowhere("a colour scale (a map and a normaliser) is written as one ColorScaleWire and read back from one: no series, colour bar, theme or image spells a half of it (the names of the old halves are gone)",
            @"\b(?:NameForJson|DescriptionForJson|ReadColorMap|DescribeNormalizer|RestoreNormalizer|RestoreColorMap)\b", under: "Src/") with { Finds = [
            "        s.RestoreNormalizer(dto);",
            "        s.RestoreColorMap(dto);",
            "        Normalizer = this.DescribeNormalizer(),",
            "        ColorMapName = ColorMap.NameForJson(),",
            "        ColorMap = ColorMap.DescriptionForJson(),",
            "        ColorMapName = colorBar.ColorMap.NameForJson(),",
            "        image.ColorMap = ColorMap.ReadColorMap(ColorMapName);"] },
        new("a normaliser is described for the document only inside the colour scale's own wire value, never at a site that writes one",
            @"\.DescribeForJson\(",
            "Src/MatPlotLibNet/Serialization/ColorScaleWire.cs") { Under = "Src/", Finds = [
            "        Normalizer = image.Normalizer.DescribeForJson(),"] },
        new("whether a colour map travels by its name or as its description is decided in one place (ColorMapOnTheWire) and read back in one (ColorMapWire.Read): no writer or reader sorts a map into the name field or the description field itself",
            @"\bNamedColorMapDto\b",
            "Src/MatPlotLibNet/Serialization/ColorMapDescriptionExtensions.cs", "Src/MatPlotLibNet/Serialization/ColorMapWire.cs",
            "Src/MatPlotLibNet/Serialization/ColorMapDto.cs", "Src/MatPlotLibNet/Styling/ColorMaps/ReversedColorMap.cs") { Under = "Src/", Finds = [
            "            (description ?? (name is null ? null : new NamedColorMapDto(name)))?.Restore();"] },
        OwnedRule.Nowhere("the fields a series of the library holds beyond any series are written and read by the series itself (ChartSeries.WriteSharedFields, ReadSharedFields, which a kind with more overrides): the shared-field extension asks one question of the type, whether it is a ChartSeries, and never tests for a kind to reach a field",
            @"\bseries is XYSeries\b|\bseries is ChartSeries \{|\bseries is ChartSeries \w+ &&",
            under: "Src/MatPlotLibNet/Serialization/SeriesSharedFieldsExtensions.cs") with { Finds = [
            "            if (series is XYSeries xy && dto.MaxDisplayPoints is { } m)"] },
        OwnedRule.Nowhere("an enum member named by a text field of a document is read by text.WireEnum<T>() (a member's name in any case, anything else announced and left out): no reader parses the text itself or hands the owner a setter",
            @"\bEnum\.(?:Try)?Parse\b|\.ApplyEnum\b", under: "Src/") with { Finds = [
            "        if (dto.MaskMode is { } m && Enum.TryParse<HeatmapMaskMode>(m, true, out var mm))",
            "        if (dto.RrgFormula is { } f) s.Formula = Enum.Parse<RrgFormula>(f, ignoreCase: true);",
            "        LineStyle.ApplyEnum<Styling.LineStyle>(v => line.LineStyle = v);"] },
        OwnedRule.Nowhere("a wire name is never compared with a literal: the reader asks WireEnum<T>() for the member and compares members",
            @"\bis\s+""[a-z]\w*""|\bstring\.Equals\([^)]*""[a-z]\w*""|^\s*""[a-z]\w*""\s*=>|==\s*[A-Z][\w.]*\.[A-Z]\w*\s*\?\s*""[a-z]\w*""\s*:",
            under: "Src/MatPlotLibNet/Serialization/") with { Finds = [
            "        var line = Orientation is \"vertical\" ? axes.AxVLine(Value) : axes.AxHLine(Value);",
            "        if (BarMode is \"stacked\") axes.BarMode = Models.BarMode.Stacked;",
            "            \"data\" => SpinePosition.Data,",
            "            BarMode = axes.BarMode == Models.BarMode.Stacked ? \"stacked\" : null,"] },
        OwnedRule.Nowhere("a wire name is never compared with a literal in a series' own reader either",
            @"\bis\s+""[a-z]\w*""|\bstring\.Equals\([^)]*""[a-z]\w*""|^\s*""[a-z]\w*""\s*=>|==\s*[A-Z][\w.]*\.[A-Z]\w*\s*\?\s*""[a-z]\w*""\s*:",
            under: "Src/MatPlotLibNet/Models/Series/") with { Finds = [
            "            Orientation = string.Equals(dto.Orientation, \"horizontal\", StringComparison.OrdinalIgnoreCase) ? BarOrientation.Horizontal : BarOrientation.Vertical,"] },
        new("the names an enum is read by are one table (EnumWireNames): the serializer's readers, its enum converter and the spec vocabulary all match through it, and none lists or matches the names itself",
            @"\bEnum\.GetNames\b",
            "Src/MatPlotLibNet/Serialization/EnumWireNames.cs") { Under = "Src/MatPlotLibNet", Finds = [
            "    private static Rule Names<TEnum>() where TEnum : struct, Enum => new(Enum.GetNames<TEnum>());"] },
        OwnedRule.Nowhere("the spec vocabulary matches a written value through the core's name rule (IndexOfWireName), never by a comparison of its own",
            @"StringComparison\.OrdinalIgnoreCase|\bLowerCase\b", under: "Src/MatPlotLibNet.Mcp/ChartSpecReader.SpecVocabulary.cs") with { Finds = [
            "    private static readonly Rule BarModes = new([\"grouped\", \"stacked\"], LowerCase: true);",
            "            var hit = Accepted.FirstOrDefault(accepted => string.Equals(accepted, written, StringComparison.OrdinalIgnoreCase));"] },
        OwnedRule.Nowhere("the enum fields a spec node of each kind carries are the vocabulary's rows (FieldsOf): the spec reader passes a node and its kind, never a field name of its own",
            @"\bNormalise(?:Fields|Enum)\([^)]*""", under: "Src/MatPlotLibNet.Mcp/ChartSpecReader.cs") with { Finds = [
            "        NormaliseFields(axes, \"grid\", path, SpecNodeKind.Grid, \"lineStyle\", \"which\", \"axis\");",
            "        NormaliseEnum(axes, SpecNodeKind.Axes, \"barMode\", path, seriesType: null);"] },
        OwnedRule.Nowhere("the spec vocabulary names a kind of series by its class (ISeriesKind), never by a name typed beside it",
            @"""[a-z0-9_]+""\s+(?:or\s+""|=>)", under: "Src/MatPlotLibNet.Mcp/ChartSpecReader.SpecVocabulary.cs") with { Finds = [
            "                \"bar\" or \"count\" or \"area\" => BarOrientations,"] },
    ];
}
