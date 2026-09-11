// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Mcp;

/// <summary>The bounds a model-authored spec must stay inside. Nothing in the library clamps a canvas — a
/// 16000×16000 request allocates a bitmap of a gigabyte and a larger one fails on allocation — and the PNG goes
/// into a context window, so the canvas ceiling is the token ceiling too. The text limit keeps a 20,000-character
/// title from becoming a multi-megabyte document.</summary>
/// <param name="MaxWidth">Largest accepted figure width, in points.</param>
/// <param name="MaxHeight">Largest accepted figure height, in points.</param>
/// <param name="MaxTextLength">Longest accepted title, label or description.</param>
internal readonly record struct RenderLimits(int MaxWidth, int MaxHeight, int MaxTextLength)
{
    /// <summary>The limits the server ships with.</summary>
    public static RenderLimits Default => new(4000, 4000, 512);
}
