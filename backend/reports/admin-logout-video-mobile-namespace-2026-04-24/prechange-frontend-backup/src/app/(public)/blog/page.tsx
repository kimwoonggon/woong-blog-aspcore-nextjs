
import Link from 'next/link'
import { redirect } from 'next/navigation'
import { Search, X } from 'lucide-react'
import { PublicBlogListAdminCreate } from '@/components/admin/PublicBlogListAdminCreate'
import { PublicAdminClientGate } from '@/components/admin/PublicAdminClientGate'
import { PublicAdminLink } from '@/components/admin/PublicAdminLink'
import { PublicResponsiveFeed } from '@/components/content/PublicResponsiveFeed'
import { EdgePaginationNav } from '@/components/layout/EdgePaginationNav'
import { PublicPagination } from '@/components/layout/PublicPagination'
import { ResponsivePageSizeSync } from '@/components/layout/ResponsivePageSizeSync'
import { fetchPublicBlogs } from '@/lib/api/blogs'
import { createPublicMetadata } from '@/lib/seo'

export const revalidate = 60
export const metadata = createPublicMetadata({
    title: 'Study',
    description: 'Study notes, engineering writeups, and implementation records.',
    path: '/blog',
})

interface PageProps {
    searchParams?: Promise<{
        page?: string
        pageSize?: string
        query?: string
        searchMode?: string
        focusSearch?: string
        __qaTagged?: string
        __qaEmpty?: string
    }>
}

const DESKTOP_PAGE_SIZE = 12
const TABLET_PAGE_SIZE = 8
const MOBILE_PAGE_SIZE = 4
const INFINITE_PAGE_SIZE = 10
const ENABLE_LOCAL_QA_FLAGS = process.env.ENABLE_LOCAL_ADMIN_SHORTCUT === 'true' || process.env.NODE_ENV !== 'production'

function buildBlogListHref({
    page,
    pageSize,
    query,
}: {
    page: number
    pageSize: number
    query?: string
}) {
    const params = new URLSearchParams({
        page: String(page),
        pageSize: String(pageSize),
    })

    if (query) {
        params.set('query', query)
    }

    return `/blog?${params.toString()}`
}

function resolveRequestedPageSize(pageSizeParam?: string) {
    const parsedPageSize = Number.parseInt(pageSizeParam ?? String(DESKTOP_PAGE_SIZE), 10) || DESKTOP_PAGE_SIZE
    const safePageSize = Math.max(1, parsedPageSize)
    const supportedPageSizes = new Set([DESKTOP_PAGE_SIZE, TABLET_PAGE_SIZE, MOBILE_PAGE_SIZE])

    if (safePageSize < MOBILE_PAGE_SIZE || supportedPageSizes.has(safePageSize)) {
        return safePageSize
    }

    return DESKTOP_PAGE_SIZE
}

