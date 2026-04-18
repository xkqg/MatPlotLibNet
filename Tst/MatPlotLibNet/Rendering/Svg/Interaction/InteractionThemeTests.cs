// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Phase 7 of the v1.7.2 plan — opacity / transition tokens are configurable
/// via <c>FigureBuilder.WithInteractionTheme</c>. Defaults match v1.7.1 hard-coded
/// values (zero-config callers see no behaviour change); custom values are emitted as
/// <c>data-mpl-*</c> attributes on the SVG so embedded scripts can read them.</summary>
public class InteractionThemeTests
{
    [Fact]
    public void DefaultTheme_DoesNotEmitDataMplAttributesAsSvgAttribute()
    {
        var svg = Plt.Create()
            .WithHighlight()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();
        // The script body MENTIONS data-mpl-highlight-opacity (in a comment + getAttribute call),
        // but the SVG opening tag must NOT carry it as an attribute when default theme is used.
        Assert.DoesNotMatch(@"<svg[^>]*\bdata-mpl-highlight-opacity=", svg);
    }

    [Fact]
    public void CustomTheme_EmitsHighlightOpacityAttribute()
    {
        var svg = Plt.Create()
            .WithHighlight()
            .WithInteractionTheme(new InteractionTheme(HighlightOpacity: 0.15))
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();
        Assert.Contains("data-mpl-highlight-opacity=\"0.15\"", svg);
    }

    [Fact]
    public void HighlightScript_ReadsThemableOpacity()
    {
        var svg = Plt.Create().WithHighlight().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg();
        Assert.Contains("data-mpl-highlight-opacity", svg);
    }
}
