// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>The base of a renderer of a series drawn in a 3-D scene: the projection it is drawn through and the one way it hands its
/// marks to the scene's depth queue, so they are drawn back to front among the marks of every other series and patch of the scene.</summary>
/// <typeparam name="T">The kind of series rendered.</typeparam>
internal abstract class SceneSeriesRenderer<T> : SeriesRenderer<T> where T : ISeries
{
    /// <inheritdoc />
    protected SceneSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <summary>The projection a 3-D series is drawn through: the 3-D axes' own; for a 3-D series placed on 2-D axes, the resting
    /// camera's (<see cref="CameraView.Resting"/>) over the extent of its drawable data, which the caller ranges through the
    /// missing-value owner. An axis with nothing drawable spans 0..1; one of no width is centred by <see cref="Projection3D"/> itself.</summary>
    /// <param name="x">The drawable x extent, or <see langword="null"/>.</param>
    /// <param name="y">The drawable y extent, or <see langword="null"/>.</param>
    /// <param name="z">The drawable z extent, or <see langword="null"/>.</param>
    /// <returns>The projection to draw through.</returns>
    protected Projection3D ProjectionFor(Range1D? x, Range1D? y, Range1D? z) =>
        Context.Projection3D ?? CameraView.Resting.ProjectionOver(Area.PlotBounds, new Box3D(x ?? NothingDrawable, y ?? NothingDrawable,
            z ?? NothingDrawable));

    /// <summary>The span of an axis of a 3-D series with nothing drawable on it, drawn off a 3-D axes.</summary>
    private static readonly Range1D NothingDrawable = new(0, 1);

    /// <summary>Draws <paramref name="marks"/> back to front with the scene they stand in: hands them to the depth queue of the 3-D
    /// axes (<see cref="SeriesRenderContext.DepthQueue"/>), which draws them among the marks of every other series and patch of the
    /// scene, in the series' group (<see cref="SeriesRenderContext.SceneGroup"/>), when it flushes; off a 3-D axes the series is a
    /// scene of its own, and its marks are drawn back to front now, through a queue of its own, inside the opacity scope of the series
    /// (<see cref="RenderContextScopeExtensions.WithOpacity"/>) when it names an opacity. The one way a 3-D renderer orders what it
    /// draws: no renderer sorts its marks.</summary>
    /// <typeparam name="TMark">The shape of a mark: what <paramref name="draw"/> draws it from, with its depth.</typeparam>
    /// <param name="marks">The marks, in the order the series made them.</param>
    /// <param name="draw">The rule that draws one mark.</param>
    /// <param name="opacity">The opacity the series is drawn at, 0 to 1; <see langword="null"/> (the default) for a kind with no
    /// opacity of its own, drawn at the opacity of the surface.</param>
    protected void DrawBackToFront<TMark>(List<TMark> marks, Action<TMark> draw, double? opacity = null) where TMark : struct, IDepthMark =>
        DrawBackToFront(marks, new EachMark<TMark>(draw), opacity);

    /// <summary>Draws <paramref name="marks"/> back to front with the scene they stand in (<see cref="DrawBackToFront{TMark}"/>), each
    /// run of them by one call of <paramref name="drawer"/>: the form for a series whose rule resolves much for each mark, so it is
    /// resolved once a run and the queue calls no delegate per mark.</summary>
    /// <typeparam name="TMark">The shape of a mark.</typeparam>
    /// <typeparam name="TDrawer">The rule that draws a run of marks.</typeparam>
    /// <param name="marks">The marks, in the order the series made them.</param>
    /// <param name="drawer">The rule that draws a run of them.</param>
    /// <param name="opacity">The opacity the series is drawn at, 0 to 1; <see langword="null"/> (the default) for a kind with no
    /// opacity of its own.</param>
    protected void DrawBackToFront<TMark, TDrawer>(List<TMark> marks, TDrawer drawer, double? opacity = null)
        where TMark : struct, IDepthMark where TDrawer : struct, IMarkRunDrawer<TMark>
    {
        var source = new DepthMarks<TMark, TDrawer>(marks, drawer, opacity, Context.SceneGroup);
        if (Context.DepthQueue is { } scene)
        {
            source.HandTo(scene);
            return;
        }

        using var told = opacity is { } a ? Ctx.WithOpacity(a) : default;
        var own = new DepthQueue3D();
        source.HandTo(own);
        own.Flush(Ctx);
    }

    /// <summary>Draws the whole drawing of a series that orders its own marks (<see cref="WholeDrawing"/>) as one mark of its scene, at
    /// <paramref name="nearest"/>, through <see cref="DrawBackToFront{TMark}"/>. A drawing that placed no point (its nearest point
    /// stands at minus infinity) draws nothing and hands the scene no mark.</summary>
    /// <param name="nearest">The depth of the drawing's nearest point; <see cref="double.NegativeInfinity"/> when it placed none.</param>
    /// <param name="draw">What draws the whole drawing, in its own order.</param>
    protected void DrawAsOneMark(double nearest, Action draw)
    {
        if (double.IsNegativeInfinity(nearest)) return;
        DrawBackToFront([new WholeDrawing(nearest)], _ => draw());
    }
}