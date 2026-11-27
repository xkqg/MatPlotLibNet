// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Transforms.Animation;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Transforms;

/// <summary>Writes an animation as one SVG file that steps through its frames: every frame is in the document, one is shown at a
/// time, and a slider, a play button and the arrow keys move between them. The same shape as the GIF transform: the frames, the
/// time between them, whether it loops, and where to write. It is the animation for a page where a GIF is the wrong tool: it stays
/// sharp at any size, its text is text, and its frames can be inspected.
/// <para>Each frame is a whole SVG, put in the document under a prefix of its own for its ids (<c>f0-</c>, <c>f1-</c>, …), so the
/// document never defines one twice and every reference still finds its id. A frame is a picture: the scripts of an interactive
/// figure are left out. Without its script the document is its first frame, which is what an <c>&lt;img&gt;</c>, a README or a
/// viewer that runs nothing shows (the controls are hidden until the script reveals them). A reader who asked the page for reduced
/// motion gets no autoplay.</para>
/// <para>The file is as large as its frames together: no frame is dropped or shared, so a long animation of detailed figures is a
/// large file. Frames of different sizes are placed at the top left, and the document is as large as the largest.</para></summary>
public sealed class StepThroughSvgTransform : IAnimationTransform
{
    /// <summary>The height, in pixels, of the strip below the frames that holds the slider and the play button.</summary>
    public const double ControlsHeight = 36;

    private static readonly XNamespace Svg = SvgAttributes.Namespace;

    private const double TrackLeft = 48;
    private const double TrackRightMargin = 16;
    private const string PlayGlyph = "M16 11 L16 25 L26 18 Z";

