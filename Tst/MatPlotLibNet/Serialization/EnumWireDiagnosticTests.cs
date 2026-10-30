// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Models;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>An enum travels in the JSON as a name, and a reader that does not know the name used to drop it without a word: a
/// figure written by a newer version with a coordinate system, a line style or a bar orientation this reader has never heard of
/// came back drawn as the default, and nothing said so. The reader stays lenient (the figure loads) but now announces the value
/// it could not place, on the channel every other silent degradation already uses.</summary>
[Collection("ChartDiagnosticsGlobalState")]
public class EnumWireDiagnosticTests
{
    private static List<ChartDiagnostic> Announced(Action act)
    {
        var seen = new List<ChartDiagnostic>();
        void Collect(ChartDiagnostic d) => seen.Add(d);
        ChartDiagnostics.Emitted += Collect;
        try
        {
            act();
        }
        finally
        {
            ChartDiagnostics.Emitted -= Collect;
        }

        return seen;
    }

    [Fact]
    public void AnUnknownName_IsAnnouncedWithTheValueAndTheEnum_AndTheSetterIsNotCalled()
    {
        LineStyle applied = LineStyle.Solid;
        bool called = false;

        var seen = Announced(() => "zigzag".ApplyEnum<LineStyle>(v => { applied = v; called = true; }));

        Assert.False(called);
        Assert.Equal(LineStyle.Solid, applied);
        var announcement = Assert.Single(seen);
        Assert.Contains("zigzag", announcement.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(LineStyle), announcement.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Dashed")]
    [InlineData("dashed")]
    [InlineData("DASHED")]
    public void AKnownName_IsAppliedInAnyCase_AndNothingIsAnnounced(string name)
    {
        var applied = LineStyle.Solid;

        var seen = Announced(() => name.ApplyEnum<LineStyle>(v => applied = v));

        Assert.Equal(LineStyle.Dashed, applied);
        Assert.Empty(seen);
    }

    [Fact]
    public void NoValue_IsNotAnUnknownValue()
    {
        string? absent = null;

        var seen = Announced(() => absent.ApplyEnum<LineStyle>(_ => throw new InvalidOperationException("nothing to apply")));

        Assert.Empty(seen);
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
        var seen = Announced(() => read = new ChartSerializer().FromJson(written));

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
        var seen = Announced(() => read = new ChartSerializer().FromJson(figure.ToJson()));

        Assert.Empty(seen);
        Assert.NotNull(read);
        var back = read!.SubPlots[0];
        Assert.Equal(CoordinateSystem.Ternary, back.CoordinateSystem);
        Assert.Equal("Sand", back.XAxis.Label);
        Assert.Equal("Clay", back.YAxis.Label);
        Assert.Equal("Silt", back.ZAxis.Label);
    }
}
