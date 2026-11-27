// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Fidelity;

/// <summary>The numbers a reference picture was drawn from, read back by the check that draws the same picture with this library.</summary>
public class ReferenceDataTests
{
    private static readonly string DataFolder = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Matplotlib", "data");

    [Fact]
    public void Vector_ReadsEveryNumberExactly()
    {
        var data = ReferenceData.Parse("{\n\"x\":[0.30000000000000004,-1.5,2]\n}", "test");

        Assert.Equal([0.30000000000000004, -1.5, 2.0], data.Vector("x"));
    }

    [Fact]
    public void Matrix_ReadsRowsAndColumns()
    {
        var data = ReferenceData.Parse("{\"m\":[[1,2,3],[4,5,6]]}", "test");

        double[,] matrix = data.Matrix("m");

        Assert.Equal(2, matrix.GetLength(0));
        Assert.Equal(3, matrix.GetLength(1));
        Assert.Equal(4.0, matrix[1, 0]);
        Assert.Equal(3.0, matrix[0, 2]);
    }

    [Fact]
    public void Rows_KeepRowsOfDifferentLengths()
    {
        var data = ReferenceData.Parse("{\"r\":[[1,2,3],[4,5]]}", "test");

        double[][] rows = data.Rows("r");

        Assert.Equal([1.0, 2.0, 3.0], rows[0]);
        Assert.Equal([4.0, 5.0], rows[1]);
    }

    [Fact]
    public void Strings_ReadsEveryText()
    {
        var data = ReferenceData.Parse("{\"c\":[\"A\",\"B\",\"A\"]}", "test");

        Assert.Equal(["A", "B", "A"], data.Strings("c"));
    }

    [Fact]
    public void AName_TheFileDoesNotHold_NamesTheOnesItDoes()
    {
        var data = ReferenceData.Parse("{\"x\":[1],\"y\":[2]}", "scatter.json");

        var ex = Assert.Throws<KeyNotFoundException>(() => data.Vector("z"));

        Assert.Contains("'z'", ex.Message);
        Assert.Contains("scatter.json", ex.Message);
        Assert.Contains("x, y", ex.Message);
    }

    [Theory]
    [InlineData("{\"m\":[[1,2,3],[4,5]]}", "Matrix", "m", "rows of different lengths")]
    [InlineData("{\"m\":[]}", "Matrix", "m", "no rows")]
    [InlineData("{\"m\":[[1,2],[3,4]]}", "Vector", "m", "'m'")]
    [InlineData("{\"x\":[1,2,3]}", "Matrix", "x", "'x'")]
    [InlineData("{\"x\":[1,2,3]}", "Strings", "x", "'x'")]
    public void AReadTheFileCannotAnswer_IsRefused_AndNamesWhatIsWrong(string json, string read, string name, string said)
    {
        var data = ReferenceData.Parse(json, "test");

        var ex = Assert.Throws<InvalidOperationException>(() => read switch
        {
            "Matrix" => (object)data.Matrix(name),
            "Vector" => data.Vector(name),
            _ => data.Strings(name),
        });

        Assert.Contains(said, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_OfAChartWithNoDataFile_SaysHowToMakeOne()
    {
        var ex = Assert.Throws<FileNotFoundException>(() => ReferenceData.Load("no_such_chart"));

        Assert.Contains("generate.py --chart no_such_chart", ex.Message);
    }

    [Fact]
    public void Load_ReadsTheFileTheGeneratorWrote()
    {
        var scatter = ReferenceData.Load("scatter");

        Assert.Equal(80, scatter.Vector("circles_x").Length);
    }

    [Fact]
    public void EveryDataFile_ParsesAndBelongsToAReferencePictureInBothStyles()
    {
        var files = Directory.GetFiles(DataFolder, "*.json");
        Assert.NotEmpty(files);

        foreach (var file in files)
        {
            string chart = Path.GetFileNameWithoutExtension(file);
            Assert.NotNull(ReferenceData.Load(chart));
            foreach (var style in new[] { "classic", "v2" })
            {
                Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Matplotlib", style, $"{chart}.png")),
                    $"data/{chart}.json has no reference picture {style}/{chart}.png");
            }
        }
    }
}
