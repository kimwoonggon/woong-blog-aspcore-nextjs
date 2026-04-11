'use client'

import Link from 'next/link'
import { useMemo, useState, useTransition } from 'react'
import { useRouter } from 'next/navigation'
import { Eye, Pencil, Trash2 } from 'lucide-react'
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
import type { WorkAdminItem } from '@/lib/api/works'
import { deleteAdminWork, deleteManyAdminWorks } from '@/lib/api/admin-mutations'
import { useResponsivePageSize } from '@/hooks/useResponsivePageSize'

interface AdminWorksTableClientProps {
  works: WorkAdminItem[]
}

interface PendingWorkDelete {
  ids: string[]
  title: string
}

function matchesWorkQuery(work: WorkAdminItem, normalizedQuery: string) {
  if (!normalizedQuery) {
    return true
  }

  return (
    work.title.toLowerCase().includes(normalizedQuery)
    || work.category.toLowerCase().includes(normalizedQuery)
    || work.tags.some((tag) => tag.toLowerCase().includes(normalizedQuery))
  )
}

export function AdminWorksTableClient({ works }: AdminWorksTableClientProps) {
  const router = useRouter()
  const returnTo = encodeURIComponent('/admin/works')
  const [selectedIds, setSelectedIds] = useState<string[]>([])
  const [query, setQuery] = useState('')
  const [pendingDelete, setPendingDelete] = useState<PendingWorkDelete | null>(null)
  const [page, setPage] = useState(1)
  const [isPending, startTransition] = useTransition()
  const pageSize = useResponsivePageSize(12, 8, 6)
  const filteredWorks = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase()
    if (!normalizedQuery) {
      return works
    }

    return works.filter((work) => matchesWorkQuery(work, normalizedQuery))
  }, [works, query])
  const totalPages = Math.max(1, Math.ceil(filteredWorks.length / pageSize))
  const currentPage = Math.min(page, totalPages)
  const visibleWorks = useMemo(() => {
    const start = (currentPage - 1) * pageSize
    return filteredWorks.slice(start, start + pageSize)
  }, [currentPage, filteredWorks, pageSize])
  const visibleIds = useMemo(() => visibleWorks.map((work) => work.id), [visibleWorks])
  const effectiveSelectedIds = useMemo(
    () => selectedIds.filter((id) => filteredWorks.some((work) => work.id === id)),
    [filteredWorks, selectedIds],
  )
  const selectedCount = effectiveSelectedIds.length
  const allSelected = visibleWorks.length > 0 && visibleIds.every((id) => effectiveSelectedIds.includes(id))
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
          await deleteAdminWork(pendingDelete.ids[0])
        } else {
          await deleteManyAdminWorks(pendingDelete.ids)
        }
        setSelectedIds((current) => current.filter((id) => !pendingDelete.ids.includes(id)))
        setPendingDelete(null)
        router.refresh()
      } catch (error) {
        window.alert(error instanceof Error ? error.message : 'Failed to delete works.')
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
                  works.some((work) =>
                    work.id === id
                    && matchesWorkQuery(work, normalizedQuery),
                  ),
                ),
              )
            }}
            placeholder="Search by title, tags, or category..."
            aria-label="Search work titles"
            className="max-w-sm"
          />
          <p className="text-sm text-muted-foreground">
            {filteredWorks.length} shown · {selectedCount > 0 ? `${selectedCount} selected` : 'Select rows to enable bulk delete.'}
          </p>
        </div>
        {selectedCount > 0 ? (
          <Button
            variant="destructive"
            size="sm"
            onClick={() => requestDelete(effectiveSelectedIds, `${selectedCount} selected works`)}
            disabled={isPending}
          >
            <Trash2 className="mr-2 h-4 w-4" />
            Delete Selected
          </Button>
        ) : null}
      </div>
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead className="w-12">
              <Checkbox
                aria-label="Select all works"
                checked={allSelected}
                onCheckedChange={toggleAll}
              />
            </TableHead>
            <TableHead>Title</TableHead>
            <TableHead>Status</TableHead>
            <TableHead>Published Date</TableHead>
            <TableHead>Category</TableHead>
            <TableHead className="text-right">Actions</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {visibleWorks.length > 0 ? (
            visibleWorks.map((work) => (
              <TableRow key={work.id} data-testid="admin-work-row">
                <TableCell>
                  <Checkbox
                    aria-label={`Select ${work.title}`}
                    checked={selectedSet.has(work.id)}
                    onCheckedChange={() => toggle(work.id)}
                  />
                </TableCell>
                <TableCell className="font-medium">
                  <Link
                    href={`/admin/works/${work.id}?returnTo=${returnTo}`}
                    className="transition-colors hover:text-primary hover:underline"
                  >
                    {work.title}
                  </Link>
                </TableCell>
                <TableCell>
                  {work.published ? (
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
                  {work.publishedAt ? new Date(work.publishedAt).toLocaleDateString() : '—'}
                </TableCell>
                <TableCell>{work.category}</TableCell>
                <TableCell className="text-right">
                  <div className="flex justify-end gap-2">
                    <Link href={`/works/${work.slug}`} target="_blank">
                      <Button variant="ghost" size="icon" title="View Public">
                        <Eye className="h-4 w-4" />
                      </Button>
                    </Link>
                    <Link href={`/admin/works/${work.id}?returnTo=${returnTo}`}>
                      <Button variant="ghost" size="icon" title="Edit">
                        <Pencil className="h-4 w-4" />
                      </Button>
                    </Link>
                    <Button
                      variant="ghost"
                      size="icon"
                      className="text-red-500 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-950/20"
                      title="Delete"
                      onClick={() => requestDelete([work.id], work.title)}
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
                No works found.
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
              This action cannot be undone. The selected work will be permanently deleted.
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
