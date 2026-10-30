// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a stacked area (stackplot) series where multiple Y datasets are stacked vertically with filled areas.</summary>
public sealed class StackedAreaSeries : ChartSeries, IHasAlpha
{
    /// <inheritdoc />
    internal override int ColorSlotSpan => Math.Max(1, YSets.Length);

    /// <inheritdoc />
    /// <remarks>With named layers, an entry for each layer that has a name, in the colour that layer is drawn in; a layer with
    /// an empty name draws no entry and a name past the last layer is ignored. The series' own name is the entry of a stack
    /// whose layers are not named, and is not a second row beside the layers' names.</remarks>
    internal override IReadOnlyList<LegendPart> LegendParts =>
        Labels is { Length: > 0 } labels
            ? [.. labels.Take(YSets.Length).Select((label, layer) => new LegendPart(label, LegendSwatch.Patch, layer))
                .Where(part => !string.IsNullOrEmpty(part.Label))]
            : base.LegendParts;

    /// <summary>The x coordinate shared by all layers, in data units; every layer has one value per entry.</summary>
    public double[] X { get; }

    /// <summary>The values of each layer, one array per layer, bottom layer first. Each array holds one value per
    /// <see cref="X"/> entry, and a layer is drawn on top of the sum of the layers before it.</summary>
    public double[][] YSets { get; }

    /// <summary>The name of each layer, in the order of <see cref="YSets"/>, shown in the legend in the colour of that
    /// layer. Null (the default) names none, and the series' own label stands for the whole stack.</summary>
    public string[]? Labels { get; set; }

    /// <inheritdoc />
    public double Alpha { get; set; } = 0.7;

    /// <summary>Where the stack starts. The default grows every layer upward from y = 0; the other choices centre the stack
    /// around y = 0 for a streamgraph look.</summary>
    public StackedBaseline Baseline { get; set; } = StackedBaseline.Zero;

    /// <summary>The pattern of lines or dots laid over every layer. Default none.</summary>
    public HatchPattern Hatch { get; set; } = HatchPattern.None;

    /// <summary>The colour of the <see cref="Hatch"/> pattern; null picks a colour that contrasts with each layer's fill.</summary>
    public Color? HatchColor { get; set; }

    /// <summary>Creates a new stacked area series from shared X values and multiple Y datasets.</summary>
    /// <param name="x">The shared X-axis data values.</param>
    /// <param name="ySets">An array of Y arrays, one per stacked layer.</param>
    public StackedAreaSeries(double[] x, double[][] ySets)
    {
        X = x;
        YSets = ySets;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (X.Length == 0 || YSets.Length == 0)
            return new(0, 1, 0, 1);

        // Every edge a layer is filled between, its baseline and its top (a layer that falls has its top below its
        // baseline), over what the Y axis can place: a missing value (NaN) and an infinity are left out, as the renderer
        // breaks a layer there, and on a log axis, which has no place for zero (the lowest baseline is drawn at the plot's
        // edge there), the range starts at the lowest edge it can place.
        var x = X.DrawableRange();
        var baselines = Baseline.ComputeFor(YSets, X.Length);
        var y = baselines.Concat(LayerTops(baselines)).DrawableRange(context?.YScale ?? AxisScale.Linear);
        double? yMin = y?.Lo, yMax = y?.Hi;

        // Stack baseline (Zero) is a hard floor iff all stacked values are non-negative.
        double? stickyYMin = Baseline == StackedBaseline.Zero && yMin >= 0 ? 0 : null;
        return new(x?.Lo, x?.Hi, yMin, yMax,
            StickyXMin: x?.Lo, StickyXMax: x?.Hi, StickyYMin: stickyYMin);
    }

    /// <summary>The top edge of every layer: its baseline plus its own value, 0 past the end of a layer shorter than
    /// <see cref="X"/>. The renderer fills each layer between this edge and its baseline, and the range spans it.</summary>
    /// <param name="baselines">The baseline of every layer, from <see cref="StackedBaselineExtensions.ComputeFor"/>.</param>
    /// <returns>One array per layer, as long as <see cref="X"/>.</returns>
    internal double[][] LayerTops(double[][] baselines) => baselines.TopEdgesFor(YSets, X.Length);

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "stackedarea",
        XData = X,
        Datasets = YSets,
        PieLabels = Labels,
        Alpha = Alpha,
        Baseline = Baseline.OrNullAtRest(StackedBaseline.Zero),
        Hatch = Hatch.OrNullAtRest(HatchPattern.None),
        HatchColor = HatchColor,
    };

    /// <summary>Reconstructs a <see cref="StackedAreaSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static StackedAreaSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.StackPlot(dto.XData ?? [], dto.Datasets ?? []);
        s.Labels = dto.PieLabels;
        if (dto.Alpha.HasValue)
        {
            s.Alpha = dto.Alpha.Value;
        }
        s.Baseline = dto.Baseline ?? StackedBaseline.Zero;
        s.Hatch = dto.Hatch ?? HatchPattern.None;
        s.HatchColor = dto.HatchColor;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>One column per stack, named by its own label: the stacks share an x, so the wide form is the
    /// readable one. The values are what each stack CONTRIBUTES, not the cumulative top it is drawn at.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        var columns = new List<ChartDataColumn>(YSets.Length + 1) { new("x", Axis: DataAxis.X) };
        var values = new List<IReadOnlyList<double>>(YSets.Length + 1) { X };
        for (int i = 0; i < YSets.Length; i++)
        {
            string header = Labels is { } labels && i < labels.Length
                ? labels[i]
                : "series " + (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            columns.Add(new(header));
            values.Add(YSets[i]);
        }

        return ChartDataTable.FromNumberColumns(columns, values);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
