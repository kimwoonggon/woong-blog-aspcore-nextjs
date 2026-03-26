import Link from 'next/link'
import { Plus, Pencil, Eye } from 'lucide-react'
import { AdminErrorPanel } from '@/components/admin/AdminErrorPanel'
import { DeleteButton } from '@/components/admin/DeleteButton'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { fetchAdminWorks, type WorkAdminItem } from '@/lib/api/works'
import { deleteWork } from './actions'

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
                <div className="rounded-md border border-gray-200 bg-white dark:border-gray-800 dark:bg-gray-950">
                    <Table>
                        <TableHeader>
                            <TableRow>
                                <TableHead>Title</TableHead>
                                <TableHead>Status</TableHead>
                                <TableHead>Published Date</TableHead>
                                <TableHead>Category</TableHead>
                                <TableHead className="text-right">Actions</TableHead>
                            </TableRow>
                        </TableHeader>
                        <TableBody>
                            {works.length > 0 ? (
                                works.map((work) => (
                                    <TableRow key={work.id}>
                                        <TableCell className="font-medium">
                                            <Link
                                                href={`/admin/works/${work.id}`}
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
                                                <Link href={`/admin/works/${work.id}`}>
                                                    <Button variant="ghost" size="icon" title="Edit">
                                                        <Pencil className="h-4 w-4" />
                                                    </Button>
                                                </Link>
                                                <DeleteButton id={work.id} action={deleteWork} />
                                            </div>
                                        </TableCell>
                                    </TableRow>
                                ))
                            ) : (
                                <TableRow>
                                    <TableCell colSpan={5} className="h-24 text-center">
                                        No works found.
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
