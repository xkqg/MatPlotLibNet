// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>One element of an SVG drawing surface, as the one envelope of the element buffer writes it
/// (<see cref="SvgElementBuffer.WriteMark{TMark}"/>, <see cref="SvgElementBuffer.WriteText{TElement}"/>,
/// <see cref="SvgElementBuffer.WriteOutsideClips{TElement}"/>): its opening, then the
/// attributes asked of the next element, then its close. A value made for the element it writes, so writing one allocates nothing.</summary>
internal interface ISvgElement
{
    /// <summary>Writes the opening tag and every attribute of the element's own, up to the attributes asked of the next element. A
    /// paint server the element is filled with is defined here, before its tag: a definition written inside a tag would break it.</summary>
    /// <param name="buffer">The buffer the element is written into.</param>
    void WriteOpening(SvgElementBuffer buffer);

    /// <summary>Writes what follows the attributes: the end of an empty element, or the content and the end tag of a text.</summary>
    /// <param name="markup">The markup the element is written into.</param>
    void WriteClosing(StringBuilder markup);
}
