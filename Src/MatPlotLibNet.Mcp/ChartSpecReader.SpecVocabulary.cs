// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Mcp;

internal sealed partial class ChartSpecReader
{
    /// <summary>The values the spec's enum-valued fields accept. The serializer reads a text field through
    /// <c>WireEnum</c>, which announces a miss on the diagnostics channel but keeps loading, so <c>"scale":"logarithmic"</c>
    /// would render a linear axis that only a log names; and it refuses a miss in an enum-typed field without saying what is
    /// accepted. The acceptance check that names the accepted values therefore lives here. A value is accepted by the one
    /// rule every reader of the serializer reads by: a member's name, in any case (<c>IndexOfWireName</c>).</summary>
    internal sealed class SpecVocabulary
    {
        /// <summary>The accepted spellings of one field.</summary>
        internal sealed record Rule(string[] Accepted)
        {
            /// <summary>The spelling the serializer reads for <paramref name="written"/>, or <see langword="null"/>
            /// when the value is not accepted.</summary>
            public string? Canonical(string written)
            {
                int at = Accepted.IndexOfWireName(written);
                return at < 0 ? null : Accepted[at];
            }
        }

        private static Rule Names<TEnum>() where TEnum : struct, Enum => new(EnumWireNames<TEnum>.Names);

        /// <summary>The kinds a described family accepts, read off the <c>JsonDerivedType</c> attributes of its base record:
        /// the one list the serializer itself reads the <c>type</c> field by, spelt exactly as it reads it.</summary>
        private static Rule KindsOf<TDescription>() => new(
            [.. typeof(TDescription).WireKinds.Select(derived => (string)derived.TypeDiscriminator!)]);

        /// <summary>The name a kind of series is written under, read from its class.</summary>
        private static string KindOf<TSeries>() where TSeries : ISeriesKind<TSeries> => TSeries.Kind;

        /// <summary>The series field whose enum depends on the kind of the series (<see cref="Mcp.ChartSpecReader.SpecVocabulary.OrientationOfKind"/>).</summary>
        private const string SeriesOrientation = "orientation";

        /// <summary>The enum a series' <c>orientation</c> holds, by the kind of the series: a bar orientation on a bar, a count plot
        /// and a filled area, a line orientation on a bullet graph.</summary>
        private static readonly IReadOnlyDictionary<string, Rule> OrientationOfKind = new Dictionary<string, Rule>
        {
            [KindOf<BarSeries>()] = Names<BarOrientation>(),
            [KindOf<CountSeries>()] = Names<BarOrientation>(),
            [KindOf<AreaSeries>()] = Names<BarOrientation>(),
            [KindOf<BulletGraphSeries>()] = Names<Orientation>(),
        };

        /// <summary>A field of one kind of node, the key of <see cref="Mcp.ChartSpecReader.SpecVocabulary.Rules"/>.</summary>
        internal readonly record struct KindField(SpecNodeKind Kind, string Field);

