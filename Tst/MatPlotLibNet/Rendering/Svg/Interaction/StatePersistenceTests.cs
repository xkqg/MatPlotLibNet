// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Phase 9 of the v1.7.2 plan — opt-in URL-hash state persistence.
/// Scripts read/write a #mpl-{id}=... segment so refresh restores zoom/pan state.</summary>
public class StatePersistenceTests
{
    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("ZoomPanScript_ContainsPersistOptInBranch", () => Plt.Create().WithZoomPan().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg(), "data-mpl-persist", "persistVB", "restoreVB"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
