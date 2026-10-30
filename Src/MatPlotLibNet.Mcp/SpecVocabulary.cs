// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json.Serialization;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Mcp;

/// <summary>Where a spec field sits. The same wire name means different things in different places:
/// <c>orientation</c> is a bar orientation on a bar series and a line orientation on a reference line.</summary>
internal enum SpecNodeKind
{
    Axes,
    Axis,
    Series,
    Annotation,
    ReferenceLine,
    Span,
    Trendline,
    HorizontalLevel,
    AxisBreak,
    Legend,
    ColorBar,
    Grid,
    Spine,
    Ticks,
    TextStyle,
    Font,
    TickLocator,
    TickFormatter,
    Normalizer,
    ColorMap,
    PropCycler,
}

/// <summary>The values the spec's enum-valued string fields accept. The DTOs carry these as strings and the
/// serializer parses them with <c>Enum.TryParse</c> that ignores a miss, so <c>"scale":"logarithmic"</c> renders
/// a linear axis without a word; the acceptance check therefore lives here. Membership is case-insensitive, as the
/// serializer's parse is, except for the two fields it reads by exact lowercase literal.</summary>
internal sealed class SpecVocabulary
{
    /// <summary>The accepted spellings of one field and how the accepted spelling is written back.</summary>
    internal sealed record Rule(IReadOnlyList<string> Accepted, bool LowerCase)
    {
        /// <summary>The spelling the serializer reads for <paramref name="written"/>, or <see langword="null"/>
        /// when the value is not accepted.</summary>
        public string? Canonical(string written)
        {
            var hit = Accepted.FirstOrDefault(accepted => string.Equals(accepted, written, StringComparison.OrdinalIgnoreCase));
            return hit is null ? null : LowerCase ? hit.ToLowerInvariant() : hit;
        }
    }

    private static Rule Names<TEnum>() where TEnum : struct, Enum => new(Enum.GetNames<TEnum>(), LowerCase: false);

    /// <summary>The kinds a described family accepts, read off the <c>JsonDerivedType</c> attributes of its base record:
    /// the one list the serializer itself reads the <c>type</c> field by, spelt exactly as it reads it.</summary>
    private static Rule KindsOf<TDescription>() => new(
        [.. typeof(TDescription).GetCustomAttributes<JsonDerivedTypeAttribute>().Select(derived => (string)derived.TypeDiscriminator!)],
        LowerCase: false);

    // The serializer reads these two by exact lowercase literal ("vertical", "stacked"), so they are written back lowercase.
    private static readonly Rule HorizontalOrVertical = new(["horizontal", "vertical"], LowerCase: true);
    private static readonly Rule BarModes = new(["grouped", "stacked"], LowerCase: true);

    private static readonly Rule BarOrientations = Names<BarOrientation>();
    private static readonly Rule LineOrientations = Names<Orientation>();

