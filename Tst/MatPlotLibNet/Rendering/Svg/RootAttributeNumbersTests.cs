// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>The root of a chart carries numbers its scripts read back: the view of the first axes and the range Home returns to,
/// and every token of the interaction theme that differs from the default. They are written as the view of every axes is, with
/// every digit, so the root and the axes' own view never name one range two ways.</summary>
public class RootAttributeNumbersTests
{
    private static string ServerChart(double xMax) => Plt.Create()
        .WithServerInteraction("c", i => i.EnableZoom())
        .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1.0], [0.0, 1.0]).SetXLim(0, xMax))
        .Build().ToSvg();

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("ATokenOfTheInteractionTheme_KeepsEveryDigit", () => Plt.Create().WithInteractionTheme(new InteractionTheme(HighlightOpacity: 0.1234567))
            .WithHighlight().Plot([0.0, 1.0], [0.0, 1.0]).ToSvg(), " data-mpl-highlight-opacity=\"0.1234567\""),
        new DrawnToken("TheRootView_KeepsEveryDigitOfTheRange_AsTheAxesViewDoes", () => ServerChart(1234567.25), " data-xmax=\"1234567.25\" data-ymin=", " data-reset-xmax=\"1234567.25\"") { Absent = ["E+06"] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Fact]
    public void OnlyTheTokensThatDiffer_AreWritten_InTheirOrder()
    {
        var svg = Plt.Create().WithInteractionTheme(new InteractionTheme(TooltipOffsetY: 6, SankeyDimLinkOpacity: 0.5))
            .Plot([0.0, 1.0], [0.0, 1.0]).ToSvg();
        string root = svg[..svg.IndexOf('>', StringComparison.Ordinal)];

        Assert.EndsWith(" data-mpl-sankey-link-opacity=\"0.5\" data-mpl-tooltip-offset-y=\"6\"", root);
    }
}
