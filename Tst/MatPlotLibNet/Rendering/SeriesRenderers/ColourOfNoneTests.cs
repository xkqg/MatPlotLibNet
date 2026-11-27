// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>A colour of none stays none under the opacity of its series, as matplotlib's <c>to_rgba('none', alpha)</c> is
/// <c>(0, 0, 0, 0)</c> (3.11.2: <c>ax.bar([0], [1], color='none', hatch='//')</c> has the face <c>(0, 0, 0, 0)</c>): a series given
/// <see cref="Colors.Transparent"/> is not filled in black because its opacity was applied over the colour's.</summary>
public class ColourOfNoneTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0, 3.0];
    private static readonly double[] Y = [1.0, 3.0, 2.0, 4.0];

    /// <summary>The fills of the shapes drawn on the plot that are an opaque or translucent black: a colour of none turned black.</summary>
    private static IReadOnlyList<Color> BlackFills(Action<AxesBuilder> build)
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, build).Build();
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);
        return [.. ctx.Of<ShapeCall>().Where(c => c is DrawRectangleCall or DrawPolygonCall or DrawPathCall)
            .Select(c => c.Fill)
            .OfType<Color>()
            .Where(f => f is { R: 0, G: 0, B: 0, A: > 0 })];
    }

    public static TheoryData<string> Kinds { get; } = new() { "bar", "hist", "area", "kde", "violin" };

    [Theory]
    [MemberData(nameof(Kinds))]
    public void ASeriesColouredNone_IsFilledInNone_UnderItsOpacity(string kind)
    {
        Action<AxesBuilder> build = kind switch
        {
            "bar" => ax => ax.Bar(["a", "b"], [3.0, 5.0], s => { s.Color = Colors.Transparent; s.Alpha = 0.5; }),
            "hist" => ax => ax.Hist([1.0, 2, 2, 3, 3, 3], 3, s => { s.Color = Colors.Transparent; s.Alpha = 0.5; }),
            "area" => ax => ax.FillBetween(X, Y, null, s => { s.Color = Colors.Transparent; s.Alpha = 0.5; }),
            "kde" => ax => ax.Kde([1.0, 2, 2, 3, 3, 3, 4], s => { s.Color = Colors.Transparent; s.Fill = true; s.Alpha = 0.5; }),
            _ => ax => ax.Violin([[1.0, 2, 2, 3, 3, 3, 4]], s => { s.Color = Colors.Transparent; s.Alpha = 0.5; }),
        };

        Assert.Empty(BlackFills(build));
    }
}
