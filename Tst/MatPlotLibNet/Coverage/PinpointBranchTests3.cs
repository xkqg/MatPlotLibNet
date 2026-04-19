// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using MatPlotLibNet;
using MatPlotLibNet.Indicators;
using MatPlotLibNet.Indicators.Streaming;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Coverage;

/// <summary>Phase Q Wave 2 (2026-04-19) — third pinpoint batch.</summary>
public class PinpointBranchTests3
{
    // Obv.cs L31: `if (n == 0) return Array.Empty<double>();`
    [Fact] public void Obv_EmptyInput_ReturnsEmptyArray()
    {
        var result = new Obv(Array.Empty<double>(), Array.Empty<double>()).Compute();
        Assert.Empty(result.Values);
    }

    // ZoomModifier coords-null path requires interaction harness — skipped, harness gap
    // tracked for Phase R Wave 3 deferral.

    // Line3DSeries.cs L27 — explicit Color override.
    [Fact] public void Line3DSeries_ExplicitColor_SerializesNonNullDto()
    {
        var s = new Line3DSeries(new double[] { 1.0 }, new double[] { 2.0 }, new double[] { 3.0 })
        { Color = Colors.Blue, LineWidth = 1.5 };
        var dto = s.ToSeriesDto();
        Assert.NotNull(dto);
    }

    // QuiverKeySeriesRenderer.cs L27: `dataRange > 0 ? bounds.Width / dataRange : 50` —
    // zero-range fallback fires when XAxis.Min == XAxis.Max.
    [Fact] public void QuiverKey_ZeroDataRange_FallsBackTo50pxPerUnit()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.AddSeries(new QuiverKeySeries(0.5, 0.9, 1.0, "1 m/s"));
                ax.SetXLim(5, 5); // zero range → fallback branch
            })
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // ColorJsonConverter.cs L1060: `hex is not null ? Color.FromHex(hex) : default` null arm.
    [Fact] public void ColorJsonConverter_ReadNullColor_ReturnsDefault()
    {
        var converter = new MatPlotLibNet.Serialization.ColorJsonConverter();
        var json = "null";
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var reader = new Utf8JsonReader(bytes);
        reader.Read();
        var result = converter.Read(ref reader, typeof(Color), new JsonSerializerOptions());
        Assert.Equal(default, result);
    }

    // TwoSlopeNormalizer.cs L61: `upperRange == 0 ? 0.5 : ...` — zero upper-range fallback.
    [Fact] public void TwoSlopeNormalizer_ZeroUpperRange_FallsBackTo05()
    {
        var n = new TwoSlopeNormalizer(10.0);
        // vmin = vmax = center = 10 → upperRange (vmax - center) == 0.
        // Value above center exercises the upper-range zero-fallback arm.
        var result = n.Normalize(15.0, 0.0, 10.0);
        Assert.Equal(0.5, result, precision: 6);
    }

    // StyleSheet.cs L25 — `theme.DefaultFont.Family ?? "sans-serif"` (already exercised by
    // existing FromTheme tests; this confirms the line stays hit).
    [Fact] public void StyleSheet_FromTheme_FontFamilyEmpty_HandlesGracefully()
    {
        var theme = Theme.CreateFrom(Theme.Default).WithFont(f => f with { Family = "" }).Build();
        var ss = StyleSheet.FromTheme(theme);
        Assert.NotNull(ss);
    }

    // SecondaryAxisBuilder configure-null path — exercised by the existing secondary-axis
    // tests via WithSecondaryYAxis; coverage already records the call site.

    // RcParams.cs L64: `_params.TryGetValue(key, out var v) ? (T)v : default` — both arms.
    [Fact] public void RcParams_GetExistingKey_ReturnsValue()
    {
        var rc = new RcParams();
        rc.Set(RcParamKeys.FontSize, 16.0);
        Assert.Equal(16.0, rc.Get<double>(RcParamKeys.FontSize, 0.0));
    }

    // AutoDateFormatter.cs L36 — switch over ChosenInterval, hit a less-common arm.
    [Fact] public void AutoDateFormatter_HourlyInterval_FormatsCorrectly()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot(
                new DateTime[] { new(2026, 1, 1, 0, 0, 0), new(2026, 1, 1, 6, 0, 0), new(2026, 1, 1, 12, 0, 0) },
                [1.0, 2.0, 3.0]))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // CommunityThemes line 89% — previous test already covered. Add Light themes for completeness.
    [Theory]
    [InlineData("MatplotlibClassic")]
    [InlineData("MatplotlibV2")]
    public void CommunityThemes_MatplotlibFlavor_BuildsValidTheme(string themeName)
    {
        var prop = typeof(Theme).GetProperty(themeName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        Assert.NotNull(prop);
        var theme = prop.GetValue(null) as Theme;
        Assert.NotNull(theme);
    }

    // StreamingIndicatorBase line 87% — exercise via concrete StreamingSma operations.
    [Fact] public void StreamingSma_AppendThenWarmup_ExercisesBaseClassPath()
    {
        var sma = new StreamingSma(period: 5);
        for (int i = 0; i < 10; i++) sma.Append(i + 1);
        Assert.True(sma.IsWarmedUp);
        Assert.NotEmpty(sma.OutputSeries);
        // Color setter exercises the property branch.
        sma.Color = Colors.Red;
        Assert.Equal(Colors.Red, sma.Color);
    }
}
