// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>Wave J.2 (v1.7.2, 2026-04-21) — surgical branch coverage for the
/// deserialization defensive arms of <see cref="ChartSerializer"/> that JSON
/// round-trip tests never reach (invalid enum strings, malformed light source,
/// missing share-by-key, null break style, unknown secondary series type).
///
/// Strategy: raw JSON injection via <c>FromJson</c> so the parser sees strings
/// that valid API calls never produce. Each test asserts the figure is returned
/// without throwing and that the defensive default was applied.</summary>
public class ChartSerializerDeserializationBranchTests
{
    private static readonly ChartSerializer S = new();

    // ── ApplyEnum null / invalid-string arms ─────────────────────────────────

    /// <summary>ApplyEnum with null → no-op (L698 false arm, setter never called).</summary>
    [Fact]
    public void ApplyEnum_NullValue_NoOp()
    {
        bool called = false;
        ChartSerializer.ApplyEnum<LineStyle>(null, _ => called = true);
        Assert.False(called);
    }

    /// <summary>ApplyEnum with unrecognised string → TryParse false, setter not called.</summary>
    [Fact]
    public void ApplyEnum_InvalidString_NoOp()
    {
        bool called = false;
        ChartSerializer.ApplyEnum<LineStyle>("notALineStyle", _ => called = true);
        Assert.False(called);
    }

    // ── ToJson indented arm ──────────────────────────────────────────────────

    /// <summary>ToJson(indented: true) — L39 true arm.</summary>
    [Fact]
    public void ToJson_Indented_ContainsNewlines()
    {
        var fig = Plt.Create().Plot([1.0], [1.0]).Build();
        var json = S.ToJson(fig, indented: true);
        Assert.Contains('\n', json);
    }

    // ── LightSourceType non-directional / malformed arms ─────────────────────

    /// <summary>LightSourceType that does NOT start with "directional:" —
    /// L230 StartsWith false arm → LightSource stays null.</summary>
    [Fact]
    public void FromJson_NonDirectionalLightSourceType_IgnoresLightSource()
    {
        const string json = """{"width":800,"height":600,"subPlots":[{"lightSourceType":"ambient:0.5"}]}""";
        var fig = S.FromJson(json);
        Assert.Null(fig.SubPlots[0].LightSource);
    }

    /// <summary>Directional light with only 2 comma-separated parts — L233 parts.Length < 5
    /// arm → LightSource stays null.</summary>
    [Fact]
    public void FromJson_MalformedDirectionalLight_TooFewParts_IgnoresLightSource()
    {
        const string json = """{"width":800,"height":600,"subPlots":[{"lightSourceType":"directional:0.5,0.3"}]}""";
        var fig = S.FromJson(json);
        Assert.Null(fig.SubPlots[0].LightSource);
    }

    /// <summary>Directional light with a non-numeric component — one TryParse fails
    /// → LightSource stays null.</summary>
    [Fact]
    public void FromJson_MalformedDirectionalLight_NonNumericPart_IgnoresLightSource()
    {
        const string json = """{"width":800,"height":600,"subPlots":[{"lightSourceType":"directional:0.5,0.3,NaN_bad,0.2,0.8"}]}""";
        var fig = S.FromJson(json);
        Assert.Null(fig.SubPlots[0].LightSource);
    }

    // ── Annotation TryParse failure arms ─────────────────────────────────────

    /// <summary>Annotation with invalid ConnectionStyle string → TryParse false (L250),
    /// ConnectionStyle stays at default Straight.</summary>
    [Fact]
    public void FromJson_AnnotationWithInvalidConnectionStyle_DefaultsStraight()
    {
        const string json = """
        {
            "width":800,"height":600,
            "subPlots":[{
                "annotations":[{
                    "text":"hi","x":1,"y":1,
                    "connectionStyle":"notAStyle"
                }]
            }]
        }
        """;
        var fig = S.FromJson(json);
        Assert.Equal(ConnectionStyle.Straight, fig.SubPlots[0].Annotations[0].ConnectionStyle);
    }

