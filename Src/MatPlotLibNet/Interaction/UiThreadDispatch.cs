// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>How a native control runs work on its own UI thread: WPF's dispatcher, Avalonia's UI thread, Uno's dispatcher
/// queue, MAUI's main thread. The control hands its own to the <see cref="StreamingFigureFollower"/>, which never knows which
/// platform it is on.</summary>
/// <param name="work">What to run on the UI thread, later, in the order it was posted.</param>
internal delegate void UiThreadDispatch(Action work);
