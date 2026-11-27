// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json.Nodes;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Tests.Models.Series;
using MatPlotLibNet.Tests.Packaging;
using Xunit.Sdk;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>Input that cannot be drawn is refused where it is built, not found when the figure is drawn. The probe takes every
/// series type the library ships, writes it to JSON and damages it, reads the figure back and draws it: each array in five ways
/// (shorter by one, empty, absent, ragged, transposed) and each number at its edges (zero, below zero). A figure that reads back
/// and then throws while it draws is a series that did not check its input when it was built. The failure message names each
/// such case, so it is the list of what is left to refuse and the guard that no new series type slips by.</summary>
public class SeriesInputRefusalTests
{
    /// <summary>The JSON properties that stand for arrays, and each way of damaging one.</summary>
    private static IEnumerable<(string Variant, JsonNode? Damaged)> Damage(JsonArray original)
    {
        // Every damage works on a copy: the original is read again for the next variant.
        JsonArray Copy() => (JsonArray)JsonNode.Parse(original.ToJsonString())!;

        if (original.Count >= 2)
        {
            var shorter = Copy();
            shorter.RemoveAt(shorter.Count - 1);
            yield return ("shorter", shorter);
        }

        yield return ("empty", new JsonArray());
        yield return ("absent", null);

        if (original.Count >= 1 && original[0] is JsonArray first && first.Count >= 2)
        {
            var ragged = Copy();
            ((JsonArray)ragged[0]!).RemoveAt(((JsonArray)ragged[0]!).Count - 1);
            yield return ("ragged", ragged);

            int rows = original.Count, columns = first.Count;
            if (original.All(row => row is JsonArray cells && cells.Count == columns))
            {
                var transposed = new JsonArray();
                for (int c = 0; c < columns; c++)
                {
                    var row = new JsonArray();
                    for (int r = 0; r < rows; r++) row.Add(JsonNode.Parse(((JsonArray)original[r]!)[c]!.ToJsonString()));
                    transposed.Add(row);
                }
                yield return ("transposed", transposed);
            }
        }
    }

    /// <summary>The JSON properties that stand for a single number, and each edge value to put there.</summary>
    private static IEnumerable<(string Variant, JsonNode? Damaged)> Edges()
    {
        yield return ("zero", JsonValue.Create(0));
        yield return ("below zero", JsonValue.Create(-1));
    }

    private static bool IsNumber(JsonNode? node) => node is JsonValue value && value.TryGetValue<double>(out _);

    /// <summary>Every series type the library ships, twice (<see cref="DrawnSeriesFixtures.Build"/>): every property at rest, so the
    /// JSON leaves them out, and every property it draws given, so a number the JSON writes only when it is given (a donut's hole, its
    /// start angle) is damaged too.</summary>
    private static List<(string Type, MatPlotLibNet.Models.Figure Figure)> Shipped() =>
        [.. DrawnSeriesFixtures.DrawnTypes.Select(type => (type.Name, DrawnSeriesFixtures.Build(type.Name, everyPropertySet: false))),
         .. DrawnSeriesFixtures.DrawnTypes.Select(type => (type.Name + " (every property given)", DrawnSeriesFixtures.Build(type.Name, everyPropertySet: true)))];

    /// <summary>Damages every property <paramref name="damagedProperties"/> picks of every shipped series, reads the figure back
    /// and draws it.</summary>
    /// <returns>How many figures were damaged, and each one that was read back and then threw while it was drawn.</returns>
    private static (int Probed, List<string> ThrewWhileDrawing) Probe(
        Func<JsonObject, IEnumerable<(string Property, string Variant, JsonNode? Damaged)>> damagedProperties)
    {
        var serializer = new ChartSerializer();
        var threwWhileDrawing = new List<string>();
        int probed = 0;

        foreach (var (type, figure) in Shipped())
        {
            var root = JsonNode.Parse(serializer.ToJson(figure))!;
            if (root["subPlots"]?[0]?["series"]?[0] is not JsonObject written) continue;

            foreach (var (property, variant, damaged) in damagedProperties(written))
            {
                var copy = (JsonObject)JsonNode.Parse(written.ToJsonString())!;
                copy.Remove(property);
                if (damaged is not null) copy[property] = damaged.DeepClone();
                var damagedRoot = JsonNode.Parse(root.ToJsonString())!;
                damagedRoot["subPlots"]![0]!["series"]![0] = copy;
                probed++;

                MatPlotLibNet.Models.Figure read;
                try { read = serializer.FromJson(damagedRoot.ToJsonString()); }
                catch { continue; }   // refused when it was read: where it belongs

                try { read.ToSvg(); }
                catch (Exception ex) { threwWhileDrawing.Add($"{type}.{property} {variant}: {ex.GetType().Name}: {SourceFile.LinesOf(ex.Message)[0]}"); }
            }
        }

        return (probed, threwWhileDrawing);
    }

