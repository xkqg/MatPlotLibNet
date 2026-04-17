// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Transforms.Animation;

/// <summary>Declarative animation: define a frame generator function, produce an animated GIF
/// or individual PNG frames. Wraps the existing <see cref="GifTransform"/> and
/// <see cref="MatPlotLibNet.Transforms.PngTransform"/> infrastructure.</summary>
/// <example>
/// <code>
/// var anim = new FuncAnimation(60, i =>
/// {
///     double t = i / 60.0 * 2 * Math.PI;
///     return Plt.Create().Plot(x, x.Select(v => Math.Sin(v + t)).ToArray()).Build();
/// });
/// anim.Save("wave.gif");
/// </code>
/// </example>
public sealed class FuncAnimation
{
    private readonly int _frameCount;
    private readonly Func<int, Figure> _frameGenerator;
    private readonly int _delayMs;
    private readonly bool _loop;

    /// <summary>Creates a declarative animation.</summary>
    /// <param name="frameCount">Total number of frames to generate.</param>
    /// <param name="frameGenerator">Function mapping frame index (0-based) to a <see cref="Figure"/>.</param>
    /// <param name="delayMs">Delay between frames in milliseconds. Default 50ms (20fps).</param>
    /// <param name="loop">Whether the animation loops. Default <c>true</c>.</param>
    public FuncAnimation(int frameCount, Func<int, Figure> frameGenerator, int delayMs = 50, bool loop = true)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(frameCount, 0);
        ArgumentNullException.ThrowIfNull(frameGenerator);

        _frameCount = frameCount;
        _frameGenerator = frameGenerator;
        _delayMs = delayMs;
        _loop = loop;
    }

    /// <summary>Saves the animation to a file. Format is determined by extension (.gif or .png for frames).</summary>
    public void Save(string filePath)
    {
        using var stream = File.Create(filePath);
        SaveGif(stream);
    }

    /// <summary>Saves the animation as an animated GIF to the specified stream.</summary>
    public void SaveGif(Stream output)
    {
        var figures = GenerateFrames();
        var gif = new GifTransform();
        gif.Transform(figures, TimeSpan.FromMilliseconds(_delayMs), _loop, output);
    }

    /// <summary>Saves each frame as an individual PNG file in the specified directory.</summary>
    /// <param name="directory">Output directory. Created if it doesn't exist.</param>
    /// <param name="prefix">Filename prefix. Frames are named <c>{prefix}_0001.png</c>, etc.</param>
    public void SaveFrames(string directory, string prefix = "frame")
    {
        Directory.CreateDirectory(directory);
        var png = new PngTransform();

        for (int i = 0; i < _frameCount; i++)
        {
            var figure = _frameGenerator(i);
            var path = Path.Combine(directory, $"{prefix}_{i + 1:D4}.png");
            using var stream = File.Create(path);
            png.Transform(figure, stream);
        }
    }

    private IEnumerable<Figure> GenerateFrames()
    {
        for (int i = 0; i < _frameCount; i++)
            yield return _frameGenerator(i);
    }
}
