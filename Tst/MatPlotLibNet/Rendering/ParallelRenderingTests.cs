// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies multi-subplot rendering behavior.</summary>
public class ParallelRenderingTests
{
    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // Verifies that rendering a 2x2 grid with mixed series types includes all subplot titles.
        new DrawnToken("ParallelRender_MultipleSubplots_AllTitlesPresent", () => Plt.Create()
            .AddSubPlot(2, 2, 1, ax => ax.WithTitle("TopLeft").Plot([1.0, 2.0], [3.0, 4.0]))
            .AddSubPlot(2, 2, 2, ax => ax.WithTitle("TopRight").Scatter([1.0, 2.0], [5.0, 6.0]))
            .AddSubPlot(2, 2, 3, ax => ax.WithTitle("BottomLeft").Bar(["A", "B"], [10.0, 20.0]))
            .AddSubPlot(2, 2, 4, ax => ax.WithTitle("BottomRight").Hist([1.0, 2.0, 3.0, 4.0]))
            .ToSvg(), "TopLeft", "TopRight", "BottomLeft", "BottomRight"),
        // Verifies that rendering works correctly with a single subplot.
        new DrawnToken("ParallelRender_SingleSubplot_Works", () => Plt.Create()
            .WithTitle("Single")
            .Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0])
            .ToSvg(), "<svg", "Single", "<polyline"),
        // Verifies that rendering a 3x3 grid includes all nine subplot titles.
        new DrawnToken("ParallelRender_ManySubplots_AllPresent", () =>
        {
            var builder = Plt.Create();
            for (int i = 1; i <= 9; i++)
            {
                int idx = i; // capture by value
                builder = builder.AddSubPlot(3, 3, idx, ax => ax.WithTitle($"Sub{idx}").Plot([1.0, 2.0], [3.0, 4.0]));
            }
            return builder.ToSvg();
        }, [.. Enumerable.Range(1, 9).Select(i => $"Sub{i}")]),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
