// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace MatPlotLibNet.Fidelity;

/// <summary>The numbers a matplotlib reference picture was drawn from, as <c>tools/mpl_reference/generate.py</c> wrote them to
/// <c>Fixtures/Matplotlib/data/&lt;chart&gt;.json</c>. A check that draws the picture with this library reads them here, so the
/// two pictures show the same data: numpy's random generator and <see cref="Random"/> do not agree, and a score between two
/// different point clouds measures the data, not the renderer.</summary>
public sealed class ReferenceData
{
    private static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Matplotlib", "data");

    private readonly Dictionary<string, JsonElement> _arrays;
    private readonly string _source;

    private ReferenceData(Dictionary<string, JsonElement> arrays, string source)
    {
        _arrays = arrays;
        _source = source;
    }

    /// <summary>Reads the numbers of one chart.</summary>
    /// <param name="chart">The name of the reference picture, such as <c>scatter</c>.</param>
    /// <exception cref="FileNotFoundException">The generator has not written numbers for that chart.</exception>
    public static ReferenceData Load(string chart)
    {
        string path = Path.Combine(Folder, $"{chart}.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"The numbers of the reference '{chart}' are not at '{path}'.\n" +
                $"Run: python tools/mpl_reference/generate.py --chart {chart}", path);
        }

        return Parse(File.ReadAllText(path), $"{chart}.json");
    }

    /// <summary>Reads numbers from the text of a data file.</summary>
    /// <param name="json">The text of the file: an object whose members are arrays.</param>
    /// <param name="source">What to call the file in a message.</param>
    public static ReferenceData Parse(string json, string source)
    {
        using var document = JsonDocument.Parse(json);
        var arrays = document.RootElement.EnumerateObject()
            .ToDictionary(member => member.Name, member => member.Value.Clone(), StringComparer.Ordinal);
        return new ReferenceData(arrays, source);
    }

    /// <summary>An array of numbers.</summary>
    /// <param name="name">The name the generator gave the array.</param>
    public double[] Vector(string name) => Numbers(Array(name), name);

    /// <summary>An array of rows of numbers, all of one length.</summary>
    /// <param name="name">The name the generator gave the array.</param>
    public double[,] Matrix(string name)
    {
        double[][] rows = Rows(name);
        if (rows.Length == 0)
        {
            throw new InvalidOperationException($"'{name}' in {_source} has no rows.");
        }

        int width = rows[0].Length;
        var matrix = new double[rows.Length, width];
        for (int r = 0; r < rows.Length; r++)
        {
            if (rows[r].Length != width)
            {
                throw new InvalidOperationException($"'{name}' in {_source} has rows of different lengths ({width} and {rows[r].Length}).");
            }

            for (int c = 0; c < width; c++)
            {
                matrix[r, c] = rows[r][c];
            }
        }

        return matrix;
    }

    /// <summary>An array of rows of numbers that may differ in length.</summary>
    /// <param name="name">The name the generator gave the array.</param>
    public double[][] Rows(string name) =>
        [.. Array(name).EnumerateArray().Select(row => Numbers(row, name, "rows of numbers"))];

    /// <summary>An array of texts.</summary>
    /// <param name="name">The name the generator gave the array.</param>
    public string[] Strings(string name) =>
        [.. Array(name).EnumerateArray().Select(item => item.ValueKind == JsonValueKind.String
            ? item.GetString()!
            : throw new InvalidOperationException($"'{name}' in {_source} holds {item.ValueKind}, not text."))];

    private JsonElement Array(string name)
    {
        if (!_arrays.TryGetValue(name, out var element))
        {
            throw new KeyNotFoundException(
                $"'{name}' is not in {_source}, which holds: {string.Join(", ", _arrays.Keys)}. " +
                "The generator names the arrays in plotted(...); run tools/mpl_reference/generate.py for the chart.");
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException($"'{name}' in {_source} is {element.ValueKind}, not an array.");
        }

        return element;
    }

    private double[] Numbers(JsonElement array, string name, string expected = "numbers")
    {
        if (array.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException($"'{name}' in {_source} does not hold {expected}: it holds {array.ValueKind}.");
        }

        var numbers = new List<double>();
        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Number)
            {
                throw new InvalidOperationException($"'{name}' in {_source} does not hold {expected}: it holds {item.ValueKind}.");
            }

            numbers.Add(item.GetDouble());
        }

        return [.. numbers];
    }
}