        private static readonly IReadOnlyDictionary<KindField, Rule> Rules = new Dictionary<KindField, Rule>
        {
            [new(SpecNodeKind.Axis, "scale")] = Names<AxisScale>(),
            [new(SpecNodeKind.Axes, "barMode")] = Names<BarMode>(),
            [new(SpecNodeKind.Annotation, "connectionStyle")] = Names<ConnectionStyle>(),
            [new(SpecNodeKind.Annotation, "boxStyle")] = Names<BoxStyle>(),
            [new(SpecNodeKind.ReferenceLine, "orientation")] = Names<Orientation>(),
            [new(SpecNodeKind.ReferenceLine, "lineStyle")] = Names<LineStyle>(),
            [new(SpecNodeKind.Span, "orientation")] = Names<Orientation>(),
            [new(SpecNodeKind.Span, "lineStyle")] = Names<LineStyle>(),
            [new(SpecNodeKind.Trendline, "lineStyle")] = Names<LineStyle>(),
            [new(SpecNodeKind.HorizontalLevel, "lineStyle")] = Names<LineStyle>(),
            [new(SpecNodeKind.AxisBreak, "style")] = Names<BreakStyle>(),
            [new(SpecNodeKind.Series, "lineStyle")] = Names<LineStyle>(),
            [new(SpecNodeKind.Series, "labelPlacement")] = Names<SeriesLabelPlacement>(),
            [new(SpecNodeKind.Series, "stepPosition")] = Names<StepPosition>(),
            [new(SpecNodeKind.Series, "maskMode")] = Names<HeatmapMaskMode>(),
            [new(SpecNodeKind.Series, "pairGridDiagonal")] = Names<PairGridDiagonalKind>(),
            [new(SpecNodeKind.Series, "pairGridOffDiagonal")] = Names<PairGridOffDiagonalKind>(),
            [new(SpecNodeKind.Series, "pairGridTriangular")] = Names<PairGridTriangle>(),
            [new(SpecNodeKind.Series, "networkGraphLayout")] = Names<GraphLayout>(),
            [new(SpecNodeKind.Series, "rrgFormula")] = Names<RrgFormula>(),
            [new(SpecNodeKind.Series, "quiverKeyLabelPosition")] = Names<QuiverKeyLabelPosition>(),
            // Enum-typed on the wire: the serializer would refuse a misspelling too, but without naming what it accepts.
            [new(SpecNodeKind.Series, "marker")] = Names<MarkerStyle>(),
            [new(SpecNodeKind.Series, "drawStyle")] = Names<DrawStyle>(),
            [new(SpecNodeKind.Series, "align")] = Names<BarAlignment>(),
            [new(SpecNodeKind.Series, "baseline")] = Names<StackedBaseline>(),
            [new(SpecNodeKind.Series, "side")] = Names<ViolinSide>(),
            [new(SpecNodeKind.Series, "hatch")] = Names<HatchPattern>(),
            [new(SpecNodeKind.Series, "hatches")] = Names<HatchPattern>(),
            [new(SpecNodeKind.Series, "blendMode")] = Names<BlendMode>(),
            [new(SpecNodeKind.Series, "labelFit")] = Names<TreemapLabelFit>(),
            [new(SpecNodeKind.Series, "histType")] = Names<HistType>(),
            [new(SpecNodeKind.Series, "nodeAlignment")] = Names<SankeyNodeAlignment>(),
            [new(SpecNodeKind.Series, "linkColorMode")] = Names<SankeyLinkColorMode>(),
            [new(SpecNodeKind.Series, "sankeyOrientation")] = Names<SankeyOrientation>(),
            [new(SpecNodeKind.Series, "dendrogramOrientation")] = Names<DendrogramOrientation>(),
            [new(SpecNodeKind.Series, "tableLocation")] = Names<TableLocation>(),
            // The matplotlib call a verb composed a series as, which decides the cycle it takes its colour from under the matplotlib
            // themes: a misspelt call would be dropped and the series would take its colour from the other cycle.
            [new(SpecNodeKind.Series, "asksAs")] = Names<CycleCall>(),
            // The condition a tile or a band was judged to be in: a misspelt severity would read as no judgement at all, and
            // a tile that nobody judged draws calm.
            [new(SpecNodeKind.Series, "conditionSeverity")] = Names<OpsSeverity>(),
            [new(SpecNodeKind.Series, "conditionVisibility")] = Names<OpsVisibility>(),
            [new(SpecNodeKind.Series, "stateSegmentHatches")] = Names<HatchPattern>(),
            [new(SpecNodeKind.Series, "columnAlignments")] = Names<TextAlignment>(),
            [new(SpecNodeKind.Series, "stateSegmentSeverities")] = Names<OpsSeverity>(),
            [new(SpecNodeKind.Series, "stateSegmentVisibilities")] = Names<OpsVisibility>(),
            // What an axes draws besides its series: enum-typed on the wire, refused here with what is accepted.
            [new(SpecNodeKind.Axes, "titleLoc")] = Names<TitleLocation>(),
            [new(SpecNodeKind.Axes, "coordinateSystem")] = Names<CoordinateSystem>(),
            [new(SpecNodeKind.Legend, "position")] = Names<LegendPosition>(),
            [new(SpecNodeKind.ColorBar, "extend")] = Names<ColorBarExtend>(),
            [new(SpecNodeKind.ColorBar, "orientation")] = Names<ColorBarOrientation>(),
            [new(SpecNodeKind.Grid, "lineStyle")] = Names<LineStyle>(),
            [new(SpecNodeKind.Grid, "which")] = Names<GridWhich>(),
            [new(SpecNodeKind.Grid, "axis")] = Names<GridAxis>(),
            [new(SpecNodeKind.Spine, "lineStyle")] = Names<LineStyle>(),
            [new(SpecNodeKind.Spine, "position")] = Names<SpinePosition>(),
            [new(SpecNodeKind.Ticks, "direction")] = Names<TickDirection>(),
            [new(SpecNodeKind.TextStyle, "fontWeight")] = Names<FontWeight>(),
            [new(SpecNodeKind.TextStyle, "fontSlant")] = Names<FontSlant>(),
            [new(SpecNodeKind.Font, "weight")] = Names<FontWeight>(),
            [new(SpecNodeKind.Font, "slant")] = Names<FontSlant>(),
            [new(SpecNodeKind.Annotation, "coordinates")] = Names<AnnotationCoordinates>(),
            [new(SpecNodeKind.Annotation, "alignment")] = Names<TextAlignment>(),
            [new(SpecNodeKind.Annotation, "verticalAlignment")] = Names<TextVerticalAlignment>(),
            [new(SpecNodeKind.Annotation, "arrowStyle")] = Names<ArrowStyle>(),
            // A text placed on the figure itself stands against its point as an annotation's text does.
            [new(SpecNodeKind.FigureText, "alignment")] = Names<TextAlignment>(),
            [new(SpecNodeKind.FigureText, "verticalAlignment")] = Names<TextVerticalAlignment>(),
            // The zoom an inset indicates on its parent: the dash pattern of its rectangle and connectors.
            [new(SpecNodeKind.ZoomIndicator, "lineStyle")] = Names<LineStyle>(),
            // A buy or sell signal: enum-typed on the wire, refused here with the two directions.
            [new(SpecNodeKind.Signal, "direction")] = Names<SignalDirection>(),
            // A patch of an axes: the kind it is, how its outline is dashed and its face hatched, and which half of an arrow is drawn.
            [new(SpecNodeKind.Patch, "type")] = KindsOf<PatchDto>(),
            [new(SpecNodeKind.Patch, "edgeLineStyle")] = Names<LineStyle>(),
            [new(SpecNodeKind.Patch, "hatch")] = Names<HatchPattern>(),
            [new(SpecNodeKind.Patch, "shape")] = Names<ArrowHeadShape>(),
            [new(SpecNodeKind.Patch, "zDir")] = Names<ZDirection>(),
            // Where a theme puts the legend of an axes that does not say, where it ends an axis that scales itself, the weight of its titles, and the pattern over
            // one node of a tree: enum-typed on the wire, refused here with what is accepted.
            [new(SpecNodeKind.Theme, "defaultLegendPosition")] = Names<LegendPosition>(),
            [new(SpecNodeKind.Theme, "autolimitMode")] = Names<AutolimitMode>(),
            [new(SpecNodeKind.Theme, "titleWeight")] = Names<FontWeight>(),
            [new(SpecNodeKind.Theme, "tickDirection")] = Names<TickDirection>(),
            // How a theme hands out the colours of its cycle: one cycle, or matplotlib's two (a lines cycle and a patch cycle).
            [new(SpecNodeKind.Theme, "cycles")] = Names<CycleRule>(),
            // How a theme joins the corners of its strokes (matplotlib's lines.solid_joinstyle): mitre, round or bevel.
            [new(SpecNodeKind.Theme, "strokeJoin")] = Names<JoinStyle>(),
            // The look a theme gives a box plot (matplotlib's boxplot.* parameters): the dash of its whiskers and the markers of its fliers and its mean.
            [new(SpecNodeKind.BoxLook, "whiskerStyle")] = Names<LineStyle>(),
            [new(SpecNodeKind.BoxLook, "flierMarker")] = Names<MarkerStyle>(),
            [new(SpecNodeKind.BoxLook, "meanMarker")] = Names<MarkerStyle>(),
            [new(SpecNodeKind.TreeNode, "hatch")] = Names<HatchPattern>(),
            [new(SpecNodeKind.TickLocator, "type")] = KindsOf<TickLocatorDto>(),
            [new(SpecNodeKind.TickFormatter, "type")] = KindsOf<TickFormatterDto>(),
            // The link of an axis (the top X axis, a Y axis past the primary): the serializer reads a kind it does not know as no link and
            // skips it with a diagnostic, so a misspelt kind would draw an axis with nothing on it.
            [new(SpecNodeKind.AxisLink, "type")] = KindsOf<AxisTransformDto>(),
            [new(SpecNodeKind.Normalizer, "type")] = KindsOf<NormalizerDto>(),
            [new(SpecNodeKind.ColorMap, "type")] = KindsOf<ColorMapDto>(),
            // A theme's cycler: lists of enum values, each item checked as a single value is.
            [new(SpecNodeKind.PropCycler, "lineStyles")] = Names<LineStyle>(),
            [new(SpecNodeKind.PropCycler, "markerStyles")] = Names<MarkerStyle>(),
        };

