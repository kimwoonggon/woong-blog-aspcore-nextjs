
import { WorkEditor } from '@/components/admin/WorkEditor'

interface PageProps {
    searchParams?: Promise<{ returnTo?: string; returnPage?: string; returnPageSize?: string; returnQuery?: string }>
}

export default async function NewWorkPage({ searchParams }: PageProps) {
    const resolvedSearchParams = await searchParams
    const returnParams = new URLSearchParams()
    if (resolvedSearchParams?.returnQuery) returnParams.set('query', resolvedSearchParams.returnQuery)
    if (resolvedSearchParams?.returnPage) returnParams.set('page', resolvedSearchParams.returnPage)
    if (resolvedSearchParams?.returnPageSize) returnParams.set('pageSize', resolvedSearchParams.returnPageSize)
    const returnTo = resolvedSearchParams?.returnTo ?? `/admin/works${returnParams.size > 0 ? `?${returnParams.toString()}` : ''}`
    return (
        <div className="space-y-8">
            <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-50">New Work</h1>
            <WorkEditor returnTo={returnTo} />
        </div>
    )
}
