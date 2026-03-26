import Link from 'next/link'
import { Plus, Pencil, Eye } from 'lucide-react'
import { AdminErrorPanel } from '@/components/admin/AdminErrorPanel'
import { DeleteButton } from '@/components/admin/DeleteButton'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { fetchAdminBlogs, type BlogAdminItem } from '@/lib/api/blogs'
import { deleteBlog } from './actions'

export const dynamic = 'force-dynamic'

export default async function AdminBlogPage() {
    let blogs: BlogAdminItem[] = []
    let loadFailed = false

    try {
        blogs = await fetchAdminBlogs()
    } catch {
        loadFailed = true
    }

    return (
        <div className="space-y-8">
            <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                <div>
                    <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-50">Blog Posts</h1>
                    <p className="mt-2 text-sm text-muted-foreground">
                        Titles now act as primary edit links, and the new Notion view keeps document browsing beside the editor.
                    </p>
                </div>
                <div className="flex flex-wrap gap-2">
                    <Link href="/admin/blog/notion">
                        <Button variant="outline">Notion View</Button>
                    </Link>
                    <Link href="/admin/blog/new">
                        <Button>
                            <Plus className="mr-2 h-4 w-4" /> Add Post
                        </Button>
                    </Link>
                </div>
            </div>

            {loadFailed ? (
                <AdminErrorPanel
                    title="Blog administration is unavailable"
                    message="Blog posts could not be loaded from the backend. Please retry after checking the API and database connection."
                />
            ) : (
                <div className="rounded-md border border-gray-200 bg-white dark:border-gray-800 dark:bg-gray-950">
                    <Table>
                        <TableHeader>
                            <TableRow>
                                <TableHead>Title</TableHead>
                                <TableHead>Status</TableHead>
                                <TableHead>Published Date</TableHead>
                                <TableHead>Tags</TableHead>
                                <TableHead className="text-right">Actions</TableHead>
                            </TableRow>
                        </TableHeader>
                        <TableBody>
                            {blogs.length > 0 ? (
                                blogs.map((blog) => (
                                    <TableRow key={blog.id}>
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
                                                <Link href={`/admin/blog/${blog.id}`}>
                                                    <Button variant="ghost" size="icon" title="Edit">
                                                        <Pencil className="h-4 w-4" />
                                                    </Button>
                                                </Link>
                                                <Link href={`/blog/${blog.slug}`} target="_blank">
                                                    <Button variant="ghost" size="icon" title="View Public">
                                                        <Eye className="h-4 w-4" />
                                                    </Button>
                                                </Link>
                                                <DeleteButton id={blog.id} action={deleteBlog} />
                                            </div>
                                        </TableCell>
                                    </TableRow>
                                ))
                            ) : (
                                <TableRow>
                                    <TableCell colSpan={5} className="h-24 text-center">
                                        No blog posts found.
                                    </TableCell>
                                </TableRow>
                            )}
                        </TableBody>
                    </Table>
                </div>
            )}
        </div>
    )
}
