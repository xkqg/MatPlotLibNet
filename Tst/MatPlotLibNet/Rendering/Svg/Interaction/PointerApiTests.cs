// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Phase 4 of the v1.7.2 plan — every mouse-listening interaction script must
/// also wire pointer-events listeners (pointerdown/pointermove/pointerup) so touch +
/// pen + mouse all dispatch through a unified path. Eight of the nine scripts were
/// mouse-only at v1.7.1 (only the SignalR variant used the pointer API).
///
/// <para>The tests below assert the SCRIPT SOURCE includes the pointer event names —
/// behavioural simulation of touch events through Jint stub-DOM is tracked separately.</para></summary>
public class PointerApiTests
{
    /// <summary>The interaction each row switches on, by the name of its verb.</summary>
    private static readonly Dictionary<string, Func<FigureBuilder, FigureBuilder>> Interactions = new()
    {
        ["WithZoomPan"] = fb => fb.WithZoomPan(),
        ["WithLegendToggle"] = fb => fb.WithLegendToggle(),
        ["WithSelection"] = fb => fb.WithSelection(),
        ["With3DRotation"] = fb => fb.With3DRotation(),
    };

    [Theory]
    [InlineData("WithZoomPan",       "pointerdown", "SvgInteractivityScript")]
    [InlineData("WithZoomPan",       "pointermove", "SvgInteractivityScript")]
    [InlineData("WithZoomPan",       "pointerup",   "SvgInteractivityScript")]
    [InlineData("WithLegendToggle",  "pointerdown", "LegendToggleScript")]
    [InlineData("WithSelection",     "pointerdown", "SelectionScript")]
    [InlineData("WithSelection",     "pointermove", "SelectionScript")]
    [InlineData("WithSelection",     "pointerup",   "SelectionScript")]
    [InlineData("With3DRotation",    "pointerdown", "Svg3DRotationScript")]
    [InlineData("With3DRotation",    "pointermove", "Svg3DRotationScript")]
    [InlineData("With3DRotation",    "pointerup",   "Svg3DRotationScript")]
    public void Script_RegistersPointerEventListener(string method, string eventName, string scriptName)
    {
        _ = scriptName; // for theory display
        var svg = Interactions[method](Plt.Create().Plot([1.0, 2.0], [3.0, 4.0])).ToSvg();
        Assert.Contains(eventName, svg);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // Pinch-to-zoom in SvgInteractivityScript: requires tracking 2 active pointers. Asserts the script captures pointerId (the only
        // practical way to track multiple concurrent pointers).
        new DrawnToken("ZoomPan_TracksPointerIdForPinch", () => Plt.Create().WithZoomPan().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg(), "pointerId"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
