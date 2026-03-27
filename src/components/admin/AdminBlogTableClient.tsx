'use client'

import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { useEffect, useMemo, useState, useTransition } from 'react'
import { Eye, Pencil, Trash2 } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Checkbox } from '@/components/ui/checkbox'
import { Input } from '@/components/ui/input'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import type { BlogAdminItem } from '@/lib/api/blogs'
import { deleteAdminBlog, deleteManyAdminBlogs } from '@/lib/api/admin-mutations'
import { useResponsivePageSize } from '@/hooks/useResponsivePageSize'

interface AdminBlogTableClientProps {
  blogs: BlogAdminItem[]
}

export function AdminBlogTableClient({ blogs }: AdminBlogTableClientProps) {
  const router = useRouter()
  const [selectedIds, setSelectedIds] = useState<string[]>([])
  const [query, setQuery] = useState('')
  const [page, setPage] = useState(1)
  const [isPending, startTransition] = useTransition()
  const pageSize = useResponsivePageSize(12, 8, 6)
  const filteredBlogs = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase()
    if (!normalizedQuery) {
      return blogs
    }

    return blogs.filter((blog) => blog.title.toLowerCase().includes(normalizedQuery))
  }, [blogs, query])
  const totalPages = Math.max(1, Math.ceil(filteredBlogs.length / pageSize))
  const currentPage = Math.min(page, totalPages)
  const visibleBlogs = useMemo(() => {
    const start = (currentPage - 1) * pageSize
    return filteredBlogs.slice(start, start + pageSize)
  }, [currentPage, filteredBlogs, pageSize])
  const visibleIds = useMemo(() => visibleBlogs.map((blog) => blog.id), [visibleBlogs])
  const selectedCount = selectedIds.length
  const allSelected = visibleBlogs.length > 0 && visibleIds.every((id) => selectedIds.includes(id))

  const selectedSet = useMemo(() => new Set(selectedIds), [selectedIds])

  useEffect(() => {
    setPage(1)
  }, [query, pageSize])

  useEffect(() => {
    setSelectedIds((current) => current.filter((id) => filteredBlogs.some((blog) => blog.id === id)))
  }, [filteredBlogs])

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

  function confirmDelete(message: string) {
    return window.prompt(message, '')?.trim().toLowerCase() === 'yes'
  }

  function runDelete(ids: string[], label: string) {
    if (ids.length === 0 || isPending) {
      return
    }

    const confirmed = confirmDelete(`Type yes to delete ${label}. This action cannot be undone.`)
    if (!confirmed) {
      return
    }

    startTransition(async () => {
      try {
        if (ids.length === 1) {
          await deleteAdminBlog(ids[0])
        } else {
          await deleteManyAdminBlogs(ids)
        }
        setSelectedIds((current) => current.filter((id) => !ids.includes(id)))
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
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Search blog titles"
            aria-label="Search blog titles"
            className="max-w-sm"
          />
          <p className="text-sm text-muted-foreground">
            {filteredBlogs.length} shown · {selectedCount > 0 ? `${selectedCount} selected` : 'Select rows to enable bulk delete.'}
          </p>
        </div>
        {selectedCount > 0 ? (
          <Button
            variant="destructive"
            size="sm"
            onClick={() => runDelete(selectedIds, `${selectedCount} selected blog posts`)}
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
                      className="text-red-500 hover:text-red-600 hover:bg-red-50"
                      title="Delete"
                      onClick={() => runDelete([blog.id], blog.title)}
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
        <Button type="button" variant="outline" size="sm" disabled={currentPage <= 1} onClick={() => setPage(1)}>
          처음
        </Button>
        <Button type="button" variant="outline" size="sm" disabled={currentPage <= 1} onClick={() => setPage((active) => Math.max(1, active - 1))}>
          이전
        </Button>
        <span className="text-sm text-muted-foreground">
          {currentPage} / {totalPages}
        </span>
        <Button type="button" variant="outline" size="sm" disabled={currentPage >= totalPages} onClick={() => setPage((active) => Math.min(totalPages, active + 1))}>
          다음
        </Button>
        <Button type="button" variant="outline" size="sm" disabled={currentPage >= totalPages} onClick={() => setPage(totalPages)}>
          끝
        </Button>
      </div>
    </div>
  )
}
