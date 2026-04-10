// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Animation;
using MatPlotLibNet.Transforms.Animation;

namespace MatPlotLibNet.Skia;

/// <summary>Convenience extension methods for animated GIF export from <see cref="AnimationBuilder"/>.</summary>
public static class AnimationSkiaExtensions
{
    private static readonly GifTransform GifTransformInstance = new();

    /// <summary>Encodes all frames as an animated GIF and returns the bytes.</summary>
    /// <param name="builder">The animation to export.</param>
    /// <returns>GIF89a bytes.</returns>
    public static byte[] ToGif(this AnimationBuilder builder)
    {
        using var ms = new MemoryStream();
        GifTransformInstance.Transform(builder.GenerateFrames(), builder.Interval, builder.Loop, ms);
        return ms.ToArray();
    }

    /// <summary>Encodes all frames as an animated GIF and saves it to <paramref name="path"/>.</summary>
    /// <param name="builder">The animation to export.</param>
    /// <param name="path">Destination file path (e.g. <c>"animation.gif"</c>).</param>
    public static void SaveGif(this AnimationBuilder builder, string path)
    {
        using var fs = File.OpenWrite(path);
        GifTransformInstance.Transform(builder.GenerateFrames(), builder.Interval, builder.Loop, fs);
    }
}
