"use client"

import Image from 'next/image'
import Link from 'next/link'
import { BriefcaseBusiness } from 'lucide-react'
import { useCallback, useEffect, useRef, useState } from 'react'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import type { BlogListItem, PagedBlogsPayload } from '@/lib/api/blogs'
import type { PagedWorksPayload, WorkListItem } from '@/lib/api/works'

type FeedKind = 'blog' | 'works'
type FeedItem = BlogListItem | WorkListItem
type FeedPayload = PagedBlogsPayload | PagedWorksPayload

interface PublicResponsiveFeedProps {
  kind: FeedKind
  query: string
  desktopPayload: FeedPayload
  mobileInitialPayload: FeedPayload
  desktopReturnTo: string
}

const MOBILE_PAGE_SIZE = 10

function formatBlogDate(publishedAt?: string | null) {
  return publishedAt
    ? new Date(publishedAt).toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
      })
    : 'Unknown Date'
}

function formatWorkDate(publishedAt?: string | null) {
  return publishedAt
    ? new Date(publishedAt).toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
      })
    : 'Unknown Date'
}

function isWorkItem(item: FeedItem): item is WorkListItem {
  return 'category' in item
}

function buildMobileReturnTo(kind: FeedKind, query: string) {
  const pathname = kind === 'blog' ? '/blog' : '/works'
  const params = new URLSearchParams({
    page: '1',
    pageSize: String(MOBILE_PAGE_SIZE),
  })

  if (query) {
    params.set('query', query)
  }

  return encodeURIComponent(`${pathname}?${params.toString()}`)
}

function buildApiUrl(kind: FeedKind, page: number, query: string) {
  const endpoint = kind === 'blog' ? '/api/public/blogs' : '/api/public/works'
  const params = new URLSearchParams({
    page: String(page),
    pageSize: String(MOBILE_PAGE_SIZE),
  })

  if (query) {
    params.set('query', query)
  }

  return `${endpoint}?${params.toString()}`
}

function dedupeItems(existing: FeedItem[], incoming: FeedItem[]) {
  const seen = new Set(existing.map((item) => item.id))
  const next = [...existing]

  for (const item of incoming) {
    if (!seen.has(item.id)) {
      seen.add(item.id)
      next.push(item)
    }
  }

  return next
}

export function PublicResponsiveFeed({
  kind,
  query,
  desktopPayload,
  mobileInitialPayload,
  desktopReturnTo,
}: PublicResponsiveFeedProps) {
  const [isCompact, setIsCompact] = useState(false)
  const [items, setItems] = useState<FeedItem[]>(mobileInitialPayload.items)
  const [page, setPage] = useState(mobileInitialPayload.page)
  const [totalPages, setTotalPages] = useState(Math.max(1, mobileInitialPayload.totalPages))
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const loadingRef = useRef(false)

  useEffect(() => {
    const queryList = window.matchMedia('(max-width: 1023px)')
    const syncMode = () => setIsCompact(queryList.matches)

    syncMode()
    queryList.addEventListener('change', syncMode)

    return () => {
      queryList.removeEventListener('change', syncMode)
    }
  }, [])

  useEffect(() => {
    setItems(mobileInitialPayload.items)
    setPage(mobileInitialPayload.page)
    setTotalPages(Math.max(1, mobileInitialPayload.totalPages))
    setError(null)
  }, [mobileInitialPayload, query])

  const hasMore = page < totalPages

  const loadNextPage = useCallback(async () => {
    if (!isCompact || !hasMore || loadingRef.current) {
      return
    }

    const nextPage = page + 1
    loadingRef.current = true
    setLoading(true)
    setError(null)

    try {
      const response = await fetch(buildApiUrl(kind, nextPage, query), {
        credentials: 'same-origin',
        cache: 'no-store',
      })

      if (!response.ok) {
        throw new Error('Failed to load more items.')
      }

      const payload = await response.json() as FeedPayload
      setItems((currentItems) => dedupeItems(currentItems, payload.items))
      setPage(payload.page)
      setTotalPages(Math.max(1, payload.totalPages))
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Failed to load more items.')
    } finally {
      loadingRef.current = false
      setLoading(false)
    }
  }, [hasMore, isCompact, kind, page, query])

  const renderedItems = isCompact ? items : desktopPayload.items
  const returnTo = isCompact ? buildMobileReturnTo(kind, query) : desktopReturnTo
  const emptyText = kind === 'blog'
    ? query ? 'No studies found.' : 'No blog posts found.'
    : 'No works found.'
  const gridClassName = kind === 'blog'
    ? 'grid gap-5 md:grid-cols-2 xl:grid-cols-3'
    : 'grid gap-5 md:grid-cols-2 xl:grid-cols-4'

  return (
    <>
      <div
        data-testid={kind === 'blog' ? 'blog-grid' : 'works-grid'}
        data-feed-testid={`${kind}-responsive-feed`}
        data-feed-mode={isCompact ? 'load-more' : 'pagination'}
        className={gridClassName}
      >
        {renderedItems.length > 0 ? (
          renderedItems.map((item) => (
            kind === 'blog'
              ? <BlogCard key={item.id} item={item as BlogListItem} returnTo={returnTo} relatedPage={isCompact ? 1 : desktopPayload.page} />
              : <WorkCard key={item.id} item={item as WorkListItem} returnTo={returnTo} relatedPage={isCompact ? 1 : desktopPayload.page} />
          ))
        ) : (
          <div className={kind === 'blog' ? 'py-20 text-center text-muted-foreground' : 'col-span-full rounded-2xl border border-dashed border-border/80 bg-muted/30 px-6 py-20 text-center text-muted-foreground'}>
            {emptyText}
          </div>
        )}
      </div>

      {isCompact ? (
        <div className="mt-8 flex flex-col items-center gap-3" data-testid={`${kind}-infinite-controls`}>
          {error ? (
            <p role="status" className="text-sm text-destructive">{error}</p>
          ) : null}
          {hasMore ? (
            <button
              type="button"
              data-testid={`${kind}-load-more`}
              onClick={() => void loadNextPage()}
              disabled={loading}
              className="inline-flex min-h-11 items-center justify-center rounded-full border border-border px-4 py-2 text-sm font-medium text-foreground transition-colors hover:bg-muted disabled:cursor-wait disabled:opacity-70"
            >
              {loading ? 'Loading...' : 'Load more'}
            </button>
          ) : (
            <p className="text-sm text-muted-foreground" role="status">End of list</p>
          )}
        </div>
      ) : null}
    </>
  )
}

