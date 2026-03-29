import Link from 'next/link'
import { Plus } from 'lucide-react'
import { AdminErrorPanel } from '@/components/admin/AdminErrorPanel'
import { AdminWorksTableClient } from '@/components/admin/AdminWorksTableClient'
import { Button } from '@/components/ui/button'
import { fetchAdminWorks, type WorkAdminItem } from '@/lib/api/works'

export const dynamic = 'force-dynamic'

export default async function AdminWorksPage() {
    let works: WorkAdminItem[] = []
    let loadFailed = false

    try {
        works = await fetchAdminWorks()
    } catch {
        loadFailed = true
    }

    return (
        <div className="space-y-8">
            <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                <div>
                    <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-50">Works</h1>
                    <p className="mt-2 text-sm text-muted-foreground">
                        Click a title to edit directly, or create a new work and return to this list as soon as it saves.
                    </p>
                </div>
                <Link href="/admin/works/new">
                    <Button>
                        <Plus className="mr-2 h-4 w-4" /> Add Work
                    </Button>
                </Link>
            </div>

            {loadFailed ? (
                <AdminErrorPanel
                    title="Work administration is unavailable"
                    message="Works could not be loaded from the backend. Please retry after checking the API and database connection."
                />
            ) : (
                <AdminWorksTableClient works={works} />
            )}
        </div>
    )
}
