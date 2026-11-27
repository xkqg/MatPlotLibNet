// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Mcp;

/// <summary>A figure record as the library's serializer reads it: its fields under their wire names (the serializer's naming
/// policy and attributes, the internal members it carries) and, for a described family, the kinds its <c>type</c> field names.
/// The seats the reader walks (<see cref="AxesSeatShape"/>), the schema the server describes (<see cref="ChartSchemaDescription"/>)
/// and the vocabulary it checks enum values by (<see cref="Mcp.ChartSpecReader.SpecVocabulary"/>) read the records through here, so none of them holds
/// a second copy of a wire name.</summary>
internal static class WireContractExtensions
{
    /// <summary>A copy of the serializer's read options that carries the default resolver: the serializer's own instance takes its
    /// resolver only when it first reads a document, and asking it for a record's members before that fails. The copy keeps its
    /// naming policy, so a member's name is the one the serializer reads it by.</summary>
    private static readonly JsonSerializerOptions Wire = new(ChartSerializer.ReadOptions) { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };

    extension(Type record)
    {
        /// <summary>The record's fields as the serializer reads them, each with its wire name and its type.</summary>
        internal IList<JsonPropertyInfo> WireFields => Wire.GetTypeInfo(record).Properties;

        /// <summary>The fields the record declares itself, without those of the layers above it: a layer of a stacked family,
        /// whose base layers are listed once rather than under every kind.</summary>
        internal IEnumerable<JsonPropertyInfo> DeclaredWireFields() =>
            record.WireFields.Where(field => ((MemberInfo)field.AttributeProvider!).DeclaringType == record);

        /// <summary>The kinds of a described family: the <c>JsonDerivedType</c> attributes of its base record, the one list the
        /// serializer reads the <c>type</c> field by.</summary>
        internal IEnumerable<JsonDerivedTypeAttribute> WireKinds => record.GetCustomAttributes<JsonDerivedTypeAttribute>();
    }
}
