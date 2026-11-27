// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        //  Verifies that ToJson produces valid JSON without calling Build().
        new DrawnToken("ToJson_WithoutBuild_ProducesValidJson", () => Plt.Create()
            .WithTitle("JSON Test")
            .Plot([1.0], [2.0])
            .ToJson(), "JSON Test"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>Verifies that Save creates an SVG file without calling Build().</summary>
    [Fact]
    public void Save_WithoutBuild_CreatesFile()
    {
        string path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.svg");
        try
        {
            Plt.Create()
                .Plot([1.0, 2.0], [3.0, 4.0])
                .Save(path);

            Assert.True(File.Exists(path));
            string content = File.ReadAllText(path);
            Assert.Contains("<svg", content);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    /// <summary>Verifies that Transform on Figure produces a TransformResult.</summary>
    [Fact]
    public void Transform_OnFigure_ProducesResult()
    {
        var result = Plt.Create()
            .Plot([1.0], [2.0])
            .Build()
            .Transform(new Transforms.SvgTransform());

        Assert.NotNull(result);
        byte[] bytes = result.ToBytes();
        Assert.True(bytes.Length > 0);
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
