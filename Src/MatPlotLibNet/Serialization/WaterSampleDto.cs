// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace MatPlotLibNet.Serialization;

/// <summary>DTO for one <see cref="Models.Series.WaterSample"/>: its name and the concentration of each ion in mg/L, with an
/// ion that is not there left out.</summary>
public sealed record WaterSampleDto
{
    /// <summary>The name of the sample as the legend and the table show it (<see cref="Models.Series.WaterSample.Label"/>);
    /// absent when it has none.</summary>
    public string? Label { get; init; }
    /// <summary>The concentration of calcium in mg/L (<see cref="Models.Series.WaterSample.Ca"/>); left out when the sample
    /// has none.</summary>
    public double? Ca { get; init; }
    /// <summary>The concentration of magnesium in mg/L (<see cref="Models.Series.WaterSample.Mg"/>); left out when the
    /// sample has none.</summary>
    public double? Mg { get; init; }
    /// <summary>The concentration of sodium in mg/L (<see cref="Models.Series.WaterSample.Na"/>); left out when the sample
    /// has none.</summary>
    public double? Na { get; init; }
    /// <summary>The concentration of potassium in mg/L (<see cref="Models.Series.WaterSample.K"/>), which the diagram adds
    /// to sodium; left out when the sample has none.</summary>
    public double? K { get; init; }
    // The camel-case policy would write these as hcO3, cO3 and sO4; the wire names are the ions' own lower-case symbols.
    /// <summary>The concentration of bicarbonate in mg/L (<see cref="Models.Series.WaterSample.HCO3"/>); left out when the
    /// sample has none. Written as <c>hco3</c>.</summary>
    [JsonPropertyName("hco3")] public double? HCO3 { get; init; }
    /// <summary>The concentration of carbonate in mg/L (<see cref="Models.Series.WaterSample.CO3"/>), which the diagram adds
    /// to bicarbonate; left out when the sample has none. Written as <c>co3</c>.</summary>
    [JsonPropertyName("co3")] public double? CO3 { get; init; }
    /// <summary>The concentration of chloride in mg/L (<see cref="Models.Series.WaterSample.Cl"/>); left out when the sample
    /// has none.</summary>
    public double? Cl { get; init; }
    /// <summary>The concentration of sulphate in mg/L (<see cref="Models.Series.WaterSample.SO4"/>); left out when the
    /// sample has none. Written as <c>so4</c>.</summary>
    [JsonPropertyName("so4")] public double? SO4 { get; init; }

    /// <summary>The DTO of <paramref name="sample"/>: every ion that is present, none that is zero.</summary>
    /// <param name="sample">The water.</param>
    internal static WaterSampleDto From(Models.Series.WaterSample sample) => new()
    {
        Label = sample.Label,
        Ca = sample.Ca.OrNullAtRest(0.0), Mg = sample.Mg.OrNullAtRest(0.0), Na = sample.Na.OrNullAtRest(0.0), K = sample.K.OrNullAtRest(0.0),
        HCO3 = sample.HCO3.OrNullAtRest(0.0), CO3 = sample.CO3.OrNullAtRest(0.0), Cl = sample.Cl.OrNullAtRest(0.0), SO4 = sample.SO4.OrNullAtRest(0.0),
    };

    /// <summary>The water this DTO stands for: an ion it leaves out is zero.</summary>
    internal Models.Series.WaterSample ToWaterSample() => new()
    {
        Label = Label,
        Ca = Ca ?? 0, Mg = Mg ?? 0, Na = Na ?? 0, K = K ?? 0, HCO3 = HCO3 ?? 0, CO3 = CO3 ?? 0, Cl = Cl ?? 0, SO4 = SO4 ?? 0,
    };
}
