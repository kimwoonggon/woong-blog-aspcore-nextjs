"use client"

import Link from 'next/link'
import { useMemo, useState } from 'react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { useResponsivePageSize } from '@/hooks/useResponsivePageSize'
import type { BlogAdminItem } from '@/lib/api/blogs'
import type { WorkAdminItem } from '@/lib/api/works'

interface CollectionItem {
  id: string
  title: string
  published: boolean
  publishedAt?: string | null
  tags?: string[]
  category?: string
  excerpt?: string
}

interface AdminCollectionSectionProps<T extends CollectionItem> {
  title: string
  emptyMessage: string
  editHrefBase: string
  items: T[]
  desktopPageSize: number
  tabletPageSize: number
  mobilePageSize: number
  renderMeta: (item: T) => React.ReactNode
}

function getPageWindow(currentPage: number, totalPages: number, windowSize = 5) {
  const half = Math.floor(windowSize / 2)
  const start = Math.max(1, Math.min(currentPage - half, totalPages - windowSize + 1))
  const end = Math.min(totalPages, start + windowSize - 1)

  return Array.from({ length: end - start + 1 }, (_, index) => start + index)
}

function formatPublishedDate(publishedAt?: string | null) {
  return publishedAt ? new Date(publishedAt).toLocaleDateString() : '—'
}

function AdminCollectionSection<T extends CollectionItem>({
  title,
  emptyMessage,
  editHrefBase,
  items,
  desktopPageSize,
  tabletPageSize,
  mobilePageSize,
  renderMeta,
}: AdminCollectionSectionProps<T>) {
  const pageSize = useResponsivePageSize(desktopPageSize, tabletPageSize, mobilePageSize)
  const [query, setQuery] = useState('')
  const [page, setPage] = useState(1)
  const filteredItems = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase()
    if (!normalizedQuery) {
      return items
    }

    return items.filter((item) => item.title.toLowerCase().includes(normalizedQuery))
  }, [items, query])
  const totalPages = Math.max(1, Math.ceil(filteredItems.length / pageSize))
  const currentPage = Math.min(page, totalPages)

  const visibleItems = useMemo(() => {
    const start = (currentPage - 1) * pageSize
    return filteredItems.slice(start, start + pageSize)
  }, [currentPage, filteredItems, pageSize])

  const pageWindow = getPageWindow(currentPage, totalPages)

  return (
    <section className="space-y-4">
      <div className="flex items-center justify-between gap-3">
        <div className="flex-1">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-gray-50">{title}</h2>
          <p className="text-sm text-muted-foreground">{filteredItems.length} shown / {items.length} total · 클릭하면 바로 편집 페이지로 이동합니다.</p>
          <Input
            value={query}
            onChange={(event) => {
              setQuery(event.target.value)
              setPage(1)
            }}
            placeholder={`${title} title search`}
            aria-label={`${title} title search`}
            className="mt-3 max-w-sm"
          />
        </div>
        <span className="rounded-full border px-3 py-1 text-xs font-medium text-muted-foreground">
          {pageSize} per page
        </span>
      </div>

      {visibleItems.length > 0 ? (
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {visibleItems.map((item) => (
            <Link
              key={item.id}
              href={`${editHrefBase}/${item.id}`}
              className="group block"
              data-testid={`${title.toLowerCase().replace(/\s+/g, '-')}-card-link`}
            >
              <article className="responsive-feed-card h-full rounded-2xl border border-border/80 bg-background p-4 shadow-sm transition hover:border-primary/30 hover:shadow-md">
                <div className="mb-3 flex items-start justify-between gap-3">
                  <h3 className="responsive-feed-title text-lg font-semibold text-gray-900 transition-colors group-hover:text-primary dark:text-gray-50">
                    {item.title}
                  </h3>
                  {item.published ? (
                    <Badge variant="secondary" className="bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-300">
                      Published
                    </Badge>
                  ) : (
                    <Badge variant="secondary" className="bg-yellow-100 text-yellow-800 dark:bg-yellow-900/40 dark:text-yellow-300">
                      Draft
                    </Badge>
                  )}
                </div>
                <div className="responsive-feed-copy space-y-2 text-sm text-muted-foreground">
                  <p>{formatPublishedDate(item.publishedAt)}</p>
                  {renderMeta(item)}
                  {item.excerpt ? <p className="line-clamp-3">{item.excerpt}</p> : null}
                </div>
              </article>
            </Link>
          ))}
        </div>
      ) : (
        <div className="rounded-2xl border border-dashed border-border/80 bg-background/60 p-6 text-sm text-muted-foreground">
          {emptyMessage}
        </div>
      )}

      <div className="rounded-2xl border border-border/70 bg-background/80 p-4 shadow-sm">
        <div className="flex flex-wrap items-center justify-center gap-2">
          {pageWindow.map((pageNumber) => (
            <Button
              key={pageNumber}
              type="button"
              variant={pageNumber === currentPage ? 'default' : 'outline'}
              size="sm"
              onClick={() => setPage(pageNumber)}
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
            onClick={() => setPage((activePage) => Math.max(1, Math.min(activePage, totalPages) - 1))}
          >
            이전
          </Button>
          <span className="text-sm text-muted-foreground">
            {currentPage} / {totalPages}
          </span>
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={currentPage >= totalPages}
            onClick={() => setPage((activePage) => Math.min(totalPages, Math.min(activePage, totalPages) + 1))}
          >
            다음
          </Button>
        </div>
      </div>
    </section>
  )
}

interface AdminDashboardCollectionsProps {
  works: WorkAdminItem[]
  blogs: BlogAdminItem[]
}

export function AdminDashboardCollections({ works, blogs }: AdminDashboardCollectionsProps) {
  return (
    <div className="space-y-10">
      <AdminCollectionSection
        title="Works"
        emptyMessage="No works found."
        editHrefBase="/admin/works"
        items={works}
        desktopPageSize={6}
        tabletPageSize={4}
        mobilePageSize={2}
        renderMeta={(item) => <p>{item.category || 'Uncategorized'}</p>}
      />

      <AdminCollectionSection
        title="Blog Posts"
        emptyMessage="No blog posts found."
        editHrefBase="/admin/blog"
        items={blogs}
        desktopPageSize={6}
        tabletPageSize={3}
        mobilePageSize={1}
        renderMeta={(item) => <p>{item.tags?.join(', ') || 'No tags'}</p>}
      />
    </div>
  )
}
