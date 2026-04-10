
import Link from 'next/link'
import Image from 'next/image'
import { InlineAdminEditorShell } from '@/components/admin/InlineAdminEditorShell'
import { PublicAdminLink } from '@/components/admin/PublicAdminLink'
import { WorkEditor } from '@/components/admin/WorkEditor'
import { EdgePaginationNav } from '@/components/layout/EdgePaginationNav'
import { PublicPagination } from '@/components/layout/PublicPagination'
import { ResponsivePageSizeSync } from '@/components/layout/ResponsivePageSizeSync'
import { Badge } from '@/components/ui/badge'
import { headers } from 'next/headers'
import { fetchServerSession } from '@/lib/api/server'
import { fetchPublicWorks } from '@/lib/api/works'

export const dynamic = 'force-dynamic'

interface PageProps {
    searchParams?: Promise<{ page?: string; pageSize?: string; __qaEmpty?: string }>
}

const DESKTOP_PAGE_SIZE = 8
const TABLET_PAGE_SIZE = 4
const MOBILE_PAGE_SIZE = 2

function formatPublishedMonth(publishedAt?: string | null) {
    return publishedAt
        ? new Date(publishedAt).toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'short',
        })
        : 'Unknown Date'
}

export default async function WorksPage({ searchParams }: PageProps) {
    const resolvedSearchParams = await searchParams
    const headerStore = await headers()
    const requestHost = (headerStore.get('x-forwarded-host') ?? headerStore.get('host') ?? '').toLowerCase()
    const qaEmptyWorks = resolvedSearchParams?.__qaEmpty === '1' && /localhost|127\.0\.0\.1/.test(requestHost)
    const currentPage = Math.max(1, Number.parseInt(resolvedSearchParams?.page ?? '1', 10) || 1)
    const currentPageSize = Math.max(1, Number.parseInt(resolvedSearchParams?.pageSize ?? String(DESKTOP_PAGE_SIZE), 10) || DESKTOP_PAGE_SIZE)
    const worksPayload = qaEmptyWorks
        ? { items: [], page: 1, pageSize: currentPageSize, totalItems: 0, totalPages: 1 }
        : await fetchPublicWorks(currentPage, currentPageSize)
    const session = await fetchServerSession()
    const totalPages = Math.max(1, worksPayload.totalPages)
    const page = worksPayload.page
    const pagedWorks = worksPayload.items

    return (
        <div className="container mx-auto max-w-7xl px-4 py-8 md:px-6 md:py-12">
            <EdgePaginationNav
                pathname="/works"
                currentPage={page}
                totalPages={totalPages}
                pageSize={currentPageSize}
            />
            <ResponsivePageSizeSync
                desktopPageSize={DESKTOP_PAGE_SIZE}
                tabletPageSize={TABLET_PAGE_SIZE}
                mobilePageSize={MOBILE_PAGE_SIZE}
            />
            <div className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                <h1 className="text-3xl font-heading font-bold md:text-4xl text-gray-900 dark:text-gray-50 opacity-0 animate-fade-in-up" style={{ animationDelay: '100ms' }}>Works</h1>
                <div className="flex flex-wrap gap-2">
                    <PublicAdminLink href="/admin/works" label="작업 관리" variant="manage" />
                </div>
            </div>
            {session.authenticated && session.role === 'admin' && (
                <InlineAdminEditorShell
                    triggerLabel="새 작업 쓰기"
                    title="Works Inline Create"
                    description="navbar를 유지한 채 현재 페이지 아래에서 새 작업을 작성합니다."
                >
                    <WorkEditor inlineMode />
                </InlineAdminEditorShell>
            )}
            <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-4">
                {pagedWorks && pagedWorks.length > 0 ? (
                    pagedWorks.map((work, index) => {
                        const thumbnailUrl = work.thumbnailUrl ?? null
                        const publishDate = formatPublishedMonth(work.publishedAt)

                        return (
                            <Link
                                key={work.id}
                                href={`/works/${work.slug}`}
                                className="group/card block h-full"
                                data-testid="work-card"
                            >
                                <article
                                    className="responsive-feed-card flex h-full flex-col overflow-hidden rounded-2xl border border-border/80 bg-background shadow-sm transition hover:border-primary/30 hover:shadow-md"
                                    style={{ animationDelay: `${(index * 100) + 200}ms` }}
                                >
                                    <div className="relative aspect-[4/3] overflow-hidden bg-gray-100 dark:bg-gray-800">
                                        {thumbnailUrl ? (
                                            <Image
                                                src={thumbnailUrl}
                                                alt={work.title}
                                                fill
                                                unoptimized
                                                sizes="(max-width: 768px) 100vw, (max-width: 1200px) 50vw, 25vw"
                                                className="responsive-feed-image object-cover transition-transform duration-500 group-hover/card:scale-105"
                                            />
                                        ) : (
                                            <div className="flex h-full w-full items-center justify-center text-sm font-medium text-gray-400">
                                                No Image
                                            </div>
                                        )}
                                    </div>
                                    <div className="flex flex-1 flex-col p-4 sm:p-5">
                                        <div className="mb-3 flex flex-wrap items-center gap-2">
                                            <Badge variant="secondary" className="rounded-full bg-[#142850] px-2.5 py-0.5 text-xs text-white hover:bg-[#142850]/90">
                                                {publishDate}
                                            </Badge>
                                            <span className="responsive-feed-copy text-xs font-medium uppercase tracking-wide text-gray-500 dark:text-gray-400">
                                                {work.category}
                                            </span>
                                        </div>
                                        <h2 className="responsive-feed-title line-clamp-2 text-lg font-heading font-bold leading-tight text-gray-900 transition-colors group-hover/card:text-[#F3434F] dark:text-gray-50 sm:text-xl">
                                            {work.title}
                                        </h2>
                                        <p className="responsive-feed-copy mt-2 line-clamp-3 flex-1 text-sm leading-relaxed text-gray-600 dark:text-gray-300">
                                            {work.excerpt}
                                        </p>
                                        <div className="mt-4 flex flex-wrap gap-1.5">
                                            {work.tags?.slice(0, 3).map((tag: string) => (
                                                <span key={tag} className="rounded bg-gray-100 px-2 py-0.5 text-[10px] font-medium uppercase tracking-wider text-gray-600 dark:bg-gray-800 dark:text-gray-400">
                                                    {tag}
                                                </span>
                                            ))}
                                        </div>
                                    </div>
                                </article>
                            </Link>
                        )
                    })
                ) : (
                    <div className="py-20 text-center text-gray-500 col-span-full">
                        No works found.
                    </div>
                )}
            </div>
            <div className="mt-6 rounded-2xl border border-border/70 bg-background/80 p-4 shadow-sm">
                <PublicPagination
                    pathname="/works"
                    currentPage={page}
                    totalPages={totalPages}
                    pageSize={currentPageSize}
                    ariaLabel="Works pagination"
                />
            </div>
        </div>
    )
}
