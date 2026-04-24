"use client"

import { useEffect } from 'react'
import { usePathname, useRouter, useSearchParams } from 'next/navigation'
import { resolveResponsivePageSize } from '@/lib/responsive-page-size'

interface ResponsivePageSizeSyncProps {
  desktopPageSize: number
  tabletPageSize: number
  mobilePageSize: number
  infiniteBelowDesktop?: boolean
  infinitePageSize?: number
}

export function ResponsivePageSizeSync({
  desktopPageSize,
  tabletPageSize,
  mobilePageSize,
  infiniteBelowDesktop = false,
  infinitePageSize = 10,
}: ResponsivePageSizeSyncProps) {
  const router = useRouter()
  const pathname = usePathname()
  const searchParams = useSearchParams()

  useEffect(() => {
    const sync = () => {
      const isBelowDesktop = window.innerWidth < 1024
      if (infiniteBelowDesktop && isBelowDesktop) {
        const currentPage = searchParams.get('page')
        const currentPageSize = searchParams.get('pageSize')
        const params = new URLSearchParams(searchParams.toString())
        params.set('page', '1')
        params.set('pageSize', String(infinitePageSize))
        params.delete('searchMode')

        if (
          currentPage === '1'
          && currentPageSize === String(infinitePageSize)
          && !searchParams.has('searchMode')
        ) {
          return
        }

        router.replace(`${pathname}?${params.toString()}`, { scroll: false })
        return
      }

      const desiredPageSize = resolveResponsivePageSize({
        width: window.innerWidth,
        height: window.innerHeight,
        desktopPageSize,
        tabletPageSize,
        mobilePageSize,
      })
      const currentPageSize = Number.parseInt(searchParams.get('pageSize') ?? '', 10)

      if (
        Number.isFinite(currentPageSize)
        && currentPageSize > 0
        && (currentPageSize === desiredPageSize || currentPageSize < mobilePageSize)
      ) {
        return
      }

      const params = new URLSearchParams(searchParams.toString())
      params.set('pageSize', String(desiredPageSize))
      if (!params.get('page')) {
        params.set('page', '1')
      }

      router.replace(`${pathname}?${params.toString()}`, { scroll: false })
    }

    sync()
    window.addEventListener('resize', sync)

    return () => {
      window.removeEventListener('resize', sync)
    }
  }, [desktopPageSize, infiniteBelowDesktop, infinitePageSize, mobilePageSize, pathname, router, searchParams, tabletPageSize])

  return null
}
