// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Transforms.Animation;

/// <summary>Encodes a sequence of <see cref="Figure"/> frames into an animated file format.</summary>
public interface IAnimationTransform
{
    /// <summary>Renders all frames and writes the encoded animation to <paramref name="output"/>.</summary>
    /// <param name="frames">The figure frames to encode.</param>
    /// <param name="interval">The delay between frames.</param>
    /// <param name="loop">Whether the animation loops continuously.</param>
    /// <param name="output">The destination stream.</param>
    void Transform(IEnumerable<Figure> frames, TimeSpan interval, bool loop, Stream output);
}
