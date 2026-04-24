import { cleanup, render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { TableOfContents, slugifyHeading } from '@/components/content/TableOfContents'

beforeEach(() => {
  class MockIntersectionObserver implements IntersectionObserver {
    readonly root = null
    readonly rootMargin = '0px'
    readonly thresholds = [0]
    disconnect() {}
    observe() {}
    takeRecords() { return [] }
    unobserve() {}
  }

  vi.stubGlobal('IntersectionObserver', MockIntersectionObserver)
})

afterEach(() => {
  cleanup()
  document.body.innerHTML = ''
  vi.unstubAllGlobals()
})

describe('slugifyHeading', () => {
  it('normalizes heading copy into stable slugs', () => {
    expect(slugifyHeading('Why Seed Data Matters')).toBe('why-seed-data-matters')
    expect(slugifyHeading('운영 관점의 이점')).toBe('운영-관점의-이점')
  })

  it('appends suffixes for duplicate headings', () => {
    const usedSlugs = new Map<string, number>()

    expect(slugifyHeading('Highlights', usedSlugs)).toBe('highlights')
    expect(slugifyHeading('Highlights', usedSlugs)).toBe('highlights-2')
  })

  it('keeps TOC shell visible with a fallback message when no headings exist', async () => {
    document.body.innerHTML = '<article id="toc-empty-content"><p>No headings yet.</p></article>'

    render(<TableOfContents contentRootId="toc-empty-content" />)

    await waitFor(() => {
      expect(screen.getByTestId('blog-toc')).toBeInTheDocument()
    })
    expect(screen.getByTestId('blog-toc-empty')).toHaveTextContent('No sections yet')
  })

  it('collects h1, h2, and h3 headings including hash-prefixed titles', async () => {
    document.body.innerHTML = `
      <article id="toc-rich-content">
        <h1># Overview</h1>
        <h2>Details</h2>
        <h3>Sub details</h3>
      </article>
    `

    render(<TableOfContents contentRootId="toc-rich-content" />)

    await waitFor(() => {
      expect(screen.getByRole('link', { name: '# Overview' })).toBeInTheDocument()
    })
    expect(screen.getByRole('link', { name: 'Details' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Sub details' })).toBeInTheDocument()
  })
})
