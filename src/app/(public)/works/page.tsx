
import Link from 'next/link'
import Image from 'next/image'
import { Search, X, BriefcaseBusiness } from 'lucide-react'
import { PublicWorksInlineCreateShell } from '@/components/admin/PublicWorksInlineCreateShell'
import { PublicAdminLink } from '@/components/admin/PublicAdminLink'
import { EdgePaginationNav } from '@/components/layout/EdgePaginationNav'
import { PublicPagination } from '@/components/layout/PublicPagination'
import { ResponsivePageSizeSync } from '@/components/layout/ResponsivePageSizeSync'
import { Badge } from '@/components/ui/badge'
import { headers } from 'next/headers'
import { fetchServerSession } from '@/lib/api/server'
import { fetchPublicWorks, type PublicWorkSearchParams } from '@/lib/api/works'

export const dynamic = 'force-dynamic'

interface PageProps {
    searchParams?: Promise<{ page?: string; pageSize?: string; query?: string; searchMode?: string; __qaEmpty?: string; __qaNoImage?: string }>
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
    const qaNoImageWorks = resolvedSearchParams?.__qaNoImage === '1' && /localhost|127\.0\.0\.1/.test(requestHost)
    const currentPage = Math.max(1, Number.parseInt(resolvedSearchParams?.page ?? '1', 10) || 1)
    const currentPageSize = Math.max(1, Number.parseInt(resolvedSearchParams?.pageSize ?? String(DESKTOP_PAGE_SIZE), 10) || DESKTOP_PAGE_SIZE)
    const searchQuery = resolvedSearchParams?.query?.trim() ?? ''
    const searchMode = resolvedSearchParams?.searchMode === 'content' ? 'content' : 'title'
    const queryParams: PublicWorkSearchParams | undefined = searchQuery ? { query: searchQuery, searchMode } : undefined
    const paginationQueryParams: Record<string, string> | undefined = searchQuery ? { query: searchQuery, searchMode } : undefined
    const worksPayload = qaEmptyWorks
        ? { items: [], page: 1, pageSize: currentPageSize, totalItems: 0, totalPages: 1 }
        : await fetchPublicWorks(currentPage, currentPageSize, queryParams)
    const session = await fetchServerSession()
    const totalPages = Math.max(1, worksPayload.totalPages)
    const page = worksPayload.page
    const pagedWorks = qaNoImageWorks
        ? worksPayload.items.map((work) => ({
            ...work,
            thumbnailUrl: null,
        }))
        : worksPayload.items
    const returnTo = encodeURIComponent(`/works?page=${page}&pageSize=${currentPageSize}`)

