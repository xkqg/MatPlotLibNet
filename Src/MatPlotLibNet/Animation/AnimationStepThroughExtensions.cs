// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Animation;

/// <summary>Writes an <see cref="AnimationBuilder"/> as an SVG that steps through its frames (<see cref="StepThroughSvgTransform"/>),
/// at the builder's <see cref="AnimationBuilder.Interval"/> and with its <see cref="AnimationBuilder.Loop"/>. The file is as
/// large as all its frames together.</summary>
public static class AnimationStepThroughExtensions
{
    extension(AnimationBuilder builder)
    {
        /// <summary>The animation as a step-through SVG document.</summary>
        /// <returns>The SVG text.</returns>
        public string ToStepThroughSvg()
        {
            using var stream = new MemoryStream();
            new StepThroughSvgTransform().Transform(builder.GenerateFrames(), builder.Interval, builder.Loop, stream);
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        /// <summary>Saves the animation as a step-through SVG, replacing a file that is there.</summary>
        /// <param name="path">Destination file path (e.g. <c>"animation.svg"</c>).</param>
        public void SaveStepThroughSvg(string path)
        {
            using var file = File.Create(path);
            new StepThroughSvgTransform().Transform(builder.GenerateFrames(), builder.Interval, builder.Loop, file);
        }
    }
}
