// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Animation;

/// <summary>The library's default <see cref="AnimationTimer"/>: a new one is stopped and ticks every 16 milliseconds (about 60 times a
/// second) once it is started, on the thread pool through <see cref="System.Threading.Timer"/>. It works on every platform; bringing a
/// tick to the UI thread is the listener's work.</summary>
public sealed class SystemThreadingAnimationTimer : AnimationTimer;
