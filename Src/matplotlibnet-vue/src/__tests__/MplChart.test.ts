// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount } from '@vue/test-utils';
import { MplChart } from '../lib/MplChart';

describe('MplChart', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('renders with mpl-chart class', () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      text: () => Promise.resolve('<svg/>'),
    }));

    const wrapper = mount(MplChart, { props: { chartUrl: '/api/chart.svg' } });

    expect(wrapper.find('.mpl-chart').exists()).toBe(true);
  });

  it('applies cssClass to the container', () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      text: () => Promise.resolve('<svg/>'),
    }));

    const wrapper = mount(MplChart, {
      props: { chartUrl: '/api/chart.svg', cssClass: 'my-chart' },
    });

    expect(wrapper.find('.mpl-chart.my-chart').exists()).toBe(true);
  });

  it('renders SVG content after fetch', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      text: () => Promise.resolve('<svg><rect/></svg>'),
    }));

    const wrapper = mount(MplChart, { props: { chartUrl: '/api/chart.svg' } });

    // Wait for fetch and reactivity
    await new Promise(r => setTimeout(r, 10));
    await wrapper.vm.$nextTick();

    expect(wrapper.html()).toContain('<svg>');
    expect(wrapper.html()).toContain('<rect');
  });
});