function BlogCard({ item, returnTo, relatedPage }: { item: BlogListItem; returnTo: string; relatedPage: number }) {
  return (
    <Link
      href={`/blog/${item.slug}?returnTo=${returnTo}&relatedPage=${relatedPage}`}
      prefetch={false}
      className="group/card block h-full min-w-0"
      data-testid="blog-card"
    >
      <Card className="responsive-feed-card flex h-full min-w-0 flex-col gap-0 overflow-hidden rounded-2xl border-border/80 bg-background py-0 shadow-sm transition hover:border-primary/30 hover:shadow-md">
        <div data-testid="blog-card-accent-stripe" className="study-card-stripe h-1 w-full rounded-t-2xl" />
        <CardHeader className="min-w-0 px-4 pt-4 pb-0 sm:px-5 sm:pt-5">
          <div className="mb-2 flex flex-wrap items-center gap-2">
            <Badge variant="secondary" className="rounded-full bg-brand-navy px-2.5 py-0.5 text-xs text-white hover:bg-brand-navy/90">
              {formatBlogDate(item.publishedAt)}
            </Badge>
            {item.tags?.slice(0, 1).map((tag) => (
              <span key={tag} className="max-w-full rounded-full bg-muted px-2 py-0.5 text-xs font-medium text-muted-foreground [overflow-wrap:anywhere]">
                {tag}
              </span>
            ))}
          </div>
          <CardTitle className="responsive-feed-title line-clamp-2 min-w-0 break-words text-lg font-heading font-bold leading-tight text-foreground transition-colors [overflow-wrap:anywhere] group-hover/card:text-brand-accent sm:text-xl">
            {item.title}
          </CardTitle>
        </CardHeader>
        <CardContent className="flex min-w-0 flex-1 flex-col px-4 pb-4 pt-3 sm:px-5 sm:pb-5">
          {item.excerpt ? (
            <p className="responsive-feed-copy line-clamp-3 min-w-0 flex-1 break-words text-sm leading-relaxed text-muted-foreground [overflow-wrap:anywhere] sm:text-base">
              {item.excerpt}
            </p>
          ) : null}
        </CardContent>
      </Card>
    </Link>
  )
}

function WorkCard({ item, returnTo, relatedPage }: { item: WorkListItem; returnTo: string; relatedPage: number }) {
  const thumbnailUrl = item.thumbnailUrl ?? null
  const publishDate = formatWorkDate(item.publishedAt)
  const category = isWorkItem(item) ? item.category : ''

  return (
    <Link
      href={`/works/${item.slug}?returnTo=${returnTo}&relatedPage=${relatedPage}`}
      prefetch={false}
      className="group/card block h-full"
      data-testid="work-card"
    >
      <article className="responsive-feed-card works-feed-card flex h-full flex-col overflow-hidden rounded-2xl border border-border/80 bg-background shadow-sm transition hover:border-primary/30 hover:shadow-md">
        <div className="relative aspect-[4/3] overflow-hidden bg-muted">
          {thumbnailUrl ? (
            <Image
              src={thumbnailUrl}
              alt={item.title}
              fill
              unoptimized
              sizes="(max-width: 768px) 100vw, (max-width: 1200px) 50vw, 25vw"
              className="responsive-feed-image object-cover transition-transform duration-500 group-hover/card:scale-105"
            />
          ) : (
            <div data-testid="work-card-no-image-placeholder" className="flex h-full w-full flex-col items-center justify-center gap-2 bg-gradient-to-br from-muted to-muted/80 text-muted-foreground">
              <BriefcaseBusiness className="h-8 w-8" aria-hidden="true" />
              <span className="text-xs font-medium">No Image</span>
            </div>
          )}
        </div>
        <div className="flex flex-1 flex-col p-4 sm:p-5">
          <div className="mb-3 flex flex-wrap items-center gap-2">
            <Badge variant="secondary" className="rounded-full bg-brand-navy px-2.5 py-0.5 text-xs text-white hover:bg-brand-navy/90">
              {publishDate}
            </Badge>
            <span className="responsive-feed-copy text-xs font-medium uppercase tracking-wide text-muted-foreground">
              {category}
            </span>
          </div>
          <h2 className="responsive-feed-title works-feed-title line-clamp-2 text-lg font-heading font-bold leading-tight text-foreground transition-colors group-hover/card:text-brand-accent sm:text-xl">
            {item.title}
          </h2>
          <p className="responsive-feed-copy works-feed-excerpt mt-2 line-clamp-3 flex-1 text-sm leading-relaxed text-foreground/80">
            {item.excerpt}
          </p>
          <div className="works-feed-tags mt-4 flex flex-wrap content-start gap-1.5 overflow-hidden">
            {item.tags?.slice(0, 2).map((tag) => (
              <span key={tag} className="rounded-full bg-muted px-2 py-0.5 text-xs font-medium text-muted-foreground">
                {tag}
              </span>
            ))}
          </div>
        </div>
      </article>
    </Link>
  )
}
