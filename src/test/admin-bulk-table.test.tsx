import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { AdminBlogTableClient } from '@/components/admin/AdminBlogTableClient'
import { AdminWorksTableClient } from '@/components/admin/AdminWorksTableClient'

const refreshMock = vi.fn()
const promptMock = vi.fn(() => 'yes')

vi.mock('next/navigation', () => ({
  useRouter: () => ({
    refresh: refreshMock,
  }),
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

describe('admin bulk selection tables', () => {
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
  })

  it('filters blog rows by title and exposes first/last pagination controls', async () => {
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
    expect(screen.getByRole('button', { name: '처음' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: '끝' })).toBeInTheDocument()
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

  it('filters works by title and exposes first/last pagination controls', () => {
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
    expect(screen.getByRole('button', { name: '처음' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: '끝' })).toBeInTheDocument()
  })
})
