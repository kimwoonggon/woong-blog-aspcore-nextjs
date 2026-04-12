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

const relatedDateFormatter = new Intl.DateTimeFormat('en', {
  month: 'short',
  day: 'numeric',
  year: 'numeric',
})

function formatPublishedDate(publishedAt?: string | null) {
  return publishedAt ? relatedDateFormatter.format(new Date(publishedAt)) : '—'
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

  const isBlogList = hrefBase === '/blog'
  const followUpCopy = isBlogList
    ? 'Continue through adjacent writing without losing your place.'
    : 'Move through neighboring case studies without resetting the page.'

  return (
    <section className="mt-12 space-y-5">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <h2 className="text-2xl font-heading font-bold text-foreground text-balance">{heading}</h2>
          <p className="max-w-2xl text-sm text-muted-foreground text-pretty">{followUpCopy}</p>
        </div>
        <span className="rounded-full border border-border/80 bg-background px-3 py-1 text-xs font-medium tabular-nums text-muted-foreground">
          {Math.min(pageSize, items.length)} visible
        </span>
      </div>

      <div data-testid={`${testIdBase}-grid`} className="grid auto-rows-fr gap-4 md:grid-cols-2 xl:grid-cols-3">
        {visibleItems.map((item) => (
          <Link
            key={item.id}
            href={`${hrefBase}/${item.slug}?relatedPage=${currentPage}`}
            className="group block h-full"
            data-testid={`${testIdBase}-card`}
            aria-label={`Open ${item.title}`}
          >
            <article className="responsive-feed-card flex h-full flex-col overflow-hidden rounded-2xl border border-border/80 bg-background py-0 shadow-sm transition hover:border-primary/30 hover:shadow-md">
              {isBlogList ? (
                <div aria-hidden="true" className="h-1 w-full bg-gradient-to-r from-brand-accent to-brand-cyan" />
              ) : null}
              <div className="flex flex-1 flex-col p-4 sm:p-5">
                <div className="mb-3 flex flex-wrap items-center gap-2 text-xs font-medium text-muted-foreground">
                  <time dateTime={item.publishedAt ?? undefined} className="font-mono tabular-nums">
                    {formatPublishedDate(item.publishedAt)}
                  </time>
                  {item.category ? (
                    <span className="rounded-full bg-muted px-2 py-0.5 text-[11px] uppercase tracking-[0.18em] text-muted-foreground">
                      {item.category}
                    </span>
                  ) : null}
                </div>
                <h3 className="responsive-feed-title line-clamp-2 min-w-0 break-words text-lg font-heading font-bold leading-tight text-foreground text-pretty transition-colors group-hover:text-brand-accent sm:text-xl">
                  {item.title}
                </h3>
                {item.excerpt ? (
                  <p className="responsive-feed-copy mt-2 line-clamp-3 flex-1 break-words text-sm leading-relaxed text-foreground/80 sm:text-base">
                    {item.excerpt}
                  </p>
                ) : (
                  <div className="flex-1" aria-hidden="true" />
                )}
                {item.tags?.length ? (
                  <ul aria-label={`${item.title} tags`} className="responsive-feed-copy mt-3 flex flex-wrap gap-2">
                    {item.tags.slice(0, 3).map((tag) => (
                      <li
                        key={tag}
                        className="rounded-full bg-muted px-2 py-0.5 text-xs font-medium text-muted-foreground"
                      >
                        #{tag}
                      </li>
                    ))}
                  </ul>
                ) : null}
              </div>
            </article>
          </Link>
        ))}
      </div>

      <nav aria-label={`${heading} pagination`} className="rounded-2xl border border-border/70 bg-background/80 p-4 shadow-sm">
        <div className="flex flex-wrap items-center justify-center gap-2">
          {pageWindow.map((pageNumber) => (
            <Button
              key={pageNumber}
              type="button"
              variant={pageNumber === currentPage ? 'default' : 'outline'}
              size="sm"
              aria-current={pageNumber === currentPage ? 'page' : undefined}
              aria-label={`Go to page ${pageNumber}`}
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
            aria-label="Go to first related page"
            onClick={() => updateRelatedPage(1)}
          >
            First
          </Button>
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={currentPage <= 1}
            aria-label="Go to previous related page"
            onClick={() => updateRelatedPage(Math.max(1, currentPage - 1))}
          >
            Previous
          </Button>
          <span className="text-sm tabular-nums text-muted-foreground">
            Page {currentPage} of {totalPages}
          </span>
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={currentPage >= totalPages}
            aria-label="Go to next related page"
            onClick={() => updateRelatedPage(Math.min(totalPages, currentPage + 1))}
          >
            Next
          </Button>
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={currentPage >= totalPages}
            aria-label="Go to last related page"
            onClick={() => updateRelatedPage(totalPages)}
          >
            Last
          </Button>
        </div>
      </nav>
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
