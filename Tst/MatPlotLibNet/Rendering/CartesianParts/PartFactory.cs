// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.CartesianParts;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.CartesianParts;

/// <summary>The parts of a Cartesian render as a test makes them, one at a time and without a renderer: each built from the render's
/// context (<see cref="CartesianPartContext"/>) over the axes, the plot, the surface, the theme and the transform the test hands it, the
/// colour slots worked out from the axes as the renderer works them out.</summary>
internal static class PartFactory
{
    /// <summary>The render's context over <paramref name="axes"/>.</summary>
    internal static CartesianPartContext Context(Axes axes, Rect plot, IRenderContext ctx, Theme theme, DataTransform transform) =>
        new(axes, new RenderPass(plot, ctx, theme), axes.ColorSlots(theme), transform);

    internal static CartesianSpansPart Spans(Axes axes, Rect plot, IRenderContext ctx, Theme theme, DataTransform transform, DataRange range) =>
        new(Context(axes, plot, ctx, theme, transform), range);

    internal static CartesianPatchesPart Patches(Axes axes, Rect plot, IRenderContext ctx, Theme theme, DataTransform transform, bool clipped) =>
        new(Context(axes, plot, ctx, theme, transform), clipped);

    internal static CartesianReferenceLinesPart ReferenceLines(Axes axes, Rect plot, IRenderContext ctx, Theme theme, DataTransform transform,
        DataRange range) => new(Context(axes, plot, ctx, theme, transform), range);

    internal static CartesianToolsPart Tools(Axes axes, Rect plot, IRenderContext ctx, Theme theme, DataTransform transform) =>
        new(Context(axes, plot, ctx, theme, transform));

    internal static CartesianBreakMarkersPart BreakMarkers(Axes axes, Rect plot, IRenderContext ctx, Theme theme, DataTransform transform) =>
        new(Context(axes, plot, ctx, theme, transform), theme.Background);

    internal static CartesianAnnotationsPart Annotations(Axes axes, Rect plot, IRenderContext ctx, Theme theme, DataTransform transform) =>
        new(Context(axes, plot, ctx, theme, transform));

    /// <summary>What a part draws: it is built over a fresh recording context and rendered once.</summary>
    /// <typeparam name="TPart">The part.</typeparam>
    /// <param name="build">Builds the part over the context it is given.</param>
    internal static RecordingRenderContext Drawn<TPart>(Func<RecordingRenderContext, TPart> build) where TPart : CartesianAxesPart
    {
        var ctx = new RecordingRenderContext();
        build(ctx).Render();
        return ctx;
    }

    /// <summary>The plot the part unit tests draw into: 400 by 300 pixels.</summary>
    internal static Rect Plot { get; } = new(0, 0, 400, 300);

    /// <summary>What the part <paramref name="make"/> builds draws for <paramref name="axes"/>, over <see cref="Plot"/> on linear axes under
    /// the default theme.</summary>
    /// <typeparam name="TPart">The part.</typeparam>
    /// <param name="axes">The axes the part draws.</param>
    /// <param name="make">One of the factories above.</param>
    internal static RecordingRenderContext Drawn<TPart>(Axes axes, Func<Axes, Rect, IRenderContext, Theme, DataTransform, TPart> make)
        where TPart : CartesianAxesPart =>
        Drawn(ctx => make(axes, Plot, ctx, Theme.Default, Plot.Linear()));

    /// <summary>Draws the axes the row arranges, which the test reads back as <paramref name="call"/> calls of one kind.</summary>
    /// <param name="arrange">Arranges a new axes.</param>
    /// <param name="drawn">What the part under test draws for the axes.</param>
    /// <param name="call">The kind of call counted.</param>
    /// <param name="expected">How many calls of that kind the part makes.</param>
    internal static void AssertCallsDrawn(Action<Axes> arrange, Func<Axes, RecordingRenderContext> drawn, Type call, int expected)
    {
        var ax = new Axes();
        arrange(ax);

        drawn(ax).AssertCalls(call, expected);
    }

    internal static CartesianSignalsPart Signals(Axes axes, Rect plot, IRenderContext ctx, Theme theme, DataTransform transform) =>
        new(Context(axes, plot, ctx, theme, transform));
}
