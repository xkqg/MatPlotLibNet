// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>The opacity a group of marks is drawn at, set by <see cref="RenderContextScopeExtensions.WithOpacity"/> and put back to the
/// one it replaced when the scope ends: a group inside a group drawn at an opacity leaves the outer one at its own. A scope that was
/// never opened (<see langword="default"/>) puts nothing back, so a site that draws at an opacity only under a condition writes
/// <c>using var opacity = condition ? ctx.WithOpacity(a) : default;</c>.</summary>
/// <param name="Context">The surface the opacity was set on; <see langword="null"/> for a scope that was never opened.</param>
/// <param name="Replaced">The opacity the surface was at before the scope set its own (<c>OpacityTold</c>: full on a surface that tells
/// none).</param>
internal readonly record struct OpacityScope(IRenderContext? Context, double Replaced = 1.0) : IDisposable
{
    /// <summary>Puts the surface back to the opacity the scope replaced.</summary>
    public void Dispose() => Context?.SetOpacity(Replaced);
}

/// <summary>The clip a group of marks is drawn inside, pushed by <see cref="RenderContextScopeExtensions.ClippedTo(IRenderContext, in ClipRegion)"/>
/// and popped when the scope ends. A scope that was never opened (<see langword="default"/>) pops nothing, so a site that clips only under
/// a condition writes <c>using var clip = condition ? ctx.ClippedTo(region) : default;</c>.</summary>
/// <param name="Context">The surface the clip was pushed on; <see langword="null"/> for a scope that was never opened.</param>
internal readonly record struct ClipScope(IRenderContext? Context) : IDisposable
{
    /// <summary>Takes the clip off.</summary>
    public void Dispose() => Context?.PopClip();
}

/// <summary>The texts a series draws while the surface cuts them by its clips (<see cref="RenderContextScopeExtensions.CuttingTexts"/>), as
/// it cuts a mark; when the scope ends the surface cuts its texts as it did before (a text stands whole past a clip). A scope on a surface
/// that does not cut texts (<see langword="default"/>) puts nothing back.</summary>
/// <param name="Context">The surface told to cut its texts; <see langword="null"/> for a surface of your own, which draws a text as it draws
/// it.</param>
/// <param name="Replaced">Whether the surface cut its texts before the scope.</param>
internal readonly record struct TextClipScope(ITextClipRenderContext? Context, bool Replaced) : IDisposable
{
    /// <summary>Puts the surface back to cutting its texts or not, as before the scope.</summary>
    public void Dispose()
    {
        if (Context is { } texts) texts.CutsTexts = Replaced;
    }
}

/// <summary>What a drawing surface is told for a group of marks, and takes back: an opacity, a clip and whether the clip cuts its texts. A renderer asks a scope, never the
/// pair of calls, so the reset is not typed beside every set and cannot be left out by a path that returns early or a mark that throws.</summary>
internal static class RenderContextScopeExtensions
{
    extension(IRenderContext ctx)
    {
        /// <summary>Draws what follows at <paramref name="opacity"/> until the scope ends, which puts the surface back to the opacity it
        /// was at (<c>OpacityTold</c>). The surface is told both, whatever the number: an SVG surface lifts the clips it has
        /// applied at each, so a call left out for a number of 1 would move bytes.</summary>
        /// <param name="opacity">The opacity of the marks drawn inside the scope, 0 to 1.</param>
        /// <returns>The scope; dispose it, or <see langword="using"/> it.</returns>
        internal OpacityScope WithOpacity(double opacity)
        {
            double replaced = ctx.OpacityTold;
            ctx.SetOpacity(opacity);
            return new OpacityScope(ctx, replaced);
        }

        /// <summary>Draws what follows at <paramref name="opacity"/> until the scope ends when it is below full (<see cref="WithOpacity"/>),
        /// and tells the surface nothing at full opacity: the scope of a group (an image's cells, a filled contour's bands) that is drawn at
        /// an opacity only when it has one.</summary>
        /// <param name="opacity">The opacity of the marks drawn inside the scope, 0 to 1.</param>
        /// <returns>The scope, or one that was never opened at full opacity; dispose it, or <see langword="using"/> it.</returns>
        internal OpacityScope WithOpacityBelowFull(double opacity) => opacity < 1.0 ? ctx.WithOpacity(opacity) : default;

        /// <summary>Puts the surface at full opacity, whatever the stage before left on it: where each stage of a frame (the figure, an
        /// axes, what lies above the subplots) starts, held above the contexts for drawing the library does not own.</summary>
        internal void AtFullOpacity() => ctx.SetOpacity(1.0);

        /// <summary>The opacity the surface was last set to (<see cref="IOpacityRenderContext"/>); full on a surface of your own, which
        /// tells none.</summary>
        internal double OpacityTold => ctx is IOpacityRenderContext told ? told.Opacity : 1.0;

        /// <summary>Cuts what follows to <paramref name="region"/> until the scope ends: a rectangle through
        /// <see cref="IRenderContext.PushClip"/>, an outline through a surface that clips to one (<see cref="IOutlineClipRenderContext"/>), and
        /// on a surface of your own that does not, the box around the outline. A renderer asks this member, never the type of its surface.</summary>
        /// <param name="region">The region the marks drawn inside the scope are cut off at.</param>
        /// <returns>The scope; dispose it, or <see langword="using"/> it.</returns>
        internal ClipScope ClippedTo(in ClipRegion region)
        {
            if (region.Outline is { } outline && ctx is IOutlineClipRenderContext clips) clips.PushClip(outline);
            else ctx.PushClip(region.Bounds);
            return new ClipScope(ctx);
        }

        /// <summary>Cuts the texts drawn until the scope ends by the clips pushed, as a mark is cut, where matplotlib gives a text a clip
        /// box (a contour's labels: <c>clabel</c> gives each the axes' box). On a surface of your own, which does not cut texts
        /// (<see cref="ITextClipRenderContext"/>), a text is drawn as it draws it.</summary>
        /// <returns>The scope; dispose it, or <see langword="using"/> it.</returns>
        internal TextClipScope CuttingTexts()
        {
            if (ctx is not ITextClipRenderContext texts) return default;
            bool replaced = texts.CutsTexts;
            texts.CutsTexts = true;
            return new TextClipScope(texts, replaced);
        }

        /// <summary>Cuts what follows to the rectangle <paramref name="bounds"/> until the scope ends.</summary>
        /// <param name="bounds">The rectangle the marks drawn inside the scope are cut off at.</param>
        /// <returns>The scope; dispose it, or <see langword="using"/> it.</returns>
        internal ClipScope ClippedTo(Rect bounds)
        {
            ctx.PushClip(bounds);
            return new ClipScope(ctx);
        }
    }
}
