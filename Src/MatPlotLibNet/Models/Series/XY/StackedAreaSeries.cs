// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a stacked area (stackplot) series where multiple Y datasets are stacked vertically with filled areas.</summary>
public sealed class StackedAreaSeries : ChartSeries, IHasAlpha, IThemedRange, IEdgedPatch, IGivenAlpha, IHatched, ISeriesKind<StackedAreaSeries>
{
    /// <summary>A stack gives no edge of its own; the opacity is the one given (<see cref="IGivenAlpha.GivenAlpha"/>), which the edge a theme decides
    /// takes too.</summary>
    GivenPatchEdge IEdgedPatch.GivenEdge => new(null, null, GivenAlpha);

    /// <inheritdoc />
    /// <remarks>matplotlib's <c>stackplot</c> draws <c>fill_between</c> collections (<see cref="MatplotlibArtist.StackPlot"/>): each layer's
    /// outline is joined as the theme joins one.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.StackPlot;

    /// <inheritdoc />
    internal override int ColorSlotSpan => Math.Max(1, YSets.Length);

    /// <inheritdoc />
    /// <remarks>With named layers, an entry for each layer that has a name, in the colour that layer is drawn in; a layer with
    /// an empty name draws no entry and a name past the last layer is ignored. The series' own name is the entry of a stack
    /// whose layers are not named, and is not a second row beside the layers' names.</remarks>
    internal override IReadOnlyList<LegendPart> LegendPartsUnder(Theme theme) =>
        Labels is { Length: > 0 } labels
            ? [.. labels.Take(YSets.Length).Select((label, layer) => new LegendPart(label, LegendSwatch.EdgedPatch, layer))
                .Where(part => !string.IsNullOrEmpty(part.Label))]
            : base.LegendPartsUnder(theme);

    /// <summary>The x coordinate shared by all layers, in data units; every layer has one value per entry.</summary>
    public double[] X { get; }

    /// <summary>The values of each layer, one array per layer, bottom layer first. Each array holds one value per
    /// <see cref="X"/> entry, and a layer is drawn on top of the sum of the layers before it.</summary>
    public double[][] YSets { get; }

    /// <summary>The name of each layer, in the order of <see cref="YSets"/>, shown in the legend in the colour of that
    /// layer. Null (the default) names none, and the series' own label stands for the whole stack.</summary>
    public string[]? Labels { get; set => Change(ref field, value); }

    /// <inheritdoc />
    /// <remarks>A stack given no alpha reads <see cref="RestingAlpha"/>. It is drawn at that value under the library's own themes and opaque under the matplotlib
    /// themes, as matplotlib's <c>stackplot</c> gives its layers no alpha of their own; an alpha that was given, the default included, is drawn
    /// as it is under every theme.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 0, above 1 or not a number.</exception>
    public double Alpha { get => GivenAlpha ?? RestingAlpha; set => GivenAlpha = value.ThrowIfNotOpacity(nameof(Alpha)); }

    /// <summary>The value of <see cref="Alpha"/> its author gave, <see langword="null"/> while none was.</summary>
    internal double? GivenAlpha { get; private set => Change(ref field, value); }

    /// <summary>The alpha a stack reads when none was given.</summary>
    internal const double RestingAlpha = 0.7;

    /// <summary>The alpha that was given, <see langword="null"/> when none was and the fill follows its theme.</summary>
    double? IGivenAlpha.GivenAlpha => GivenAlpha;

    double IGivenAlpha.RestingAlpha => RestingAlpha;

    private const StackedBaseline RestingBaseline = StackedBaseline.Zero;
    /// <summary>Where the stack starts. The default grows every layer upward from y = 0; the other choices centre the stack
    /// around y = 0 for a streamgraph look.</summary>
    public StackedBaseline Baseline { get; set => Change(ref field, value); } = RestingBaseline;

    /// <summary>The pattern of lines or dots laid over every layer. Default none.</summary>
    public HatchPattern Hatch { get; set => Change(ref field, value); } = HatchPattern.None;

    /// <summary>The colour of the <see cref="Hatch"/> pattern. Null draws it black under the matplotlib themes (matplotlib's
    /// <c>hatch.color</c>, with no edge to follow) and in a colour that contrasts with each layer's fill under the library's own themes.</summary>
    public Color? HatchColor { get; set => Change(ref field, value); }

