// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests;

/// <summary>A figure written to JSON and read back: the one way a test sends a figure through the document, so every test that
/// holds what the JSON carries reads it through the same serializer, never the replaceable process-wide one a test may swap.</summary>
internal static class FigureRoundTripExtensions
{
    private static readonly ChartSerializer Serializer = new();

    extension(Figure figure)
    {
        /// <summary>The figure its JSON reads back as.</summary>
        public Figure RoundTripped() => Serializer.FromJson(Serializer.ToJson(figure));
    }
}
