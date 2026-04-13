import { describe, expect, it } from 'vitest'
import { slugifyHeading } from '@/components/content/TableOfContents'

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
})
