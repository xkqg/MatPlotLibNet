// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A transform draws with the <see cref="IChartRenderer"/> it was made with, whichever one that is: the library's own
/// renderer, or one of the caller's that knows nothing of how the library draws. The protected <c>Renderer</c> of the old shape
/// still hands the library's renderer to a transform that reads it.</summary>
public class FigureTransformRendererTests
{
    private static Figure TwoPanels() => Plt.Create().WithSize(300, 200)
        .AddSubPlot(1, 2, 1, ax => ax.Plot([0.0, 1, 2], [1.0, 2, 1]))
        .AddSubPlot(1, 2, 2, ax => ax.Plot([0.0, 1, 2], [2.0, 1, 2]))
        .Build();

    /// <summary>A renderer of the caller's: draws one rectangle in a colour nothing else in a figure has.</summary>
    private static RecordingChartRenderer CallersRenderer() =>
        new((_, ctx) => ctx.DrawRectangle(new Rect(1, 2, 3, 4), new ShapeStyle(Color.FromHex("#123456"), null, 0)));

    /// <summary>A transform outside the library that reads what its base holds.</summary>
    private sealed class Reading : FigureTransform
    {
        public Reading() { }
        public Reading(IChartRenderer renderer) : base(renderer) { }

        public IChartRenderer Held => FigureRenderer;

        public override void Transform(Figure figure, Stream output) { }
    }

    [Fact]
    public void AnSvgTransform_DrawsWithARendererOfTheCallers()
    {
        var callers = CallersRenderer();

        string svg = new SvgTransform(callers).Render(TwoPanels());

        Assert.Equal(1, callers.Renders);
        Assert.Contains("#123456", svg, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("</svg>", svg.TrimEnd());
    }

    [Fact]
    public void AnSvgTransform_DrawingWithARendererOfTheCallers_DoesNotAskItForALayout()
    {
        var callers = CallersRenderer();

        _ = new SvgTransform(callers).Render(TwoPanels());

        Assert.Equal(0, callers.Layouts);
    }

    [Fact]
    public void AnSvgTransform_WithARendererOfTheCallers_DrawsAgainOnTheNextRender()
    {
        var callers = CallersRenderer();
        var transform = new SvgTransform(callers);
        var figure = TwoPanels();

        string first = transform.Render(figure);
        string second = transform.Render(figure);

        Assert.Equal(2, callers.Renders);
        Assert.Equal(first, second);
    }

    [Fact]
    public void ATransform_HoldsTheRendererItWasMadeWith()
    {
        var callers = CallersRenderer();

        Assert.Same(callers, new Reading(callers).Held);
    }

    [Fact]
    public void ATransformMadeWithoutARenderer_HoldsTheLibrarysRenderer()
    {
        Assert.IsType<ChartRenderer>(new Reading().Held);
    }

    [Fact]
    public void TheLibrarysRenderer_DrawsTheSameSvgThroughATransformMadeWithIt()
    {
        var figure = TwoPanels();

        Assert.Equal(new SvgTransform().Render(TwoPanels()), new SvgTransform(new ChartRenderer()).Render(figure));
    }

    [Fact]
    public void ATransform_RefusesNoRenderer()
    {
        Assert.Throws<ArgumentNullException>(() => new Reading(null!));
    }
}
