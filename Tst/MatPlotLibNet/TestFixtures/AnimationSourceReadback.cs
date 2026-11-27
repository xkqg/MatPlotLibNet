// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Animation;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests;

/// <summary>What a native control says of where its figure comes from: the figure it was handed and the animation source it was given read
/// back as given, and the source it listens to is the one set last. The controls of the toolkits share no base type a suite could read,
/// so a suite hands over how to read and set each; every suite of a control links this one file.</summary>
internal static class AnimationSourceReadback
{
    /// <summary>Gives <paramref name="control"/> two sources one after the other and then none, and checks who listens each time.</summary>
    /// <typeparam name="TControl">The control's own type.</typeparam>
    /// <param name="control">A control made with <paramref name="figure"/> as its figure and no animation source.</param>
    /// <param name="figure">The figure the control was made with.</param>
    /// <param name="figureOf">Reads the control's figure.</param>
    /// <param name="sourceOf">Reads the control's animation source.</param>
    /// <param name="follow">Sets the control's animation source.</param>
    internal static void Verify<TControl>(TControl control, Figure figure, Func<TControl, Figure?> figureOf,
        Func<TControl, IAnimationSource?> sourceOf, Action<TControl, IAnimationSource?> follow)
    {
        var first = new FakeAnimationSource();
        var second = new FakeAnimationSource();
        Assert.Null(sourceOf(control));

        follow(control, first);
        follow(control, second);

        Assert.Same(figure, figureOf(control));
        Assert.Same(second, sourceOf(control));
        Assert.Equal(0, first.SubscriberCount);
        Assert.Equal(1, second.SubscriberCount);

        follow(control, null);
        Assert.Equal(0, second.SubscriberCount);
    }
}
