// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MatPlotLibNet.AspNetCore.Tests")]
// The GraphQL decorator sends its subscribers the SVG the publisher just published (IKeepingChartPublisher.PublishSvgAndHandOnAsync),
// so a figure is drawn once per publish; how the two packages share it is not a public API.
[assembly: InternalsVisibleTo("MatPlotLibNet.GraphQL")]
// Its suite checks that the decorator hands on what the library's publisher is told when a chart ends (IKeepingChartPublisher.ForgetAsync).
[assembly: InternalsVisibleTo("MatPlotLibNet.GraphQL.Tests")]
// The benchmark harness drives a chart session and a publisher over a stand-in hub, to measure what a gesture costs the server.
[assembly: InternalsVisibleTo("MatPlotLibNet.Benchmarks")]
