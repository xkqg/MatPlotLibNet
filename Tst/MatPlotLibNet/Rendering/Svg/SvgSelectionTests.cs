// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>Verifies SVG Shift+drag rectangular selection script injection and behavior.</summary>
public class SvgSelectionTests
{
    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // Verifies that enabling selection injects a script element.
        new DrawnToken("WithSelection_InjectsScriptElement", () => Plt.Create()
            .WithSelection()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg(), "<script"),
        // Verifies that the selection script requires the Shift key to start a selection.
        new DrawnToken("SelectionScript_RequiresShiftKey", () => Plt.Create()
            .WithSelection()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg(), "shiftKey"),
        // Verifies that the selection script dispatches an mpl:selection custom event.
        new DrawnToken("SelectionScript_DispatchesMplSelectionEvent", () => Plt.Create()
            .WithSelection()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg(), "mpl:selection"),
        // Verifies that the selection script includes x1, y1, x2, y2 in the event detail.
        new DrawnToken("SelectionScript_IncludesCoordinatesInEventDetail", () => Plt.Create()
            .WithSelection()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg(), "x1", "y1", "x2", "y2"),
        // Verifies that the selection script draws a visible selection rectangle.
        new DrawnToken("SelectionScript_DrawsSelectionRectangle", () => Plt.Create()
            .WithSelection()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg(), "createElementNS", "'rect'"),
        // Verifies that WithSelection(false) disables the feature.
        new DrawnToken("WithSelectionFalse_NoScript", () => Plt.Create()
            .WithSelection(false)
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg()) { Absent = ["<script"] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
