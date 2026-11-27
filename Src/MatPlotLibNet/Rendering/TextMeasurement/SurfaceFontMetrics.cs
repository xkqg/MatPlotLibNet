// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.TextMeasurement;

/// <summary>The font metrics a layout is handed when it has to measure texts as a surface draws them: a text that holds math as the rich
/// text it parses to (<see cref="TextExtentExtensions"/>, the one measure of a text the renderers ask), any other text as the box of
/// the surface's own measure (<see cref="IRenderContext.MeasureText"/>). The label layout and the word cloud take an
/// <see cref="IFontMetrics"/>; with this they size a label with math by what is drawn, where the installed metrics know no math, and
/// every other label by the box they always measured it by, under the matplotlib looks too (whose ink measure is a tick label's, not a
/// placed label's), so no picture moves.</summary>
/// <param name="surface">The surface the texts are drawn on, and so measured by.</param>
internal sealed class SurfaceFontMetrics(IRenderContext surface) : IFontMetrics
{
    /// <inheritdoc />
    public Size Measure(string text, Font font) =>
        TextLabel.Of(text) is { IsMath: true } math ? surface.ExtentOf(math, font).Box : surface.MeasureText(text, font);
}
