// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MatPlotLibNet.Animation;
using MatPlotLibNet.Models;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Tests.Animation;

/// <summary>An animation as one SVG file that steps through its frames: every frame is in the document, one is shown at a time, and
/// a slider, a play button and the arrow keys move between them. It is the animation for a page that cannot play a GIF well (it
/// stays sharp at any size, its text selectable, its frames inspectable) and, without its script, it is the first frame: the
/// picture in an <c>&lt;img&gt;</c>, in a README, in a viewer that runs nothing. The frames are whole SVGs, so the ids in each
/// are given a prefix of their own and the document never defines one twice.</summary>
public class StepThroughSvgTransformTests
{
    private static Figure Frame(int k, int width = 400, int height = 300, bool interactive = false)
    {
        var builder = Plt.Create().WithSize(width, height).WithTitle($"frame {k}")
            .Plot([0.0, 1.0, 2.0], [0.0, k, 2.0 * k], s => s.Label = $"s{k}");
        if (interactive) builder.WithZoomPan().WithSelection().WithLegendToggle();
        return builder.Build();
    }

    private static string Steps(IEnumerable<Figure> frames, double intervalMs = 100, bool loop = true)
    {
        using var stream = new MemoryStream();
        new StepThroughSvgTransform().Transform(frames, TimeSpan.FromMilliseconds(intervalMs), loop, stream);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static XElement Root(string svg) => XDocument.Parse(svg).Root!;

    private static IEnumerable<XElement> FrameGroups(XElement root) =>
        root.Descendants().Where(e => e.Name.LocalName == "g" && e.Attribute("data-frame") is not null);

    // ── the document ────────────────────────────────────────────────────────────

    [Fact]
    public void TheDocument_IsWellFormed_AndHoldsEveryFrameInOrder()
    {
        var root = Root(Steps([Frame(1), Frame(2), Frame(3)]));

        var frames = FrameGroups(root).ToArray();
        Assert.Equal(3, frames.Length);
        Assert.Equal(["0", "1", "2"], frames.Select(f => f.Attribute("data-frame")!.Value));
        Assert.All(frames, f => Assert.Single(f.Elements(), e => e.Name.LocalName == "svg"));   // each frame is a whole SVG
        Assert.Equal("group", root.Attribute("role")!.Value);
        Assert.Contains("3 frames", root.Attribute("aria-label")!.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void WithoutItsScript_TheFirstFrameIsWhatIsShown()
    {
        var root = Root(Steps([Frame(1), Frame(2), Frame(3)]));

        var frames = FrameGroups(root).ToArray();
        Assert.Null(frames[0].Attribute("display"));
        Assert.Equal("none", frames[1].Attribute("display")!.Value);
        Assert.Equal("none", frames[2].Attribute("display")!.Value);
        // The controls need the script; without it they are not drawn, so the picture in an <img> is the frame alone.
        var controls = root.Descendants().Single(e => e.Attribute("data-steps-controls") is not null);
        Assert.Equal("none", controls.Attribute("display")!.Value);
    }

    [Fact]
    public void EveryIdOfTheDocument_IsDefinedOnce_EvenWhenEveryFrameIsTheSameFigure()
    {
        var same = Frame(1);
        var root = Root(Steps([same, same, same]));

        var ids = root.DescendantsAndSelf().Select(e => e.Attribute("id")?.Value).Where(id => id is not null).ToArray();
        Assert.NotEmpty(ids);
        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void EveryReferenceOfTheDocument_StillFindsItsId()
    {
        string svg = Steps([Frame(1), Frame(2)]);
        var root = Root(svg);
        var ids = root.DescendantsAndSelf().Select(e => e.Attribute("id")?.Value).Where(id => id is not null).ToHashSet(StringComparer.Ordinal);

        foreach (Match reference in Regex.Matches(svg, @"url\(#([^)]+)\)"))
            Assert.Contains(reference.Groups[1].Value, ids);
        foreach (var element in root.DescendantsAndSelf())
        {
            foreach (string id in (element.Attribute("aria-labelledby")?.Value ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries))
                Assert.Contains(id, ids);
        }
    }

    [Fact]
    public void EachFrame_HasItsIdsUnderAPrefixOfItsOwn()
    {
        var root = Root(Steps([Frame(1), Frame(2)]));

        foreach (var frame in FrameGroups(root))
        {
            string k = frame.Attribute("data-frame")!.Value;
            var inside = frame.DescendantsAndSelf().Select(e => e.Attribute("id")?.Value).Where(id => id is not null).ToArray();
            Assert.NotEmpty(inside);
            Assert.All(inside, id => Assert.StartsWith($"f{k}-", id, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void TheScriptsOfTheFrames_AreLeftOut_AndTheDocumentHasOne()
    {
        string svg = Steps([Frame(1, interactive: true), Frame(2, interactive: true)]);

        Assert.Single(Regex.Matches(svg, "<script"));   // the frames are pictures; their interactivity is not stepped through
        Assert.DoesNotContain("mpl:selection", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void TheDocument_SaysHowFastItPlays_AndWhetherItLoops()
    {
        var root = Root(Steps([Frame(1), Frame(2)], intervalMs: 250, loop: false));

        Assert.Equal("250", root.Attribute("data-interval-ms")!.Value);
        Assert.Equal("false", root.Attribute("data-loop")!.Value);
        Assert.Equal("true", Root(Steps([Frame(1), Frame(2)])).Attribute("data-loop")!.Value);
    }

    [Fact]
    public void TheDocument_IsAsLargeAsTheLargestFrame_AndHasRoomForTheControlsBelow()
    {
        var root = Root(Steps([Frame(1, 400, 300), Frame(2, 640, 200)]));

        string[] box = root.Attribute("viewBox")!.Value.Split(' ');
        Assert.Equal("640", box[2]);
        Assert.Equal(300 + StepThroughSvgTransform.ControlsHeight, double.Parse(box[3], System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ASingleFrame_IsThePictureAlone_WithNothingToStepThrough()
    {
        string svg = Steps([Frame(1)]);

        Assert.DoesNotContain("<script", svg, StringComparison.Ordinal);
        Assert.DoesNotContain(Root(svg).Descendants(), e => e.Attribute("data-steps-controls") is not null);
        Assert.Single(FrameGroups(Root(svg)));
    }

    [Fact]
    public void TheControls_AreAnAccessibleSlider_AndAPlayButton()
    {
        var root = Root(Steps([Frame(1), Frame(2), Frame(3)]));

        var slider = root.Descendants().Single(e => e.Attribute("role")?.Value == "slider");
        Assert.Equal("0", slider.Attribute("aria-valuemin")!.Value);
        Assert.Equal("2", slider.Attribute("aria-valuemax")!.Value);
        Assert.Equal("0", slider.Attribute("aria-valuenow")!.Value);
        Assert.Equal("Frame 1 of 3", slider.Attribute("aria-valuetext")!.Value);
        Assert.Equal("0", slider.Attribute("tabindex")!.Value);
        var button = root.Descendants().Single(e => e.Attribute("role")?.Value == "button");
        Assert.Equal("Play", button.Attribute("aria-label")!.Value);
        Assert.Equal("0", button.Attribute("tabindex")!.Value);
    }

    // ── what is refused ─────────────────────────────────────────────────────────

    [Fact]
    public void NoFrames_AreRefused()
    {
        var refusal = Assert.Throws<ArgumentException>(() => Steps([]));

        Assert.Contains("frame", refusal.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-5.0)]
    public void AnIntervalThatIsNotAPositiveTime_IsRefused(double ms)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Steps([Frame(1), Frame(2)], ms));
    }

    [Fact]
    public void TheFramesAreEnumeratedOnce_AsAGeneratorIsCostly()
    {
        int made = 0;
        IEnumerable<Figure> Generate()
        {
            for (int k = 0; k < 3; k++)
            {
                made++;
                yield return Frame(k + 1);
            }
        }

        Steps(Generate());

        Assert.Equal(3, made);
    }

    // ── the builder ─────────────────────────────────────────────────────────────

    [Fact]
    public void AnAnimationBuilder_WritesItsFramesAsAStepThroughSvg_AtItsInterval()
    {
        var animation = new AnimationBuilder(4, k => Frame(k + 1)) { Interval = TimeSpan.FromMilliseconds(80), Loop = false };

        var root = Root(animation.ToStepThroughSvg());

        Assert.Equal(4, FrameGroups(root).Count());
        Assert.Equal("80", root.Attribute("data-interval-ms")!.Value);
        Assert.Equal("false", root.Attribute("data-loop")!.Value);
    }

    [Fact]
    public void AnAnimationBuilder_SavesIt_ToAFile()
    {
        string path = Path.Combine(Path.GetTempPath(), $"steps-{Guid.NewGuid():N}.svg");
        try
        {
            new AnimationBuilder(2, k => Frame(k + 1)).SaveStepThroughSvg(path);

            Assert.Equal(2, FrameGroups(Root(File.ReadAllText(path))).Count());
        }
        finally
        {
            File.Delete(path);
        }
    }
}