    /// <summary>Annotation with invalid BoxStyle string → TryParse false (L254),
    /// BoxStyle stays at default None.</summary>
    [Fact]
    public void FromJson_AnnotationWithInvalidBoxStyle_DefaultsNone()
    {
        const string json = """
        {
            "width":800,"height":600,
            "subPlots":[{
                "annotations":[{
                    "text":"hi","x":1,"y":1,
                    "boxStyle":"notABoxStyle"
                }]
            }]
        }
        """;
        var fig = S.FromJson(json);
        Assert.Equal(BoxStyle.None, fig.SubPlots[0].Annotations[0].BoxStyle);
    }

    // ── ReferenceLines TryParse failure arm ──────────────────────────────────

    /// <summary>ReferenceLine with invalid LineStyle string → TryParse false (L264),
    /// LineStyle stays at default Solid.</summary>
    [Fact]
    public void FromJson_ReferenceLineWithInvalidLineStyle_DefaultsSolid()
    {
        const string json = """
        {
            "width":800,"height":600,
            "subPlots":[{
                "referenceLines":[{
                    "value":5.0,"orientation":"horizontal",
                    "lineStyle":"notAStyle","lineWidth":1.0
                }]
            }]
        }
        """;
        var fig = S.FromJson(json);
        // TryParse fails → LineStyle stays at the ReferenceLine default (Dashed)
        Assert.Equal(LineStyle.Dashed, fig.SubPlots[0].ReferenceLines[0].LineStyle);
    }

    // ── SecondaryYAxis unknown series type arm ────────────────────────────────

    /// <summary>SecondaryYAxis present but secondary series has an unknown type —
    /// switch default arm (L278 `_ => null`) + `if (sec is not null)` false arm (L283).</summary>
    [Fact]
    public void FromJson_SecondarySeriesUnknownType_SkipsGracefully()
    {
        const string json = """
        {
            "width":800,"height":600,
            "subPlots":[{
                "secondaryYAxis":{"label":"right"},
                "secondarySeries":[{"type":"futureType","label":"skip"}]
            }]
        }
        """;
        var fig = S.FromJson(json);
        Assert.NotNull(fig.SubPlots[0].SecondaryYAxis);
        Assert.Empty(fig.SubPlots[0].SecondarySeries);
    }

    // ── Spans TryParse failure arm ────────────────────────────────────────────

    /// <summary>Span with invalid LineStyle string → TryParse false (L292),
    /// LineStyle stays at default None.</summary>
    [Fact]
    public void FromJson_SpanWithInvalidLineStyle_DefaultsNone()
    {
        const string json = """
        {
            "width":800,"height":600,
            "subPlots":[{
                "spans":[{
                    "min":1,"max":3,"orientation":"horizontal","alpha":0.3,
                    "lineStyle":"notAStyle"
                }]
            }]
        }
        """;
        var fig = S.FromJson(json);
        Assert.Equal(LineStyle.None, fig.SubPlots[0].Spans[0].LineStyle);
    }

    // ── BreakStyle null / invalid arms ───────────────────────────────────────

    /// <summary>XBreak DTO with null Style → `bDto.Style is not null` false arm (L314),
    /// defaults to Zigzag.</summary>
    [Fact]
    public void FromJson_XBreakWithNullStyle_DefaultsZigzag()
    {
        const string json = """
        {
            "width":800,"height":600,
            "subPlots":[{
                "xBreaks":[{"from":10,"to":20,"style":null}]
            }]
        }
        """;
        var fig = S.FromJson(json);
        Assert.Single(fig.SubPlots[0].XBreaks);
        Assert.Equal(BreakStyle.Zigzag, fig.SubPlots[0].XBreaks[0].Style);
    }

    /// <summary>YBreak with invalid Style string → TryParse false (L319), defaults to Zigzag.</summary>
    [Fact]
    public void FromJson_YBreakWithInvalidStyle_DefaultsZigzag()
    {
        const string json = """
        {
            "width":800,"height":600,
            "subPlots":[{
                "yBreaks":[{"from":5,"to":15,"style":"notAStyle"}]
            }]
        }
        """;
        var fig = S.FromJson(json);
        Assert.Single(fig.SubPlots[0].YBreaks);
        Assert.Equal(BreakStyle.Zigzag, fig.SubPlots[0].YBreaks[0].Style);
    }

    // ── ShareX/Y key not found arm ────────────────────────────────────────────

