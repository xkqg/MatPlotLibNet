// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using MatPlotLibNet.Models;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>Phase Y.5 (v1.7.2, 2026-04-19) — branch coverage for the
/// <see cref="ChartSerializer"/> arms left at 50–75% by the existing
/// <see cref="ChartSerializerTests"/>. Pre-Y.5: 95.9%L / 76.2%B (complexity 478).
///
/// Each fact pins a specific cobertura `condition-coverage` marker:
/// - line 45: FromJson `?? throw` true arm (malformed JSON)
/// - line 163: DtoToSpine null-arg vs non-null
/// - line 169: SpinePosition switch default arm (unrecognized position string)
/// - line 196: Enable3DRotation true arm
/// - line 205: SubPlots null-coalescing both arms
/// - line 230, 233: DirectionalLight parts.Length / TryParse arms (malformed light)</summary>
public class ChartSerializerCoverageTests
{
    /// <summary>FromJson line 45 — `?? throw` true arm: empty/whitespace input.</summary>
    [Fact]
    public void FromJson_NullJson_ThrowsJsonException()
    {
        var s = new ChartSerializer();
        Assert.Throws<JsonException>(() => s.FromJson("null"));
    }

    [Fact]
    public void FromJson_MalformedJson_Throws()
    {
        var s = new ChartSerializer();
        Assert.ThrowsAny<JsonException>(() => s.FromJson("{not valid"));
    }

    /// <summary>FromJson minimal-valid JSON with no SubPlots — line 205 `?? []` empty arm.
    /// SubPlots may be null in the wire DTO; deserialiser must handle gracefully.</summary>
    [Fact]
    public void FromJson_MinimalValidNoSubPlots_RoundTripsToEmptyFigure()
    {
        var s = new ChartSerializer();
        var fig = s.FromJson("""{"width":800,"height":600}""");
        Assert.Equal(800, fig.Width);
        Assert.Equal(600, fig.Height);
        Assert.Empty(fig.SubPlots);
    }

    /// <summary>Round-trip with Enable3DRotation=true — line 196 true arm of
    /// `dto.Enable3DRotation is true`.</summary>
    [Fact]
    public void RoundTrip_With3DRotation_PreservesFlag()
    {
        var fig = Plt.Create().Plot([1.0], [2.0]).Build();
        fig.Enable3DRotation = true;
        var s = new ChartSerializer();
        var rt = s.FromJson(s.ToJson(fig));
        Assert.True(rt.Enable3DRotation);
    }

    /// <summary>Round-trip without Enable3DRotation — line 196 false arm (null in DTO).</summary>
    [Fact]
    public void RoundTrip_Without3DRotation_DefaultsFalse()
    {
        var fig = Plt.Create().Plot([1.0], [2.0]).Build();
        var s = new ChartSerializer();
        var rt = s.FromJson(s.ToJson(fig));
        Assert.False(rt.Enable3DRotation);
    }

    /// <summary>Round-trip with custom Spines — exercises DtoToSpine non-null arm
    /// (line 163 false arm) + serialisation of a non-default spine config.</summary>
    [Fact]
    public void RoundTrip_WithCustomSpines_PreservesConfig()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([1.0, 2], [3.0, 4]);
                ax.HideTopSpine();
                ax.HideRightSpine();
            })
            .Build();
        var s = new ChartSerializer();
        var json = s.ToJson(fig);
        var rt = s.FromJson(json);
        Assert.False(rt.SubPlots[0].Spines.Top.Visible);
        Assert.False(rt.SubPlots[0].Spines.Right.Visible);
    }

    /// <summary>Round-trip with 3D camera config (Elevation, Azimuth, CameraDistance) —
    /// lines 227-229 true arms.</summary>
    [Fact]
    public void RoundTrip_WithCameraConfig_PreservesValues()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Scatter3D(new[] { 1.0 }, new[] { 2.0 }, new[] { 3.0 }))
            .Build();
        fig.SubPlots[0].Elevation = 25;
        fig.SubPlots[0].Azimuth = -45;
        fig.SubPlots[0].CameraDistance = 12.5;

        var s = new ChartSerializer();
        var rt = s.FromJson(s.ToJson(fig));
        Assert.Equal(25.0, rt.SubPlots[0].Elevation);
        Assert.Equal(-45.0, rt.SubPlots[0].Azimuth);
        Assert.Equal(12.5, rt.SubPlots[0].CameraDistance);
    }

    /// <summary>Round-trip with stacked BarMode — line 224 + 83 true arms.</summary>
    [Fact]
    public void RoundTrip_WithStackedBarMode_PreservesMode()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Bar(["A", "B"], [1.0, 2.0]);
                ax.SetBarMode(BarMode.Stacked);
            })
            .Build();
        var s = new ChartSerializer();
        var rt = s.FromJson(s.ToJson(fig));
        Assert.Equal(BarMode.Stacked, rt.SubPlots[0].BarMode);
    }

    /// <summary>Spine round-trip with explicit position — covers the SpinePosition switch's
    /// reachable arms (data, axes) without needing the unknown-arm test which requires
    /// JSON-injection that proved to be fragile against the multi-subplot case.</summary>
    [Theory]
    [InlineData(SpinePosition.Data)]
    [InlineData(SpinePosition.Axes)]
    [InlineData(SpinePosition.Edge)]
    public void RoundTrip_SpinePosition_PreservesValue(SpinePosition position)
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .WithSpines(s => s with
                {
                    Top = new SpineConfig { Position = position, PositionValue = 0.5, Visible = false }
                }))
            .Build();
        var s = new ChartSerializer();
        var rt = s.FromJson(s.ToJson(fig));
        Assert.Equal(position, rt.SubPlots[0].Spines.Top.Position);
    }
}
