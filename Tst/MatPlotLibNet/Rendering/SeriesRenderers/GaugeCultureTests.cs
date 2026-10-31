// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>The value a gauge writes under its needle reads the same on every machine: before, a German machine wrote
/// <c>12,5</c> where the runner wrote <c>12.5</c>.</summary>
public class GaugeCultureTests
{
    [Theory]
    [InlineData("de-DE")]
    [InlineData("nl-NL")]
    [InlineData("en-US")]
    public void TheValue_IsWrittenTheSameUnderEveryCulture(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);
            var bounds = new Rect(0, 0, 400, 300);
            var ctx = new RecordingRenderContext();
            new GaugeSeriesRenderer(new SeriesRenderContext(new DataTransform(0, 1, 0, 1, bounds), ctx, Colors.Tab10Blue,
                new RenderArea(bounds, ctx))).Render(new GaugeSeries(12.5));

            var text = Assert.Single(ctx.OfKind("DrawText")).Args!;
            Assert.Equal("12.5", text.GetType().GetProperty("text")!.GetValue(text));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