    /// <summary>Creates a new stacked area series from shared X values and multiple Y datasets.</summary>
    /// <param name="x">The shared X-axis data values.</param>
    /// <param name="ySets">An array of Y arrays, one per stacked layer.</param>
    public StackedAreaSeries(double[] x, double[][] ySets)
    {
        X = x;
        YSets = ySets;
    }

    /// <inheritdoc />
    internal override bool HoldsNoData => X.Length == 0 || YSets.Length == 0;

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (HoldsNoData)
            return NoDataUnderTheLibrary;

        // Every edge a layer is filled between, its baseline and its top (a layer that falls has its top below its
        // baseline), over what the Y axis can place: a missing value (NaN) and an infinity are left out, as the renderer
        // breaks a layer there, and on a log axis, which has no place for zero (the lowest baseline is drawn at the plot's
        // edge there), the range starts at the lowest edge it can place.
        var x = X.DrawableRange();
        var baselines = Baseline.ComputeFor(YSets, X.Length);
        var y = baselines.Concat(LayerTops(baselines)).DrawableRange(context?.YScale.Rule);
        double? yMin = y?.Lo, yMax = y?.Hi;

        // Stack baseline (Zero) is a hard floor iff all stacked values are non-negative.
        double? stickyYMin = Baseline == StackedBaseline.Zero && yMin >= 0 ? 0 : null;
        return new(x?.Lo, x?.Hi, yMin, yMax,
            StickyXMin: x?.Lo, StickyXMax: x?.Hi, StickyYMin: stickyYMin);
    }

    /// <inheritdoc />
    /// <remarks>Edged as its layers are under the matplotlib themes: the theme's edge, black under classic.</remarks>
    internal override LegendSwatch LegendSwatch => LegendSwatch.EdgedPatch;

    /// <inheritdoc />
    /// <remarks>Under the matplotlib themes (<see cref="PatchLook.LikeCollections"/>) the ends of the stack are not sticky, so the
    /// axes' margin pads past them, as matplotlib's <c>stackplot</c> registers only its floor of zero (printed: <c>sticky_edges.x</c>
    /// empty, <c>.y</c> [0]; xlim -0.5 .. 10.5 over x 0 .. 10 under the default style). Under the library's own themes the ends stay
    /// sticky (<see cref="ComputeDataRange"/>).</remarks>
    DataRangeContribution IThemedRange.RangeUnder(IAxesContext context, Theme theme) =>
        theme.Looks.Patch.LikeCollections ? ComputeDataRange(context) with { StickyXMin = null, StickyXMax = null } : ComputeDataRange(context);

    /// <summary>The top edge of every layer: its baseline plus its own value, 0 past the end of a layer shorter than
    /// <see cref="X"/>. The renderer fills each layer between this edge and its baseline, and the range spans it.</summary>
    /// <param name="baselines">The baseline of every layer, from <see cref="StackedBaselineExtensions.ComputeFor"/>.</param>
    /// <returns>One array per layer, as long as <see cref="X"/>.</returns>
    internal double[][] LayerTops(double[][] baselines) => baselines.TopEdgesFor(YSets, X.Length);

    static string ISeriesKind<StackedAreaSeries>.Kind => "stackedarea";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        XData = X,
        Datasets = YSets,
        PieLabels = Labels,
        Alpha = GivenAlpha,
        Baseline = Baseline.OrNullAtRest(RestingBaseline),
        Hatching = this.Hatching,
    };

    /// <summary>The reader of a <see cref="StackedAreaSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static StackedAreaSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.StackPlot(dto.XData ?? [], dto.Datasets ?? []);
        s.Labels = dto.PieLabels;
        if (dto.Alpha is { } alpha) s.Alpha = alpha;
        if (dto.Baseline is { } baseline) s.Baseline = baseline;
        s.LayHatching(dto.Hatching);
        return s;
    }


    /// <inheritdoc />
    /// <remarks>One column per stack, named by its own label: the stacks share an x, so the wide form is the
    /// readable one. The values are what each stack CONTRIBUTES, not the cumulative top it is drawn at.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        var columns = new List<ChartDataColumn>(YSets.Length + 1) { ChartDataColumn.Position(DataAxis.X) };
        var values = new List<IReadOnlyList<double>>(YSets.Length + 1) { X };
        for (int i = 0; i < YSets.Length; i++)
        {
            string header = Labels.NameOr(i, "series ");
            columns.Add(new(header));
            values.Add(YSets[i]);
        }

        return ChartDataTable.FromNumberColumns(columns, values);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
