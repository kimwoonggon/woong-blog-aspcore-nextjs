import { describe, expect, it } from 'vitest'
import { resolveResponsivePageSize } from '@/lib/responsive-page-size'

describe('resolveResponsivePageSize', () => {
  it('returns mobile page size for narrow viewports', () => {
    expect(resolveResponsivePageSize({
      width: 390,
      height: 844,
      desktopPageSize: 10,
      tabletPageSize: 4,
      mobilePageSize: 1,
    })).toBe(1)
  })

  it('interpolates between mobile and tablet sizes on mid-width screens', () => {
    expect(resolveResponsivePageSize({
      width: 900,
      height: 1200,
      desktopPageSize: 10,
      tabletPageSize: 4,
      mobilePageSize: 1,
    })).toBeGreaterThanOrEqual(2)
  })

  it('interpolates between mobile and desktop sizes on wide screens', () => {
    expect(resolveResponsivePageSize({
      width: 1440,
      height: 1800,
      desktopPageSize: 10,
      tabletPageSize: 4,
      mobilePageSize: 1,
    })).toBe(10)
  })

  it('never returns more than the configured desktop page size', () => {
    expect(resolveResponsivePageSize({
      width: 1600,
      height: 2400,
      desktopPageSize: 6,
      tabletPageSize: 4,
      mobilePageSize: 2,
    })).toBe(6)
  })

  it('falls back to the safer higher floor when desktop is configured below mobile', () => {
    expect(resolveResponsivePageSize({
      width: 1440,
      height: 1800,
      desktopPageSize: 1,
      tabletPageSize: 1,
      mobilePageSize: 2,
    })).toBe(2)
  })
})
