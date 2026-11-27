// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>A drawing surface that tells the opacity it was last set to (<see cref="IRenderContext.SetOpacity"/>), so a scope that
/// sets one for a group of marks puts back the one it replaced (<see cref="RenderContextScopeExtensions.WithOpacity"/>). The library's
/// own surfaces tell it; a surface of your own, which tells none, is put back to full opacity, asked through
/// <see cref="RenderContextScopeExtensions"/>' <c>OpacityTold</c>.</summary>
/// <remarks>Internal, beside the public <see cref="IRenderContext"/>: no member is added to the public interface.</remarks>
internal interface IOpacityRenderContext : IRenderContext
{
    /// <summary>The opacity the surface was last set to, 0 to 1; 1 before it was set.</summary>
    double Opacity { get; }
}
