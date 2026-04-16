import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { InlineBlogEditorSection } from '@/components/admin/InlineBlogEditorSection'

const mocks = vi.hoisted(() => ({
  deleteAdminBlog: vi.fn(),
  push: vi.fn(),
  refresh: vi.fn(),
  toast: {
    success: vi.fn(),
    error: vi.fn(),
  },
}))

vi.mock('next/navigation', () => ({
  useRouter: () => ({
    push: mocks.push,
    refresh: mocks.refresh,
  }),
}))

vi.mock('@/lib/api/admin-mutations', () => ({
  deleteAdminBlog: mocks.deleteAdminBlog,
}))

vi.mock('sonner', () => ({
  toast: mocks.toast,
}))

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
  beforeEach(() => {
    vi.clearAllMocks()
    mocks.deleteAdminBlog.mockResolvedValue(undefined)
    vi.spyOn(window, 'confirm').mockReturnValue(true)
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

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

  it('deletes the blog and returns to the requested page', async () => {
    render(
      <InlineBlogEditorSection
        initialBlog={{
          id: 'blog-1',
          title: 'Deleted blog',
        }}
        afterDeleteHref="/blog?page=2&pageSize=12"
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: '삭제' }))

    expect(window.confirm).toHaveBeenCalled()
    await waitFor(() => {
      expect(mocks.deleteAdminBlog).toHaveBeenCalledWith('blog-1')
    })
    expect(mocks.push).toHaveBeenCalledWith('/blog?page=2&pageSize=12')
    expect(mocks.refresh).toHaveBeenCalled()
    expect(mocks.toast.success).toHaveBeenCalledWith('Study deleted')
  })
})
