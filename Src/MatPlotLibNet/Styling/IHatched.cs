// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Something that names a pattern to lay over a fill and the colour of its strokes: a series that hatches, a patch, a tree
/// node. It holds them as the two public settings they have always been, and <see cref="HatchedExtensions"/> reads them as one
/// <see cref="HatchFill"/>.</summary>
/// <remarks>Internal, satisfied by the owner's own public properties: no member is added to a public type.</remarks>
internal interface IHatchSource
{
    /// <summary>The pattern laid over the fill.</summary>
    HatchPattern Hatch { get; }

    /// <summary>The colour of the strokes of the pattern, <see langword="null"/> while none was named.</summary>
    Color? HatchColor { get; }
}

/// <summary>A hatch source whose pattern and colour can be set after it is made: a series or a patch (a tree node is set once, when it
/// is made, so it is only a <see cref="IHatchSource"/>).</summary>
internal interface IHatched : IHatchSource
{
    /// <inheritdoc cref="IHatchSource.Hatch"/>
    new HatchPattern Hatch { get; set; }

    /// <inheritdoc cref="IHatchSource.HatchColor"/>
    new Color? HatchColor { get; set; }
}

/// <summary>The pattern and colour of a hatch source as one value, so a renderer, the JSON writer and the JSON reader each handle one
/// thing and none restates the pair.</summary>
internal static class HatchedExtensions
{
    extension(IHatchSource owner)
    {
        /// <summary>The pattern and the colour its author named.</summary>
        internal HatchFill Hatching => new(owner.Hatch, owner.HatchColor);
    }

    extension(IHatched owner)
    {
        /// <summary>Sets the pattern and the colour from <paramref name="hatching"/>.</summary>
        /// <param name="hatching">The pattern and the colour its author named.</param>
        internal void LayHatching(HatchFill hatching)
        {
            owner.Hatch = hatching.Pattern;
            owner.HatchColor = hatching.Color;
        }
    }
}