    /// <summary>ShareXKey pointing to a non-existent key → TryGetValue false (L335),
    /// ShareXWith stays null.</summary>
    [Fact]
    public void FromJson_ShareXKeyNotFound_ShareXWithStaysNull()
    {
        const string json = """
        {
            "width":800,"height":600,
            "subPlots":[
                {"key":"ax1"},
                {"shareXKey":"doesNotExist"}
            ]
        }
        """;
        var fig = S.FromJson(json);
        Assert.Null(fig.SubPlots[1].ShareXWith);
    }

    // ── ApplyAxis invalid scale string ────────────────────────────────────────

    /// <summary>XAxis with an unrecognised scale string → TryParse false (L349),
    /// Scale stays at default Linear.</summary>
    [Fact]
    public void FromJson_AxisWithInvalidScale_DefaultsLinear()
    {
        const string json = """
        {
            "width":800,"height":600,
            "subPlots":[{
                "xAxis":{"scale":"notAScale"}
            }]
        }
        """;
        var fig = S.FromJson(json);
        Assert.Equal(AxisScale.Linear, fig.SubPlots[0].XAxis.Scale);
    }

    // ── Inset without InsetBounds — defensive continue arm ───────────────────

    /// <summary>Inset DTO missing InsetBounds → L301 `continue` arm fires,
    /// no inset is added to the figure.</summary>
    [Fact]
    public void FromJson_InsetWithoutInsetBounds_SkipsInset()
    {
        const string json = """
        {
            "width":800,"height":600,
            "subPlots":[{
                "insets":[{"series":[]}]
            }]
        }
        """;
        var fig = S.FromJson(json);
        Assert.Empty(fig.SubPlots[0].Insets);
    }

    // ── ShareY key found — TRUE arm ──────────────────────────────────────────

    /// <summary>ShareYKey pointing to an existing key → L337 TryGetValue TRUE arm,
    /// ShareYWith is resolved.</summary>
    [Fact]
    public void FromJson_ShareYKeyFound_ShareYWithResolved()
    {
        const string json = """
        {
            "width":800,"height":600,
            "subPlots":[
                {"key":"ax1"},
                {"shareYKey":"ax1"}
            ]
        }
        """;
        var fig = S.FromJson(json);
        Assert.NotNull(fig.SubPlots[1].ShareYWith);
        Assert.Same(fig.SubPlots[0], fig.SubPlots[1].ShareYWith);
    }

    // ── SecondaryYAxis "scatter" type arm ────────────────────────────────────

    /// <summary>SecondaryYAxis with a "scatter" series → L422 CreateSecondaryScatter arm.</summary>
    [Fact]
    public void FromJson_SecondarySeriesScatterType_CreatesScatterSeries()
    {
        const string json = """
        {
            "width":800,"height":600,
            "subPlots":[{
                "secondaryYAxis":{"label":"right"},
                "secondarySeries":[{
                    "type":"scatter","xData":[1,2],"yData":[10,20],"label":"sec-scatter"
                }]
            }]
        }
        """;
        var fig = S.FromJson(json);
        Assert.NotNull(fig.SubPlots[0].SecondaryYAxis);
        Assert.Single(fig.SubPlots[0].SecondarySeries);
        Assert.IsType<MatPlotLibNet.Models.Series.ScatterSeries>(fig.SubPlots[0].SecondarySeries[0]);
    }

    // ── SeriesRegistry null-fallback arms (L36, L45, L105, L116, L124 etc.) ──
    // Each test omits one or more required series fields from JSON so the
    // ?? [] / ?? [0.0, 1.0] null-coalescing TRUE arms fire during deserialization.

    /// <summary>Hexbin with no XData/YData → L36 both ?? arms fire, series created.</summary>
    [Fact]
    public void FromJson_HexbinWithNoXYData_UsesNullFallback()
    {
        const string json = """{"width":800,"height":600,"subPlots":[{"series":[{"type":"hexbin"}]}]}""";
        var fig = S.FromJson(json);
        Assert.Single(fig.SubPlots[0].Series);
    }

