// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Phase 10 of the v1.7.2 plan — the unused/duplicate <c>SvgPanZoomScript</c>
/// was retired in favour of the canonical <c>SvgInteractivityScript</c>. This test
/// guards against accidental re-introduction.</summary>
public class PanZoomRetirementTests
{
    [Fact]
    public void SvgPanZoomScript_TypeIsGone()
    {
        var t = typeof(Plt).Assembly
            .GetType("MatPlotLibNet.Rendering.Svg.SvgPanZoomScript");
        Assert.Null(t);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // Cursor UX (grab/grabbing) was the one feature SvgPanZoomScript had that SvgInteractivityScript lacked. Phase 10 ports it.
        new DrawnToken("ZoomPanScript_SetsGrabCursor", () => Plt.Create().WithZoomPan().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg(), "grab"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
