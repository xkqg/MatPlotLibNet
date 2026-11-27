// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace MatPlotLibNet.Rendering;

/// <summary>How a surface cuts what it draws to one clip, and takes the innermost cut off again: an SVG writer opens and closes a
/// group drawn in a <c>clipPath</c>, a canvas saves its state with the clip and restores it. What <see cref="MarkClips{TClip}"/> drives.</summary>
/// <typeparam name="TClip">What the surface keeps of a clip to cut with it again: the id of its <c>clipPath</c>, its rectangle or outline.</typeparam>
internal interface IClipSurface<TClip>
{
    /// <summary>Cuts what is drawn next to <paramref name="clip"/>, inside every clip applied before it.</summary>
    /// <param name="clip">The clip.</param>
    void Apply(TClip clip);

    /// <summary>Takes the innermost applied clip off.</summary>
    void Lift();
}

/// <summary>The one rule of a clip on every surface: a clip cuts the marks drawn under it, never a text. matplotlib draws a text
/// unclipped (<c>ax.text</c> is <c>clip_on False</c>; a bar label or an annotation has no clip box), while every artist an axes adds is
/// cut off at it; so the label, the value or the name a series writes beside its marks stands whole past the plot, while the marks before
/// and after it are cut. The one text matplotlib cuts is a text it gives a clip box: a contour's label (<c>clabel</c> gives each the axes'
/// box), which the series draws while the surface cuts its texts (<see cref="CutsTexts"/>, through
/// <see cref="RenderContextScopeExtensions.CuttingTexts"/>): such a text is cut as a mark is. The surface keeps the clips pushed on it here, in order, and how many of them are applied: a mark applies the
/// ones missing (<see cref="Mark"/>), a text lifts the applied ones (<see cref="Lift"/>), and a clip pushed while all before it are
/// applied is applied at once, so a clip that holds marks alone is applied once, as it always was.</summary>
/// <typeparam name="TClip">What the surface keeps of a clip to cut with it again.</typeparam>
/// <remarks>A value the surface holds in a field of its own (not read-only), so a context allocates nothing for it until its first clip.</remarks>
internal struct MarkClips<TClip>
{
    /// <summary>The clips pushed and not yet popped, outermost first; made by the first push.</summary>
    private List<TClip>? _clips;

    /// <summary>How many of <see cref="_clips"/>, from the outermost, are applied on the surface.</summary>
    private int _applied;

    /// <summary>Whether a text drawn now is cut by the clips as a mark is: set while a series draws the texts matplotlib clips (a
    /// contour's labels), through <see cref="ITextClipRenderContext"/>.</summary>
    internal bool CutsTexts { readonly get; set; }

    /// <summary>Pushes <paramref name="clip"/>, applied at once when every clip before it is.</summary>
    /// <param name="surface">The surface that cuts.</param>
    /// <param name="clip">The clip.</param>
    internal void Push(IClipSurface<TClip> surface, TClip clip)
    {
        var clips = _clips ??= [];
        clips.Add(clip);
        if (_applied < clips.Count - 1) return;
        surface.Apply(clip);
        _applied++;
    }

    /// <summary>Pops the innermost clip, taking it off the surface when it is applied. A pop with no clip pushed does nothing.</summary>
    /// <param name="surface">The surface that cuts.</param>
    /// <returns>The clip popped, or the default for none, for the surface to release what it holds.</returns>
    internal TClip? Pop(IClipSurface<TClip> surface)
    {
        if (_clips is not { Count: > 0 } clips) return default;
        if (_applied == clips.Count)
        {
            surface.Lift();
            _applied--;
        }

        var clip = clips[^1];
        clips.RemoveAt(clips.Count - 1);
        return clip;
    }

    /// <summary>Before a mark: applies every pushed clip a text lifted, outermost first. Asked before every mark a surface draws, so
    /// the case of nothing to do (every clip applied, or none pushed) is one comparison, inlined at the mark.</summary>
    /// <param name="surface">The surface that cuts.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Mark(IClipSurface<TClip> surface)
    {
        if (_clips is { } clips && _applied < clips.Count) ApplyLifted(surface, clips);
    }

    /// <summary>Applies the clips from the first one lifted on.</summary>
    private void ApplyLifted(IClipSurface<TClip> surface, List<TClip> clips)
    {
        while (_applied < clips.Count) surface.Apply(clips[_applied++]);
    }

    /// <summary>Before a text: takes every applied clip off (<see cref="Lift"/>), so the text stands whole; while the surface cuts its texts
    /// (<see cref="CutsTexts"/>) applies the ones a text lifted instead (<see cref="Mark"/>), so the text is cut as a mark is.</summary>
    /// <param name="surface">The surface that cuts.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Text(IClipSurface<TClip> surface)
    {
        if (CutsTexts) Mark(surface);
        else Lift(surface);
    }

    /// <summary>Before a text that stands whole (and, on a surface that nests groups, before a group opens or closes, so an applied clip
    /// only ever holds marks and cut texts): takes every applied clip off, innermost first. The clips stay pushed for the next mark.</summary>
    /// <param name="surface">The surface that cuts.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Lift(IClipSurface<TClip> surface)
    {
        if (_applied > 0) LiftApplied(surface);
    }

    /// <summary>Takes every applied clip off, innermost first.</summary>
    private void LiftApplied(IClipSurface<TClip> surface)
    {
        while (_applied > 0)
        {
            surface.Lift();
            _applied--;
        }
    }
}