    /// <summary>Writes the animation to <paramref name="output"/>.</summary>
    /// <param name="frames">The frames, in order; enumerated once. At least one.</param>
    /// <param name="interval">The time each frame is shown while the animation plays.</param>
    /// <param name="loop">Whether it starts again after the last frame, or stops there.</param>
    /// <param name="output">The destination stream.</param>
    /// <exception cref="ArgumentException"><paramref name="frames"/> holds no frame.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="interval"/> is not a positive time.</exception>
    public void Transform(IEnumerable<Figure> frames, TimeSpan interval, bool loop, Stream output)
    {
        ArgumentNullException.ThrowIfNull(frames);
        ArgumentNullException.ThrowIfNull(output);
        interval.ThrowIfNot(interval > TimeSpan.Zero, nameof(interval), "must be a positive time", "The time between frames");

        var pictures = new List<Picture>();
        foreach (var figure in frames)
            pictures.Add(Draw(figure, pictures.Count));
        pictures.ThrowIfEmpty("A step-through SVG needs at least one frame.", nameof(frames));

        double width = pictures.Max(p => p.Width), height = pictures.Max(p => p.Height);
        bool steps = pictures.Count > 1;
        double total = height + (steps ? ControlsHeight : 0);

        var root = new XElement(Svg + "svg",
            new XAttribute("viewBox", $"0 0 {width.ToSvgNumber()} {total.ToSvgNumber()}"),
            new XAttribute("width", width.ToSvgNumber()),
            new XAttribute("height", total.ToSvgNumber()),
            new XAttribute("role", "group"),
            new XAttribute("aria-label", string.Invariant($"Animation of {pictures.Count} {(pictures.Count == 1 ? "frame" : "frames")}")),
            new XAttribute("aria-roledescription", "animation"),
            new XAttribute("data-interval-ms", ((long)interval.TotalMilliseconds).InvariantText()),
            new XAttribute("data-loop", loop ? "true" : "false"));
        if (steps) root.Add(new XAttribute("tabindex", "0"));

        for (int k = 0; k < pictures.Count; k++)
        {
            var frame = new XElement(Svg + "g", new XAttribute("data-frame", k.InvariantText()), pictures[k].Element);
            if (k > 0) frame.Add(new XAttribute("display", "none"));
            root.Add(frame);
        }

        if (steps)
        {
            root.Add(Controls(width, height, pictures.Count));
            root.Add(ScriptElement());
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(root.ToSvgText(indent: false));
        output.Write(bytes, 0, bytes.Length);
    }

    /// <summary>A frame as a whole SVG under the id prefix of its place, without scripts, sized as the figure it was drawn from is.</summary>
    private static Picture Draw(Figure figure, int position)
    {
        string svg = SvgRendererDefault.Current.Render(figure).WithSaveOptions(new SaveOptions { SvgIdPrefix = $"f{position.InvariantText()}-" });
        var element = XDocument.Parse(svg).Root!;

        element.Descendants().Where(e => e.Name.LocalName == "script").ToList().ForEach(e => e.Remove());

        double width = figure.Width, height = figure.Height;

        // The frame keeps the size it was drawn at inside the animation: a figure that scales to its container would scale to
        // the whole animation instead.
        element.SetAttributeValue("width", width.ToSvgNumber());
        element.SetAttributeValue("height", height.ToSvgNumber());
        element.SetAttributeValue("style", null);
        element.SetAttributeValue("x", "0");
        element.SetAttributeValue("y", "0");
        return new Picture(element, width, height);
    }

    private readonly record struct Picture(XElement Element, double Width, double Height);

    /// <summary>The strip below the frames: a play button and a slider whose track is clicked to go to a frame. Hidden until the
    /// script runs.</summary>
    private static XElement Controls(double width, double top, int frames)
    {
        double track = Math.Max(1, width - TrackLeft - TrackRightMargin);

        return new XElement(Svg + "g",
            new XAttribute("data-steps-controls", ""),
            new XAttribute("display", "none"),
            new XAttribute("transform", $"translate(0,{top.ToSvgNumber()})"),
            new XElement(Svg + "rect", new XAttribute("width", width.ToSvgNumber()), new XAttribute("height", ControlsHeight.ToSvgNumber()), new XAttribute("fill", "#f3f3f3")),
            new XElement(Svg + "g",
                new XAttribute("role", "button"), new XAttribute("tabindex", "0"),
                new XAttribute("aria-label", "Play"), new XAttribute("aria-pressed", "false"),
                new XAttribute("style", "cursor:pointer"),
                new XElement(Svg + "rect", new XAttribute("x", "8"), new XAttribute("y", "6"), new XAttribute("width", "24"), new XAttribute("height", "24"),
                    new XAttribute("rx", "4"), new XAttribute("fill", "#dcdcdc")),
                new XElement(Svg + "path", new XAttribute("data-steps-glyph", ""), new XAttribute("d", PlayGlyph), new XAttribute("fill", "#333333"))),
            new XElement(Svg + "g",
                new XAttribute("data-steps-track", ""), new XAttribute("data-width", track.ToSvgNumber()),
                new XAttribute("transform", $"translate({TrackLeft.ToSvgNumber()},0)"),
                new XAttribute("style", "cursor:pointer"),
                new XElement(Svg + "rect", new XAttribute("x", "0"), new XAttribute("y", "6"), new XAttribute("width", track.ToSvgNumber()), new XAttribute("height", "24"),
                    new XAttribute("fill", "transparent")),
                new XElement(Svg + "rect", new XAttribute("x", "0"), new XAttribute("y", "16"), new XAttribute("width", track.ToSvgNumber()), new XAttribute("height", "4"),
                    new XAttribute("rx", "2"), new XAttribute("fill", "#c8c8c8")),
                new XElement(Svg + "circle",
                    new XAttribute("role", "slider"), new XAttribute("tabindex", "0"), new XAttribute("aria-label", "Frame"),
                    new XAttribute("aria-valuemin", "0"), new XAttribute("aria-valuemax", (frames - 1).InvariantText()),
                    new XAttribute("aria-valuenow", "0"), new XAttribute("aria-valuetext", $"Frame 1 of {frames.InvariantText()}"),
                    new XAttribute("cx", "0"), new XAttribute("cy", "18"), new XAttribute("r", "7"), new XAttribute("fill", "#555555"))));
    }

    /// <summary>The script, as an element: its source is data inside CDATA, so it is added as a node and not parsed.</summary>
    private static XElement ScriptElement()
    {
        var parsed = XElement.Parse(GetScript());
        return new XElement(Svg + "script", new XAttribute("type", "text/ecmascript"), new XCData(parsed.Value));
    }

    // What the comments of the script below said. They are not emitted: a figure that carries the script carries no
    // comment of it. Each note stands at the line it was written beside.
    // at the locator (SvgScriptPreamble.Root)
    //   Self-locate via document.currentScript: the controls and frames of THIS animation, whatever else the page holds.
    // at `if (index >= count - 1 && !loop) show(0);`
    //   Playing again after the end begins at the beginning.
    // at `function go(i) { stop(); show(Math.max(0, Math.min(count - 1, i))); }`
    //   Stepping by hand takes over from the playback.
    /// <summary>
    /// Returns a <c>&lt;script&gt;</c> block that shows one frame (<c>[data-frame]</c>) at a time and moves between them. It plays
    /// on load, every <c>data-interval-ms</c>, unless the reader asked for reduced motion (<c>prefers-reduced-motion</c>), and then
    /// only when they press play; it stops at the last frame unless <c>data-loop</c> is true. The arrow keys step one frame, Home
    /// and End go to the first and the last, the space bar plays and pauses, and a click on the track goes to the frame at that
    /// place; stepping by hand takes over from the playback. The slider (<c>role="slider"</c>) carries the frame as
    /// <c>aria-valuenow</c> and in words as <c>aria-valuetext</c>, and the button (<c>role="button"</c>) says what it will do as
    /// its <c>aria-label</c> and what it is doing as <c>aria-pressed</c>. The controls are hidden in the file and appear only when
    /// this script runs, so the document without it is the first frame alone.
    /// </summary>
    internal static string GetScript() => $$"""
        <script type="text/ecmascript"><![CDATA[
        (function() {
        {{SvgScriptPreamble.Root}}
            var frames = svg.querySelectorAll('[data-frame]');
            var count = frames.length;
            if (count < 2) return;
            var interval = Number(svg.getAttribute('data-interval-ms')) || 100;
            var loop = svg.getAttribute('data-loop') !== 'false';
            var controls = svg.querySelector('[data-steps-controls]');
            var button = svg.querySelector("[role='button']");
            var slider = svg.querySelector("[role='slider']");
            var track = svg.querySelector('[data-steps-track]');
            var glyph = svg.querySelector('[data-steps-glyph]');
            var trackWidth = Number(track.getAttribute('data-width')) || 1;
            var PLAY = 'M16 11 L16 25 L26 18 Z', PAUSE = 'M15 11 L15 25 L19 25 L19 11 Z M23 11 L23 25 L27 25 L27 11 Z';
            var index = 0, timer = null;

            function show(i) {
                for (var k = 0; k < count; k++) {
                    if (k === i) frames[k].removeAttribute('display');
                    else frames[k].setAttribute('display', 'none');
                }
                index = i;
                slider.setAttribute('aria-valuenow', String(i));
                slider.setAttribute('aria-valuetext', 'Frame ' + (i + 1) + ' of ' + count);
                slider.setAttribute('cx', String(trackWidth * i / (count - 1)));
            }

            function setPlaying(on) {
                button.setAttribute('aria-label', on ? 'Pause' : 'Play');
                button.setAttribute('aria-pressed', on ? 'true' : 'false');
                glyph.setAttribute('d', on ? PAUSE : PLAY);
            }

            function stop() {
                if (timer !== null) { clearInterval(timer); timer = null; }
                setPlaying(false);
            }

            function tick() {
                if (index < count - 1) show(index + 1);
                else if (loop) show(0);
                else stop();
            }

            function start() {
                if (timer !== null) return;
                if (index >= count - 1 && !loop) show(0);
                timer = setInterval(tick, interval);
                setPlaying(true);
            }

            function toggle() { if (timer === null) start(); else stop(); }

            function go(i) { stop(); show(Math.max(0, Math.min(count - 1, i))); }

            function handle(e) {
                var used = true;
                switch (e.key) {
                    case 'ArrowRight': go(index + 1); break;
                    case 'ArrowLeft': go(index - 1); break;
                    case 'Home': go(0); break;
                    case 'End': go(count - 1); break;
                    case ' ': toggle(); break;
                    default: used = false;
                }
                if (used) {
                    if (e.preventDefault) e.preventDefault();
                    if (e.stopPropagation) e.stopPropagation();
                }
            }

            controls.removeAttribute('display');
            show(0);
            svg.addEventListener('keydown', handle);
            slider.addEventListener('keydown', handle);
            button.addEventListener('keydown', function(e) {
                if (e.key === 'Enter') { toggle(); if (e.preventDefault) e.preventDefault(); if (e.stopPropagation) e.stopPropagation(); }
                else handle(e);
            });
            button.addEventListener('click', toggle);
            track.addEventListener('click', function(e) {
                var r = track.getBoundingClientRect();
                var fraction = r.width > 0 ? (e.clientX - r.left) / r.width : 0;
                go(Math.round(Math.max(0, Math.min(1, fraction)) * (count - 1)));
            });

        {{SvgScriptPreamble.ReducedMotion}}
            if (reduceMotion) setPlaying(false);
            else start();
        })();
        ]]></script>
        """;
}
