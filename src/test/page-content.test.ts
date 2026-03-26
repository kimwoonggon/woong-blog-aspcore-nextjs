import { describe, expect, it } from 'vitest'
import {
  isBlockPageContent,
  isHtmlPageContent,
  parsePageContentJson,
  toHomeContent,
} from '@/lib/content/page-content'

describe('page-content helpers', () => {
  it('parses raw JSON strings into unknown content values', () => {
    expect(parsePageContentJson(undefined)).toBeNull()
    expect(parsePageContentJson('{"html":"<p>Hello</p>"}')).toEqual({ html: '<p>Hello</p>' })
  })

  it('detects html and block content safely', () => {
    expect(isHtmlPageContent({ html: '<p>Hello</p>' })).toBe(true)
    expect(isHtmlPageContent({ html: 1 })).toBe(false)

    expect(isBlockPageContent({
      blocks: [{ id: '1', type: 'p', text: 'Hello' }],
    })).toBe(true)
    expect(isBlockPageContent({
      blocks: [{ id: 1, type: 'p' }],
    })).toBe(false)
  })

  it('filters home content down to optional string fields', () => {
    expect(toHomeContent(null)).toEqual({})
    expect(toHomeContent({
      headline: 'Headline',
      introText: 'Intro',
      profileImageUrl: '/avatar.png',
      ignored: 1,
    })).toEqual({
      headline: 'Headline',
      introText: 'Intro',
      profileImageUrl: '/avatar.png',
    })
    expect(toHomeContent({ headline: 1 })).toEqual({
      headline: undefined,
      introText: undefined,
      profileImageUrl: undefined,
    })
  })
})
