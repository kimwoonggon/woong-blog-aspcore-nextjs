'use client'

import Link from 'next/link'
import { usePathname, useRouter, useSearchParams } from 'next/navigation'
import { useEffect, useMemo, useRef, useState, useSyncExternalStore, useTransition } from 'react'
import { ChevronLeft, ChevronRight, Eye, Pencil, Sparkles, Trash2 } from 'lucide-react'
import { AdminBlogBatchAiPanel } from '@/components/admin/AdminBlogBatchAiPanel'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Checkbox } from '@/components/ui/checkbox'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import type { BlogAdminItem } from '@/lib/api/blogs'
import { deleteAdminBlog, deleteManyAdminBlogs } from '@/lib/api/admin-mutations'
import { useResponsivePageSize } from '@/hooks/useResponsivePageSize'
import { anyContainsNormalizedSearch } from '@/lib/search/normalized-search'
import { toast } from 'sonner'

interface AdminBlogTableClientProps {
  blogs: BlogAdminItem[]
}

interface PendingBlogDelete {
  ids: string[]
  title: string
}

function matchesBlogQuery(blog: BlogAdminItem, query: string) {
  return anyContainsNormalizedSearch([blog.title, ...blog.tags], query)
}

function normalizePageParam(value: string | null) {
  const parsed = Number.parseInt(value ?? '', 10)
  return Number.isFinite(parsed) && parsed > 0 ? parsed : 1
}

function deferStateUpdate(callback: () => void) {
  queueMicrotask(callback)
}

function replaceBrowserUrl(pathname: string, queryString: string) {
  if (typeof window === 'undefined') {
    return
  }

  window.history.replaceState(window.history.state, '', queryString ? `${pathname}?${queryString}` : pathname)
}

function subscribeToHydration() {
  return () => undefined
}

function getHydratedSnapshot() {
  return true
}

function getServerHydratedSnapshot() {
  return false
}

