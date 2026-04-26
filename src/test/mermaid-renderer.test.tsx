import { render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { MermaidRenderer } from '@/components/content/MermaidRenderer'

vi.mock('mermaid', () => ({
  default: {
    initialize: vi.fn(),
    render: vi.fn(async () => ({
      svg: '<svg id="mermaid-test" width="640" height="320"><g></g></svg>',
    })),
  },
}))

describe('MermaidRenderer', () => {
  it('centers rendered diagrams with overflow-safe SVG rules', async () => {
    const { container } = render(<MermaidRenderer code="flowchart TD\n  A --> B" />)

    await waitFor(() => {
      expect(container.querySelector('svg#mermaid-test')).toBeInTheDocument()
    })

    const wrapper = screen.getByTestId('mermaid-renderer')
    expect(wrapper.className).toContain('overflow-x-auto')
    expect(wrapper.className).toContain('text-center')
    expect(wrapper.className).toContain('[&_svg]:mx-auto')
    expect(wrapper.className).toContain('[&_svg]:block')
    expect(wrapper.className).toContain('[&_svg]:max-w-full')
  })

  it('keeps centering rules when callers provide a custom className', async () => {
    render(<MermaidRenderer code="flowchart LR\n  A --> B" className="my-custom-mermaid" />)

    await waitFor(() => {
      expect(screen.getByTestId('mermaid-renderer')).toBeInTheDocument()
    })

    const wrapper = screen.getByTestId('mermaid-renderer')
    expect(wrapper.className).toContain('my-custom-mermaid')
    expect(wrapper.className).toContain('[&_svg]:mx-auto')
  })
})
