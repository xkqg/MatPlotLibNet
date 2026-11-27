// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Models;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>An enum travels in the JSON as a name, and a reader that does not know the name used to drop it without a word: a
/// figure written by a newer version with a coordinate system, a line style or a bar orientation this reader has never heard of
/// came back drawn as the default, and nothing said so. The reader stays lenient (the figure loads) but announces the text it could
/// not place, on the channel every other silent degradation already uses. A member is named by its name, in any case, and by nothing
/// else (<see cref="EnumWireNames{T}"/>).</summary>
[Collection("ChartDiagnosticsGlobalState")]
public class EnumWireDiagnosticTests
{
    [Fact]
    public void AnUnknownName_IsAnnouncedWithTheValueAndTheEnum_AndNamesNoMember()
    {
        LineStyle? read = LineStyle.Solid;

        var seen = DiagnosticsCapture.During(() => read = "zigzag".WireEnum<LineStyle>());

        Assert.Null(read);
        var announcement = Assert.Single(seen);
        Assert.Contains("'zigzag'", announcement.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(LineStyle), announcement.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Dashed")]
    [InlineData("dashed")]
    [InlineData("DASHED")]
    public void AKnownName_IsReadInAnyCase_AndNothingIsAnnounced(string name)
    {
        LineStyle? read = null;

        var seen = DiagnosticsCapture.During(() => read = name.WireEnum<LineStyle>());

        Assert.Equal(LineStyle.Dashed, read);
        Assert.Empty(seen);
    }

    /// <summary>A text <see cref="Enum.TryParse{TEnum}(string?, bool, out TEnum)"/> would make a value of names no member: the
    /// ordinal of a member, an ordinal no member has, two names joined by a comma, a name with a space around it, an empty text.</summary>
    [Theory]
    [InlineData("1")]
    [InlineData("99")]
    [InlineData("-1")]
    [InlineData("solid, dashed")]
    [InlineData(" dashed")]
    [InlineData("dashed ")]
    [InlineData("")]
    public void ATextTryParseWouldAccept_NamesNoMember_AndIsAnnounced(string text)
    {
        LineStyle? read = LineStyle.Solid;

        var seen = DiagnosticsCapture.During(() => read = text.WireEnum<LineStyle>());

        Assert.Null(read);
        Assert.Contains($"'{text}'", Assert.Single(seen).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NoValue_IsNotAnUnknownValue()
    {
        string? absent = null;
        LineStyle? read = LineStyle.Solid;

        var seen = DiagnosticsCapture.During(() => read = absent.WireEnum<LineStyle>());

        Assert.Null(read);
        Assert.Empty(seen);
    }

    [Fact]
    public void TheNameRule_FindsANameInAnyCase_AndNothingElse()
    {
        string[] names = ["Grouped", "Stacked"];

        Assert.Equal(1, names.IndexOfWireName("stacked"));
        Assert.Equal(0, names.IndexOfWireName("GROUPED"));
        Assert.Equal(-1, names.IndexOfWireName("stack"));
        Assert.Equal(-1, names.IndexOfWireName("stackedx"));
        Assert.Equal(-1, ((string[])[]).IndexOfWireName("stacked"));
    }

    [Fact]
    public void TheTable_MatchesEveryMemberByItsName()
    {
        foreach (var member in Enum.GetValues<CoordinateSystem>())
        {
            Assert.True(EnumWireNames<CoordinateSystem>.TryMatch(member.ToWireName(), out var read));
            Assert.Equal(member, read);
        }

        Assert.False(EnumWireNames<CoordinateSystem>.TryMatch("2", out var none));
        Assert.Equal(default, none);
    }

    [Fact]
    public void AFigureWithACoordinateSystemThisReaderDoesNotKnow_LoadsAsCartesian_AndSaysSo()
    {
        var figure = new Figure { Width = 400, Height = 300 };
        figure.AddSubPlot(1, 1, 1).CoordinateSystem = CoordinateSystem.Ternary;
        // What a newer writer would send: a name this reader has never heard of.
        string written = figure.ToJson().Replace("\"ternary\"", "\"hexagonal\"", StringComparison.Ordinal);
        Assert.Contains("hexagonal", written, StringComparison.Ordinal);

        Figure? read = null;
        var seen = DiagnosticsCapture.During(() => read = new ChartSerializer().FromJson(written));

        Assert.NotNull(read);
        Assert.Equal(CoordinateSystem.Cartesian, read!.SubPlots[0].CoordinateSystem);
        Assert.Contains(seen, d => d.Message.Contains("hexagonal", StringComparison.Ordinal)
                                   && d.Message.Contains(nameof(CoordinateSystem), StringComparison.Ordinal));
    }

    [Fact]
    public void ATernaryAxes_SurvivesTheJson()
    {
        var figure = new Figure { Width = 400, Height = 300 };
        var axes = figure.AddSubPlot(1, 1, 1);
        axes.CoordinateSystem = CoordinateSystem.Ternary;
        axes.XAxis.Label = "Sand";
        axes.YAxis.Label = "Clay";
        axes.ZAxis.Label = "Silt";

        Figure? read = null;
        var seen = DiagnosticsCapture.During(() => read = figure.RoundTripped());

        Assert.Empty(seen);
        Assert.NotNull(read);
        var back = read!.SubPlots[0];
        Assert.Equal(CoordinateSystem.Ternary, back.CoordinateSystem);
        Assert.Equal("Sand", back.XAxis.Label);
        Assert.Equal("Clay", back.YAxis.Label);
        Assert.Equal("Silt", back.ZAxis.Label);
    }
}