    return (
        <div className="container mx-auto max-w-7xl px-4 py-8 md:px-6 md:py-12">
            <EdgePaginationNav
                pathname="/works"
                currentPage={page}
                totalPages={totalPages}
                pageSize={currentPageSize}
                queryParams={paginationQueryParams}
            />
            <ResponsivePageSizeSync
                desktopPageSize={DESKTOP_PAGE_SIZE}
                tabletPageSize={TABLET_PAGE_SIZE}
                mobilePageSize={MOBILE_PAGE_SIZE}
            />
            <div className="mb-8 flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
                <h1 className="text-3xl font-heading font-bold text-foreground md:text-4xl">Works</h1>
                <div className="flex flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-center sm:justify-end">
                    <form action="/works" method="get" role="search" className="flex flex-col gap-2 sm:flex-row sm:items-center">
                        <input type="hidden" name="page" value="1" />
                        <input type="hidden" name="pageSize" value={currentPageSize} />
                        <label htmlFor="work-search" className="sr-only">Search work</label>
                        <div className="flex min-h-11 items-center gap-2 rounded-full border border-border bg-background px-3 transition-colors focus-within:border-ring focus-within:ring-[3px] focus-within:ring-ring/20">
                            <Search className="h-4 w-4 text-muted-foreground" aria-hidden="true" />
                            <input
                                id="work-search"
                                name="query"
                                defaultValue={searchQuery}
                                placeholder="Search work"
                                className="w-full min-w-0 bg-transparent text-sm text-foreground outline-none placeholder:text-muted-foreground sm:w-56"
                            />
                        </div>
                        <label htmlFor="work-search-mode" className="sr-only">Search mode</label>
                        <select
                            id="work-search-mode"
                            name="searchMode"
                            defaultValue={searchMode}
                            className="min-h-11 rounded-full border border-border bg-background px-3 text-sm text-foreground"
                            aria-label="Work search mode"
                        >
                            <option value="title">Title</option>
                            <option value="content">Content</option>
                        </select>
                        <button
                            type="submit"
                            className="inline-flex min-h-11 items-center justify-center gap-2 rounded-full bg-foreground px-4 py-2 text-sm font-semibold text-background transition-colors hover:bg-foreground/90"
                        >
                            <Search className="h-4 w-4" aria-hidden="true" />
                            Search
                        </button>
                        {searchQuery ? (
                            <Link
                                href={`/works?page=1&pageSize=${currentPageSize}`}
                                className="inline-flex min-h-11 items-center justify-center gap-2 rounded-full border border-border px-4 py-2 text-sm font-medium text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
                            >
                                <X className="h-4 w-4" aria-hidden="true" />
                                Clear
                            </Link>
                        ) : null}
                    </form>
                    <PublicAdminLink href="/admin/works" label="작업 관리" variant="manage" />
                </div>
            </div>
            {session.authenticated && session.role === 'admin' && (
                <PublicWorksInlineCreateShell />
            )}
            <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-4">
                {pagedWorks && pagedWorks.length > 0 ? (
                    pagedWorks.map((work) => {
                        const thumbnailUrl = work.thumbnailUrl ?? null
                        const publishDate = formatPublishedMonth(work.publishedAt)

                        return (
                            <Link
                                key={work.id}
                                href={`/works/${work.slug}?returnTo=${returnTo}&relatedPage=${page}`}
                                className="group/card block h-full"
                                data-testid="work-card"
                            >
                                <article className="responsive-feed-card works-feed-card flex h-full flex-col overflow-hidden rounded-2xl border border-border/80 bg-background shadow-sm transition hover:border-primary/30 hover:shadow-md">
                                    <div className="relative aspect-[4/3] overflow-hidden bg-muted">
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
                                            <div
                                                data-testid="work-card-no-image-placeholder"
                                                className="flex h-full w-full flex-col items-center justify-center gap-2 bg-gradient-to-br from-muted to-muted/80 text-muted-foreground"
                                            >
                                                <BriefcaseBusiness className="h-8 w-8" aria-hidden="true" />
                                                <span className="text-xs font-medium">No Image</span>
                                            </div>
                                        )}
                                    </div>
                                    <div className="flex flex-1 flex-col p-4 sm:p-5">
                                        <div className="mb-3 flex flex-wrap items-center gap-2">
                                            <Badge variant="secondary" className="rounded-full bg-brand-navy px-2.5 py-0.5 text-xs text-white hover:bg-brand-navy/90">
                                                {publishDate}
                                            </Badge>
                                            <span className="responsive-feed-copy text-xs font-medium uppercase tracking-wide text-muted-foreground">
                                                {work.category}
                                            </span>
                                        </div>
                                        <h2 className="responsive-feed-title works-feed-title line-clamp-2 text-lg font-heading font-bold leading-tight text-foreground transition-colors group-hover/card:text-brand-accent sm:text-xl">
                                            {work.title}
                                        </h2>
                                        <p className="responsive-feed-copy works-feed-excerpt mt-2 line-clamp-3 flex-1 text-sm leading-relaxed text-foreground/80">
                                            {work.excerpt}
                                        </p>
                                        <div className="works-feed-tags mt-4 flex flex-wrap content-start gap-1.5 overflow-hidden">
                                            {work.tags?.slice(0, 3).map((tag: string) => (
                                                <span key={tag} className="rounded-full bg-muted px-2 py-0.5 text-xs font-medium text-muted-foreground">
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
                    <div className="col-span-full rounded-[2rem] border border-dashed border-border/80 bg-muted/30 px-6 py-20 text-center text-muted-foreground">
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
                    queryParams={paginationQueryParams}
                />
            </div>
        </div>
    )
}
