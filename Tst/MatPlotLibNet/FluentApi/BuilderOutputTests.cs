// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.FluentApi;

/// <summary>Verifies that <see cref="FigureBuilder"/> output methods work without explicit Build().</summary>
public class BuilderOutputTests
{
    /// <summary>Verifies that ToSvg produces valid SVG without calling Build().</summary>
    [Fact]
    public void ToSvg_WithoutBuild_ProducesValidSvg()
    {
        string svg = Plt.Create()
            .WithTitle("No Build")
            .Plot([1.0, 2.0, 3.0], [10.0, 20.0, 15.0])
            .ToSvg();

        Assert.StartsWith("<svg", svg.TrimStart());
        Assert.Contains("No Build", svg);
    }

    /// <summary>Verifies that ToJson produces valid JSON without calling Build().</summary>
    [Fact]
    public void ToJson_WithoutBuild_ProducesValidJson()
    {
        string json = Plt.Create()
            .WithTitle("JSON Test")
            .Plot([1.0], [2.0])
            .ToJson();

        Assert.Contains("JSON Test", json);
    }

    /// <summary>Verifies that SaveSvg creates a file without calling Build().</summary>
    [Fact]
    public void SaveSvg_WithoutBuild_CreatesFile()
    {
        string path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.svg");
        try
        {
            Plt.Create()
                .Plot([1.0, 2.0], [3.0, 4.0])
                .SaveSvg(path);

            Assert.True(File.Exists(path));
            string content = File.ReadAllText(path);
            Assert.Contains("<svg", content);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    /// <summary>Verifies that Transform produces a TransformResult without calling Build().</summary>
    [Fact]
    public void Transform_WithoutBuild_ProducesResult()
    {
        var result = Plt.Create()
            .Plot([1.0], [2.0])
            .Transform(new Transforms.SvgTransform());

        Assert.NotNull(result);
        byte[] bytes = result.ToBytes();
        Assert.True(bytes.Length > 0);
    }

    /// <summary>Verifies that subplots work with direct ToSvg without Build().</summary>
    [Fact]
    public void ToSvg_WithSubPlots_WithoutBuild()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.Plot([1.0], [2.0]))
            .AddSubPlot(1, 2, 2, ax => ax.Scatter([1.0], [2.0]))
            .ToSvg();

        Assert.Contains("<svg", svg);
    }

    /// <summary>Verifies that Save auto-detects SVG from .svg extension.</summary>
    [Fact]
    public void Save_SvgExtension_CreatesSvgFile()
    {
        string path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.svg");
        try
        {
            Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Save(path);

            Assert.True(File.Exists(path));
            Assert.Contains("<svg", File.ReadAllText(path));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    /// <summary>Verifies that Save auto-detects JSON from .json extension.</summary>
    [Fact]
    public void Save_JsonExtension_CreatesJsonFile()
    {
        string path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.json");
        try
        {
            Plt.Create().Plot([1.0], [2.0]).Save(path);

            Assert.True(File.Exists(path));
            Assert.Contains("\"type\"", File.ReadAllText(path));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
