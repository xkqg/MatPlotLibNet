// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Rendering;

/// <summary>A surface that writes markup for a browser, and what it adds around what it draws: the library's SVG context. A
/// renderer reaches these members through the same names on any <see cref="IRenderContext"/>
/// (<see cref="RenderContextMarkupExtensions"/>), where a surface that paints pixels has nothing to add; it never asks the
/// type of its surface. They live here and not on <see cref="IRenderContext"/> so that an application can still implement,
/// proxy or mock the public interface.</summary>
internal interface IMarkupRenderContext : IRenderContext
{
    /// <summary>What the figure asked the subplot drawn into this context to write for the browser
    /// (<see cref="Svg.SvgEmit.Of(Models.Axes, IRenderContext)"/> reads it).</summary>
    internal SvgEmit Emit { get; }

    /// <summary>Begins a group with a CSS class and an accessible name (<c>aria-label</c>), closed by
    /// <see cref="IRenderContext.EndGroup"/>.</summary>
    /// <param name="cssClass">The group's class.</param>
    /// <param name="ariaLabel">The group's accessible name.</param>
    internal void BeginAccessibleGroup(string cssClass, string ariaLabel);

    /// <summary>Opens the group a series is drawn in, by what the draw writes for the browser: with interactive attributes a
    /// group carrying the series' slot (and, with the data cursor, the transform it is drawn with), else a group named by the
    /// series' label when it has one.</summary>
    /// <param name="emit">What the axes writes for the browser (<see cref="Svg.SvgEmit.Of(Models.Axes, IRenderContext)"/>).</param>
    /// <param name="label">The series' label, or <see langword="null"/>.</param>
    /// <param name="slotIndex">The series' identity slot.</param>
    /// <param name="drawnWith">The transform the series is drawn with, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when a group was opened; the caller closes it with <see cref="IRenderContext.EndGroup"/>.</returns>
    internal bool OpenSeriesGroup(SvgEmit emit, string? label, int slotIndex, DataTransform? drawnWith);

    /// <summary>Opens the group a labelled patch is drawn in, in a picture with interactive attributes: carrying the index its
    /// legend row toggles (<c>data-series-index</c>) and its label, so the legend toggle hides and shows it as it does a series.</summary>
    /// <param name="index">The index the patch's legend row toggles.</param>
    /// <param name="label">The patch's label.</param>
    internal void OpenPatchGroup(int index, string label);

    /// <summary>Opens the group of a legend entry a click toggles, carrying the index of its series.</summary>
    /// <param name="legendIndex">The series index the entry toggles.</param>
    /// <param name="ariaLabel">The entry's accessible name, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when a group was opened; the caller closes it with <see cref="IRenderContext.EndGroup"/>.</returns>
    internal bool BeginLegendItemGroup(int legendIndex, string? ariaLabel);

    /// <summary>Opens a group whose title a browser shows as a tooltip over what is drawn until <see cref="EndTooltipGroup"/>.</summary>
    /// <param name="tooltipText">The tooltip's text.</param>
    internal void BeginTooltipGroup(string tooltipText);

    /// <summary>Closes the group <see cref="BeginTooltipGroup"/> opened.</summary>
    internal void EndTooltipGroup();

    /// <summary>Gives the next text drawn its depth in a tree (<c>aria-level</c>, 1-based), whether it is drawn as a plain text or as
    /// math; <see langword="null"/> takes it back. The one way a text carries a level (<see cref="IRenderContext.DrawTextWithLevel"/>
    /// is the same, asked of the public interface).</summary>
    /// <param name="level">The depth, or <see langword="null"/> for none.</param>
    internal void SetNextTextLevel(int? level);

    /// <summary>Gives the next drawn element a <c>class</c>.</summary>
    /// <param name="className">The class.</param>
    internal void SetNextElementClass(string className);

    /// <summary>Writes the view of one axes for the server-interaction script.</summary>
    /// <param name="axesIndex">The axes' index in the figure's subplots.</param>
    /// <param name="plotArea">The axes' plot area.</param>
    /// <param name="drawnWith">The transform the axes drew its primary series with, or <see langword="null"/>.</param>
    /// <param name="secondary">The own transform of every Y axis stacked past the primary, or <see langword="null"/>.</param>
    internal void WriteAxesView(int axesIndex, Rect plotArea, DataTransform? drawnWith, IReadOnlyList<SecondaryAxisTransform>? secondary);

    /// <summary>Opens the group of a 3-D scene that a browser turns, carrying the camera.</summary>
    /// <param name="elevation">The camera's elevation, in degrees.</param>
    /// <param name="azimuth">The camera's azimuth, in degrees.</param>
    /// <param name="distance">The camera's distance, or <see langword="null"/> for the default.</param>
    /// <param name="plotBounds">The square the scene is projected into.</param>
    /// <param name="faces">Which side of each axis the camera puts at the back.</param>
    /// <returns><see langword="true"/> when the scene was opened; the caller closes it with <see cref="IRenderContext.EndGroup"/>.</returns>
    internal bool Begin3DSceneGroup(double elevation, double azimuth, double? distance, Rect plotBounds, CubeFaceSelection faces);

    /// <summary>Opens one tier of an open 3-D scene.</summary>
    /// <param name="className">The tier's class.</param>
    internal void Begin3DSubgroup(string className);

    /// <summary>Closes the tier <see cref="Begin3DSubgroup"/> opened.</summary>
    internal void End3DSubgroup();
}
