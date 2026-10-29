// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Tests;

/// <summary>What <c>Figure.Save(path)</c> does with an extension the library knows a format for but cannot write on its
/// own: core writes SVG and JSON, and <c>.png</c> / <c>.pdf</c> need the exporters of <c>MatPlotLibNet.Skia</c>, <c>.gif</c>
/// is an animation. Before, those saves wrote an SVG document under a raster or PDF name and said nothing. This process
/// never loads Skia, so every row below is the "no exporter" case. The transform registry is process-global, so the class
/// joins the collection that keeps it away from the render tests.</summary>
[Collection("ChartServicesGlobalState")]
public class SaveWithoutAnExporterTests
{
    private static Figure Chart() =>
        Plt.Create().WithSize(500, 350).WithTitle("Revenue").Plot([0.0, 1.0, 2.0], [1.0, 3.0, 2.0]).Build();

    private static string TempPath(string fileName) =>
        Path.Combine(Path.GetTempPath(), $"s10_{Guid.NewGuid():N}_{fileName}");

    private static void Cleanup(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

    /// <summary>Measured before the change: <c>Save("a.png")</c> wrote a file that starts with <c>&lt;svg</c>, so a viewer
    /// opened a PNG that was not one.</summary>
    [Theory]
    [InlineData("a.png")]
    [InlineData("a.PNG")]
    [InlineData("a.Png")]
    [InlineData("a.pdf")]
    [InlineData("a.Pdf")]
    public void APngOrPdfSave_WithoutSkia_IsRefused_NamingThePackage(string fileName)
    {
        string path = TempPath(fileName);
        try
        {
            var ex = Assert.Throws<InvalidOperationException>(() => Chart().Save(path));

            Assert.Contains("MatPlotLibNet.Skia", ex.Message);
            Assert.False(File.Exists(path), "a refused save must not leave a file behind");
        }
        finally { Cleanup(path); }
    }

    [Fact]
    public void ARefusedSave_WithSaveOptions_IsRefusedTheSameWay()
    {
        string path = TempPath("a.png");
        try
        {
            var ex = Assert.Throws<InvalidOperationException>(() => Chart().Save(path, new SaveOptions { Dpi = 300 }));

            Assert.Contains("MatPlotLibNet.Skia", ex.Message);
            Assert.False(File.Exists(path));
        }
        finally { Cleanup(path); }
    }

    [Fact]
    public void AGifSave_IsRefused_NamingTheAnimationPath()
    {
        string path = TempPath("a.gif");
        try
        {
            var ex = Assert.Throws<InvalidOperationException>(() => Chart().Save(path));

            Assert.Contains("SaveGif", ex.Message);
            Assert.Contains("MatPlotLibNet.Skia", ex.Message);
            Assert.False(File.Exists(path));
        }
        finally { Cleanup(path); }
    }

    /// <summary>The refusal comes before the file is opened, so a file that is already there survives a save that cannot
    /// succeed (the documented rule of <c>Save</c>: the file is written only when the export has succeeded).</summary>
    [Fact]
    public void ARefusedSave_KeepsTheFileThatWasThere()
    {
        string path = TempPath("keep.png");
        File.WriteAllText(path, "the earlier file");
        try
        {
            Assert.Throws<InvalidOperationException>(() => Chart().Save(path));

            Assert.Equal("the earlier file", File.ReadAllText(path));
        }
        finally { Cleanup(path); }
    }

    /// <summary>The refusal is only the registry's starting entry: a transform an application registers for .png (its own
    /// exporter, or Skia's) replaces it. The test puts the refusal back, so no later test sees its recorder.</summary>
    [Fact]
    public void ATransformRegisteredForPng_ReplacesTheRefusal()
    {
        string path = TempPath("a.png");
        var recorder = new RecordingTransform();
        FigureExtensions.RegisterTransform(".png", recorder);
        try
        {
            Chart().Save(path);

            Assert.Equal(1, recorder.Calls);
            Assert.Equal("recorded", File.ReadAllText(path));
        }
        finally
        {
            FigureExtensions.RegisterTransform(".png", RefusingTransform.WithoutSkia(".png"));
            Cleanup(path);
        }
    }

    private sealed class RecordingTransform : IFigureTransform
    {
        public int Calls { get; private set; }

        public void Transform(Figure figure, Stream output)
        {
            Calls++;
            output.Write("recorded"u8);
        }
    }

    // ── what stays as it was ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("a.svg")]
    [InlineData("a.xyz")]
    [InlineData("a.chart")]
    public void ASvgOrAnUnknownExtension_StillWritesAnSvg(string fileName)
    {
        string path = TempPath(fileName);
        try
        {
            Chart().Save(path);

            Assert.StartsWith("<svg", File.ReadAllText(path));
        }
        finally { Cleanup(path); }
    }

    [Fact]
    public void APathWithNoExtension_GetsSvgAdded()
    {
        string path = TempPath("noextension");
        try
        {
            Chart().Save(path);

            Assert.StartsWith("<svg", File.ReadAllText(path + ".svg"));
        }
        finally { Cleanup(path); Cleanup(path + ".svg"); }
    }

    [Fact]
    public void AJsonSave_StillWritesTheFigureAsJson()
    {
        string path = TempPath("a.json");
        try
        {
            var figure = Chart();
            figure.Save(path);

            Assert.Equal(figure.ToJson(), File.ReadAllText(path));
        }
        finally { Cleanup(path); }
    }

    [Fact]
    public void SaveOptions_OnAnUnknownExtension_StillWriteAnSvg()
    {
        string path = TempPath("a.xyz");
        try
        {
            Chart().Save(path, new SaveOptions { Dpi = 300, Title = "T" });

            Assert.StartsWith("<svg", File.ReadAllText(path));
        }
        finally { Cleanup(path); }
    }
}
