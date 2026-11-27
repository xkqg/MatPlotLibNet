// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="StatTileSeries"/> — a single-value headline "stat tile" (big number + label),
/// built for compact dashboard KPIs. No axes/data; the value and label render as centred text.</summary>
public class StatTileSeriesTests
{
    [Fact]
    public void TheValue_IsFormattedToTwoDecimals_UntilAFormatIsGiven()
    {
        Assert.Equal("1.23", new StatTileSeries(1.234).FormattedValue);
        Assert.Equal("42", new StatTileSeries(42) { Format = "0" }.FormattedValue);
    }

    [Fact]
    public void ComputeDataRange_ContributesNothing()
    {
        var r = new StatTileSeries(1).ComputeDataRange(null!);
        Assert.False(r.XMin.HasValue);
        Assert.False(r.YMax.HasValue);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("Render_DrawsTheValueAndLabel_AsText", () => Plt.Create().StatTile(42, t => t.Label = "Participants").ToSvg(), "42", "Participants"),
        //  no Label set — the label branch is skipped, no crash
        new DrawnToken("Render_WithoutALabel_DrawsOnlyTheValue", () => Plt.Create().StatTile(7).ToSvg(), "7"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
