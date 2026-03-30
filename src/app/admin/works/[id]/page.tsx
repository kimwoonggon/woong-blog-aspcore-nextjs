import { notFound } from 'next/navigation'
import { WorkEditor } from '@/components/admin/WorkEditor'
import { fetchAdminWorkById } from '@/lib/api/works'
import { AdminErrorPanel } from '@/components/admin/AdminErrorPanel'

export const revalidate = 0

interface PageProps {
    params: Promise<{ id: string }>
    searchParams?: Promise<{ returnTo?: string; returnPage?: string; returnPageSize?: string; returnQuery?: string }>
}

export default async function EditWorkPage({ params, searchParams }: PageProps) {
    const { id } = await params
    const resolvedSearchParams = await searchParams
    const returnParams = new URLSearchParams()
    if (resolvedSearchParams?.returnQuery) returnParams.set('query', resolvedSearchParams.returnQuery)
    if (resolvedSearchParams?.returnPage) returnParams.set('page', resolvedSearchParams.returnPage)
    if (resolvedSearchParams?.returnPageSize) returnParams.set('pageSize', resolvedSearchParams.returnPageSize)
    const returnTo = resolvedSearchParams?.returnTo ?? `/admin/works${returnParams.size > 0 ? `?${returnParams.toString()}` : ''}`
    let work = null
    let loadFailed = false

    try {
        work = await fetchAdminWorkById(id)
    } catch {
        loadFailed = true
    }

    if (!loadFailed && !work) {
        notFound()
    }

    const initialWork = work
        ? {
            ...work,
            thumbnail_url: work.thumbnail_url,
            icon_url: work.icon_url
        }
        : undefined

    return (
        <div className="space-y-8">
            <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-50">Edit Work</h1>
            {loadFailed || !initialWork ? (
                <AdminErrorPanel
                    title="Work editor is unavailable"
                    message="The selected work entry could not be loaded. Please retry after checking the backend connection."
                />
            ) : (
                <WorkEditor initialWork={initialWork} returnTo={returnTo} />
            )}
        </div>
    )
}
