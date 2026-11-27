// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>A surface that can cut a text by its clips as it cuts a mark: the SVG writer, the Skia canvas and the MAUI canvas. A clip
/// cuts the marks drawn under it, never a text (<see cref="MarkClips{TClip}"/>), but matplotlib cuts the texts it gives a clip box, a
/// contour's labels among them. A renderer reaches it through <see cref="RenderContextScopeExtensions.CuttingTexts"/> on any
/// <see cref="IRenderContext"/>, where a surface of your own draws a text as it draws it. It lives here and not on
/// <see cref="IRenderContext"/> so that an application can still implement, proxy or mock the public interface.</summary>
internal interface ITextClipRenderContext : IRenderContext
{
    /// <summary>Whether a text drawn now is cut by the clips pushed, as a mark is.</summary>
    bool CutsTexts { get; set; }
}
