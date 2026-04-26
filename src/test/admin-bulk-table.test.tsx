import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { AdminBlogTableClient } from '@/components/admin/AdminBlogTableClient'
import { AdminWorksTableClient } from '@/components/admin/AdminWorksTableClient'
import {
  deleteAdminBlog,
  deleteAdminWork,
  deleteManyAdminBlogs,
  deleteManyAdminWorks,
} from '@/lib/api/admin-mutations'

const mocks = vi.hoisted(() => ({
  refresh: vi.fn(),
  replace: vi.fn(),
  prompt: vi.fn(() => 'yes'),
  toastError: vi.fn(),
  toastSuccess: vi.fn(),
}))

vi.mock('next/navigation', () => ({
  useRouter: () => ({
    refresh: mocks.refresh,
    replace: mocks.replace,
  }),
  usePathname: () => '/admin/blog',
  useSearchParams: () => new URLSearchParams(),
}))

vi.mock('next/link', () => ({
  default: ({
    href,
    children,
    ...props
  }: {
    href: string
    children: React.ReactNode
    prefetch?: boolean
  }) => {
    const { prefetch, ...anchorProps } = props
    void prefetch
    return <a href={href} {...anchorProps}>{children}</a>
  },
}))

vi.mock('@/lib/api/admin-mutations', () => ({
  deleteAdminBlog: vi.fn(async () => undefined),
  deleteManyAdminBlogs: vi.fn(async () => undefined),
  deleteAdminWork: vi.fn(async () => undefined),
  deleteManyAdminWorks: vi.fn(async () => undefined),
}))

vi.mock('sonner', () => ({
  toast: {
    error: mocks.toastError,
    success: mocks.toastSuccess,
  },
}))

vi.mock('@/hooks/useResponsivePageSize', () => ({
  useResponsivePageSize: () => 12,
}))

function expectSelectionSummary(itemCount: number, selectedCount: number) {
  expect(
    screen.getByText((_, element) =>
      element?.tagName.toLowerCase() === 'p'
      && element.textContent === `${itemCount} shown · ${selectedCount} selected`,
    ),
  ).toBeInTheDocument()
}

