// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="Text3DSeries"/> as text labels projected from 3D to 2D, drawn back to front with the scene.</summary>
internal sealed class Text3DSeriesRenderer : SceneSeriesRenderer<Text3DSeries>
{
    /// <inheritdoc />
    public Text3DSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(Text3DSeries series, MarkObstacles.Builder marks) => AddMarksByDrawing(series, marks);

    /// <inheritdoc />
    public override void Render(Text3DSeries series)
    {
        if (series.Annotations.Count == 0) return;

        var color = ColorOf(series);

        // Fitted to the coordinates that can be placed: a missing one (NaN) and an infinity are left out.
        var proj = ProjectionFor(
            series.Annotations.Select(a => a.X).ToArray().DrawableRange(),
            series.Annotations.Select(a => a.Y).ToArray().DrawableRange(),
            series.Annotations.Select(a => a.Z).ToArray().DrawableRange());

        // Each label at the depth of its point; a label at a point that cannot be placed is skipped.
        var labels = new List<Label>(series.Annotations.Count);
        foreach (var a in series.Annotations)
        {
            var pt = proj.Project(a.X, a.Y, a.Z);
            if (!pt.IsDrawable()) continue;
            labels.Add(new(proj.Depth(a.X, a.Y, a.Z), pt, a.Text));
        }

        var font = Context.Theme.DataTextFont(series.TextSizeUnder(Context.Theme), color);
        DrawBackToFront(labels, label => Ctx.Draw(TextLabel.Of(label.Text), font, new TextPlacement(label.At, TextAlignment.Center)));
    }

    /// <summary>A label: its text and where it stands on the canvas, at the depth of its point.</summary>
    private readonly record struct Label(double Depth, Point At, string Text) : IDepthMark;
}
