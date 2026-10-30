// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>Provides the embedded JavaScript of a step-through SVG: the animation that shows one of its frames at a time.</summary>
internal static class SvgStepThroughScript
{
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
    internal static string GetScript() => """
        <script type="text/ecmascript"><![CDATA[
        (function() {
            // Self-locate via document.currentScript: the controls and frames of THIS animation, whatever else the page holds.
            var svg = (document.currentScript && document.currentScript.parentNode) || document.querySelector('svg');
            if (!svg || svg.tagName !== 'svg') return;
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
                // Playing again after the end begins at the beginning.
                if (index >= count - 1 && !loop) show(0);
                timer = setInterval(tick, interval);
                setPlaying(true);
            }

            function toggle() { if (timer === null) start(); else stop(); }

            // Stepping by hand takes over from the playback.
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

            var reduceMotion = !!(window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches);
            if (reduceMotion) setPlaying(false);
            else start();
        })();
        ]]></script>
        """;
}