export function AdminBlogTableClient({ blogs }: AdminBlogTableClientProps) {
  const router = useRouter()
  const pathname = usePathname()
  const searchParams = useSearchParams()
  const requestedPage = normalizePageParam(searchParams.get('page'))
  const requestedQuery = searchParams.get('query') ?? ''
  const searchParamsKey = searchParams.toString()
  const lastAppliedUrlPageRef = useRef(requestedPage)
  const lastWrittenSearchParamsRef = useRef<string | null>(null)
  const hasMountedSearchSyncRef = useRef(false)
  const hasMountedUrlWriteRef = useRef(false)
  const searchInputRef = useRef<HTMLInputElement>(null)
  const [selectedIds, setSelectedIds] = useState<string[]>([])
  const [showBatchAiPanel, setShowBatchAiPanel] = useState(false)
  const [query, setQuery] = useState(requestedQuery)
  const [pendingDelete, setPendingDelete] = useState<PendingBlogDelete | null>(null)
  const [page, setPage] = useState(requestedPage)
  const deleteRestoreFocusRef = useRef<HTMLElement | null>(null)
  const isInteractive = useSyncExternalStore(subscribeToHydration, getHydratedSnapshot, getServerHydratedSnapshot)
  const [isPending, startTransition] = useTransition()
  const pageSize = useResponsivePageSize(12, 8, 6)
  const filteredBlogs = useMemo(() => {
    const trimmedQuery = query.trim()
    if (!trimmedQuery) {
      return blogs
    }

    return blogs.filter((blog) => matchesBlogQuery(blog, trimmedQuery))
  }, [blogs, query])
  const totalPages = Math.max(1, Math.ceil(filteredBlogs.length / pageSize))
  const currentPage = Math.min(page, totalPages)
  const returnTo = useMemo(() => {
    const params = new URLSearchParams()
    if (currentPage > 1) {
      params.set('page', String(currentPage))
    }
    if (pageSize > 0) {
      params.set('pageSize', String(pageSize))
    }
    if (query.trim()) {
      params.set('query', query.trim())
    }

    const suffix = params.toString()
    return encodeURIComponent(suffix ? `${pathname}?${suffix}` : pathname)
  }, [currentPage, pageSize, pathname, query])
  const visibleBlogs = useMemo(() => {
    const start = (currentPage - 1) * pageSize
    return filteredBlogs.slice(start, start + pageSize)
  }, [currentPage, filteredBlogs, pageSize])
  const visibleIds = useMemo(() => visibleBlogs.map((blog) => blog.id), [visibleBlogs])
  const effectiveSelectedIds = useMemo(
    () => selectedIds.filter((id) => filteredBlogs.some((blog) => blog.id === id)),
    [filteredBlogs, selectedIds],
  )
  const selectedCount = effectiveSelectedIds.length
  const selectedBlogTitles = useMemo(
    () => blogs.filter((blog) => effectiveSelectedIds.includes(blog.id)).map((blog) => blog.title),
    [blogs, effectiveSelectedIds],
  )
  const allSelected = visibleBlogs.length > 0 && visibleIds.every((id) => effectiveSelectedIds.includes(id))
  const selectedSet = useMemo(() => new Set(effectiveSelectedIds), [effectiveSelectedIds])
  const emptyTableMessage = query.trim() ? 'No matching blog posts found.' : 'No blog posts found.'

  useEffect(() => {
    if (!hasMountedSearchSyncRef.current) {
      hasMountedSearchSyncRef.current = true
      return
    }

    if (lastWrittenSearchParamsRef.current === searchParamsKey) {
      lastWrittenSearchParamsRef.current = null
      return
    }

    let cancelled = false
    deferStateUpdate(() => {
      if (!cancelled) {
        if (document.activeElement === searchInputRef.current) {
          return
        }
        setQuery(requestedQuery)
        setSelectedIds([])
      }
    })

    return () => {
      cancelled = true
    }
  }, [requestedQuery, searchParamsKey])

  useEffect(() => {
    const nextRequestedPage = normalizePageParam(new URLSearchParams(searchParamsKey).get('page'))
    if (lastAppliedUrlPageRef.current === nextRequestedPage) {
      return
    }

    lastAppliedUrlPageRef.current = nextRequestedPage
    let cancelled = false
    deferStateUpdate(() => {
      if (!cancelled) {
        setPage(nextRequestedPage)
        setSelectedIds([])
      }
    })

    return () => {
      cancelled = true
    }
  }, [searchParamsKey])

  useEffect(() => {
    if (page !== currentPage) {
      let cancelled = false
      deferStateUpdate(() => {
        if (!cancelled) {
          setPage(currentPage)
          setSelectedIds([])
        }
      })

      return () => {
        cancelled = true
      }
    }
  }, [currentPage, page])

  useEffect(() => {
    if (!hasMountedUrlWriteRef.current) {
      hasMountedUrlWriteRef.current = true
      return
    }

    const params = new URLSearchParams(searchParamsKey)

    if (currentPage > 1) {
      params.set('page', String(currentPage))
    } else {
      params.delete('page')
    }

    params.set('pageSize', String(pageSize))

    if (query.trim()) {
      params.set('query', query.trim())
    } else {
      params.delete('query')
    }

    const nextQueryString = params.toString()
    const currentQueryString = searchParamsKey
    if (nextQueryString === currentQueryString) {
      return
    }

    lastAppliedUrlPageRef.current = currentPage
    lastWrittenSearchParamsRef.current = nextQueryString
    replaceBrowserUrl(pathname, nextQueryString)
  }, [currentPage, pageSize, pathname, query, router, searchParamsKey])

  function toggle(id: string) {
    setSelectedIds((current) =>
      current.includes(id) ? current.filter((item) => item !== id) : [...current, id],
    )
  }

  function toggleAll() {
    setSelectedIds((current) => (
      visibleIds.every((id) => current.includes(id))
        ? current.filter((id) => !visibleIds.includes(id))
        : [...new Set([...current, ...visibleIds])]
    ))
  }

  function requestDelete(ids: string[], title: string, restoreFocusTarget?: HTMLElement | null) {
    if (ids.length === 0 || isPending) {
      return
    }

    deleteRestoreFocusRef.current = restoreFocusTarget ?? null
    setPendingDelete({ ids, title })
  }

  function closeDeleteDialog({ restoreFocus = true } = {}) {
    const focusTarget = deleteRestoreFocusRef.current
    deleteRestoreFocusRef.current = null
    setPendingDelete(null)

    if (restoreFocus && focusTarget && document.contains(focusTarget)) {
      queueMicrotask(() => {
        focusTarget.focus()
      })
    }
  }

  function runDelete() {
    if (!pendingDelete || isPending) {
      return
    }

    startTransition(async () => {
      try {
        if (pendingDelete.ids.length === 1) {
          const blog = blogs.find((item) => item.id === pendingDelete.ids[0])
          await deleteAdminBlog(pendingDelete.ids[0], blog?.slug)
        } else {
          await deleteManyAdminBlogs(pendingDelete.ids)
        }
        setSelectedIds((current) => current.filter((id) => !pendingDelete.ids.includes(id)))
        closeDeleteDialog({ restoreFocus: false })
        router.refresh()
      } catch (error) {
        toast.error(error instanceof Error ? error.message : 'Failed to delete blogs.')
      }
    })
  }

  return (
    <div className="rounded-md border border-border bg-background">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border px-4 py-3">
        <div className="flex min-w-[240px] flex-1 items-center gap-3">
          <Input
            ref={searchInputRef}
            value={query}
            onChange={(event) => {
              const nextQuery = event.target.value
              setQuery(nextQuery)
              setPage(1)
              setSelectedIds([])
            }}
            placeholder="Search by title or tags…"
            aria-label="Search blog titles"
            className="max-w-sm"
            disabled={!isInteractive}
          />
          <p className="text-sm text-muted-foreground">
            {filteredBlogs.length} shown · {selectedCount > 0 ? `${selectedCount} selected` : 'Select rows to enable bulk delete.'}
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            type="button"
            onClick={() => setShowBatchAiPanel((open) => !open)}
          >
            <Sparkles className="mr-2 h-4 w-4" />
            {showBatchAiPanel ? 'Hide Batch AI Fix' : selectedCount > 0 ? `Batch AI Fix (${selectedCount})` : 'Batch AI Fix'}
          </Button>
          {selectedCount > 0 ? (
            <Button
              variant="destructive"
              size="sm"
              onClick={(event) => requestDelete(effectiveSelectedIds, `${selectedCount} selected blog posts`, event.currentTarget)}
              disabled={isPending}
            >
              <Trash2 className="mr-2 h-4 w-4" />
              Delete Selected
            </Button>
          ) : null}
        </div>
      </div>
      <AdminBlogBatchAiPanel
        isOpen={showBatchAiPanel}
        selectedBlogIds={effectiveSelectedIds}
        selectedBlogTitles={selectedBlogTitles}
        availableBlogs={filteredBlogs.map((blog) => ({
          id: blog.id,
          title: blog.title,
          publishedAt: blog.publishedAt ?? null,
          updatedAt: blog.updatedAt ?? null,
        }))}
        onApplied={() => {
          router.refresh()
        }}
      />
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead className="w-12">
              <Checkbox
                aria-label="Select all blogs"
                checked={allSelected}
                onCheckedChange={toggleAll}
              />
            </TableHead>
            <TableHead>Title</TableHead>
            <TableHead>Status</TableHead>
            <TableHead>Published Date</TableHead>
            <TableHead>Tags</TableHead>
            <TableHead className="text-right">Actions</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {visibleBlogs.length > 0 ? (
            visibleBlogs.map((blog) => (
              <TableRow
                key={blog.id}
                data-testid="admin-blog-row"
                data-state={selectedSet.has(blog.id) ? 'selected' : undefined}
              >
                <TableCell>
                  <Checkbox
                    aria-label={`Select ${blog.title}`}
                    checked={selectedSet.has(blog.id)}
                    onCheckedChange={() => toggle(blog.id)}
                  />
                </TableCell>
                <TableCell className="min-w-0 font-medium">
                  <Link
                    href={`/admin/blog/${blog.id}?returnTo=${returnTo}`}
                    prefetch={false}
                    className="block truncate transition-colors hover:text-primary hover:underline"
                  >
                    {blog.title}
                  </Link>
                </TableCell>
                <TableCell>
                  {blog.published ? (
                    <Badge variant="secondary" className="bg-green-100 text-green-800 hover:bg-green-100/80 dark:bg-green-900/40 dark:text-green-300">
                      Published
                    </Badge>
                  ) : (
                    <Badge variant="secondary" className="bg-yellow-100 text-yellow-800 hover:bg-yellow-100/80 dark:bg-yellow-900/40 dark:text-yellow-300">
                      Draft
                    </Badge>
                  )}
                </TableCell>
                <TableCell className="text-sm tabular-nums text-muted-foreground">
                  {blog.publishedAt ? new Date(blog.publishedAt).toLocaleDateString() : '—'}
                </TableCell>
                <TableCell>
                  {blog.tags.length > 0 ? (
                    <div className="flex flex-wrap gap-1">
                      {blog.tags.slice(0, 3).map((tag) => (
                        <Badge key={tag} variant="secondary" className="text-xs">
                          {tag}
                        </Badge>
                      ))}
                      {blog.tags.length > 3 ? (
                        <Badge variant="outline" className="text-xs">
                          +{blog.tags.length - 3}
                        </Badge>
                      ) : null}
                    </div>
                  ) : (
                    <span className="text-sm text-muted-foreground">No tags</span>
                  )}
                </TableCell>
                <TableCell className="text-right">
                  <div className="flex justify-end gap-2">
                    <Button asChild variant="ghost" size="icon">
                      <Link
                        href={`/blog/${blog.slug}`}
                        prefetch={false}
                        target="_blank"
                        rel="noreferrer"
                        aria-label={`View public post: ${blog.title}`}
                        title="View Public"
                      >
                        <Eye className="h-4 w-4" />
                      </Link>
                    </Button>
                    <Button asChild variant="ghost" size="icon">
                      <Link href={`/admin/blog/${blog.id}?returnTo=${returnTo}`} prefetch={false} aria-label={`Edit post: ${blog.title}`} title="Edit">
                        <Pencil className="h-4 w-4" />
                      </Link>
                    </Button>
                    <Button
                      variant="ghost"
                      size="icon"
                      className="text-red-500 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-950/20"
                      aria-label={`Delete post: ${blog.title}`}
                      title="Delete"
                      onClick={(event) => requestDelete([blog.id], blog.title, event.currentTarget)}
                      disabled={isPending}
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </div>
                </TableCell>
              </TableRow>
            ))
          ) : (
            <TableRow>
              <TableCell colSpan={6} className="h-24 text-center">
                {emptyTableMessage}
              </TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
      <div className="flex flex-wrap items-center justify-center gap-3 border-t border-border px-4 py-3 sm:justify-between">
        <Button
          type="button"
          variant="outline"
          size="sm"
          aria-label="Previous page"
          disabled={currentPage <= 1}
          onClick={() => {
            setSelectedIds([])
            setPage((active) => Math.max(1, active - 1))
          }}
        >
          <ChevronLeft className="h-4 w-4" aria-hidden="true" />
          Previous
        </Button>
        <span className="text-sm tabular-nums text-muted-foreground">
          Page {currentPage} of {totalPages}
        </span>
        <Button
          type="button"
          variant="outline"
          size="sm"
          aria-label="Next page"
          disabled={currentPage >= totalPages}
          onClick={() => {
            setSelectedIds([])
            setPage((active) => Math.min(totalPages, active + 1))
          }}
        >
          Next
          <ChevronRight className="h-4 w-4" aria-hidden="true" />
        </Button>
      </div>
      <Dialog open={pendingDelete !== null} onOpenChange={(open) => !open && closeDeleteDialog()}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {pendingDelete ? `Delete ${pendingDelete.title}?` : 'Delete item?'}
            </DialogTitle>
            <DialogDescription>
              This action cannot be undone. The selected post will be permanently deleted.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => closeDeleteDialog()} disabled={isPending}>
              Cancel
            </Button>
            <Button variant="destructive" onClick={runDelete} disabled={isPending}>
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