    private static readonly IReadOnlyDictionary<(SpecNodeKind Kind, string Field), Rule> Rules = new Dictionary<(SpecNodeKind, string), Rule>
    {
        [(SpecNodeKind.Axis, "scale")] = Names<AxisScale>(),
        [(SpecNodeKind.Axes, "barMode")] = BarModes,
        [(SpecNodeKind.Annotation, "connectionStyle")] = Names<ConnectionStyle>(),
        [(SpecNodeKind.Annotation, "boxStyle")] = Names<BoxStyle>(),
        [(SpecNodeKind.ReferenceLine, "orientation")] = HorizontalOrVertical,
        [(SpecNodeKind.ReferenceLine, "lineStyle")] = Names<LineStyle>(),
        [(SpecNodeKind.Span, "orientation")] = HorizontalOrVertical,
        [(SpecNodeKind.Span, "lineStyle")] = Names<LineStyle>(),
        [(SpecNodeKind.Trendline, "lineStyle")] = Names<LineStyle>(),
        [(SpecNodeKind.HorizontalLevel, "lineStyle")] = Names<LineStyle>(),
        [(SpecNodeKind.AxisBreak, "style")] = Names<BreakStyle>(),
        [(SpecNodeKind.Series, "lineStyle")] = Names<LineStyle>(),
        [(SpecNodeKind.Series, "labelPlacement")] = Names<SeriesLabelPlacement>(),
        [(SpecNodeKind.Series, "stepPosition")] = Names<StepPosition>(),
        [(SpecNodeKind.Series, "maskMode")] = Names<HeatmapMaskMode>(),
        [(SpecNodeKind.Series, "pairGridDiagonal")] = Names<PairGridDiagonalKind>(),
        [(SpecNodeKind.Series, "pairGridOffDiagonal")] = Names<PairGridOffDiagonalKind>(),
        [(SpecNodeKind.Series, "pairGridTriangular")] = Names<PairGridTriangle>(),
        [(SpecNodeKind.Series, "networkGraphLayout")] = Names<GraphLayout>(),
        [(SpecNodeKind.Series, "rrgFormula")] = Names<RrgFormula>(),
        // Enum-typed on the wire: the serializer would refuse a misspelling too, but without naming what it accepts.
        [(SpecNodeKind.Series, "marker")] = Names<MarkerStyle>(),
        [(SpecNodeKind.Series, "drawStyle")] = Names<DrawStyle>(),
        [(SpecNodeKind.Series, "align")] = Names<BarAlignment>(),
        [(SpecNodeKind.Series, "baseline")] = Names<StackedBaseline>(),
        [(SpecNodeKind.Series, "side")] = Names<ViolinSide>(),
        [(SpecNodeKind.Series, "hatch")] = Names<HatchPattern>(),
        [(SpecNodeKind.Series, "hatches")] = Names<HatchPattern>(),
        [(SpecNodeKind.Series, "blendMode")] = Names<BlendMode>(),
        [(SpecNodeKind.Series, "labelFit")] = Names<TreemapLabelFit>(),
        [(SpecNodeKind.Series, "histType")] = Names<HistType>(),
        [(SpecNodeKind.Series, "nodeAlignment")] = Names<SankeyNodeAlignment>(),
        [(SpecNodeKind.Series, "linkColorMode")] = Names<SankeyLinkColorMode>(),
        [(SpecNodeKind.Series, "sankeyOrientation")] = Names<SankeyOrientation>(),
        [(SpecNodeKind.Series, "dendrogramOrientation")] = Names<DendrogramOrientation>(),
        // The condition a tile or a band was judged to be in: a misspelt severity would read as no judgement at all, and
        // a tile that nobody judged draws calm.
        [(SpecNodeKind.Series, "conditionSeverity")] = Names<OpsSeverity>(),
        [(SpecNodeKind.Series, "conditionVisibility")] = Names<OpsVisibility>(),
        [(SpecNodeKind.Series, "stateSegmentHatches")] = Names<HatchPattern>(),
        [(SpecNodeKind.Series, "columnAlignments")] = Names<TextAlignment>(),
        [(SpecNodeKind.Series, "stateSegmentSeverities")] = Names<OpsSeverity>(),
        [(SpecNodeKind.Series, "stateSegmentVisibilities")] = Names<OpsVisibility>(),
        // What an axes draws besides its series: enum-typed on the wire, refused here with what is accepted.
        [(SpecNodeKind.Axes, "titleLoc")] = Names<TitleLocation>(),
        [(SpecNodeKind.Axes, "coordinateSystem")] = Names<CoordinateSystem>(),
        [(SpecNodeKind.Legend, "position")] = Names<LegendPosition>(),
        [(SpecNodeKind.ColorBar, "extend")] = Names<ColorBarExtend>(),
        [(SpecNodeKind.ColorBar, "orientation")] = Names<ColorBarOrientation>(),
        [(SpecNodeKind.Grid, "lineStyle")] = Names<LineStyle>(),
        [(SpecNodeKind.Grid, "which")] = Names<GridWhich>(),
        [(SpecNodeKind.Grid, "axis")] = Names<GridAxis>(),
        [(SpecNodeKind.Spine, "lineStyle")] = Names<LineStyle>(),
        [(SpecNodeKind.Spine, "position")] = Names<SpinePosition>(),
        [(SpecNodeKind.Ticks, "direction")] = Names<TickDirection>(),
        [(SpecNodeKind.TextStyle, "fontWeight")] = Names<FontWeight>(),
        [(SpecNodeKind.TextStyle, "fontSlant")] = Names<FontSlant>(),
        [(SpecNodeKind.Font, "weight")] = Names<FontWeight>(),
        [(SpecNodeKind.Font, "slant")] = Names<FontSlant>(),
        [(SpecNodeKind.Annotation, "coordinates")] = Names<AnnotationCoordinates>(),
        [(SpecNodeKind.Annotation, "alignment")] = Names<TextAlignment>(),
        [(SpecNodeKind.Annotation, "verticalAlignment")] = Names<TextVerticalAlignment>(),
        [(SpecNodeKind.Annotation, "arrowStyle")] = Names<ArrowStyle>(),
        [(SpecNodeKind.TickLocator, "type")] = KindsOf<TickLocatorDto>(),
        [(SpecNodeKind.TickFormatter, "type")] = KindsOf<TickFormatterDto>(),
        [(SpecNodeKind.Normalizer, "type")] = KindsOf<NormalizerDto>(),
        [(SpecNodeKind.ColorMap, "type")] = KindsOf<ColorMapDto>(),
        // A theme's cycler: lists of enum values, each item checked as a single value is.
        [(SpecNodeKind.PropCycler, "lineStyles")] = Names<LineStyle>(),
        [(SpecNodeKind.PropCycler, "markerStyles")] = Names<MarkerStyle>(),
    };

    /// <summary>The series fields that carry an enum value, including the type-dependent <c>orientation</c>.</summary>
    public IReadOnlyList<string> SeriesFields { get; } =
        Rules.Keys.Where(key => key.Kind == SpecNodeKind.Series).Select(key => key.Field).Append("orientation").ToArray();

    /// <summary>The rule for <paramref name="field"/> at <paramref name="kind"/>, or <see langword="null"/> when the
    /// field is not enum-valued there. <paramref name="seriesType"/> decides which orientation a series carries.</summary>
    public Rule? RuleFor(SpecNodeKind kind, string field, string? seriesType)
    {
        if (kind == SpecNodeKind.Series && field == "orientation")
        {
            return seriesType switch
            {
                "bar" or "count" or "area" => BarOrientations,
                "bulletgraph" => LineOrientations,
                _ => null,
            };
        }

        return Rules.TryGetValue((kind, field), out var rule) ? rule : null;
    }
}
