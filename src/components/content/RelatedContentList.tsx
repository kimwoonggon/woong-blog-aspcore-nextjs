"use client"

import Link from 'next/link'
import { useMemo, useState } from 'react'
import { usePathname, useRouter, useSearchParams } from 'next/navigation'
import { Button } from '@/components/ui/button'
import { useResponsivePageSize } from '@/hooks/useResponsivePageSize'

interface RelatedContentItem {
  id: string
  slug: string
  title: string
  excerpt: string
  publishedAt?: string | null
  tags?: string[]
  category?: string
}

interface RelatedContentListProps {
  heading: string
  hrefBase: '/blog' | '/works'
  items: RelatedContentItem[]
  desktopPageSize?: number
  tabletPageSize?: number
  mobilePageSize?: number
  testIdBase: string
}

function formatPublishedDate(publishedAt?: string | null) {
  return publishedAt ? new Date(publishedAt).toLocaleDateString() : '—'
}

interface RelatedContentPagerProps extends RelatedContentListProps {
  pageSize: number
}

function getPageWindow(currentPage: number, totalPages: number, radius = 2) {
  const windowSize = radius * 2 + 1
  const start = Math.max(1, Math.min(currentPage - radius, totalPages - windowSize + 1))
  const end = Math.min(totalPages, start + windowSize - 1)
  return Array.from({ length: end - start + 1 }, (_, index) => start + index)
}

function RelatedContentPager({
  heading,
  hrefBase,
  items,
  pageSize,
  testIdBase,
}: RelatedContentPagerProps) {
  const router = useRouter()
  const pathname = usePathname()
  const searchParams = useSearchParams()
  const initialPage = Math.max(1, Number.parseInt(searchParams.get('relatedPage') ?? '1', 10) || 1)
  const [page, setPage] = useState(initialPage)
  const totalPages = Math.max(1, Math.ceil(items.length / pageSize))
  const currentPage = Math.min(page, totalPages)

  const visibleItems = useMemo(() => {
    const start = (currentPage - 1) * pageSize
    return items.slice(start, start + pageSize)
  }, [currentPage, items, pageSize])

  const pageWindow = getPageWindow(currentPage, totalPages)

  const updateRelatedPage = (nextPage: number) => {
    const params = new URLSearchParams(searchParams.toString())
    params.set('relatedPage', String(nextPage))
    router.replace(`${pathname}?${params.toString()}`, { scroll: false })
    setPage(nextPage)
  }

  if (items.length === 0) {
    return null
  }

  const followUpCopy = hrefBase === '/works'
    ? '다른 작업을 이어서 볼 수 있습니다.'
    : '다른 게시물을 바로 이어서 볼 수 있습니다.'

  return (
    <section className="mt-12 space-y-4">
      <div className="flex items-center justify-between gap-3">
        <div>
          <h2 className="text-2xl font-heading font-bold text-gray-900 dark:text-gray-50">{heading}</h2>
          <p className="text-sm text-muted-foreground">{followUpCopy}</p>
        </div>
        <span className="rounded-full border px-3 py-1 text-xs font-medium text-muted-foreground">
          {pageSize} per page
        </span>
      </div>

      <div data-testid={`${testIdBase}-grid`} className="grid auto-rows-fr gap-4 md:grid-cols-2 xl:grid-cols-3">
        {visibleItems.map((item) => (
          <Link
            key={item.id}
            href={`${hrefBase}/${item.slug}?relatedPage=${currentPage}`}
            className="group block h-full"
            data-testid={`${testIdBase}-card`}
          >
            <article className="responsive-feed-card flex h-full flex-col rounded-2xl border border-border/80 bg-background p-4 shadow-sm transition hover:border-primary/30 hover:shadow-md">
              <div className="mb-3 flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
                <span>{formatPublishedDate(item.publishedAt)}</span>
                {item.category ? (
                  <span className="rounded-full border px-2 py-0.5 uppercase tracking-wide">{item.category}</span>
                ) : null}
              </div>
              <h3 className="responsive-feed-title line-clamp-2 text-lg font-semibold text-gray-900 transition-colors group-hover:text-primary dark:text-gray-50">
                {item.title}
              </h3>
              <p className="responsive-feed-copy mt-2 line-clamp-3 flex-1 text-sm text-muted-foreground">{item.excerpt}</p>
              {item.tags?.length ? (
                <div className="responsive-feed-copy mt-3 flex flex-wrap gap-2 text-xs text-muted-foreground">
                  {item.tags.slice(0, 3).map((tag) => (
                    <span key={tag}>#{tag}</span>
                  ))}
                </div>
              ) : null}
            </article>
          </Link>
        ))}
      </div>

      <div className="rounded-2xl border border-border/70 bg-background/80 p-4 shadow-sm">
        <div className="flex flex-wrap items-center justify-center gap-2">
          {pageWindow.map((pageNumber) => (
            <Button
              key={pageNumber}
              type="button"
              variant={pageNumber === currentPage ? 'default' : 'outline'}
              size="sm"
              onClick={() => updateRelatedPage(pageNumber)}
            >
              {pageNumber}
            </Button>
          ))}
        </div>
        <div className="mt-3 flex items-center justify-center gap-3">
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={currentPage <= 1}
            onClick={() => updateRelatedPage(1)}
          >
            처음
          </Button>
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={currentPage <= 1}
            onClick={() => updateRelatedPage(Math.max(1, currentPage - 1))}
          >
            이전
          </Button>
          <span data-testid={`${testIdBase}-pagination-status`} className="text-sm text-muted-foreground">
            {currentPage} / {totalPages}
          </span>
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={currentPage >= totalPages}
            onClick={() => updateRelatedPage(Math.min(totalPages, currentPage + 1))}
          >
            다음
          </Button>
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={currentPage >= totalPages}
            onClick={() => updateRelatedPage(totalPages)}
          >
            마지막
          </Button>
        </div>
      </div>
    </section>
  )
}

export function RelatedContentList({
  desktopPageSize = 6,
  tabletPageSize = 4,
  mobilePageSize = 2,
  ...props
}: RelatedContentListProps) {
  const pageSize = useResponsivePageSize(desktopPageSize, tabletPageSize, mobilePageSize)

  if (props.items.length === 0) {
    return null
  }

  return <RelatedContentPager key={`${pageSize}-${props.items.length}`} {...props} pageSize={pageSize} />
}
