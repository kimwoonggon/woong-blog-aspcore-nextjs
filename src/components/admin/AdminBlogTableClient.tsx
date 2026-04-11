'use client'

import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { useMemo, useState, useTransition } from 'react'
import { Eye, Pencil, Sparkles, Trash2 } from 'lucide-react'
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

interface AdminBlogTableClientProps {
  blogs: BlogAdminItem[]
}

interface PendingBlogDelete {
  ids: string[]
  title: string
}

function matchesBlogQuery(blog: BlogAdminItem, normalizedQuery: string) {
  if (!normalizedQuery) {
    return true
  }

  return (
    blog.title.toLowerCase().includes(normalizedQuery)
    || blog.tags.some((tag) => tag.toLowerCase().includes(normalizedQuery))
  )
}

export function AdminBlogTableClient({ blogs }: AdminBlogTableClientProps) {
  const router = useRouter()
  const [selectedIds, setSelectedIds] = useState<string[]>([])
  const [showBatchAiPanel, setShowBatchAiPanel] = useState(false)
  const [query, setQuery] = useState('')
  const [pendingDelete, setPendingDelete] = useState<PendingBlogDelete | null>(null)
  const [page, setPage] = useState(1)
  const [isPending, startTransition] = useTransition()
  const pageSize = useResponsivePageSize(12, 8, 6)
  const filteredBlogs = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase()
    if (!normalizedQuery) {
      return blogs
    }

    return blogs.filter((blog) => matchesBlogQuery(blog, normalizedQuery))
  }, [blogs, query])
  const totalPages = Math.max(1, Math.ceil(filteredBlogs.length / pageSize))
  const currentPage = Math.min(page, totalPages)
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

  function requestDelete(ids: string[], title: string) {
    if (ids.length === 0 || isPending) {
      return
    }

    setPendingDelete({ ids, title })
  }

  function runDelete() {
    if (!pendingDelete || isPending) {
      return
    }

    startTransition(async () => {
      try {
        if (pendingDelete.ids.length === 1) {
          await deleteAdminBlog(pendingDelete.ids[0])
        } else {
          await deleteManyAdminBlogs(pendingDelete.ids)
        }
        setSelectedIds((current) => current.filter((id) => !pendingDelete.ids.includes(id)))
        setPendingDelete(null)
        router.refresh()
      } catch (error) {
        window.alert(error instanceof Error ? error.message : 'Failed to delete blogs.')
      }
    })
  }

  return (
    <div className="rounded-md border border-gray-200 bg-white dark:border-gray-800 dark:bg-gray-950">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-gray-200 px-4 py-3 dark:border-gray-800">
        <div className="flex min-w-[240px] flex-1 items-center gap-3">
          <Input
            value={query}
            onChange={(event) => {
              const nextQuery = event.target.value
              const normalizedQuery = nextQuery.trim().toLowerCase()
              setQuery(nextQuery)
              setPage(1)
              setSelectedIds((current) =>
                current.filter((id) =>
                  blogs.some((blog) =>
                    blog.id === id
                    && matchesBlogQuery(blog, normalizedQuery),
                  ),
                ),
              )
            }}
            placeholder="Search by title or tags..."
            aria-label="Search blog titles"
            className="max-w-sm"
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
              onClick={() => requestDelete(effectiveSelectedIds, `${selectedCount} selected blog posts`)}
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
              <TableRow key={blog.id} data-testid="admin-blog-row">
                <TableCell>
                  <Checkbox
                    aria-label={`Select ${blog.title}`}
                    checked={selectedSet.has(blog.id)}
                    onCheckedChange={() => toggle(blog.id)}
                  />
                </TableCell>
                <TableCell className="font-medium">
                  <Link
                    href={`/admin/blog/${blog.id}`}
                    className="transition-colors hover:text-primary hover:underline"
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
                <TableCell className="text-sm text-gray-500">
                  {blog.publishedAt ? new Date(blog.publishedAt).toLocaleDateString() : '—'}
                </TableCell>
                <TableCell>{blog.tags?.join(', ')}</TableCell>
                <TableCell className="text-right">
                  <div className="flex justify-end gap-2">
                    <Link href={`/blog/${blog.slug}`} target="_blank">
                      <Button variant="ghost" size="icon" title="View Public">
                        <Eye className="h-4 w-4" />
                      </Button>
                    </Link>
                    <Link href={`/admin/blog/${blog.id}`}>
                      <Button variant="ghost" size="icon" title="Edit">
                        <Pencil className="h-4 w-4" />
                      </Button>
                    </Link>
                    <Button
                      variant="ghost"
                      size="icon"
                      className="text-red-500 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-950/20"
                      title="Delete"
                      onClick={() => requestDelete([blog.id], blog.title)}
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
                No blog posts found.
              </TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
      <div className="flex flex-wrap items-center justify-center gap-2 border-t border-gray-200 px-4 py-3 dark:border-gray-800">
        <Button
          type="button"
          variant="outline"
          size="sm"
          aria-label="처음"
          disabled={currentPage <= 1}
          onClick={() => setPage(1)}
        >
          First
        </Button>
        <Button
          type="button"
          variant="outline"
          size="sm"
          aria-label="이전"
          disabled={currentPage <= 1}
          onClick={() => setPage((active) => Math.max(1, active - 1))}
        >
          Previous
        </Button>
        <span className="text-sm text-muted-foreground">
          {currentPage} / {totalPages}
        </span>
        <Button
          type="button"
          variant="outline"
          size="sm"
          aria-label="다음"
          disabled={currentPage >= totalPages}
          onClick={() => setPage((active) => Math.min(totalPages, active + 1))}
        >
          Next
        </Button>
        <Button
          type="button"
          variant="outline"
          size="sm"
          aria-label="끝"
          disabled={currentPage >= totalPages}
          onClick={() => setPage(totalPages)}
        >
          Last
        </Button>
      </div>
      <Dialog open={pendingDelete !== null} onOpenChange={(open) => !open && setPendingDelete(null)}>
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
            <Button variant="outline" onClick={() => setPendingDelete(null)} disabled={isPending}>
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
