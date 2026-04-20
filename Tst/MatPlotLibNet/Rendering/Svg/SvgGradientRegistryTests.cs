// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>
/// Phase F.2.d — RED: pins the extracted <see cref="SvgGradientRegistry"/> instance
/// collaborator. Replaces the private <c>_gradientId</c> counter + inline
/// <c>&lt;linearGradient&gt;</c> emission inside <see cref="SvgRenderContext"/> with a
/// dedicated SRP class (gradient emission + id generation). TDD per
/// <c>feedback_real_tdd_solid.md</c>: every test targets one branch family of the
/// original private impl.
/// </summary>
public class SvgGradientRegistryTests
{
    [Fact]
    public void Register_FirstGradient_ReturnsId0()
    {
        var sb = new StringBuilder();
        var reg = new SvgGradientRegistry(sb);
        var id = reg.Register(Color.FromHex("#FF0000"), Color.FromHex("#00FF00"), 0, 0, 100, 100);
        Assert.Equal("grad-0", id);
    }

    [Fact]
    public void Register_SecondGradient_ReturnsIncrementedId()
    {
        var sb = new StringBuilder();
        var reg = new SvgGradientRegistry(sb);
        reg.Register(Color.FromHex("#FF0000"), Color.FromHex("#00FF00"), 0, 0, 10, 0);
        var id2 = reg.Register(Color.FromHex("#0000FF"), Color.FromHex("#FFFF00"), 0, 0, 20, 0);
        Assert.Equal("grad-1", id2);
    }

    [Fact]
    public void Register_EmitsDefsLinearGradientWrapper()
    {
        var sb = new StringBuilder();
        new SvgGradientRegistry(sb).Register(Color.FromHex("#FF0000"), Color.FromHex("#00FF00"), 0, 0, 100, 100);
        var s = sb.ToString();
        Assert.Contains("<defs><linearGradient id=\"grad-0\"", s);
        Assert.Contains("</linearGradient></defs>", s);
    }

    [Fact]
    public void Register_EmitsGradientUnitsUserSpaceOnUse()
    {
        var sb = new StringBuilder();
        new SvgGradientRegistry(sb).Register(Color.FromHex("#FF0000"), Color.FromHex("#00FF00"), 0, 0, 100, 100);
        Assert.Contains("gradientUnits=\"userSpaceOnUse\"", sb.ToString());
    }

    [Fact]
    public void Register_EmitsExactCoordinates()
    {
        var sb = new StringBuilder();
        new SvgGradientRegistry(sb).Register(Color.FromHex("#FF0000"), Color.FromHex("#00FF00"), 1.5, 2.5, 10.25, 20.75);
        var s = sb.ToString();
        Assert.Contains("x1=\"1.5\"", s);
        Assert.Contains("y1=\"2.5\"", s);
        Assert.Contains("x2=\"10.25\"", s);
        Assert.Contains("y2=\"20.75\"", s);
    }

    [Fact]
    public void Register_EmitsTwoStopsAtOffsetZeroAndOne()
    {
        var sb = new StringBuilder();
        new SvgGradientRegistry(sb).Register(Color.FromHex("#FF0000"), Color.FromHex("#00FF00"), 0, 0, 100, 100);
        var s = sb.ToString();
        Assert.Contains("<stop offset=\"0\" stop-color=\"#FF0000\"", s);
        Assert.Contains("<stop offset=\"1\" stop-color=\"#00FF00\"", s);
    }

    [Fact]
    public void Register_OpaqueColors_NoStopOpacity()
    {
        var sb = new StringBuilder();
        new SvgGradientRegistry(sb).Register(Color.FromHex("#FF0000"), Color.FromHex("#00FF00"), 0, 0, 100, 100);
        Assert.DoesNotContain("stop-opacity", sb.ToString());
    }

    [Fact]
    public void Register_TransparentFromColor_EmitsStopOpacity()
    {
        var sb = new StringBuilder();
        var half = new Color(255, 0, 0, 128);
        new SvgGradientRegistry(sb).Register(half, Color.FromHex("#00FF00"), 0, 0, 100, 100);
        Assert.Contains("stop-opacity=\"", sb.ToString());
    }

    [Fact]
    public void Register_ByteEquivalent_ToPreExtractionSvgRenderContextOutput()
    {
        // Pin byte-exact output shape matching the private impl that lived in SvgRenderContext
        // before this extraction. If SVG consumers (browsers, the fidelity suite) depended on
        // any whitespace or attribute-order quirk, this test catches a drift.
        var sb = new StringBuilder();
        new SvgGradientRegistry(sb).Register(Color.FromHex("#AABBCC"), Color.FromHex("#112233"), 10, 20, 30, 40);
        var expected =
            "<defs><linearGradient id=\"grad-0\" gradientUnits=\"userSpaceOnUse\" x1=\"10\" y1=\"20\" x2=\"30\" y2=\"40\">" +
            "<stop offset=\"0\" stop-color=\"#AABBCC\" />" +
            "<stop offset=\"1\" stop-color=\"#112233\" />" +
            "</linearGradient></defs>" + Environment.NewLine;
        Assert.Equal(expected, sb.ToString());
    }
}
