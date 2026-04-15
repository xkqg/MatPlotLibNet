// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.TextMeasurement;

/// <summary>
/// Pluggable text measurement abstraction shared by every <see cref="IRenderContext"/>
/// so layout decisions (tick label widths, axis margins, plot bounds, legend sizing)
/// are identical regardless of which backend draws the final pixels.
/// </summary>
/// <remarks>
/// The core assembly ships a pure-managed default <see cref="DefaultFontMetrics"/> that
/// uses a per-character width table. When <c>MatPlotLibNet.Skia</c> is loaded its module
/// initializer replaces <see cref="ChartServices.FontMetrics"/> with a Skia-backed
/// implementation that uses the bundled DejaVu Sans TTFs via <c>SKFont.MeasureText</c>.
/// That makes SVG and PNG output byte-identical in layout because both
/// <see cref="Svg.SvgRenderContext"/> and <c>SkiaRenderContext</c> delegate to the same
/// metrics source.
/// </remarks>
public interface IFontMetrics
{
    /// <summary>Measures the pixel size of <paramref name="text"/> rendered in <paramref name="font"/>.</summary>
    Size Measure(string text, Font font);
}
