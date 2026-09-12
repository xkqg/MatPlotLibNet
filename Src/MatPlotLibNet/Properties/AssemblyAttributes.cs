// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MatPlotLibNet.Tests")]
[assembly: InternalsVisibleTo("MatPlotLibNet.Benchmarks")]
// The MCP server validates a model-authored spec against the SAME DTO shape the serializer reads, and
// describes that shape to the model, so it reads the internal DTO records rather than keeping a copy.
[assembly: InternalsVisibleTo("MatPlotLibNet.Mcp")]
// The Skia package itemizes text into runs for the shaper with the bidi algorithm Core owns
// (Rendering/Text); the algorithm is a Unicode fact, not a public API of this library.
[assembly: InternalsVisibleTo("MatPlotLibNet.Skia")]
