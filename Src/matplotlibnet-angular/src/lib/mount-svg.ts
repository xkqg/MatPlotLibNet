// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

/** The connection the script inside a chart reaches the server through: a SignalR `HubConnection`, or anything with its `invoke` and `on`. */
export interface MplHub {
  invoke(method: string, ...args: unknown[]): unknown;
  on(name: string, handler: (...args: any[]) => void): void;
}

/** The host element as the chart's script sees it: the home of the connection it sends through. */
type LiveHost = HTMLElement & { __mplHub?: MplHub; __mplHeldBack?: string | null };

/**
 * Mounts a chart's SVG markup in `host` and runs the scripts inside it.
 *
 * The interaction script of a server-interactive chart travels inside the SVG, and a browser never runs a
 * `<script>` that arrives through `innerHTML`; each one is replaced by a fresh copy, which the browser runs as it is
 * inserted. The host also becomes the home of `hub`: the script looks for its connection on the host of its own chart
 * (`data-mpl-live`) before it looks at a page global, so two live charts on a page cannot reach for the same one.
 *
 * A chart the viewer is in the middle of a drag or a band on says so (`data-mpl-busy`). Replacing it would cut the
 * gesture off, so the latest markup waits for the chart's `mpl:idle` event and is mounted then.
 */
export function mountSvg(host: HTMLElement, svgMarkup: string, hub?: MplHub): void {
  const live = host as LiveHost;

  const busy = live.querySelector('svg[data-mpl-busy]');
  if (busy) {
    const first = !live.__mplHeldBack;
    live.__mplHeldBack = svgMarkup;
    if (first) {
      busy.addEventListener('mpl:idle', () => {
        const held = live.__mplHeldBack;
        live.__mplHeldBack = null;
        if (held) mountSvg(host, held, hub);
      }, { once: true });
    }
    return;
  }

  if (hub) {
    live.__mplHub = hub;
    live.setAttribute('data-mpl-live', '');
  }

  live.innerHTML = svgMarkup;
  live.querySelectorAll('script').forEach((inert) => {
    const fresh = document.createElementNS(inert.namespaceURI, 'script');
    for (const attr of Array.from(inert.attributes)) fresh.setAttribute(attr.name, attr.value);
    fresh.textContent = inert.textContent;
    inert.parentNode!.replaceChild(fresh, inert);
  });
}
