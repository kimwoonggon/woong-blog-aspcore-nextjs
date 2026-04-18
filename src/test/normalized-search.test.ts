import { describe, expect, it } from 'vitest'
import { containsNormalizedSearch, normalizeSearchText } from '@/lib/search/normalized-search'

describe('normalized search', () => {
  it('ignores case, punctuation, and spacing when matching compact acronyms', () => {
    const target = 'T,B,N 안녕하세요'

    expect(containsNormalizedSearch(target, 'TB')).toBe(true)
    expect(containsNormalizedSearch(target, 'TBN')).toBe(true)
    expect(containsNormalizedSearch(target, 'tbn')).toBe(true)
  })

  it('keeps Korean and English text searchable after normalization', () => {
    expect(normalizeSearchText(' Next.js  테스트 ')).toContain('nextjs')
    expect(normalizeSearchText(' Next.js  테스트 ')).toContain('테스트')
  })
})