        /// <summary>The enum-valued fields of each kind of node, read off the keys of <see cref="Mcp.ChartSpecReader.SpecVocabulary.Rules"/> (and a series' type-dependent
        /// <c>orientation</c>), in the order of the rows: the one list the reader normalises a node by.</summary>
        private static readonly IReadOnlyDictionary<SpecNodeKind, string[]> FieldsByKind = Rules.Keys
            .Append(new KindField(SpecNodeKind.Series, SeriesOrientation))
            .GroupBy(key => key.Kind)
            .ToDictionary(group => group.Key, group => group.Select(key => key.Field).ToArray());

        /// <summary>The enum-valued fields a node of <paramref name="kind"/> carries; empty for a kind that carries none.</summary>
        /// <param name="kind">Where the node sits.</param>
        /// <returns>The wire names of its enum-valued fields.</returns>
        public IReadOnlyList<string> FieldsOf(SpecNodeKind kind) => FieldsByKind.TryGetValue(kind, out var fields) ? fields : [];

        /// <summary>The series fields that hold the root of a tree (a treemap's, a sunburst's, a clustermap's two), by wire name: the
        /// members of the series record of the node type, so a tree added to the wire format is read here on the next build.</summary>
        public IReadOnlyList<string> TreeFields { get; } =
            [.. typeof(SeriesDto).WireFields.Where(field => field.PropertyType == typeof(TreeNodeDto)).Select(field => field.Name)];

        /// <summary>The rule for <paramref name="field"/> at <paramref name="kind"/>, or <see langword="null"/> when the
        /// field is not enum-valued there. <paramref name="seriesType"/> decides which orientation a series carries.</summary>
        public Rule? RuleFor(SpecNodeKind kind, string field, string? seriesType)
        {
            if (kind == SpecNodeKind.Series && field == SeriesOrientation)
            {
                return seriesType is not null && OrientationOfKind.TryGetValue(seriesType, out var orientation) ? orientation : null;
            }

            return Rules.TryGetValue(new KindField(kind, field), out var rule) ? rule : null;
        }
    }
}
