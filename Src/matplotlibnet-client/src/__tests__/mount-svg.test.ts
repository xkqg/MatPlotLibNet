// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

import { describe, it, expect, vi, afterEach } from 'vitest';
import { mountSvg } from '../lib/mount-svg';

const SVG = '<svg xmlns="http://www.w3.org/2000/svg"><script>/* chart script */</script><rect/></svg>';

describe('mountSvg', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('puts the markup in the host', () => {
    const host = document.createElement('div');

    mountSvg(host, SVG);

    expect(host.querySelector('rect')).not.toBeNull();
  });

  it('replaces the script elements with fresh ones, because a script that arrives through innerHTML never runs', () => {
    const host = document.createElement('div');
    const created = vi.spyOn(document, 'createElementNS');

    mountSvg(host, SVG);

    const scripts = created.mock.calls.filter(([, tag]) => tag === 'script');
    expect(scripts).toHaveLength(1);
    expect(host.querySelectorAll('script')).toHaveLength(1);
    expect(host.querySelector('script')!.textContent).toContain('chart script');
  });

  it('copies the attributes of the script it replaces', () => {
    const host = document.createElement('div');

    mountSvg(host, '<svg xmlns="http://www.w3.org/2000/svg"><script type="text/ecmascript" data-x="1"></script></svg>');

    expect(host.querySelector('script')!.getAttribute('data-x')).toBe('1');
    expect(host.querySelector('script')!.getAttribute('type')).toBe('text/ecmascript');
  });

  it('makes the host the home of the connection the chart script reaches the server through', () => {
    const host = document.createElement('div');
    const hub = { invoke: vi.fn(), on: vi.fn() };

    mountSvg(host, SVG, hub);

    expect(host.hasAttribute('data-mpl-live')).toBe(true);
    expect((host as unknown as { __mplHub: unknown }).__mplHub).toBe(hub);
  });

  it('leaves the host unmarked when it has no connection to give', () => {
    const host = document.createElement('div');

    mountSvg(host, SVG);

    expect(host.hasAttribute('data-mpl-live')).toBe(false);
  });

  it('does not replace a chart the viewer is in the middle of a gesture on, and mounts the latest when it goes idle', () => {
    const host = document.createElement('div');
    mountSvg(host, '<svg xmlns="http://www.w3.org/2000/svg" data-test="old"></svg>');
    const busy = host.querySelector('svg')!;
    busy.setAttribute('data-mpl-busy', 'true');

    mountSvg(host, '<svg xmlns="http://www.w3.org/2000/svg" data-test="first"></svg>');
    mountSvg(host, '<svg xmlns="http://www.w3.org/2000/svg" data-test="second"></svg>');
    expect(host.querySelector('svg')!.getAttribute('data-test')).toBe('old');

    busy.removeAttribute('data-mpl-busy');
    busy.dispatchEvent(new CustomEvent('mpl:idle', { bubbles: true }));

    expect(host.querySelector('svg')!.getAttribute('data-test')).toBe('second');
  });

  it('mounts at once when the chart is not busy', () => {
    const host = document.createElement('div');
    mountSvg(host, '<svg xmlns="http://www.w3.org/2000/svg" data-test="old"></svg>');

    mountSvg(host, '<svg xmlns="http://www.w3.org/2000/svg" data-test="new"></svg>');

    expect(host.querySelector('svg')!.getAttribute('data-test')).toBe('new');
  });

  it('holds back one picture per idle, not one per update', () => {
    const host = document.createElement('div');
    mountSvg(host, '<svg xmlns="http://www.w3.org/2000/svg" data-test="old"></svg>');
    const busy = host.querySelector('svg')!;
    busy.setAttribute('data-mpl-busy', 'true');
    const add = vi.spyOn(busy, 'addEventListener');

    mountSvg(host, '<svg xmlns="http://www.w3.org/2000/svg" data-test="a"></svg>');
    mountSvg(host, '<svg xmlns="http://www.w3.org/2000/svg" data-test="b"></svg>');
    mountSvg(host, '<svg xmlns="http://www.w3.org/2000/svg" data-test="c"></svg>');

    expect(add.mock.calls.filter(([type]) => type === 'mpl:idle')).toHaveLength(1);
  });
});
