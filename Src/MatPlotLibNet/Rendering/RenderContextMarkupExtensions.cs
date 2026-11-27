// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Rendering;

/// <summary>What a surface that writes markup for a browser adds around what it draws, asked of any surface. The library's SVG
/// context writes it (<see cref="IMarkupRenderContext"/>); a surface that paints pixels has nothing to add, so for it each member
/// does nothing (an accessible group is the plain group, and nothing is opened that the caller would have to close). A renderer
/// asks these members, never the type of its surface.</summary>
internal static class RenderContextMarkupExtensions
{
    extension(IRenderContext ctx)
    {
        /// <summary>What the figure asked the subplot drawn into this context to write for the browser
        /// (<see cref="Svg.SvgEmit.Of(Models.Axes, IRenderContext)"/> reads it); none for a surface without markup.</summary>
        internal SvgEmit Emit => ctx is IMarkupRenderContext markup ? markup.Emit : default;

        /// <summary>Begins a group with a CSS class and an accessible name (<c>aria-label</c>). A surface without markup has no
        /// name to give it and opens the plain group (<see cref="IRenderContext.BeginGroup"/>); either way
        /// <see cref="IRenderContext.EndGroup"/> closes it.</summary>
        /// <param name="cssClass">The group's class.</param>
        /// <param name="ariaLabel">The group's accessible name.</param>
        internal void BeginAccessibleGroup(string cssClass, string ariaLabel)
        {
            if (ctx is IMarkupRenderContext markup) markup.BeginAccessibleGroup(cssClass, ariaLabel);
            else ctx.BeginGroup(cssClass);
        }

        /// <summary>Opens the group a series is drawn in (<see cref="IMarkupRenderContext.OpenSeriesGroup"/>). A surface without
        /// markup opens none.</summary>
        /// <param name="emit">What the axes writes for the browser.</param>
        /// <param name="label">The series' label, or <see langword="null"/>.</param>
        /// <param name="slotIndex">The series' identity slot.</param>
        /// <param name="drawnWith">The transform the series is drawn with, or <see langword="null"/>.</param>
        /// <returns><see langword="true"/> when a group was opened; the caller closes it with <see cref="IRenderContext.EndGroup"/>.</returns>
        internal bool OpenSeriesGroup(SvgEmit emit, string? label, int slotIndex, DataTransform? drawnWith) =>
            ctx is IMarkupRenderContext markup && markup.OpenSeriesGroup(emit, label, slotIndex, drawnWith);

        /// <summary>Opens the group a labelled patch is drawn in (<see cref="IMarkupRenderContext.OpenPatchGroup"/>) when the axes
        /// writes interactive attributes for the browser. A surface without markup, or a picture without them, opens none.</summary>
        /// <param name="emit">What the axes writes for the browser.</param>
        /// <param name="index">The index the patch's legend row toggles.</param>
        /// <param name="label">The patch's label.</param>
        /// <returns><see langword="true"/> when a group was opened; the caller closes it with <see cref="IRenderContext.EndGroup"/>.</returns>
        internal bool OpenPatchGroup(SvgEmit emit, int index, string label)
        {
            if (!emit.InteractiveAttributes || ctx is not IMarkupRenderContext markup) return false;
            markup.OpenPatchGroup(index, label);
            return true;
        }

        /// <summary>Opens the group of a legend entry a click toggles, carrying the index of its series. A surface without markup
        /// opens none.</summary>
        /// <param name="legendIndex">The series index the entry toggles.</param>
        /// <param name="ariaLabel">The entry's accessible name, or <see langword="null"/>.</param>
        /// <returns><see langword="true"/> when a group was opened; the caller closes it with <see cref="IRenderContext.EndGroup"/>.</returns>
        internal bool BeginLegendItemGroup(int legendIndex, string? ariaLabel) =>
            ctx is IMarkupRenderContext markup && markup.BeginLegendItemGroup(legendIndex, ariaLabel);

        /// <summary>Opens a group whose title a browser shows as a tooltip over what is drawn until <c>EndTooltipGroup</c>.
        /// Nothing on a surface without markup.</summary>
        /// <param name="tooltipText">The tooltip's text.</param>
        internal void BeginTooltipGroup(string tooltipText)
        {
            if (ctx is IMarkupRenderContext markup) markup.BeginTooltipGroup(tooltipText);
        }

        /// <summary>Closes the group <c>BeginTooltipGroup</c> opened. Nothing on a surface without markup.</summary>
        internal void EndTooltipGroup()
        {
            if (ctx is IMarkupRenderContext markup) markup.EndTooltipGroup();
        }

        /// <summary>Gives the next text drawn its depth in a tree (<c>aria-level</c>), or takes it back with <see langword="null"/>. Nothing
        /// on a surface without markup, which draws it as an ordinary text.</summary>
        /// <param name="level">The depth, or <see langword="null"/> for none.</param>
        internal void SetNextTextLevel(int? level)
        {
            if (ctx is IMarkupRenderContext markup) markup.SetNextTextLevel(level);
        }

        /// <summary>Gives the next drawn element a <c>class</c>. Nothing on a surface without markup.</summary>
        /// <param name="className">The class.</param>
        internal void SetNextElementClass(string className)
        {
            if (ctx is IMarkupRenderContext markup) markup.SetNextElementClass(className);
        }

        /// <summary>Writes the view of one axes for the server-interaction script. Nothing on a surface without markup.</summary>
        /// <param name="axesIndex">The axes' index in the figure's subplots.</param>
        /// <param name="plotArea">The axes' plot area.</param>
        /// <param name="drawnWith">The transform the axes drew its primary series with, or <see langword="null"/>.</param>
        /// <param name="secondary">The own transform of every Y axis stacked past the primary, or <see langword="null"/>.</param>
        internal void WriteAxesView(int axesIndex, Rect plotArea, DataTransform? drawnWith,
            IReadOnlyList<SecondaryAxisTransform>? secondary = null)
        {
            if (ctx is IMarkupRenderContext markup) markup.WriteAxesView(axesIndex, plotArea, drawnWith, secondary);
        }

        /// <summary>Opens the group of a 3-D scene that a browser turns, carrying the camera. A surface without markup opens
        /// none, and the axes then writes no vertex data either.</summary>
        /// <param name="elevation">The camera's elevation, in degrees.</param>
        /// <param name="azimuth">The camera's azimuth, in degrees.</param>
        /// <param name="distance">The camera's distance, or <see langword="null"/> for the default.</param>
        /// <param name="plotBounds">The square the scene is projected into.</param>
        /// <param name="faces">Which side of each axis the camera puts at the back.</param>
        /// <returns><see langword="true"/> when the scene was opened; the caller closes it with <see cref="IRenderContext.EndGroup"/>.</returns>
        internal bool Begin3DSceneGroup(double elevation, double azimuth, double? distance, Rect plotBounds, CubeFaceSelection faces) =>
            ctx is IMarkupRenderContext markup && markup.Begin3DSceneGroup(elevation, azimuth, distance, plotBounds, faces);

        /// <summary>Opens one tier of an open 3-D scene. Nothing on a surface without markup.</summary>
        /// <param name="className">The tier's class.</param>
        internal void Begin3DSubgroup(string className)
        {
            if (ctx is IMarkupRenderContext markup) markup.Begin3DSubgroup(className);
        }

        /// <summary>Closes the tier <c>Begin3DSubgroup</c> opened. Nothing on a surface without markup.</summary>
        internal void End3DSubgroup()
        {
            if (ctx is IMarkupRenderContext markup) markup.End3DSubgroup();
        }
    }
}
