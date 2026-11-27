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
// The Skia suite holds what the Skia render context answers to the pixel-grid ask the core renderers make of any context
// (Rendering/IPixelGridRenderContext), which is an interface between the two packages and not a public API.
[assembly: InternalsVisibleTo("MatPlotLibNet.Skia.Tests")]
// A chart session applies the events a client sends and counts the ones the figure could not take (an event for a
// subplot it lacks); how an event ended (FigureInteractionEvent.TryApplyTo) is an answer between the two packages that
// ship together, not a public API.
[assembly: InternalsVisibleTo("MatPlotLibNet.AspNetCore")]
// A map series writes its colour map and its normaliser by the description every colour-mapped series uses
// (Serialization/ColorMapDescriptionExtensions, NormalizableExtensions) and leaves a value at rest out the way every
// series does (RestingValueExtensions), rather than keeping a second copy of any of them in the package that ships with this one.
[assembly: InternalsVisibleTo("MatPlotLibNet.Geo")]
// The data-frame package numbers the hue labels of a pair grid by the library's one rule (HueGrouper.Codes: the order the labels
// are first seen) instead of keeping a second numbering of its own.
[assembly: InternalsVisibleTo("MatPlotLibNet.DataFrame")]
// The Geo suite resets the process-wide series registry (SeriesRegistry.ResetForTestsInternal) to hold that the package's
// reader comes back when it registers again; a reset of the registry is test infrastructure, not a public API.
[assembly: InternalsVisibleTo("MatPlotLibNet.Geo.Tests")]
// The routing contract of the server package (the hub, the sink of a native control and the script of a page spell the same
// hub method names) is held by its own test project, which reads the names from where they are written.
[assembly: InternalsVisibleTo("MatPlotLibNet.AspNetCore.Tests")]
// A native control paints through the one paint of the library (Rendering/Frames: the frame target, the brush band), which
// draws and lays out a figure in one pass; MAUI draws with its own graphics context, the other heads through the Skia package.
[assembly: InternalsVisibleTo("MatPlotLibNet.Maui")]
// The MAUI suite compiles the view's sources into its own assembly (the package itself needs the platform workloads).
[assembly: InternalsVisibleTo("MatPlotLibNet.Maui.Tests")]
// Every native control follows a streaming figure through the one follower (Interaction/StreamingFigureFollower) and turns
// its platform's input into the neutral records by the one set of rules (Interaction/InputArgs/NativeInputExtensions); how a
// control is wired is not a public API. MAUI is named above.
[assembly: InternalsVisibleTo("MatPlotLibNet.Wpf")]
[assembly: InternalsVisibleTo("MatPlotLibNet.Avalonia")]
[assembly: InternalsVisibleTo("MatPlotLibNet.Uno")]
// The Uno suite compiles the package's input adapter into its own assembly (the elements need Uno's build pipeline).
[assembly: InternalsVisibleTo("MatPlotLibNet.Uno.Tests")]
// The Blazor chart component follows its animation source through the same follower as the native controls
// (Interaction/AnimationSourceFollower); how a control follows a source is not a public API.
[assembly: InternalsVisibleTo("MatPlotLibNet.Blazor")]
