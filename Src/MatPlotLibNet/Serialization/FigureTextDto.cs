// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Serialization;

/// <summary>The figure JSON's form of a <see cref="FigureText"/>, an entry of the figure's <c>texts</c>: its words and its place
/// as fractions of the figure, and each setting only when it differs from a new text's, so a text that sets none writes three
/// fields.</summary>
internal sealed record FigureTextDto
{
    public string? Text { get; init; }

    /// <summary>The horizontal place as a fraction of the figure's width from the left; a value outside 0 to 1, or no number
    /// (written as the string <c>"NaN"</c>), is kept and not drawn, as the model keeps it.</summary>
    public double X { get; init; }

    /// <summary>The vertical place as a fraction of the figure's height counted up from the bottom.</summary>
    public double Y { get; init; }

    /// <summary>The font of the text; absent when the text takes the theme's.</summary>
    public FontDto? Font { get; init; }

    /// <summary>The colour of the text; absent for the theme's text colour.</summary>
    public Color? Color { get; init; }

    /// <summary>How the text stands against its point horizontally; absent for left.</summary>
    public TextAlignment? Alignment { get; init; }

    /// <summary>How the text stands against its point vertically; absent for the baseline.</summary>
    public TextVerticalAlignment? VerticalAlignment { get; init; }

    /// <summary>The angle the text is turned through, in degrees counter-clockwise; absent for none.</summary>
    public double? Rotation { get; init; }

    private static readonly FigureText Rest = new("", 0, 0);

    /// <summary>The JSON's form of <paramref name="text"/>.</summary>
    internal static FigureTextDto From(FigureText text) => new()
    {
        Text = text.Text,
        X = text.X,
        Y = text.Y,
        Font = FontDto.From(text.Font),
        Color = text.Color,
        Alignment = text.Alignment.OrNullAtRest(Rest.Alignment),
        VerticalAlignment = text.VerticalAlignment.OrNullAtRest(Rest.VerticalAlignment),
        Rotation = text.Rotation.OrNullAtRest(Rest.Rotation),
    };

    /// <summary>Adds the text this record describes to <paramref name="figure"/> and returns it. A record without words is a
    /// text of no characters, which draws nothing.</summary>
    /// <param name="figure">The figure the text is placed on.</param>
    internal FigureText AddTo(Figure figure)
    {
        var text = figure.AddText(Text ?? "", X, Y);
        text.Font = Font?.ToFont();
        text.Color = Color;
        text.Alignment = Alignment ?? Rest.Alignment;
        text.VerticalAlignment = VerticalAlignment ?? Rest.VerticalAlignment;
        text.Rotation = Rotation ?? Rest.Rotation;
        return text;
    }
}