describe('admin bulk selection tables', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.stubGlobal('prompt', mocks.prompt)
    vi.mocked(deleteAdminBlog).mockResolvedValue(undefined)
    vi.mocked(deleteAdminWork).mockResolvedValue(undefined)
    vi.mocked(deleteManyAdminBlogs).mockResolvedValue(undefined)
    vi.mocked(deleteManyAdminWorks).mockResolvedValue(undefined)
  })

  it('keeps the active admin blog page in edit links and URL state', async () => {
    render(
      <AdminBlogTableClient
        blogs={Array.from({ length: 13 }, (_, index) => ({
          id: `b${index + 1}`,
          title: `Blog ${index + 1}`,
          slug: `blog-${index + 1}`,
          excerpt: '',
          tags: [],
          published: true,
          publishedAt: null,
        }))}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Next page' }))

    expect(mocks.replace).not.toHaveBeenCalled()
    expect(window.location.pathname).toBe('/admin/blog')
    expect(window.location.search).toBe('?page=2&pageSize=12')
    expect(screen.getByLabelText('Edit post: Blog 13')).toHaveAttribute(
      'href',
      '/admin/blog/b13?returnTo=%2Fadmin%2Fblog%3Fpage%3D2%26pageSize%3D12',
    )
  })

  it('shows blog bulk delete button when rows are selected', async () => {
    render(
      <AdminBlogTableClient
        blogs={[
          { id: 'b1', title: 'Blog 1', slug: 'blog-1', excerpt: '', tags: [], published: true, publishedAt: null },
          { id: 'b2', title: 'Blog 2', slug: 'blog-2', excerpt: '', tags: [], published: false, publishedAt: null },
        ]}
      />,
    )

    expect(screen.queryByText('Delete Selected')).not.toBeInTheDocument()
    fireEvent.click(screen.getByLabelText('Select Blog 1'))
    expect(screen.getByText('Delete Selected')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Generate AI Fix job' })).not.toBeInTheDocument()
  })

  it('filters blog rows by title and exposes previous/next pagination controls', async () => {
    render(
      <AdminBlogTableClient
        blogs={[
          { id: 'b1', title: 'Alpha Blog', slug: 'alpha-blog', excerpt: '', tags: [], published: true, publishedAt: null },
          { id: 'b2', title: 'Beta Blog', slug: 'beta-blog', excerpt: '', tags: [], published: false, publishedAt: null },
        ]}
      />,
    )

    fireEvent.change(screen.getByLabelText('Search blog titles'), { target: { value: 'beta' } })
    expect(screen.getByText('Beta Blog')).toBeInTheDocument()
    expect(screen.queryByText('Alpha Blog')).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Previous page' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Next page' })).toBeInTheDocument()
  })

  it('shows works bulk delete button when rows are selected', async () => {
    render(
      <AdminWorksTableClient
        works={[
          { id: 'w1', title: 'Work 1', slug: 'work-1', excerpt: '', tags: [], published: true, publishedAt: null, category: 'cat' },
        ]}
      />,
    )

    fireEvent.click(screen.getByLabelText('Select Work 1'))
    expect(screen.getByText('Delete Selected')).toBeInTheDocument()
    await waitFor(() => {
      expect(mocks.refresh).not.toHaveBeenCalled()
    })
  })

  it('filters works by title and exposes previous/next pagination controls', () => {
    render(
      <AdminWorksTableClient
        works={[
          { id: 'w1', title: 'Alpha Work', slug: 'alpha-work', excerpt: '', tags: [], published: true, publishedAt: null, category: 'cat' },
          { id: 'w2', title: 'Beta Work', slug: 'beta-work', excerpt: '', tags: [], published: false, publishedAt: null, category: 'cat' },
        ]}
      />,
    )

    fireEvent.change(screen.getByLabelText('Search work titles'), { target: { value: 'beta' } })
    expect(screen.getByText('Beta Work')).toBeInTheDocument()
    expect(screen.queryByText('Alpha Work')).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Previous page' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Next page' })).toBeInTheDocument()
  })

  it('opens and cancels a blog single delete without calling the delete API or hiding the row', async () => {
    render(
      <AdminBlogTableClient
        blogs={[
          { id: 'b1', title: 'Keep Blog', slug: 'keep-blog', excerpt: '', tags: [], published: true, publishedAt: null },
        ]}
      />,
    )

    const row = screen.getByTestId('admin-blog-row')
    fireEvent.click(screen.getByRole('button', { name: 'Delete post: Keep Blog' }))

    const dialog = screen.getByRole('dialog')
    expect(dialog).toBeVisible()
    expect(deleteAdminBlog).not.toHaveBeenCalled()
    expect(dialog.querySelector('[data-variant="destructive"]')).toHaveTextContent('Delete')

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(row).toBeInTheDocument()
    expect(screen.getByText('Keep Blog')).toBeInTheDocument()
    expect(deleteAdminBlog).not.toHaveBeenCalled()
    expect(mocks.toastSuccess).not.toHaveBeenCalled()
    expect(mocks.refresh).not.toHaveBeenCalled()
  })

  it('keeps a blog row visible and retryable when a single delete fails', async () => {
    vi.mocked(deleteAdminBlog)
      .mockRejectedValueOnce(new Error('Delete blocked by test'))
      .mockResolvedValueOnce(undefined)

    render(
      <AdminBlogTableClient
        blogs={[
          { id: 'b1', title: 'Failing Blog', slug: 'failing-blog', excerpt: '', tags: [], published: true, publishedAt: null },
        ]}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Delete post: Failing Blog' }))
    fireEvent.click(screen.getByRole('button', { name: 'Delete' }))

    await waitFor(() => expect(mocks.toastError).toHaveBeenCalledWith('Delete blocked by test'))
    expect(screen.getByText('Failing Blog')).toBeInTheDocument()
    expect(screen.getByRole('dialog')).toBeVisible()
    expect(mocks.toastSuccess).not.toHaveBeenCalled()
    expect(mocks.refresh).not.toHaveBeenCalled()

    fireEvent.click(screen.getByRole('button', { name: 'Delete' }))

    await waitFor(() => expect(deleteAdminBlog).toHaveBeenCalledTimes(2))
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(mocks.refresh).toHaveBeenCalled()
  })

  it.each([
    ['401', new Error('Session expired')],
    ['403', new Error('Forbidden')],
  ])('keeps a work row visible and does not claim success after a %s single delete failure', async (_status, error) => {
    vi.mocked(deleteAdminWork).mockRejectedValueOnce(error)

    render(
      <AdminWorksTableClient
        works={[
          { id: 'w1', title: 'Protected Work', slug: 'protected-work', excerpt: '', tags: [], published: true, publishedAt: null, category: 'secure' },
        ]}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Delete work: Protected Work' }))
    fireEvent.click(screen.getByRole('button', { name: 'Delete' }))

    await waitFor(() => expect(mocks.toastError).toHaveBeenCalledWith(error.message))
    expect(screen.getByText('Protected Work')).toBeInTheDocument()
    expect(screen.getByRole('dialog')).toBeVisible()
    expect(mocks.toastSuccess).not.toHaveBeenCalled()
    expect(mocks.refresh).not.toHaveBeenCalled()
  })

  it('opens and cancels a blog bulk delete without deleting rows and preserves selection', async () => {
    render(
      <AdminBlogTableClient
        blogs={[
          { id: 'b1', title: 'Bulk Blog 1', slug: 'bulk-blog-1', excerpt: '', tags: [], published: true, publishedAt: null },
          { id: 'b2', title: 'Bulk Blog 2', slug: 'bulk-blog-2', excerpt: '', tags: [], published: false, publishedAt: null },
        ]}
      />,
    )

    fireEvent.click(screen.getByLabelText('Select Bulk Blog 1'))
    fireEvent.click(screen.getByLabelText('Select Bulk Blog 2'))
    fireEvent.click(screen.getByRole('button', { name: 'Delete Selected' }))

    expect(screen.getByRole('dialog')).toBeVisible()
    expect(deleteManyAdminBlogs).not.toHaveBeenCalled()

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(screen.getByText('Bulk Blog 1')).toBeInTheDocument()
    expect(screen.getByText('Bulk Blog 2')).toBeInTheDocument()
    expectSelectionSummary(2, 2)
    expect(screen.getByLabelText('Select Bulk Blog 1')).toBeChecked()
    expect(screen.getByLabelText('Select Bulk Blog 2')).toBeChecked()
    expect(deleteManyAdminBlogs).not.toHaveBeenCalled()
    expect(mocks.toastSuccess).not.toHaveBeenCalled()
  })

  it('does not claim full success or remove rows when blog bulk delete fails', async () => {
    vi.mocked(deleteManyAdminBlogs).mockRejectedValueOnce(new Error('One selected blog could not be deleted'))

    render(
      <AdminBlogTableClient
        blogs={[
          { id: 'b1', title: 'Bulk Fail Blog 1', slug: 'bulk-fail-blog-1', excerpt: '', tags: [], published: true, publishedAt: null },
          { id: 'b2', title: 'Bulk Fail Blog 2', slug: 'bulk-fail-blog-2', excerpt: '', tags: [], published: false, publishedAt: null },
        ]}
      />,
    )

    fireEvent.click(screen.getByLabelText('Select Bulk Fail Blog 1'))
    fireEvent.click(screen.getByLabelText('Select Bulk Fail Blog 2'))
    fireEvent.click(screen.getByRole('button', { name: 'Delete Selected' }))
    fireEvent.click(screen.getByRole('button', { name: 'Delete' }))

    await waitFor(() => expect(mocks.toastError).toHaveBeenCalledWith('One selected blog could not be deleted'))
    expect(screen.getByText('Bulk Fail Blog 1')).toBeInTheDocument()
    expect(screen.getByText('Bulk Fail Blog 2')).toBeInTheDocument()
    expectSelectionSummary(2, 2)
    expect(screen.getByRole('dialog')).toBeVisible()
    expect(mocks.toastSuccess).not.toHaveBeenCalled()
    expect(mocks.refresh).not.toHaveBeenCalled()
  })

  it('opens and cancels a works bulk delete without deleting rows and preserves selection', async () => {
    render(
      <AdminWorksTableClient
        works={[
          { id: 'w1', title: 'Bulk Work 1', slug: 'bulk-work-1', excerpt: '', tags: [], published: true, publishedAt: null, category: 'cat' },
          { id: 'w2', title: 'Bulk Work 2', slug: 'bulk-work-2', excerpt: '', tags: [], published: false, publishedAt: null, category: 'cat' },
        ]}
      />,
    )

    fireEvent.click(screen.getByLabelText('Select Bulk Work 1'))
    fireEvent.click(screen.getByLabelText('Select Bulk Work 2'))
    fireEvent.click(screen.getByRole('button', { name: 'Delete Selected' }))

    expect(screen.getByRole('dialog')).toBeVisible()
    expect(deleteManyAdminWorks).not.toHaveBeenCalled()

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(screen.getByText('Bulk Work 1')).toBeInTheDocument()
    expect(screen.getByText('Bulk Work 2')).toBeInTheDocument()
    expectSelectionSummary(2, 2)
    expect(screen.getByLabelText('Select Bulk Work 1')).toBeChecked()
    expect(screen.getByLabelText('Select Bulk Work 2')).toBeChecked()
    expect(deleteManyAdminWorks).not.toHaveBeenCalled()
    expect(mocks.toastSuccess).not.toHaveBeenCalled()
  })

  it('does not claim full success or remove rows when works bulk delete fails', async () => {
    vi.mocked(deleteManyAdminWorks).mockRejectedValueOnce(new Error('One selected work could not be deleted'))

    render(
      <AdminWorksTableClient
        works={[
          { id: 'w1', title: 'Bulk Fail Work 1', slug: 'bulk-fail-work-1', excerpt: '', tags: [], published: true, publishedAt: null, category: 'cat' },
          { id: 'w2', title: 'Bulk Fail Work 2', slug: 'bulk-fail-work-2', excerpt: '', tags: [], published: false, publishedAt: null, category: 'cat' },
        ]}
      />,
    )

    fireEvent.click(screen.getByLabelText('Select Bulk Fail Work 1'))
    fireEvent.click(screen.getByLabelText('Select Bulk Fail Work 2'))
    fireEvent.click(screen.getByRole('button', { name: 'Delete Selected' }))
    fireEvent.click(screen.getByRole('button', { name: 'Delete' }))

    await waitFor(() => expect(mocks.toastError).toHaveBeenCalledWith('One selected work could not be deleted'))
    expect(screen.getByText('Bulk Fail Work 1')).toBeInTheDocument()
    expect(screen.getByText('Bulk Fail Work 2')).toBeInTheDocument()
    expectSelectionSummary(2, 2)
    expect(screen.getByRole('dialog')).toBeVisible()
    expect(mocks.toastSuccess).not.toHaveBeenCalled()
    expect(mocks.refresh).not.toHaveBeenCalled()
  })
})
