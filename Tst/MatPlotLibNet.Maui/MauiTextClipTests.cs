// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Maui.Graphics;
using NSubstitute;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests;
using MplColor = MatPlotLibNet.Styling.Colors;
using MplFont = MatPlotLibNet.Styling.Font;
using MplPoint = MatPlotLibNet.Rendering.Point;
using MplRect = MatPlotLibNet.Rendering.Rect;

namespace MatPlotLibNet.Maui.Tests;

/// <summary>On a MAUI canvas a clip cuts the marks drawn under it, never a text: the clip's state is taken off before a text is drawn and
/// put back before the next mark, and the opacity set under the clip outlives it.</summary>
public class MauiTextClipTests
{
    [Fact]
    public void ATextDrawnUnderAClip_IsDrawnWithTheClipTakenOff_AndTheNextMarkIsCutAgain()
    {
        var (canvas, ctx) = MauiRig.Fresh();

        ctx.PushClip(new MplRect(10, 20, 100, 50));
        ctx.DrawRectangle(new MplRect(0, 0, 5, 5), new ShapeStyle(MplColor.Red, null, 0));
        ctx.DrawText("label", new MplPoint(50, 90), new MplFont(), MatPlotLibNet.Rendering.TextAlignment.Left);
        ctx.DrawRectangle(new MplRect(0, 0, 5, 5), new ShapeStyle(MplColor.Blue, null, 0));
        ctx.PopClip();

        Received.InOrder(() =>
        {
            canvas.SaveState();
            canvas.ClipRectangle(10f, 20f, 100f, 50f);
            canvas.FillRectangle(0f, 0f, 5f, 5f);
            canvas.RestoreState();
            canvas.DrawString("label", 50f, 90f, HorizontalAlignment.Left);
            canvas.SaveState();
            canvas.ClipRectangle(10f, 20f, 100f, 50f);
            canvas.FillRectangle(0f, 0f, 5f, 5f);
            canvas.RestoreState();
        });
        canvas.Received(2).SaveState();
        canvas.Received(2).RestoreState();
    }

    /// <summary>A text drawn while the surface cuts its texts (a contour's labels, which matplotlib clips at the axes) is drawn under the
    /// clip, as a mark is; the text drawn after the scope ends has the clip taken off again.</summary>
    [Fact]
    public void ATextDrawnWhileTheTextsAreCut_IsDrawnUnderTheClip_AndTheNextTextIsNot()
    {
        var (canvas, ctx) = MauiRig.Fresh();

        ctx.PushClip(new MplRect(10, 20, 100, 50));
        ctx.DrawRectangle(new MplRect(0, 0, 5, 5), new ShapeStyle(MplColor.Red, null, 0));
        using (ctx.CuttingTexts())
        {
            ctx.DrawText("cut", new MplPoint(50, 90), new MplFont(), MatPlotLibNet.Rendering.TextAlignment.Left);
        }
        ctx.DrawText("whole", new MplPoint(50, 95), new MplFont(), MatPlotLibNet.Rendering.TextAlignment.Left);
        ctx.PopClip();

        Received.InOrder(() =>
        {
            canvas.SaveState();
            canvas.ClipRectangle(10f, 20f, 100f, 50f);
            canvas.FillRectangle(0f, 0f, 5f, 5f);
            canvas.DrawString("cut", 50f, 90f, HorizontalAlignment.Left);
            canvas.RestoreState();
            canvas.DrawString("whole", 50f, 95f, HorizontalAlignment.Left);
        });
        canvas.Received(1).SaveState();
        canvas.Received(1).RestoreState();
    }

    /// <summary>The label of a contour line past the axes is drawn under the clip of the axes in the MAUI view (the x axis is cut at 8, the
    /// line and its label stand at 8.5), as are the labels inside; the canvas cuts it off.</summary>
    [Fact]
    public void TheLabelsOfAContour_AreDrawnUnderTheClipOfTheAxes_InTheView()
    {
        var figure = Theme.MatplotlibV2.LabelledContourPastTheAxes();
        var canvas = Substitute.For<ICanvas>();

        new MplChartView { Figure = figure }.Drawable!.Draw(canvas, new RectF(0, 0, 400, 300));

        // Walk the canvas' states in the order they were set: a state saved and then clipped is a clip, any other a plain one.
        var states = new Stack<bool>();
        var labelsUnderAClip = new Dictionary<string, bool>();
        foreach (var call in canvas.ReceivedCalls())
        {
            switch (call.GetMethodInfo().Name)
            {
                case nameof(ICanvas.SaveState): states.Push(false); break;
                case nameof(ICanvas.ClipRectangle) or nameof(ICanvas.ClipPath):
                    if (states.Count > 0) states.Pop();
                    states.Push(true);
                    break;
                case nameof(ICanvas.RestoreState): states.Pop(); break;
                case nameof(ICanvas.DrawString) when call.GetArguments()[0] is string text && text is "2.5" or "5.5" or "8.5":
                    labelsUnderAClip[text] = states.Contains(true);
                    break;
            }
        }

        Assert.Equal(3, labelsUnderAClip.Count);
        Assert.All(labelsUnderAClip, label => Assert.True(label.Value, $"the contour label '{label.Key}' is drawn past the clip"));
    }

    [Fact]
    public void TheOpacitySetUnderAClip_OutlivesTheClipTakenOffForAText()
    {
        var (canvas, ctx) = MauiRig.Fresh();

        ctx.PushClip(new MplRect(10, 20, 100, 50));
        ctx.SetOpacity(0.5);
        canvas.ClearReceivedCalls();
        ctx.DrawText("label", new MplPoint(50, 90), new MplFont(), MatPlotLibNet.Rendering.TextAlignment.Left);

        Received.InOrder(() =>
        {
            canvas.RestoreState();
            canvas.Alpha = 0.5f;
            canvas.DrawString("label", 50f, 90f, HorizontalAlignment.Left);
        });
    }
}
