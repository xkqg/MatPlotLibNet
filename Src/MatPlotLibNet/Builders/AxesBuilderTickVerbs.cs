// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet;

/// <summary>The ticks of an axis: where they stand (a locator), how they are written (a formatter, a date format), the turn and
/// the side of their labels, and the minor ticks.</summary>
public static class AxesBuilderTickVerbs
{
    extension(AxesBuilder axes)
    {
        /// <summary>Rotates X-axis tick labels by <paramref name="degrees"/>. Matches matplotlib's
        /// <c>ax.tick_params(axis='x', labelrotation=...)</c>. Pass 0 to restore horizontal labels
        /// — at 0 the renderer auto-rotates to 30° if adjacent labels would overlap
        /// (<c>Figure.autofmt_xdate</c> parity).</summary>
        public AxesBuilder WithXTickLabelRotation(double degrees) { axes.X.Ticks.LabelRotation(degrees); return axes; }

        /// <summary>Rotates Y-axis tick labels by <paramref name="degrees"/>. Matches matplotlib's
        /// <c>ax.tick_params(axis='y', labelrotation=...)</c>.</summary>
        public AxesBuilder WithYTickLabelRotation(double degrees) { axes.Y.Ticks.LabelRotation(degrees); return axes; }

        /// <summary>Mirrors Y-axis ticks and labels on both left and right spines.
        /// Useful for wide figures where reading the Y scale from the right side is more convenient.</summary>
        /// <remarks>The layout makes room for the right-hand labels, and end-of-line labels start past them. A secondary Y
        /// axis with series takes the right spine for its own ticks, so the mirrored ticks are not drawn there.</remarks>
        public AxesBuilder WithYTicksMirrored() { axes.Y.Ticks.Mirror(); return axes; }

        /// <summary>Mirrors X-axis ticks and labels on both top and bottom spines.</summary>
        /// <remarks>The axes title moves up above the top labels and the layout makes room for both. A secondary X axis
        /// with series takes the top spine for its own ticks, so the mirrored ticks are not drawn there.</remarks>
        public AxesBuilder WithXTicksMirrored() { axes.X.Ticks.Mirror(); return axes; }

        /// <summary>Moves the X tick labels to the top edge; the tick marks stay on both edges, as matplotlib's <c>matshow</c> numbers its
        /// columns (<c>xaxis.tick_top()</c>, then <c>set_ticks_position('both')</c>). The layout keeps room above the plot for the labels and
        /// none below it.</summary>
        /// <returns>This builder.</returns>
        public AxesBuilder WithXTickLabelsOnTop() { axes.X.Ticks.LabelsOnTheOtherSide(); return axes; }

        /// <summary>Moves the Y tick labels to the right edge; the tick marks stay on both edges. The layout keeps room right of the plot for
        /// the labels and none left of it.</summary>
        /// <returns>This builder.</returns>
        public AxesBuilder WithYTickLabelsOnRight() { axes.Y.Ticks.LabelsOnTheOtherSide(); return axes; }

        /// <summary>Sets the X-axis to date scale with the specified format.</summary>
        public AxesBuilder SetXDateFormat(string format = Rendering.TickFormatters.DateTimeTickFormatter.DefaultFormat)
        {
            axes.X.DateFormat(format);
            return axes;
        }

        /// <summary>Sets the Y-axis to date scale with the specified format.</summary>
        public AxesBuilder SetYDateFormat(string format = Rendering.TickFormatters.DateTimeTickFormatter.DefaultFormat)
        {
            axes.Y.DateFormat(format);
            return axes;
        }

        /// <summary>Sets a custom tick formatter on the X-axis.</summary>
        public AxesBuilder SetXTickFormatter(Rendering.TickFormatters.ITickFormatter formatter) { axes.X.Ticks.Formatter(formatter); return axes; }

        /// <summary>Sets a custom tick formatter on the Y-axis.</summary>
        public AxesBuilder SetYTickFormatter(Rendering.TickFormatters.ITickFormatter formatter) { axes.Y.Ticks.Formatter(formatter); return axes; }

        /// <summary>Sets a custom tick locator on the X-axis, overriding the default nice-number algorithm.</summary>
        /// <param name="locator">The <see cref="Rendering.TickLocators.ITickLocator"/> implementation to use.</param>
        /// <remarks>Also overrides any <see cref="Models.TickConfig.Spacing"/> set on the axis. Tick formatting via the axis <c>TickFormatter</c> is still applied after the locator produces its positions.</remarks>
        public AxesBuilder SetXTickLocator(Rendering.TickLocators.ITickLocator locator) { axes.X.Ticks.Locator(locator); return axes; }

        /// <summary>Sets a custom tick locator on the Y-axis, overriding the default nice-number algorithm.</summary>
        /// <param name="locator">The <see cref="Rendering.TickLocators.ITickLocator"/> implementation to use.</param>
        /// <remarks>Also overrides any <see cref="Models.TickConfig.Spacing"/> set on the axis. Tick formatting via the axis <c>TickFormatter</c> is still applied after the locator produces its positions.</remarks>
        public AxesBuilder SetYTickLocator(Rendering.TickLocators.ITickLocator locator) { axes.Y.Ticks.Locator(locator); return axes; }

        /// <summary>Switches the minor tick marks of both axes on or off. A linear axis' minor ticks split each step between two major
        /// ticks; a log axis' stand at 2 to 9 times each power of ten, and a symmetric-log axis has none. Under the matplotlib themes a log
        /// or a logit axis draws its minor ticks without this call, as matplotlib does, and <c>false</c> takes them away; a grid that asks
        /// for its minor lines (<see cref="Styling.GridWhich.Minor"/>, <see cref="Styling.GridWhich.Both"/>) draws them only where the minor
        /// ticks stand. The library's own themes draw the minor marks only after this call and the minor grid either way.</summary>
        /// <param name="visible">When <c>true</c> (default), minor ticks are shown; when <c>false</c>, they are hidden.</param>
        /// <returns>This builder, for chaining.</returns>
        public AxesBuilder WithMinorTicks(bool visible = true)
        {
            axes.X.Ticks.Minor(visible);
            axes.Y.Ticks.Minor(visible);
            return axes;
        }
    }
}
