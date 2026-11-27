// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Animation;
using MatPlotLibNet.Transforms;
using MatPlotLibNet.Transforms.Animation;

namespace MatPlotLibNet.Skia;

/// <summary>What the Skia package writes of an <see cref="AnimationBuilder"/>: an animated GIF, and one PNG per frame. The SVG that
/// steps through the frames is written by the core (<see cref="AnimationStepThroughExtensions"/>).</summary>
public static class AnimationSkiaExtensions
{
    private static readonly GifTransform GifTransformInstance = new();

    extension(AnimationBuilder builder)
    {
        /// <summary>Encodes all frames as an animated GIF and returns the bytes.</summary>
        /// <returns>GIF89a bytes.</returns>
        public byte[] ToGif()
        {
            using var ms = new MemoryStream();
            GifTransformInstance.Transform(builder.GenerateFrames(), builder.Interval, builder.Loop, ms);
            return ms.ToArray();
        }

        /// <summary>Encodes all frames as an animated GIF and saves it to <paramref name="path"/>, replacing a file that is there
        /// once the whole GIF is made: nothing of a longer file is left after it, and a frame that fails leaves that file as it
        /// was.</summary>
        /// <param name="path">Destination file path (e.g. <c>"animation.gif"</c>).</param>
        public void SaveGif(string path) =>
            path.ReplaceFileWith(file => GifTransformInstance.Transform(builder.GenerateFrames(), builder.Interval, builder.Loop, file));

        /// <summary>Saves each frame as a PNG of its own in <paramref name="directory"/>, named <c>{prefix}_0001.png</c>,
        /// <c>{prefix}_0002.png</c> and so on, each drawn at the resolution of its figure. Each file is replaced once its picture is
        /// made, so a frame that fails leaves the file that was there.</summary>
        /// <param name="directory">The directory the frames are written to; made when it is not there.</param>
        /// <param name="prefix">What the name of every frame's file starts with.</param>
        public void SaveFrames(string directory, string prefix = "frame")
        {
            Directory.CreateDirectory(directory);
            var png = new PngTransform();
            for (int i = 0; i < builder.FrameCount; i++)
            {
                var figure = builder.GenerateFrame(i);
                var path = Path.Combine(directory, string.Invariant($"{prefix}_{i + 1:D4}.png"));
                path.ReplaceFileWith(stream => png.Transform(figure, stream));
            }
        }
    }
}
