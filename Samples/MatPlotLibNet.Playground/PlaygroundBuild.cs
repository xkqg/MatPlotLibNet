// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Playground;

/// <summary>What a Playground example builds: the figure the page draws and the C# a user copies to draw it.</summary>
/// <param name="Figure">The figure the example draws under the current options.</param>
/// <param name="Code">The call chain that draws it, ready to copy.</param>
public readonly record struct PlaygroundBuild(Figure Figure, string Code);