    /// <summary>Regression with no XData/YData → L45 both ?? arms fire.</summary>
    [Fact]
    public void FromJson_RegressionWithNoXYData_UsesNullFallback()
    {
        const string json = """{"width":800,"height":600,"subPlots":[{"series":[{"type":"regression"}]}]}""";
        var fig = S.FromJson(json);
        Assert.Single(fig.SubPlots[0].Series);
    }

    /// <summary>Surface with no XData/YData → L105 both ?? arms fire.</summary>
    [Fact]
    public void FromJson_SurfaceWithNoXYData_UsesNullFallback()
    {
        const string json = """{"width":800,"height":600,"subPlots":[{"series":[{"type":"surface"}]}]}""";
        var fig = S.FromJson(json);
        Assert.Single(fig.SubPlots[0].Series);
    }

    /// <summary>Wireframe with no XData/YData → L116 both ?? arms fire.</summary>
    [Fact]
    public void FromJson_WireframeWithNoXYData_UsesNullFallback()
    {
        const string json = """{"width":800,"height":600,"subPlots":[{"series":[{"type":"wireframe"}]}]}""";
        var fig = S.FromJson(json);
        Assert.Single(fig.SubPlots[0].Series);
    }

    /// <summary>Scatter3D with no XData/YData → L124 two ?? arms fire.</summary>
    [Fact]
    public void FromJson_Scatter3DWithNoXYData_UsesNullFallback()
    {
        const string json = """{"width":800,"height":600,"subPlots":[{"series":[{"type":"scatter3d"}]}]}""";
        var fig = S.FromJson(json);
        Assert.Single(fig.SubPlots[0].Series);
    }

    /// <summary>Rugplot with no Data → L133 ?? arm fires.</summary>
    [Fact]
    public void FromJson_RugplotWithNoData_UsesNullFallback()
    {
        const string json = """{"width":800,"height":600,"subPlots":[{"series":[{"type":"rugplot"}]}]}""";
        var fig = S.FromJson(json);
        Assert.Single(fig.SubPlots[0].Series);
    }

    /// <summary>Stripplot with no Datasets → L142 ?? arm fires.</summary>
    [Fact]
    public void FromJson_StripplotWithNoDatasets_UsesNullFallback()
    {
        const string json = """{"width":800,"height":600,"subPlots":[{"series":[{"type":"stripplot"}]}]}""";
        var fig = S.FromJson(json);
        Assert.Single(fig.SubPlots[0].Series);
    }

    /// <summary>Eventplot with no EventPositions → L151 ?? arm fires.</summary>
    [Fact]
    public void FromJson_EventplotWithNoPositions_UsesNullFallback()
    {
        const string json = """{"width":800,"height":600,"subPlots":[{"series":[{"type":"eventplot"}]}]}""";
        var fig = S.FromJson(json);
        Assert.Single(fig.SubPlots[0].Series);
    }

    /// <summary>BrokenBarH with no RangeStarts/RangeWidths → L158+L159 arms fire.</summary>
    [Fact]
    public void FromJson_BrokenBarWithNoRanges_UsesNullFallback()
    {
        const string json = """{"width":800,"height":600,"subPlots":[{"series":[{"type":"brokenbar"}]}]}""";
        var fig = S.FromJson(json);
        Assert.Single(fig.SubPlots[0].Series);
    }

    /// <summary>Countplot with no Categories → L177 ?? arm fires.</summary>
    [Fact]
    public void FromJson_CountplotWithNoCategories_UsesNullFallback()
    {
        const string json = """{"width":800,"height":600,"subPlots":[{"series":[{"type":"count"}]}]}""";
        var fig = S.FromJson(json);
        Assert.Single(fig.SubPlots[0].Series);
    }

    /// <summary>Pcolormesh with no XData/YData → L185 two ?? arms fire.</summary>
    [Fact]
    public void FromJson_PcolormeshWithNoXYData_UsesNullFallback()
    {
        const string json = """{"width":800,"height":600,"subPlots":[{"series":[{"type":"pcolormesh"}]}]}""";
        var fig = S.FromJson(json);
        Assert.Single(fig.SubPlots[0].Series);
    }

    /// <summary>Residual with no XData/YData → L192 two ?? arms fire.</summary>
    [Fact]
    public void FromJson_ResidualWithNoXYData_UsesNullFallback()
    {
        const string json = """{"width":800,"height":600,"subPlots":[{"series":[{"type":"residual"}]}]}""";
        var fig = S.FromJson(json);
        Assert.Single(fig.SubPlots[0].Series);
    }

