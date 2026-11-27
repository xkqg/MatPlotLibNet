// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Samples.Console;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>The package icon is drawn by the library, by the sample program, and committed as <c>icon.png</c>, which every package
/// packs. A renderer change that moves a pixel of the icon leaves the committed one showing a library that no longer exists, and
/// every run of the sample program then rewrites a tracked file. This holds the committed file to the drawing, so the icon is
/// redrawn in the same change that moves it.</summary>
public class PackageIconTests
{
    private static string CommittedIcon() => Path.Combine(RepositoryRoot.Folder, "icon.png");

    private static byte[] Drawn()
    {
        using var stream = new MemoryStream();
        PackageIcon.Build().Transform(new PngTransform()).ToStream(stream);
        return stream.ToArray();
    }

    [Fact]
    public void TheCommittedIcon_IsWhatTheSampleProgramDraws()
    {
        Assert.True(File.ReadAllBytes(CommittedIcon()).AsSpan().SequenceEqual(Drawn()),
            "icon.png is not what PackageIcon draws: run the sample program (Samples/MatPlotLibNet.Samples.Console) and commit the icon.");
    }

    [Fact]
    public void TheIcon_IsDrawnTheSameEveryTime()
    {
        Assert.True(Drawn().AsSpan().SequenceEqual(Drawn()));
    }
}
