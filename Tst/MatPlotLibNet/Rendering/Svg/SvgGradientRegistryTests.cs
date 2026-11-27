// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>
/// Phase F.2.d — RED: pins the extracted <see cref="Rendering.Svg.SvgElementBuffer.SvgGradientRegistry"/> instance
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
        var reg = new SvgElementBuffer.SvgGradientRegistry(sb);
        var id = reg.Register(Color.FromHex("#FF0000"), Color.FromHex("#00FF00"), 0, 0, 100, 100);
        Assert.Equal("grad-0", id);
    }

    [Fact]
    public void Register_SecondGradient_ReturnsIncrementedId()
    {
        var sb = new StringBuilder();
        var reg = new SvgElementBuffer.SvgGradientRegistry(sb);
        reg.Register(Color.FromHex("#FF0000"), Color.FromHex("#00FF00"), 0, 0, 10, 0);
        var id2 = reg.Register(Color.FromHex("#0000FF"), Color.FromHex("#FFFF00"), 0, 0, 20, 0);
        Assert.Equal("grad-1", id2);
    }

    /// <summary>What a fresh registry writes when it registers a gradient.</summary>
    private static string RegisteredSvg(Color from, Color to, double x1, double y1, double x2, double y2)
    {
        var sb = new StringBuilder();
        new SvgElementBuffer.SvgGradientRegistry(sb).Register(from, to, x1, y1, x2, y2);
        return sb.ToString();
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("Register_EmitsDefsLinearGradientWrapper", () => RegisteredSvg(Color.FromHex("#FF0000"), Color.FromHex("#00FF00"), 0, 0, 100, 100), "<defs><linearGradient id=\"grad-0\"", "</linearGradient></defs>"),
        new DrawnToken("Register_EmitsGradientUnitsUserSpaceOnUse", () => RegisteredSvg(Color.FromHex("#FF0000"), Color.FromHex("#00FF00"), 0, 0, 100, 100), "gradientUnits=\"userSpaceOnUse\""),
        new DrawnToken("Register_EmitsExactCoordinates", () => RegisteredSvg(Color.FromHex("#FF0000"), Color.FromHex("#00FF00"), 1.5, 2.5, 10.25, 20.75), "x1=\"1.5\"", "y1=\"2.5\"", "x2=\"10.25\"", "y2=\"20.75\""),
        new DrawnToken("Register_EmitsTwoStopsAtOffsetZeroAndOne", () => RegisteredSvg(Color.FromHex("#FF0000"), Color.FromHex("#00FF00"), 0, 0, 100, 100), "<stop offset=\"0\" stop-color=\"#FF0000\"", "<stop offset=\"1\" stop-color=\"#00FF00\""),
        new DrawnToken("Register_OpaqueColors_NoStopOpacity", () => RegisteredSvg(Color.FromHex("#FF0000"), Color.FromHex("#00FF00"), 0, 0, 100, 100)) { Absent = ["stop-opacity"] },
        new DrawnToken("Register_TransparentFromColor_EmitsStopOpacity", () => RegisteredSvg(new Color(255, 0, 0, 128), Color.FromHex("#00FF00"), 0, 0, 100, 100), "stop-opacity=\""),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Fact]
    public void Register_ByteEquivalent_ToPreExtractionSvgRenderContextOutput()
    {
        // Pin byte-exact output shape matching the private impl that lived in SvgRenderContext
        // before this extraction. If SVG consumers (browsers, the fidelity suite) depended on
        // any whitespace or attribute-order quirk, this test catches a drift.
        var expected =
            "<defs><linearGradient id=\"grad-0\" gradientUnits=\"userSpaceOnUse\" x1=\"10\" y1=\"20\" x2=\"30\" y2=\"40\">" +
            "<stop offset=\"0\" stop-color=\"#AABBCC\" />" +
            "<stop offset=\"1\" stop-color=\"#112233\" />" +
            "</linearGradient></defs>\n";
        Assert.Equal(expected, RegisteredSvg(Color.FromHex("#AABBCC"), Color.FromHex("#112233"), 10, 20, 30, 40));
    }
}
