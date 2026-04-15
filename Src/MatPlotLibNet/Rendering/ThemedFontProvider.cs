// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>
/// Single source of truth for every themed font the render pipeline uses. Before v1.2.1
/// the library carried duplicate font factories in <see cref="AxesRenderer"/>
/// (<c>TickFont</c>, <c>LabelFont</c>, <c>TitleFont</c>), in <c>ConstrainedLayoutEngine</c>
/// (its own <c>TickFont</c> / <c>LabelFont</c> / <c>TitleFont</c> / <c>SupTitleFont</c>),
/// and in <see cref="ChartRenderer"/> (figure <c>TitleFont</c>). These drifted — the
/// legend-clipping bug in v1.1.4 was caused by the engine's <c>TickFont</c> formula using
/// <c>theme.DefaultFont.Size - 2</c> while the renderer's <c>TickFont</c> used
/// <c>Theme.DefaultFont.Size</c>. Two parallel "formulas for the same role" are exactly
/// the kind of bug this class exists to prevent.
/// </summary>
/// <remarks>
/// <para>Every consumer (renderers, measurers, layout engine) should consult this provider
/// instead of rolling its own <c>Font</c> factory. Adding a new role means adding a new
/// method here; changing a formula means changing one line; there is no way for two
/// call sites to disagree on the size of a "tick label font" because there is exactly
/// one place the size is defined.</para>
///
/// <para>The four roles mirror matplotlib's font hierarchy:</para>
/// <list type="bullet">
///   <item><description><see cref="TickFont"/> — tick labels and legend entries. Size = <c>DefaultFont.Size</c>. Non-bold.</description></item>
///   <item><description><see cref="LabelFont"/> — axis labels (X / Y / Z). Size = <c>DefaultFont.Size</c>. Non-bold.</description></item>
///   <item><description><see cref="TitleFont"/> — axes title (per-subplot). Size = <c>DefaultFont.Size + 2</c>. Bold.</description></item>
///   <item><description><see cref="SupTitleFont"/> — figure-level suptitle. Size = <c>DefaultFont.Size + 4</c>. Bold.</description></item>
/// </list>
/// </remarks>
internal static class ThemedFontProvider
{
    /// <summary>Font for tick labels and legend entries — same size as axis labels, matching
    /// matplotlib's default. Non-bold.</summary>
    public static Font TickFont(Theme theme) => new()
    {
        Family = theme.DefaultFont.Family,
        Size   = theme.DefaultFont.Size,
        Color  = theme.ForegroundText,
    };

    /// <summary>Font for axis labels (X / Y / Z label text below or beside the spine).
    /// Same size as tick labels, non-bold.</summary>
    public static Font LabelFont(Theme theme) => new()
    {
        Family = theme.DefaultFont.Family,
        Size   = theme.DefaultFont.Size,
        Color  = theme.ForegroundText,
    };

    /// <summary>Font for the per-subplot axes title — 2 points larger than axis labels,
    /// bold. Matches matplotlib's <c>axes.titlesize = 'large'</c> default.</summary>
    public static Font TitleFont(Theme theme) => new()
    {
        Family = theme.DefaultFont.Family,
        Size   = theme.DefaultFont.Size + 2,
        Weight = FontWeight.Bold,
        Color  = theme.ForegroundText,
    };

    /// <summary>Font for the figure-level suptitle — 4 points larger than axis labels,
    /// bold. Matches matplotlib's <c>figure.titlesize = 'large'</c> default.</summary>
    public static Font SupTitleFont(Theme theme) => new()
    {
        Family = theme.DefaultFont.Family,
        Size   = theme.DefaultFont.Size + 4,
        Weight = FontWeight.Bold,
        Color  = theme.ForegroundText,
    };
}
