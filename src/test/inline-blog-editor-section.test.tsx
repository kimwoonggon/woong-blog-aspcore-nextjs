import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { InlineBlogEditorSection } from '@/components/admin/InlineBlogEditorSection'

vi.mock('@/components/admin/BlogEditor', () => ({
  BlogEditor: ({ onSaved }: { onSaved?: () => void }) => (
    <div>
      <p>Mock inline editor</p>
      <button type="button" onClick={() => onSaved?.()}>
        Complete save
      </button>
    </div>
  ),
}))

describe('InlineBlogEditorSection', () => {
  it('closes the inline shell after the editor reports save completion', () => {
    render(
      <InlineBlogEditorSection
        initialBlog={{
          id: 'blog-1',
          title: 'Saved blog',
        }}
      />,
    )

    expect(screen.queryByText('Mock inline editor')).not.toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: /글 수정/i }))
    expect(screen.getByText('Mock inline editor')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /뒤로가기/i })).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: /Complete save/i }))

    expect(screen.queryByText('Mock inline editor')).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /뒤로가기/i })).not.toBeInTheDocument()
  })
})