    private static string Listed(List<string> threwWhileDrawing, string taken = "read back") =>
        $"{threwWhileDrawing.Count} figure(s) {taken} and then threw while they were drawn:\n  " + string.Join("\n  ", threwWhileDrawing.Order(StringComparer.Ordinal));

    [Fact]
    public void NoFigureThatReadsBack_ThrowsWhileItIsDrawn()
    {
        var (probed, threwWhileDrawing) = Probe(written =>
            written.Where(p => p.Value is JsonArray).Select(p => p.Key).ToList()
                .SelectMany(property => Damage((JsonArray)written[property]!).Select(d => (property, d.Variant, d.Damaged))));

        Assert.True(probed > 400, $"the probe damaged only {probed} arrays");
        Assert.True(threwWhileDrawing.Count == 0, Listed(threwWhileDrawing));
    }

    [Fact]
    public void NoFigureWithANumberAtItsEdge_ThrowsWhileItIsDrawn()
    {
        var (probed, threwWhileDrawing) = Probe(written =>
            written.Where(p => IsNumber(p.Value)).Select(p => p.Key).ToList()
                .SelectMany(property => Edges().Select(e => (property, e.Variant, e.Damaged))));

        Assert.True(probed > 100, $"the probe damaged only {probed} numbers");
        Assert.True(threwWhileDrawing.Count == 0, Listed(threwWhileDrawing));
    }

    /// <summary>The values a setter is handed in place of the one the every-property-given instance holds: an array shorter by one
    /// and an empty one, a whole number at zero and below zero.</summary>
    private static IEnumerable<(string Variant, object Damaged)> SetterDamage(PropertyInfo property, object? current)
    {
        if (property.PropertyType.IsArray && current is Array { Length: >= 1 } array)
        {
            var element = property.PropertyType.GetElementType()!;
            if (array.Length >= 2)
            {
                var shorter = Array.CreateInstance(element, array.Length - 1);
                Array.Copy(array, shorter, shorter.Length);
                yield return ("shorter", shorter);
            }

            yield return ("empty", Array.CreateInstance(element, 0));
        }
        else if (property.PropertyType == typeof(int) || property.PropertyType == typeof(int?))
        {
            yield return ("zero", 0);
            yield return ("below zero", -1);
        }
    }

    /// <summary>The same rule through the setters a caller writes: a figure read from JSON builds a series through its verb, which
    /// checks what the verb takes, but a property set afterwards (a scatter's sizes, a bar's feet, the second curve of a fill, a point
    /// budget) is checked by its setter or by nobody. Every series type, with every property it draws given, has each of its arrays
    /// and whole numbers set to an edge and is drawn: a setter that takes the value and a draw that then throws is the case listed.</summary>
    [Fact]
    public void NoSeriesGivenAnEdgeThroughItsSetter_ThrowsWhileItIsDrawn()
    {
        var threwWhileDrawing = new List<string>();
        int probed = 0;
        foreach (var type in DrawnSeriesFixtures.DrawnTypes)
        {
            var settable = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.SetMethod is { IsPublic: true } && p.GetIndexParameters().Length == 0);
            foreach (var property in settable)
            {
                var current = property.GetValue(DrawnSeriesFixtures.Build(type.Name, everyPropertySet: true).SubPlots[0].Series[0]);
                foreach (var (variant, damaged) in SetterDamage(property, current))
                {
                    var figure = DrawnSeriesFixtures.Build(type.Name, everyPropertySet: true);
                    probed++;
                    try { property.SetValue(figure.SubPlots[0].Series[0], damaged); }
                    catch (TargetInvocationException refused) when (refused.InnerException is ArgumentException) { continue; }   // refused where it was given

                    try { figure.ToSvg(); }
                    catch (Exception ex) { threwWhileDrawing.Add($"{type.Name}.{property.Name} {variant}: {ex.GetType().Name}: {SourceFile.LinesOf(ex.Message)[0]}"); }
                }
            }
        }

        Assert.True(probed > 300, $"the probe damaged only {probed} values");
        Assert.True(threwWhileDrawing.Count == 0, Listed(threwWhileDrawing, "given the value by a setter"));
    }
}
