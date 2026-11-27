// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.LegendRendering;

/// <summary>The one rule for each setting of a legend that its author may leave alone and its theme decides: what the author named,
/// else what the theme's <see cref="LegendLook"/> says (<see cref="LegendLook.Library"/> under the library's own themes, matplotlib's
/// <c>legend.*</c> rcParams under the matplotlib themes). The legend's drawing and the layout that reserves its room read them all
/// through here.</summary>
internal static class LegendLookExtensions
{
    extension(Legend legend)
    {
        /// <summary>The place <paramref name="legend"/> stands in under <paramref name="theme"/>: the one it names, or, when it was left at
        /// <see cref="LegendPosition.Best"/>, the theme's own (<see cref="Theme.DefaultLegendPosition"/>, matplotlib's <c>legend.loc</c>).
        /// The one rule every reader of a legend's place asks: the layout of the box, the renderer's decision to collect the marks the
        /// legend keeps clear of, the place of a figure's legend (<c>OnTheFigure</c>) and the room the constrained layout keeps for a
        /// legend outside the plot. A theme that leaves the place at Best leaves it at Best here.</summary>
        /// <param name="theme">The theme the legend is drawn under.</param>
        internal LegendPosition PositionUnder(Theme theme) =>
            legend.Position == LegendPosition.Best ? theme.DefaultLegendPosition : legend.Position;

        /// <summary>The opacity of the frame's fill under <paramref name="theme"/>: the one the legend was given, else the theme's.</summary>
        /// <param name="theme">The theme the legend is drawn under.</param>
        internal double FrameAlphaUnder(Theme theme) => theme.Looks.Legend.FrameAlphaOver(legend.GivenFrameAlpha);

        /// <summary>The colour of the frame's outline under <paramref name="theme"/>: the one the legend was given, else the theme's;
        /// at the frame's opacity (<see cref="FrameAlphaUnder"/>) where the theme's look applies it to the whole frame
        /// (<see cref="LegendFrameOpacity.Patch"/>, matplotlib's frame patch), opaque otherwise.</summary>
        /// <param name="theme">The theme the legend is drawn under.</param>
        internal Color EdgeColorUnder(Theme theme)
        {
            var edge = theme.Looks.Legend.EdgeColorOver(legend.EdgeColor);
            return theme.Looks.Legend.FrameOpacity == LegendFrameOpacity.Patch ? legend.AtFrameAlpha(edge, theme) : edge;
        }

        /// <summary>The colour of the frame's fill under <paramref name="theme"/>, before its opacity: the one the legend was given, else
        /// the figure's background or the axes' colour, as the theme's look says (<see cref="LegendLook.Face"/>).</summary>
        /// <param name="theme">The theme the legend is drawn under.</param>
        internal Color FaceColorUnder(Theme theme) =>
            legend.FaceColor ?? (theme.Looks.Legend.Face == LegendFace.Axes ? theme.AxesBackground : theme.Background);

        /// <summary>The colour the frame is filled with under <paramref name="theme"/>: its face (<see cref="FaceColorUnder"/>) at the
        /// frame's opacity (<see cref="FrameAlphaUnder"/>).</summary>
        /// <param name="theme">The theme the legend is drawn under.</param>
        internal Color FillColorUnder(Theme theme) => legend.AtFrameAlpha(legend.FaceColorUnder(theme), theme);

        /// <summary><paramref name="color"/> at the frame's opacity under <paramref name="theme"/>, the one conversion of that opacity
        /// to a colour's alpha the fill and the outline share.</summary>
        private Color AtFrameAlpha(Color color, Theme theme) => color.WithOpacityGiven(legend.FrameAlphaUnder(theme));

        /// <summary>Whether the frame's corners are rounded under <paramref name="theme"/>: as the legend was given, else as the theme says.</summary>
        /// <param name="theme">The theme the legend is drawn under.</param>
        internal bool FancyBoxUnder(Theme theme) => theme.Looks.Legend.FancyBoxOver(legend.GivenFancyBox);

        /// <summary>How many markers the handle of a line shows under <paramref name="theme"/>: as the legend was given, else as the theme says.</summary>
        /// <param name="theme">The theme the legend is drawn under.</param>
        internal int NumPointsUnder(Theme theme) => Math.Max(1, theme.Looks.Legend.NumPointsOver(legend.NumPoints));

        /// <summary>How many markers the handle of a scatter shows under <paramref name="theme"/>: as the legend was given, else as the theme says.</summary>
        /// <param name="theme">The theme the legend is drawn under.</param>
        internal int ScatterPointsUnder(Theme theme) => Math.Max(1, theme.Looks.Legend.ScatterPointsOver(legend.ScatterPoints));

        /// <summary>Where the markers of a scatter's handle stand under <paramref name="theme"/>: spread over the handle when the legend
        /// names how many it shows, else as the theme's look says (<see cref="LegendLook.ScatterHandle"/>).</summary>
        /// <param name="theme">The theme the legend is drawn under.</param>
        internal LegendScatterHandle ScatterHandleUnder(Theme theme) =>
            legend.ScatterPoints is null ? theme.Looks.Legend.ScatterHandle : LegendScatterHandle.Spread;
    }
}
