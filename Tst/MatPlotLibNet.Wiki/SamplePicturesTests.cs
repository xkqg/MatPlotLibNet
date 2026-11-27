// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace MatPlotLibNet.Wiki.Tests;

/// <summary>
/// The pictures the repository keeps in its <c>images</c> folder, and the wiki's copies of them, are the sample program's output.
/// A renderer change that moves a picture and does not draw it again leaves a picture here that shows a library that no longer
/// exists, and <see cref="WikiPicturesTests"/> cannot see that: it holds the wiki to the repository, and both can be equally old.
/// This runs the sample program into a folder of its own (<c>MPL_SAMPLES_OUT</c>, so the repository's folder is never written)
/// and holds every file it draws to the file the repository keeps, and every file the repository keeps to a file the program draws.
/// </summary>
/// <remarks>
/// <para>
/// Held on Windows only, the platform the repository's pictures are drawn on. Measured in the ubuntu 24.04 container, from the
/// committed tree: of the 313 pictures the program writes, none is the same file on both platforms. The 200 PNGs differ in 0.5 to
/// 1.5 % of their pixels, in small clusters where text stands (the two builds of Skia set a glyph differently). The 112 SVGs differ in the outline of every glyph, which
/// is the same shape written with another verb for a curve and with a line back to the start of a contour before its close.
/// The PDF differs as well. So no picture is held on every platform, and a Linux run would be a failure that
/// reports the platform, not a picture; the Fidelity suite is what judges a drawing on every platform, by score.
/// </para>
/// <para>
/// After a rendering change: run the sample program (<c>Samples/MatPlotLibNet.Samples.Console</c>) and copy the pictures that
/// changed over <c>images</c>, then over the wiki's <c>images</c>.
/// </para>
/// </remarks>
public sealed class SamplePicturesTests
{
    private const string TheSampleProgramIsHeldOnWindowsOnly =
        "The repository's pictures are drawn on Windows. No picture is byte-identical between Windows and Linux (the glyph outlines and the pixels differ), so another platform cannot be held to them.";

    private static string KeptPictures => Path.Join(RepositoryRoot.Folder, "images");

    [Fact]
    public void EveryPictureTheSampleProgramDraws_IsTheOneTheRepositoryKeeps()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), TheSampleProgramIsHeldOnWindowsOnly);

        string drawnIn = Path.Join(Path.GetTempPath(), "MatPlotLibNet.SamplePictures." + Guid.NewGuid().ToString("N"));

        try
        {
            DrawEveryPictureInto(drawnIn);

            string[] drawn = [.. Directory.GetFiles(Path.Join(drawnIn, "images")).Select(file => Path.GetFileName(file)).Order(StringComparer.Ordinal)];
            string[] kept = [.. Directory.GetFiles(KeptPictures).Select(file => Path.GetFileName(file)).Order(StringComparer.Ordinal)];

            Assert.True(drawn.Length > 0, "The sample program drew no picture.");
            string[] notKept = [.. drawn.Except(kept)];
            string[] notDrawn = [.. kept.Except(drawn)];
            string[] stale = [.. drawn.Intersect(kept).Where(name => !Path.Join(KeptPictures, name).IsTheSamePictureAs(Path.Join(drawnIn, "images", name)))];

            Assert.True(notKept.Length == 0, "The sample program draws pictures the repository's images folder does not keep: " + string.Join(", ", notKept) + ". Copy them in.");
            Assert.True(notDrawn.Length == 0, "The repository keeps pictures the sample program no longer draws: " + string.Join(", ", notDrawn) + ". Delete them here and from the wiki's images folder.");
            Assert.True(stale.Length == 0, "The repository keeps an older picture than the sample program draws: " + string.Join(", ", stale)
                + ". Run Samples/MatPlotLibNet.Samples.Console and copy the pictures it changed over images, then over the wiki's images.");
            Assert.True(Path.Join(RepositoryRoot.Folder, "icon.png").IsTheSamePictureAs(Path.Join(drawnIn, "icon.png")),
                "The package icon at the repository's root is not the one the sample program draws.");
        }
        finally
        {
            Directory.Delete(drawnIn, recursive: true);
        }
    }

    private static void DrawEveryPictureInto(string folder)
    {
        Directory.CreateDirectory(folder);

        string configuration = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar)).Parent!.Name;
        string program = Path.Join(RepositoryRoot.Folder, "Samples", "MatPlotLibNet.Samples.Console", "bin", configuration, "net10.0", "MatPlotLibNet.Samples.Console.exe");
        Assert.True(File.Exists(program), $"The sample program is not built at {program}. Build the {configuration} configuration of Samples/MatPlotLibNet.Samples.Console.");

        var start = new ProcessStartInfo(program) { WorkingDirectory = folder, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.Environment["MPL_SAMPLES_OUT"] = folder;

        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(TimeSpan.FromMinutes(10)))
        {
            process.Kill(entireProcessTree: true);
            Assert.Fail("The sample program did not finish within ten minutes.");
        }

        process.WaitForExit();
        Assert.True(process.ExitCode == 0, $"The sample program exited with {process.ExitCode}: {errors.Result}{Environment.NewLine}{output.Result}");
    }
}
