// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.


namespace MatPlotLibNet.Mcp.Tests;

/// <summary>The reader and the schema description the spec tests hold their specs to, made once, and how a test reads a section of the
/// schema an agent is shown.</summary>
internal static class SpecFixtures
{
    /// <summary>The spec reader, over every chart type, under the default render limits.</summary>
    public static ChartSpecReader Reader { get; } = new(new ChartTypeCatalog(), RenderLimits.Default);

    /// <summary>The description of the schema, over every chart type.</summary>
    public static ChartSchemaDescription Description { get; } = new(new ChartTypeCatalog());

    extension(ChartSchemaDescription description)
    {
        /// <summary>The lines under <paramref name="heading"/> in the schema, down to the first blank line.</summary>
        /// <param name="heading">A heading of the schema, such as <c>Axis link kinds</c>.</param>
        public string[] SectionLines(string heading)
        {
            string schema = description.Describe(null);
            int at = schema.IndexOf(heading, StringComparison.Ordinal);
            Assert.True(at >= 0, $"'{heading}' is not in the schema.");
            return [.. SourceFile.LinesOf(schema[at..]).Skip(1).TakeWhile(line => line.Trim().Length > 0)];
        }

        /// <summary>The lines under <paramref name="heading"/> in the schema, down to the first blank line, each without its indentation.</summary>
        /// <param name="heading">A heading of the schema, such as <c>Figure fields:</c>.</param>
        public string[] SectionFields(string heading) => [.. description.SectionLines(heading).Select(line => line.Trim())];

        /// <summary>The fields the schema lists for one kind under a heading: the lines indented under the kind's own line.</summary>
        /// <param name="heading">A heading of the schema, such as <c>Patch kinds</c>.</param>
        /// <param name="kind">The kind, such as <c>rectangle</c>.</param>
        public string[] FieldsListedUnder(string heading, string kind)
        {
            var lines = description.SectionLines(heading);
            int at = Array.FindIndex(lines, line => line.StartsWith($"  {kind} ", StringComparison.Ordinal));
            Assert.True(at >= 0, $"The schema lists no kind '{kind}' under '{heading}'.");
            return [.. lines.Skip(at + 1).TakeWhile(line => line.StartsWith("    ", StringComparison.Ordinal)).Select(line => line.FieldName())];
        }
    }

    extension(string schema)
    {
        /// <summary>The one line of the schema that starts with <paramref name="heading"/>: the heading of a section.</summary>
        /// <param name="heading">The start of the heading, such as <c>Legend fields</c>.</param>
        public string HeadingLine(string heading) => SourceFile.LinesOf(schema).Single(line => line.StartsWith(heading, StringComparison.Ordinal));

        /// <summary>The one line of the schema that describes <paramref name="field"/>: it starts with the field's name after its indentation.</summary>
        /// <param name="field">The start of the field's line, such as <c>packageData:</c>.</param>
        public string FieldLine(string field) => SourceFile.LinesOf(schema).Single(line => line.TrimStart().StartsWith(field, StringComparison.Ordinal));
    }

    extension(string line)
    {
        /// <summary>The name a schema line gives, the text before its colon.</summary>
        public string FieldName() => line.Trim().Split(':')[0];
    }
}
