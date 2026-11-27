// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

// The animation contract moved to the core, where the SVG that steps through its frames is written by it too; a type compiled
// against this package before the move still finds the contract through here.
[assembly: TypeForwardedTo(typeof(MatPlotLibNet.Transforms.Animation.IAnimationTransform))]

[assembly: InternalsVisibleTo("MatPlotLibNet.Skia.Tests")]
// The native controls that draw with Skia paint through the one painter of this package (SkiaChartPainter), which keeps a
// recording of every subplot that did not change between paints; how a control paints is not a public API.
[assembly: InternalsVisibleTo("MatPlotLibNet.Wpf")]
[assembly: InternalsVisibleTo("MatPlotLibNet.Uno")]
[assembly: InternalsVisibleTo("MatPlotLibNet.Avalonia")]
// Their test suites paint a figure the way the control does, through the same painter.
[assembly: InternalsVisibleTo("MatPlotLibNet.Wpf.Tests")]
[assembly: InternalsVisibleTo("MatPlotLibNet.Uno.Tests")]
[assembly: InternalsVisibleTo("MatPlotLibNet.Avalonia.Tests")]
// The benchmark harness drives the same painter a native control does (the drag run), so it can show what a paint costs.
[assembly: InternalsVisibleTo("MatPlotLibNet.Benchmarks")]
