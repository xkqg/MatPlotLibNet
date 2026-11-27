// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet;

/// <summary>The texts of an axes and of its axes: the title of the axes, the title of each axis with its style, and the colour
/// of an axis.</summary>
public static class AxesBuilderLabelVerbs
{
    extension(AxesBuilder axes)
    {
        /// <summary>Sets the axes title.</summary>
        public AxesBuilder WithTitle(string title) { axes.Axes.Title = title; return axes; }

        /// <summary>Sets the axes title with an optional text style override.</summary>
        /// <param name="title">The title text.</param>
        /// <param name="configure">Function that receives the current <see cref="TextStyle"/> and returns a modified copy; <see langword="null"/> leaves the style unchanged.</param>
        public AxesBuilder WithTitle(string title, Func<TextStyle, TextStyle>? configure)
        {
            axes.Axes.Title = title;
            if (configure is not null) axes.Axes.TitleStyle = configure(axes.Axes.TitleStyle ?? new TextStyle());
            return axes;
        }

        /// <summary>Sets the X-axis label.</summary>
        public AxesBuilder SetXLabel(string label) { axes.X.Label(label); return axes; }

        /// <summary>Sets the X-axis label with an optional text style override.</summary>
        /// <param name="label">The axis label text.</param>
        /// <param name="configure">Function that receives the current <see cref="TextStyle"/> and returns a modified copy; <see langword="null"/> leaves the style unchanged.</param>
        public AxesBuilder SetXLabel(string label, Func<TextStyle, TextStyle>? configure) { axes.X.Label(label, configure); return axes; }

        /// <summary>Sets the Y-axis label.</summary>
        public AxesBuilder SetYLabel(string label) { axes.Y.Label(label); return axes; }

        /// <summary>Sets the Y-axis label with an optional text style override.</summary>
        /// <param name="label">The axis label text.</param>
        /// <param name="configure">Function that receives the current <see cref="TextStyle"/> and returns a modified copy; <see langword="null"/> leaves the style unchanged.</param>
        public AxesBuilder SetYLabel(string label, Func<TextStyle, TextStyle>? configure) { axes.Y.Label(label, configure); return axes; }

        /// <summary>Colours the left Y axis in one call: its spine, its tick marks, its tick labels and its title.
        /// Pairs the axis with a series drawn in the same colour; keep <see cref="extension(AxesBuilder).SetYLabel(string)"/> naming the series and
        /// its unit, so the colour is never the only thing that says which series the axis belongs to.</summary>
        /// <param name="color">The colour all four take.</param>
        /// <returns>This builder, for chaining.</returns>
        /// <remarks>Everything else the axis carries (tick length, label rotation, font size, spine width) is kept.
        /// Every further Y axis is coloured with <see cref="YAxisSlotBuilder.SetYAxisColor"/>: slot 0 of the right side
        /// colours the right spine, an axis stacked past slot 0 colours its ticks, tick labels and title.</remarks>
        public AxesBuilder SetYAxisColor(Color color) { axes.Y.Color(color); return axes; }

        /// <summary>Colours the bottom X axis in one call: its spine, its tick marks, its tick labels and its title.</summary>
        /// <param name="color">The colour all four take.</param>
        /// <returns>This builder, for chaining.</returns>
        /// <remarks>Everything else the axis carries (tick length, label rotation, font size, spine width) is kept.
        /// The top axis is coloured with <see cref="SecondaryXAxisBuilder.SetXAxisColor"/>.</remarks>
        public AxesBuilder SetXAxisColor(Color color) { axes.X.Color(color); return axes; }

        /// <summary>Sets the Z-axis label (3D charts only).</summary>
        public AxesBuilder SetZLabel(string label) { axes.Z.Label(label); return axes; }
    }
}
