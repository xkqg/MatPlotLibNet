// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>Verifies JSON serialization round-trip for <see cref="AxisBreak"/>.</summary>
public class BrokenAxisSerializationTests
{
    private static double[] X => [0, 50, 100, 150, 200];
    private static double[] Y => [0, 1, 2, 3, 4];

    /// <summary>A break on either axis comes back from JSON with its ends.</summary>
    [Theory]
    [InlineData("X", 60.0, 90.0)]
    [InlineData("Y", 1.5, 2.5)]
    public void RoundTrip_ABreakOnEitherAxis_PreservesFromTo(string axis, double from, double to)
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot(X, Y);
                _ = axis == "X" ? ax.WithXBreak(from, to) : ax.WithYBreak(from, to);
            })
            .Build();
        var restored = figure.RoundTripped().SubPlots[0];

        var only = Assert.Single(axis == "X" ? restored.XBreaks : restored.YBreaks);
        Assert.Equal(from, only.From, 1e-9);
        Assert.Equal(to, only.To, 1e-9);
    }

    [Fact]
    public void RoundTrip_BreakStyle_Straight()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot(X, Y);
                ax.WithXBreak(60, 90, BreakStyle.Straight);
            })
            .Build();
        var restored = figure.RoundTripped();
        Assert.Equal(BreakStyle.Straight, restored.SubPlots[0].XBreaks[0].Style);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("NoBreaks_XBreaksProperty_AbsentFromJson", () =>
        {
            var figure = Plt.Create()
                .AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y))
                .Build();
            return new ChartSerializer().ToJson(figure);
        }) { Absent = ["\"xBreaks\"", "\"yBreaks\""] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Fact]
    public void MultipleXBreaks_AllRoundTrip()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot(X, Y);
                ax.WithXBreak(20, 40).WithXBreak(80, 100);
            })
            .Build();
        var restored = figure.RoundTripped();
        Assert.Equal(2, restored.SubPlots[0].XBreaks.Count);
    }
}
