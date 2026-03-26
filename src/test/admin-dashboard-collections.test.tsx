import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { AdminDashboardCollections } from '@/components/admin/AdminDashboardCollections'

vi.mock('@/hooks/useResponsivePageSize', () => ({
  useResponsivePageSize: vi.fn(() => 1),
}))

describe('AdminDashboardCollections', () => {
  it('renders empty states when no works or blog posts exist', () => {
    render(<AdminDashboardCollections works={[]} blogs={[]} />)

    expect(screen.getByText('No works found.')).toBeInTheDocument()
    expect(screen.getByText('No blog posts found.')).toBeInTheDocument()
    expect(screen.getAllByText('1 / 1')).toHaveLength(2)
  })

  it('paginates works and blog posts independently and renders branch-specific metadata', () => {
    render(
      <AdminDashboardCollections
        works={[
          {
            id: 'work-1',
            title: 'Work one',
            slug: 'work-one',
            excerpt: 'work excerpt',
            category: 'platform',
            tags: [],
            published: true,
            publishedAt: '2024-01-01T00:00:00.000Z',
          },
          {
            id: 'work-2',
            title: 'Work two',
            slug: 'work-two',
            excerpt: '',
            category: '',
            tags: [],
            published: false,
          },
        ]}
        blogs={[
          {
            id: 'blog-1',
            title: 'Blog one',
            slug: 'blog-one',
            excerpt: 'blog excerpt',
            tags: ['alpha'],
            published: true,
            publishedAt: '2024-01-02T00:00:00.000Z',
          },
          {
            id: 'blog-2',
            title: 'Blog two',
            slug: 'blog-two',
            excerpt: '',
            tags: [],
            published: false,
          },
        ]}
      />,
    )

    expect(screen.getByText('Work one')).toBeInTheDocument()
    expect(screen.getByText('platform')).toBeInTheDocument()
    expect(screen.getByText('Blog one')).toBeInTheDocument()
    expect(screen.getByText('alpha')).toBeInTheDocument()
    expect(screen.getAllByText('Published')).toHaveLength(2)

    const nextButtons = screen.getAllByRole('button', { name: '다음' })
    fireEvent.click(nextButtons[0])
    fireEvent.click(nextButtons[1])

    expect(screen.getByText('Work two')).toBeInTheDocument()
    expect(screen.getByText('Uncategorized')).toBeInTheDocument()
    expect(screen.getByText('Blog two')).toBeInTheDocument()
    expect(screen.getByText('No tags')).toBeInTheDocument()
    expect(screen.getAllByText('Draft')).toHaveLength(2)
    expect(screen.getAllByText('—')).toHaveLength(2)

    const previousButtons = screen.getAllByRole('button', { name: '이전' })
    fireEvent.click(previousButtons[0])
    fireEvent.click(previousButtons[1])

    expect(screen.getByText('Work one')).toBeInTheDocument()
    expect(screen.getByText('Blog one')).toBeInTheDocument()
  })
})
