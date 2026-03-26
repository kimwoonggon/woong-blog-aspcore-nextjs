import Link from 'next/link'
import { redirect } from 'next/navigation'
import { ArrowUpRight, Briefcase, FileText, Home, LayoutDashboard, Settings, Users } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { fetchServerSession } from '@/lib/api/server'

export default async function AdminLayout({
    children,
}: {
    children: React.ReactNode
}) {
    const session = await fetchServerSession()

    if (!session.authenticated) {
        redirect('/login')
    }

    if (session.role !== 'admin') {
        redirect('/')
    }

    return (
        <div className="flex min-h-screen flex-col bg-gray-50 md:flex-row dark:bg-gray-900">
            <aside className="w-full border-b border-gray-200 bg-white p-6 md:w-80 md:border-b-0 md:border-r dark:border-gray-800 dark:bg-gray-950">
                <div className="mb-8 space-y-3">
                    <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">Content workspace</p>
                    <div>
                        <h1 className="text-2xl font-semibold text-gray-900 dark:text-gray-50">Admin Panel</h1>
                        <p className="mt-2 text-sm text-muted-foreground">
                            Modernized shortcuts keep the public site, list views, and blog Notion workspace within one click.
                        </p>
                    </div>
                </div>

                <div className="mb-6 flex flex-wrap gap-2">
                    <Link href="/">
                        <Button variant="outline" className="gap-2">
                            <Home size={16} />
                            Public Home
                        </Button>
                    </Link>
                    <Link href="/" target="_blank">
                        <Button variant="outline" className="gap-2">
                            <ArrowUpRight size={16} />
                            Open Site
                        </Button>
                    </Link>
                </div>

                <nav className="flex flex-col gap-2">
                    <Link href="/admin/dashboard">
                        <Button variant="ghost" className="w-full justify-start gap-2 rounded-2xl px-4 py-6">
                            <LayoutDashboard size={20} />
                            Dashboard
                        </Button>
                    </Link>
                    <Link href="/admin/works">
                        <Button variant="ghost" className="w-full justify-start gap-2 rounded-2xl px-4 py-6">
                            <Briefcase size={20} />
                            Works
                        </Button>
                    </Link>
                    <Link href="/admin/blog">
                        <Button variant="ghost" className="w-full justify-start gap-2 rounded-2xl px-4 py-6">
                            <FileText size={20} />
                            Blog
                        </Button>
                    </Link>
                    <Link href="/admin/blog/notion">
                        <Button variant="ghost" className="w-full justify-start gap-2 rounded-2xl px-4 py-6">
                            <FileText size={20} />
                            Blog Notion View
                        </Button>
                    </Link>
                    <Link href="/admin/pages">
                        <Button variant="ghost" className="w-full justify-start gap-2 rounded-2xl px-4 py-6">
                            <Settings size={20} />
                            Pages &amp; Settings
                        </Button>
                    </Link>
                    <Link href="/admin/members">
                        <Button variant="ghost" className="w-full justify-start gap-2 rounded-2xl px-4 py-6">
                            <Users size={20} />
                            Members
                        </Button>
                    </Link>
                </nav>
            </aside>

            <main className="flex-1 bg-gray-50 p-6 md:p-12 dark:bg-gray-900">
                {children}
            </main>
        </div>
    )
}
