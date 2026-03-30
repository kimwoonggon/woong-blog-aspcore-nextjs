import Link from 'next/link'
import { Plus } from 'lucide-react'
import { AdminErrorPanel } from '@/components/admin/AdminErrorPanel'
import { AdminBlogTableClient } from '@/components/admin/AdminBlogTableClient'
import { Button } from '@/components/ui/button'
import { fetchAdminBlogs, type BlogAdminItem } from '@/lib/api/blogs'

export const dynamic = 'force-dynamic'

interface PageProps {
    searchParams?: Promise<{ query?: string; page?: string; pageSize?: string }>
}

export default async function AdminBlogPage({ searchParams }: PageProps) {
    const resolvedSearchParams = await searchParams
    let blogs: BlogAdminItem[] = []
    let loadFailed = false
    const returnParams = new URLSearchParams()

    if (resolvedSearchParams?.query) returnParams.set('returnQuery', resolvedSearchParams.query)
    if (resolvedSearchParams?.page) returnParams.set('returnPage', resolvedSearchParams.page)
    if (resolvedSearchParams?.pageSize) returnParams.set('returnPageSize', resolvedSearchParams.pageSize)

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
                        Titles now act as primary edit links, and the new Notion view keeps document browsing beside the editor with local batch-selection scaffolding for future bulk actions.
                    </p>
                </div>
                <div className="flex flex-wrap gap-2">
                    <Link href="/admin/blog/notion">
                        <Button variant="outline">Notion View</Button>
                    </Link>
                    <Link href={`/admin/blog/new${returnParams.size > 0 ? `?${returnParams.toString()}` : ''}`}>
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
                <AdminBlogTableClient blogs={blogs} />
            )}
        </div>
    )
}
