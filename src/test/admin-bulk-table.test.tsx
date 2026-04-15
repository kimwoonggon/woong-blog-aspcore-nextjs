import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { AdminBlogTableClient } from '@/components/admin/AdminBlogTableClient'
import { AdminWorksTableClient } from '@/components/admin/AdminWorksTableClient'

const refreshMock = vi.fn()
const replaceMock = vi.fn()
const promptMock = vi.fn(() => 'yes')

vi.mock('next/navigation', () => ({
  useRouter: () => ({
    refresh: refreshMock,
    replace: replaceMock,
  }),
  usePathname: () => '/admin/blog',
  useSearchParams: () => new URLSearchParams(),
}))

vi.mock('next/link', () => ({
  default: ({ href, children, ...props }: { href: string; children: React.ReactNode }) => <a href={href} {...props}>{children}</a>,
}))

vi.mock('@/lib/api/admin-mutations', () => ({
  deleteAdminBlog: vi.fn(async () => undefined),
  deleteManyAdminBlogs: vi.fn(async () => undefined),
  deleteAdminWork: vi.fn(async () => undefined),
  deleteManyAdminWorks: vi.fn(async () => undefined),
}))

vi.mock('@/hooks/useResponsivePageSize', () => ({
  useResponsivePageSize: () => 12,
}))

describe('admin bulk selection tables', () => {
  it('keeps the active admin blog page in edit links and URL state', async () => {
    vi.stubGlobal('prompt', promptMock)

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

    replaceMock.mockClear()
    fireEvent.click(screen.getByRole('button', { name: 'Next page' }))

    expect(replaceMock).toHaveBeenCalledWith('/admin/blog?page=2&pageSize=12', { scroll: false })
    expect(screen.getByLabelText('Edit post: Blog 13')).toHaveAttribute(
      'href',
      '/admin/blog/b13?returnTo=%2Fadmin%2Fblog%3Fpage%3D2%26pageSize%3D12',
    )
  })

  it('shows blog bulk delete button when rows are selected', async () => {
    vi.stubGlobal('prompt', promptMock)

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
    vi.stubGlobal('prompt', promptMock)

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
    vi.stubGlobal('prompt', promptMock)

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
      expect(refreshMock).not.toHaveBeenCalled()
    })
  })

  it('filters works by title and exposes previous/next pagination controls', () => {
    vi.stubGlobal('prompt', promptMock)

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
})