    // ── ChartSerializer CreateXxx null-fallback arms (L366, L377, L389 etc.) ──

    /// <summary>Line with no XData/YData → CreateLine L366 two ?? arms fire.</summary>
    [Fact]
    public void FromJson_LineWithNoXYData_UsesNullFallback()
    {
        const string json = """{"width":800,"height":600,"subPlots":[{"series":[{"type":"line"}]}]}""";
        var fig = S.FromJson(json);
        Assert.Single(fig.SubPlots[0].Series);
    }

    /// <summary>Scatter with no XData/YData → CreateScatter L377 two ?? arms fire.</summary>
    [Fact]
    public void FromJson_ScatterWithNoXYData_UsesNullFallback()
    {
        const string json = """{"width":800,"height":600,"subPlots":[{"series":[{"type":"scatter"}]}]}""";
        var fig = S.FromJson(json);
        Assert.Single(fig.SubPlots[0].Series);
    }

    /// <summary>Bar with no Categories/Values → CreateBar L389 two ?? arms fire.</summary>
    [Fact]
    public void FromJson_BarWithNoCategories_UsesNullFallback()
    {
        const string json = """{"width":800,"height":600,"subPlots":[{"series":[{"type":"bar"}]}]}""";
        var fig = S.FromJson(json);
        Assert.Single(fig.SubPlots[0].Series);
    }

    /// <summary>Histogram with no Data → CreateHistogram L402 one ?? arm fires.</summary>
    [Fact]
    public void FromJson_HistogramWithNoData_UsesNullFallback()
    {
        const string json = """{"width":800,"height":600,"subPlots":[{"series":[{"type":"histogram"}]}]}""";
        var fig = S.FromJson(json);
        Assert.Single(fig.SubPlots[0].Series);
    }

    /// <summary>Quiver with no XData/YData/UData/VData → CreateQuiver L451 four ?? arms fire.</summary>
    [Fact]
    public void FromJson_QuiverWithNoXYUV_UsesNullFallback()
    {
        const string json = """{"width":800,"height":600,"subPlots":[{"series":[{"type":"quiver"}]}]}""";
        var fig = S.FromJson(json);
        Assert.Single(fig.SubPlots[0].Series);
    }

    /// <summary>Candlestick with no Open/High/Low/Close → CreateCandlestick L476 four ?? arms fire.</summary>
    [Fact]
    public void FromJson_CandlestickWithNoOHLC_UsesNullFallback()
    {
        const string json = """{"width":800,"height":600,"subPlots":[{"series":[{"type":"candlestick"}]}]}""";
        var fig = S.FromJson(json);
        Assert.Single(fig.SubPlots[0].Series);
    }

    /// <summary>ErrorBar with no XData/YData/YErrorLow/YErrorHigh → CreateErrorBar L489 four ?? arms fire.</summary>
    [Fact]
    public void FromJson_ErrorBarWithNoXYAndErrors_UsesNullFallback()
    {
        const string json = """{"width":800,"height":600,"subPlots":[{"series":[{"type":"errorbar"}]}]}""";
        var fig = S.FromJson(json);
        Assert.Single(fig.SubPlots[0].Series);
    }

    /// <summary>Surface with ShowWireframe=false (non-default; default is true) →
    /// L107 <c>if (dto.ShowWireframe.HasValue)</c> TRUE arm fires and sets ShowWireframe=false.</summary>
    [Fact]
    public void FromJson_SurfaceWithShowWireframeFalse_SetsWireframeFalse()
    {
        const string json = """
        {
            "width":800,"height":600,
            "subPlots":[{"series":[{
                "type":"surface",
                "xData":[0,1],"yData":[0,1],
                "zGridData":[[0,1],[1,0]],
                "showWireframe":false
            }]}]
        }
        """;
        var fig = S.FromJson(json);
        var surface = Assert.IsType<MatPlotLibNet.Models.Series.SurfaceSeries>(fig.SubPlots[0].Series[0]);
        Assert.False(surface.ShowWireframe);
    }
}
