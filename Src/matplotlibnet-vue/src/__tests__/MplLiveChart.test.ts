// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount } from '@vue/test-utils';
import { ref } from 'vue';

vi.mock('../lib/use-mpl-live-chart', () => ({
  useMplLiveChart: vi.fn().mockReturnValue({
    svgContent: ref(''),
    isConnected: ref(false),
  }),
}));

import { MplLiveChart } from '../lib/MplLiveChart';
import { useMplLiveChart } from '../lib/use-mpl-live-chart';

describe('MplLiveChart', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders with mpl-chart and mpl-live classes', () => {
    const wrapper = mount(MplLiveChart, { props: { chartId: 'test' } });

    expect(wrapper.find('.mpl-chart.mpl-live').exists()).toBe(true);
  });

  it('applies cssClass to the container', () => {
    const wrapper = mount(MplLiveChart, {
      props: { chartId: 'test', cssClass: 'custom' },
    });

    expect(wrapper.find('.mpl-chart.mpl-live.custom').exists()).toBe(true);
  });

  it('renders initialSvg when no live content available', () => {
    const wrapper = mount(MplLiveChart, {
      props: { chartId: 'test', initialSvg: '<svg>initial</svg>' },
    });

    expect(wrapper.html()).toContain('<svg>initial</svg>');
  });

  it('renders live SVG when available', () => {
    vi.mocked(useMplLiveChart).mockReturnValue({
      svgContent: ref('<svg>live</svg>') as any,
      isConnected: ref(true) as any,
    });

    const wrapper = mount(MplLiveChart, {
      props: { chartId: 'test', initialSvg: '<svg>initial</svg>' },
    });

    expect(wrapper.html()).toContain('<svg>live</svg>');
    expect(wrapper.html()).not.toContain('<svg>initial</svg>');
  });

  it('passes chartId and hubUrl to the composable', () => {
    mount(MplLiveChart, {
      props: { chartId: 'sensor-1', hubUrl: '/my-hub' },
    });

    expect(useMplLiveChart).toHaveBeenCalledWith('sensor-1', '/my-hub');
  });
});
