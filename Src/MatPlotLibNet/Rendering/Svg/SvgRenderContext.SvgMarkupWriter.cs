// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using System.Runtime.CompilerServices;
using System.Text;

namespace MatPlotLibNet.Rendering.Svg;

public sealed partial class SvgRenderContext
{
    /// <summary>What an SVG drawing surface writes for the browser around what it draws: groups with a class, an accessible name or the
    /// attributes the scripts read (a series' slot and mapping, a legend entry's index, an axes' view, a 3-D scene's camera), links,
    /// tooltips and the opacity wrapper. Every one is written outside the clips (<see cref="SvgElementBuffer.OutsideClips"/>), so a group
    /// drawn in a clip only ever holds marks. A value the surface holds in a field of its own (it keeps whether an opacity wrapper is open),
    /// so it allocates nothing.</summary>
    /// <param name="buffer">The buffer the markup is written into.</param>
    internal struct SvgMarkupWriter(SvgElementBuffer buffer)
    {
        /// <summary>The style of an element a click acts on (a link, a legend entry): the pointer shows it can be clicked.</summary>
        private const string PointerCursor = " style=\"cursor:pointer\"";

        /// <summary>Whether an opacity wrapper (<see cref="Rendering.Svg.SvgRenderContext.SvgMarkupWriter.WrapInOpacity"/>) is open.</summary>
        private bool _opacityGroupOpen;

        /// <summary>Opens a group with a class.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly void BeginGroup(string cssClass) => OpenGroupOf(buffer.OutsideClips(), cssClass).AppendLineLf(">");

        /// <summary>Opens a group with a class and an accessible name: <c>&lt;g class="…" aria-label="…"&gt;</c>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly void BeginAccessibleGroup(string cssClass, string ariaLabel) =>
            AppendName(OpenGroupOf(buffer.OutsideClips(), cssClass), ariaLabel).AppendLineLf(">");

        /// <summary>Closes the current group: the one writer of a group's end tag (an opacity wrapper, a 3-D tier and a tooltip group close
        /// through it).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly void EndGroup() => buffer.OutsideClips().AppendLineLf("</g>");

        /// <summary>Opens a link: <c>&lt;a href="…" style="cursor:pointer" aria-label="…"&gt;</c>, and <c>aria-expanded</c> for a link that
        /// discloses something on the same page. <c>href</c> (SVG 2), not <c>xlink:href</c>: every browser the library targets resolves it,
        /// inline in HTML and as a standalone file alike, and it needs no extra namespace on the root.</summary>
        internal readonly void BeginHyperlink(string url, string? ariaLabel, bool? expanded)
        {
            var markup = buffer.OutsideClips().Append("<a href=\"").Append(url.EscapeForXml()).Append('"').Append(PointerCursor);
            AppendAriaLabel(markup, ariaLabel);
            if (expanded is { } state)
            {
                markup.Append(" aria-expanded=\"").Append(state ? "true" : "false").Append('"');
            }
            markup.AppendLineLf(">");
        }

        /// <summary>Closes the current link.</summary>
        internal readonly void EndHyperlink() => buffer.OutsideClips().AppendLineLf("</a>");

        /// <summary>Opens the group a series is drawn in: with interactive attributes a data group carrying the series' slot (and, with the
        /// data cursor, the transform it is drawn with); without them an accessible group for a labelled series and none for another.</summary>
        /// <returns><see langword="true"/> when a group was opened.</returns>
        internal readonly bool OpenSeriesGroup(SvgEmit emit, string? label, int slotIndex, DataTransform? drawnWith)
        {
            if (emit.InteractiveAttributes)
            {
                BeginDataGroup("series", slotIndex, label, emit.DataCursorMapping ? drawnWith : null);
                return true;
            }
            if (string.IsNullOrEmpty(label)) return false;
            BeginAccessibleGroup("series", label);
            return true;
        }

        /// <summary>Opens the group of a labelled patch, carrying the index its legend row toggles.</summary>
        internal readonly void OpenPatchGroup(int index, string label) => BeginDataGroup("patch", index, label, null);

        /// <summary>Writes the view of one axes for the server-interaction script: an empty group carrying <c>data-axes-index</c>, the plot
        /// area and, when the axes drew a Cartesian range, that range, written by the same code as a series group's mapping, then the own
        /// view of every Y axis stacked past the primary. The script finds the axes under the pointer by its plot area and converts with its
        /// range; an axes without a range (polar, 3-D) is still found, and the script falls back to the limits cached on the root.</summary>
        internal readonly void WriteAxesView(int axesIndex, Rect plotArea, DataTransform? drawnWith, IReadOnlyList<SecondaryAxisTransform>? secondary) =>
            buffer.OutsideClips().Append("<g ").Append(SvgAttributes.AxesIndex).Append("=\"").AppendInvariant(axesIndex).Append('"').AppendView(drawnWith?.PlotBounds ?? plotArea, drawnWith).AppendSecondaryViews(secondary).AppendLineLf(" />");

