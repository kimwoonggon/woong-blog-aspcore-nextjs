import { notFound } from 'next/navigation'
import { BlogEditor } from '@/components/admin/BlogEditor'
import { fetchAdminBlogById } from '@/lib/api/blogs'
import { AdminErrorPanel } from '@/components/admin/AdminErrorPanel'

export const revalidate = 0

interface PageProps {
    params: Promise<{ id: string }>
    searchParams?: Promise<{ returnTo?: string; returnPage?: string; returnPageSize?: string; returnQuery?: string }>
}

export default async function EditBlogPage({ params, searchParams }: PageProps) {
    const { id } = await params
    const resolvedSearchParams = await searchParams
    const returnParams = new URLSearchParams()
    if (resolvedSearchParams?.returnQuery) returnParams.set('query', resolvedSearchParams.returnQuery)
    if (resolvedSearchParams?.returnPage) returnParams.set('page', resolvedSearchParams.returnPage)
    if (resolvedSearchParams?.returnPageSize) returnParams.set('pageSize', resolvedSearchParams.returnPageSize)
    const returnTo = resolvedSearchParams?.returnTo ?? `/admin/blog${returnParams.size > 0 ? `?${returnParams.toString()}` : ''}`
    let blog = null
    let loadFailed = false

    try {
        blog = await fetchAdminBlogById(id)
    } catch {
        loadFailed = true
    }

    if (!loadFailed && !blog) {
        notFound()
    }

    return (
        <div className="space-y-8">
            <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-50">Edit Post</h1>
            {loadFailed || !blog ? (
                <AdminErrorPanel
                    title="Blog editor is unavailable"
                    message="The selected blog post could not be loaded. Please retry after checking the backend connection."
                />
            ) : (
                <BlogEditor initialBlog={blog} returnTo={returnTo} />
            )}
        </div>
    )
}