export default async function BlogPage({ searchParams }: PageProps) {
    const resolvedSearchParams = await searchParams
    const qaEmptyBlogs = resolvedSearchParams?.__qaEmpty === '1' && ENABLE_LOCAL_QA_FLAGS
    const qaTaggedBlogs = resolvedSearchParams?.__qaTagged === '1' && ENABLE_LOCAL_QA_FLAGS
    const currentPage = Math.max(1, Number.parseInt(resolvedSearchParams?.page ?? '1', 10) || 1)
    const currentPageSize = resolveRequestedPageSize(resolvedSearchParams?.pageSize)
    const searchQuery = resolvedSearchParams?.query?.trim() ?? ''
    const shouldFocusSearch = resolvedSearchParams?.focusSearch === '1'
    const legacySearchMode = resolvedSearchParams?.searchMode === 'content' || resolvedSearchParams?.searchMode === 'title'
        ? resolvedSearchParams.searchMode
        : undefined
    const searchQueryParams = searchQuery ? { query: searchQuery } : undefined
    const legacySearchQueryParams = searchQuery ? { query: searchQuery, legacySearchMode } : undefined
    const [blogsPayload, mobileBlogsPayload] = qaEmptyBlogs
        ? [
            { items: [], page: 1, pageSize: currentPageSize, totalItems: 0, totalPages: 1 },
            { items: [], page: 1, pageSize: INFINITE_PAGE_SIZE, totalItems: 0, totalPages: 1 },
        ]
        : await Promise.all([
            fetchPublicBlogs(currentPage, currentPageSize, legacySearchQueryParams),
            fetchPublicBlogs(1, INFINITE_PAGE_SIZE, legacySearchQueryParams),
        ])
    const totalPages = Math.max(1, blogsPayload.totalPages)
    const clampedPage = Math.min(currentPage, totalPages)
    if (resolvedSearchParams?.page && Number.parseInt(resolvedSearchParams.page, 10) !== clampedPage) {
        redirect(buildBlogListHref({
            page: clampedPage,
            pageSize: currentPageSize,
            query: searchQuery,
        }))
    }

    const page = Math.min(Math.max(1, blogsPayload.page), totalPages)
    const returnToParams = new URLSearchParams({
        page: String(page),
        pageSize: String(currentPageSize),
    })
    if (searchQuery) {
        returnToParams.set('query', searchQuery)
    }
    const returnTo = encodeURIComponent(`/blog?${returnToParams.toString()}`)
    const pagedBlogs = qaTaggedBlogs
        ? blogsPayload.items.map((blog, index) => ({
            ...blog,
            tags: blog.tags.length > 0
                ? blog.tags
                : index % 2 === 0
                    ? ['playwright', 'qa']
                    : ['seed', 'migration'],
        }))
        : blogsPayload.items

    return (
        <div className="container mx-auto max-w-7xl px-4 py-8 md:px-6 md:py-12">
            <EdgePaginationNav
                pathname="/blog"
                currentPage={page}
                totalPages={totalPages}
                pageSize={currentPageSize}
                queryParams={searchQueryParams}
            />
            <ResponsivePageSizeSync
                desktopPageSize={DESKTOP_PAGE_SIZE}
                tabletPageSize={TABLET_PAGE_SIZE}
                mobilePageSize={MOBILE_PAGE_SIZE}
                infiniteBelowDesktop
                infinitePageSize={INFINITE_PAGE_SIZE}
            />
            <div className="mb-8 flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
                <h1 className="text-3xl font-heading font-bold text-foreground md:text-4xl">Study</h1>
                <div className="flex flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-center sm:justify-end">
                    <form action="/blog" method="get" role="search" className="flex flex-col gap-2 sm:flex-row sm:items-center">
                        <label htmlFor="study-search" className="sr-only">Search studies</label>
                        <div className="flex min-h-11 items-center gap-2 rounded-full border border-border bg-background px-3 transition-colors focus-within:border-ring focus-within:ring-[3px] focus-within:ring-ring/20">
                            <Search className="h-4 w-4 text-muted-foreground" aria-hidden="true" />
                            <input
                                id="study-search"
                                name="query"
                                defaultValue={searchQuery}
                                autoFocus={shouldFocusSearch}
                                placeholder="Search studies"
                                className="w-full min-w-0 bg-transparent text-sm text-foreground outline-none placeholder:text-muted-foreground sm:w-56"
                            />
                        </div>
                        <button
                            type="submit"
                            aria-label="Search studies"
                            title="Search studies"
                            className="inline-flex min-h-11 min-w-11 items-center justify-center rounded-full bg-foreground p-2 text-sm font-semibold text-background transition-colors hover:bg-foreground/90"
                        >
                            <Search className="h-4 w-4" aria-hidden="true" />
                        </button>
                        {searchQuery ? (
                            <Link
                                href="/blog"
                                aria-label="Clear study search"
                                title="Clear study search"
                                className="inline-flex min-h-11 items-center justify-center gap-2 rounded-full border border-border px-4 py-2 text-sm font-medium text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
                            >
                                <X className="h-4 w-4" aria-hidden="true" />
                                Clear
                            </Link>
                        ) : null}
                    </form>
                    <PublicAdminClientGate>
                        <PublicAdminLink href="/admin/blog" label="글 관리" canShow variant="manage" />
                    </PublicAdminClientGate>
                </div>
            </div>
            <PublicBlogListAdminCreate
                afterSaveHref={buildBlogListHref({
                    page: 1,
                    pageSize: currentPageSize,
                })}
            />
            <PublicResponsiveFeed
                kind="blog"
                query={searchQuery}
                desktopPayload={{ ...blogsPayload, items: pagedBlogs }}
                mobileInitialPayload={mobileBlogsPayload}
                desktopReturnTo={returnTo}
            />
            <div className="mt-6 hidden rounded-2xl border border-border/70 bg-background/80 p-4 shadow-sm lg:block">
                <PublicPagination
                    pathname="/blog"
                    currentPage={page}
                    totalPages={totalPages}
                    pageSize={currentPageSize}
                    ariaLabel="Study pagination"
                    queryParams={searchQueryParams}
                />
            </div>
        </div>
    )
}