        /// <summary>Opens the group of a 3-D scene a browser turns, carrying the camera, which side of each axis the server put at the back
        /// (x, y, z as 0 for the minimum and 1 for the maximum: the script re-runs the same selection every frame and mirrors the elements
        /// whose axis flipped) and the plot area. The distance is always written, the camera's own or its resting one
        /// (<see cref="Camera3D.RestingDistance"/>), so the script and the server stay in lockstep. The light is not written: a face is
        /// shaded in data space, which a turn does not change.</summary>
        internal readonly void Begin3DSceneGroup(double elevation, double azimuth, double? distance, Rect plotBounds, CubeFaceSelection faces) =>
            OpenGroupOf(buffer.OutsideClips(), "mpl-3d-scene")
                  .Append(" data-elevation=\"").AppendExactSvgNumber(elevation).Append('"')
                  .Append(" data-azimuth=\"").AppendExactSvgNumber(azimuth).Append('"')
                  .Append(" data-faces=\"").Append(BackOf(faces.X)).Append(BackOf(faces.Y)).Append(BackOf(faces.Z)).Append('"')
                  .Append(" data-distance=\"").AppendExactSvgNumber(distance ?? Camera3D.RestingDistance).Append('"').AppendPlotArea(plotBounds).AppendLineLf(">");

        /// <summary>Opens one tier of a 3-D scene (matplotlib's draw order, axes3d.py: the back with panes, edges, grid and axis labels; the
        /// data, which the script sorts by depth in place; the front with the ticks), carrying what was asked of it (a marker group's
        /// <c>data-v3d</c>). Keeps the frame of the axes out of the depth sort, so a pane never paints over a surface on a turn.</summary>
        internal readonly void Begin3DSubgroup(string className) => buffer.WriteOutsideClips(new TierElement(className));

        /// <summary>Opens the group of a legend entry a click toggles, carrying the index of its series.</summary>
        internal readonly void BeginLegendItemGroup(int legendIndex, string? ariaLabel)
        {
            var markup = buffer.OutsideClips().Append("<g ").Append(SvgAttributes.LegendIndex).Append("=\"").AppendInvariant(legendIndex).Append('"')
                               .Append(PointerCursor);
            AppendAriaLabel(markup, ariaLabel);
            markup.AppendLineLf(">");
        }

        /// <summary>Opens a group whose <c>&lt;title&gt;</c> a browser shows as a tooltip over what is drawn in it.</summary>
        internal readonly void BeginTooltipGroup(string tooltipText) =>
            buffer.OutsideClips().Append("<g><title>").Append(tooltipText.EscapeForXml()).AppendLineLf("</title>");

        /// <summary>Wraps what is drawn next in a group of <paramref name="opacity"/>: the wrapper open before is closed (a renderer sets an
        /// opacity, then sets full opacity again), and a new one opens only below full opacity, so full opacity writes no wrapper.</summary>
        /// <param name="opacity">The opacity the surface holds, between 0 and 1.</param>
        internal void WrapInOpacity(double opacity)
        {
            var markup = buffer.OutsideClips();
            if (_opacityGroupOpen)
            {
                markup.AppendLineLf("</g>");
                _opacityGroupOpen = false;
            }
            if (opacity < 1.0)
            {
                markup.Append("<g opacity=\"").AppendExactSvgNumber(opacity).AppendLineLf("\">");
                _opacityGroupOpen = true;
            }
        }

        /// <summary>Opens a group with a class and <c>data-series-index</c> for the scripts, an accessible name when it has one and, when
        /// <paramref name="mapping"/> is given, the view of the transform the series inside is drawn with (the inverse of which is
        /// <see cref="DataTransform.PixelToData"/>, what the data cursor converts a click with).</summary>
        private readonly void BeginDataGroup(string cssClass, int seriesIndex, string? ariaLabel, DataTransform? mapping)
        {
            var markup = OpenGroupOf(buffer.OutsideClips(), cssClass)
                .Append(' ').Append(SvgAttributes.SeriesIndex).Append("=\"").AppendInvariant(seriesIndex).Append('"');
            AppendAriaLabel(markup, ariaLabel);
            if (mapping is not null)
                markup.AppendView(mapping.PlotBounds, mapping);
            markup.AppendLineLf(">");
        }

        /// <summary>The one writer of the opening of a group with a class, up to its other attributes: <c>&lt;g class="…"</c>. A plain group,
        /// an accessible one, a data group, a 3-D scene and its tiers open through it.</summary>
        private static StringBuilder OpenGroupOf(StringBuilder markup, string cssClass) => markup.Append("<g class=\"").Append(cssClass).Append('"');

        /// <summary>The one writer of an accessible name: <c>aria-label="…"</c>, escaped.</summary>
        private static StringBuilder AppendName(StringBuilder markup, string ariaLabel) =>
            markup.Append(" aria-label=\"").Append(ariaLabel.EscapeForXml()).Append('"');

        /// <summary>The accessible name of the element being opened, when it has one; nothing for <see langword="null"/> or an empty name. A
        /// data group, a link and a legend entry name themselves through it.</summary>
        private static void AppendAriaLabel(StringBuilder markup, string? ariaLabel)
        {
            if (!string.IsNullOrEmpty(ariaLabel)) AppendName(markup, ariaLabel);
        }

        /// <summary>The side of one axis the camera puts at the back, as the scene writes it: 0 for the minimum, 1 for the maximum.</summary>
        private static char BackOf(CubePlane plane) => plane.Back == CubeSide.Max ? '1' : '0';

        /// <summary>The opening of a tier of a 3-D scene: a group with a class, then what was asked of it.</summary>
        private readonly record struct TierElement(string ClassName) : ISvgElement
        {
            public void WriteOpening(SvgElementBuffer buffer) => OpenGroupOf(buffer.Markup, ClassName);

            public void WriteClosing(StringBuilder markup) => markup.AppendLineLf(">");
        }

        // 
    }
}
