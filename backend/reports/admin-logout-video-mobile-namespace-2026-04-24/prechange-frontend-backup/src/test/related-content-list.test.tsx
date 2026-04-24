import { fireEvent, render, screen } from '@testing-library/react'
import type { ReactNode } from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { RelatedContentList } from '@/components/content/RelatedContentList'

const mocks = vi.hoisted(() => ({
  replace: vi.fn(),
  searchParams: '',
}))

vi.mock('next/link', () => ({
  default: ({ href, children, ...props }: { href: string; children: ReactNode }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}))

vi.mock('next/navigation', () => ({
  useRouter: () => ({ replace: mocks.replace }),
  usePathname: () => '/blog/current',
  useSearchParams: () => new URLSearchParams(mocks.searchParams),
}))

vi.mock('@/hooks/useResponsivePageSize', () => ({
  useResponsivePageSize: () => 2,
}))

const items = [
  { id: 'post-1', slug: 'post-1', title: 'Post 1', excerpt: 'Excerpt 1', publishedAt: '2026-04-01T00:00:00.000Z' },
  { id: 'post-2', slug: 'post-2', title: 'Post 2', excerpt: 'Excerpt 2', publishedAt: '2026-04-02T00:00:00.000Z' },
  { id: 'post-3', slug: 'post-3', title: 'Post 3', excerpt: 'Excerpt 3', publishedAt: '2026-04-03T00:00:00.000Z' },
]

describe('RelatedContentList', () => {
  beforeEach(() => {
    mocks.replace.mockClear()
    mocks.searchParams = ''
  })

  it('opens on the page containing the current item and highlights it without a link', () => {
    render(
      <RelatedContentList
        heading="More Studies"
        hrefBase="/blog"
        items={items}
        currentItemId="post-3"
        testIdBase="related-blog"
      />,
    )

    const currentCard = screen.getByTestId('related-blog-current-card')
    expect(currentCard).toHaveTextContent('Post 3')
    expect(currentCard).toHaveTextContent('Current')
    expect(currentCard.querySelector('a')).toBeNull()
    expect(screen.queryByText('Excerpt 3')).not.toBeInTheDocument()
    expect(screen.getByText('Page 2 of 2')).toBeInTheDocument()
    expect(screen.getByText('1 visible')).toBeInTheDocument()
  })

  it('updates relatedPage while preserving navigation through router state', () => {
    render(
      <RelatedContentList
        heading="More Studies"
        hrefBase="/blog"
        items={items}
        currentItemId="post-1"
        testIdBase="related-blog"
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Go to page 2' }))

    expect(mocks.replace).toHaveBeenCalledWith('/blog/current?relatedPage=2', { scroll: false })
  })

  it('uses Previous for the front page and Next for the back page', () => {
    mocks.searchParams = 'relatedPage=2'

    render(
      <RelatedContentList
        heading="More Studies"
        hrefBase="/blog"
        items={items}
        currentItemId="post-1"
        testIdBase="related-blog"
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Go to previous related page' }))
    expect(mocks.replace).toHaveBeenCalledWith('/blog/current?relatedPage=1', { scroll: false })

    fireEvent.click(screen.getByRole('button', { name: 'Go to next related page' }))
    expect(mocks.replace).toHaveBeenCalledWith('/blog/current?relatedPage=2', { scroll: false })
  })
})
