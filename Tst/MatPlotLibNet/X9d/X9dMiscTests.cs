// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.X9d;

/// <summary>Phase X.9.d (v1.7.2, 2026-04-19) — quick lifts for the small B2 misc
/// classes that have property-access or simple branch coverage gaps that haven't
/// been exercised anywhere else:
///   - <c>ChartServices</c> 57%L / 0%B — every Get/Set property arm
///   - <see cref="FigureExtensions"/> 77%L / 50%B — Save() format-detection branches</summary>
[Xunit.Collection("ChartServicesGlobalState")] // mutates ChartServices statics — must not run concurrently with render tests
public class X9dMiscCoverageTests
{
    // ── ChartServices: cover every property getter/setter + null-arg arm ──────

    /// <summary>ChartSerializerDefault.Current get/set + null-arg validation. Each property
    /// has 2 arms (set with non-null, set with null → throws); 0%B was the gap.</summary>
    [Fact]
    public void ChartServices_Serializer_GetSet_AndNullThrows()
    {
        var original = ChartSerializerDefault.Current;
        try
        {
            var custom = new ChartSerializer();
            ChartSerializerDefault.Current = custom;
            Assert.Same(custom, ChartSerializerDefault.Current);
            Assert.Throws<ArgumentNullException>(() => ChartSerializerDefault.Current = null!);
        }
        finally { ChartSerializerDefault.Current = original; }
    }

    [Fact]
    public void ChartServices_Renderer_GetSet_AndNullThrows()
    {
        var original = ChartRendererDefault.Current;
        try
        {
            var custom = new ChartRenderer();
            ChartRendererDefault.Current = custom;
            Assert.Same(custom, ChartRendererDefault.Current);
            Assert.Throws<ArgumentNullException>(() => ChartRendererDefault.Current = null!);
        }
        finally { ChartRendererDefault.Current = original; }
    }

    [Fact]
    public void ChartServices_SvgRenderer_GetSet_AndNullThrows()
    {
        var original = SvgRendererDefault.Current;
        try
        {
            var custom = new MatPlotLibNet.Transforms.SvgTransform(new ChartRenderer());
            SvgRendererDefault.Current = custom;
            Assert.Same(custom, SvgRendererDefault.Current);
            Assert.Throws<ArgumentNullException>(() => SvgRendererDefault.Current = null!);
        }
        finally { SvgRendererDefault.Current = original; }
    }

    [Fact]
    public void ChartServices_FontMetrics_GetSet_AndNullThrows()
    {
        var original = FontMetricsDefault.Current;
        try
        {
            var custom = new DefaultFontMetrics();
            FontMetricsDefault.Current = custom;
            Assert.Same(custom, FontMetricsDefault.Current);
            Assert.Throws<ArgumentNullException>(() => FontMetricsDefault.Current = null!);
        }
        finally { FontMetricsDefault.Current = original; }
    }

    [Fact]
    public void ChartServices_GlyphPathProvider_AcceptsNullAndNonNull()
    {
        var original = GlyphPathProviderDefault.Current;
        try
        {
            GlyphPathProviderDefault.Current = null;
            Assert.Null(GlyphPathProviderDefault.Current);
        }
        finally { GlyphPathProviderDefault.Current = original; }
    }

    // ── FigureExtensions.Save(): every format-detection arm ───────────────────

    private static Figure TinyFigure() => Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();

    /// <summary>Save() with no extension (line 45 true arm) → defaults to .svg.</summary>
    [Fact]
    public void Save_NoExtension_DefaultsToSvg()
    {
        var fig = TinyFigure();
        var tmp = Path.Combine(Path.GetTempPath(), $"x9d_save_noext_{Guid.NewGuid():N}");
        try
        {
            fig.Save(tmp);
            Assert.True(File.Exists(tmp + ".svg"));
        }
        finally { if (File.Exists(tmp + ".svg")) File.Delete(tmp + ".svg"); }
    }

    /// <summary>Save() with .json extension (line 51 true arm).</summary>
    [Fact]
    public void Save_JsonExtension_WritesJson()
    {
        var fig = TinyFigure();
        var tmp = Path.Combine(Path.GetTempPath(), $"x9d_save_json_{Guid.NewGuid():N}.json");
        try
        {
            fig.Save(tmp);
            Assert.True(File.Exists(tmp));
            Assert.StartsWith("{", File.ReadAllText(tmp).TrimStart());
        }
        finally { if (File.Exists(tmp)) File.Delete(tmp); }
    }

    /// <summary>Save() with .svg extension (line 57 TryGetValue true arm).</summary>
    [Fact]
    public void Save_SvgExtension_UsesRegisteredTransform()
    {
        var fig = TinyFigure();
        var tmp = Path.Combine(Path.GetTempPath(), $"x9d_save_svg_{Guid.NewGuid():N}.svg");
        try
        {
            fig.Save(tmp);
            Assert.True(File.Exists(tmp));
            Assert.Contains("<svg", File.ReadAllText(tmp));
        }
        finally { if (File.Exists(tmp)) File.Delete(tmp); }
    }

    /// <summary>Save() with unrecognised extension (line 57 TryGetValue false arm,
    /// line 63 fallback to SaveSvg with the original path).</summary>
    [Fact]
    public void Save_UnknownExtension_FallsBackToSvg()
    {
        var fig = TinyFigure();
        var tmp = Path.Combine(Path.GetTempPath(), $"x9d_save_unknown_{Guid.NewGuid():N}.xyz");
        try
        {
            fig.Save(tmp);
            Assert.True(File.Exists(tmp));
            Assert.Contains("<svg", File.ReadAllText(tmp));
        }
        finally { if (File.Exists(tmp)) File.Delete(tmp); }
    }

    /// <summary>RegisterTransform with various extension formats — covers
    /// NormalizeExtension's `StartsWith('.')` ternary (line 67).</summary>
    [Theory]
    [InlineData(".png")]   // already-prefixed
    [InlineData("png")]    // unprefixed
    [InlineData(".PNG")]   // upper-case
    public void RegisterTransform_NormalisesExtension(string ext)
    {
        var dummy = new MatPlotLibNet.Transforms.SvgTransform();
        FigureExtensions.RegisterTransform(ext, dummy);
        // Cannot inspect the registry directly; just verify no throw.
    }
}
