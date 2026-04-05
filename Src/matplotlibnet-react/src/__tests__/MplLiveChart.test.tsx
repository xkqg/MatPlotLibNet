// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render } from '@testing-library/react';
import { MplLiveChart } from '../lib/MplLiveChart';

vi.mock('../lib/use-mpl-live-chart', () => ({
  useMplLiveChart: vi.fn().mockReturnValue({ svgContent: '', isConnected: false }),
}));

import { useMplLiveChart } from '../lib/use-mpl-live-chart';

describe('MplLiveChart', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders with mpl-chart and mpl-live classes', () => {
    const { container } = render(<MplLiveChart chartId="test" />);

    expect(container.querySelector('.mpl-chart.mpl-live')).not.toBeNull();
  });

  it('applies cssClass to the container', () => {
    const { container } = render(<MplLiveChart chartId="test" cssClass="custom" />);

    expect(container.querySelector('.mpl-chart.mpl-live.custom')).not.toBeNull();
  });

  it('renders initialSvg when no live content available', () => {
    const { container } = render(
      <MplLiveChart chartId="test" initialSvg="<svg>initial</svg>" />
    );

    expect(container.innerHTML).toContain('<svg>initial</svg>');
  });

  it('renders live SVG when available', () => {
    vi.mocked(useMplLiveChart).mockReturnValue({
      svgContent: '<svg>live</svg>',
      isConnected: true,
    });

    const { container } = render(
      <MplLiveChart chartId="test" initialSvg="<svg>initial</svg>" />
    );

    expect(container.innerHTML).toContain('<svg>live</svg>');
    expect(container.innerHTML).not.toContain('<svg>initial</svg>');
  });

  it('passes chartId and hubUrl to the hook', () => {
    render(<MplLiveChart chartId="sensor-1" hubUrl="/my-hub" />);

    expect(useMplLiveChart).toHaveBeenCalledWith('sensor-1', '/my-hub');
  });
});
