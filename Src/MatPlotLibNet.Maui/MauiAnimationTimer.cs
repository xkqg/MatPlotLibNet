// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Animation;

namespace MatPlotLibNet.Maui;

/// <summary>The <see cref="AnimationTimer"/> of the MAUI package: a new one is stopped and ticks every 16 milliseconds once it is
/// started, on the thread pool through <see cref="System.Threading.Timer"/>. Use <see cref="MainThread.BeginInvokeOnMainThread"/>
/// inside the handler when touching UI elements.</summary>
public sealed class MauiAnimationTimer : AnimationTimer;
