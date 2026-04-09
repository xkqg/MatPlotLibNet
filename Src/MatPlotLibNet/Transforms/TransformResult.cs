// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Transforms;

/// <summary>Binds a <see cref="Figure"/> to an <see cref="IFigureTransform"/>, providing a fluent API for output.</summary>
/// <param name="Figure">The figure to transform.</param>
/// <param name="Transform">The transform to apply.</param>
/// <remarks>Obtained via <c>figure.Transform(new SvgTransform())</c>. The same result can write to a stream,
/// save to a file, or return bytes — all polymorphically dispatching through the bound transform.</remarks>
public sealed record TransformResult(Figure Figure, IFigureTransform Transform)
{
    /// <summary>Writes the transformed output to the given stream.</summary>
    public void ToStream(Stream output) => Transform.Transform(Figure, output);

    /// <summary>Saves the transformed output to a file at the given path.</summary>
    public void ToFile(string path)
    {
        using var stream = File.Create(path);
        ToStream(stream);
    }

    /// <summary>Returns the transformed output as a byte array.</summary>
    public byte[] ToBytes()
    {
        using var stream = new MemoryStream();
        ToStream(stream);
        return stream.ToArray();
    }
}
