
import Link from 'next/link'
import { redirect } from 'next/navigation'
import { Search, X } from 'lucide-react'
import { InlineBlogEditorSection } from '@/components/admin/InlineBlogEditorSection'
import { PublicAdminLink } from '@/components/admin/PublicAdminLink'
import { EdgePaginationNav } from '@/components/layout/EdgePaginationNav'
import { PublicPagination } from '@/components/layout/PublicPagination'
import { ResponsivePageSizeSync } from '@/components/layout/ResponsivePageSizeSync'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { headers } from 'next/headers'
import { getPublicAdminAffordanceState } from '@/lib/auth/public-admin'
import { fetchPublicBlogs } from '@/lib/api/blogs'

export const dynamic = 'force-dynamic'

interface PageProps {
    searchParams?: Promise<{ page?: string; pageSize?: string; query?: string; searchMode?: string; __qaTagged?: string; __qaEmpty?: string }>
}

const DESKTOP_PAGE_SIZE = 12
const TABLET_PAGE_SIZE = 8
const MOBILE_PAGE_SIZE = 4

function formatPublishedDate(publishedAt?: string | null) {
    return publishedAt
        ? new Date(publishedAt).toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
        })
        : 'Unknown Date'
}

function buildBlogListHref({
    page,
    pageSize,
    query,
    searchMode,
}: {
    page: number
    pageSize: number
    query?: string
    searchMode?: string
}) {
    const params = new URLSearchParams({
        page: String(page),
        pageSize: String(pageSize),
    })

    if (query) {
        params.set('query', query)
        params.set('searchMode', searchMode === 'content' ? 'content' : 'title')
    }

    return `/blog?${params.toString()}`
}

export default async function BlogPage({ searchParams }: PageProps) {
    const resolvedSearchParams = await searchParams
    const headerStore = await headers()
    const requestHost = (headerStore.get('x-forwarded-host') ?? headerStore.get('host') ?? '').toLowerCase()
    const qaEmptyBlogs = resolvedSearchParams?.__qaEmpty === '1' && /localhost|127\.0\.0\.1/.test(requestHost)
    const qaTaggedBlogs = resolvedSearchParams?.__qaTagged === '1' && /localhost|127\.0\.0\.1/.test(requestHost)
    const currentPage = Math.max(1, Number.parseInt(resolvedSearchParams?.page ?? '1', 10) || 1)
    const currentPageSize = Math.max(1, Number.parseInt(resolvedSearchParams?.pageSize ?? String(DESKTOP_PAGE_SIZE), 10) || DESKTOP_PAGE_SIZE)
    const searchQuery = resolvedSearchParams?.query?.trim() ?? ''
    const searchMode = resolvedSearchParams?.searchMode === 'content' ? 'content' : 'title'
    const searchQueryParams = searchQuery ? { query: searchQuery, searchMode } : undefined
    const blogsPayload = qaEmptyBlogs
        ? { items: [], page: 1, pageSize: currentPageSize, totalItems: 0, totalPages: 1 }
        : await fetchPublicBlogs(currentPage, currentPageSize, searchQueryParams)
    const { canShowAdminAffordances } = await getPublicAdminAffordanceState()
    const totalPages = Math.max(1, blogsPayload.totalPages)
    const clampedPage = Math.min(currentPage, totalPages)
    if (resolvedSearchParams?.page && Number.parseInt(resolvedSearchParams.page, 10) !== clampedPage) {
        redirect(buildBlogListHref({
            page: clampedPage,
            pageSize: currentPageSize,
            query: searchQuery,
            searchMode,
        }))
    }

    const page = Math.min(Math.max(1, blogsPayload.page), totalPages)
    const returnToParams = new URLSearchParams({
        page: String(page),
        pageSize: String(currentPageSize),
    })
    if (searchQuery) {
        returnToParams.set('query', searchQuery)
        returnToParams.set('searchMode', searchMode)
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
            />
            <div className="mb-8 flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
                <h1 className="text-3xl font-heading font-bold text-foreground md:text-4xl">Study</h1>
                <div className="flex flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-center sm:justify-end">
                    <form action="/blog" method="get" role="search" className="flex flex-col gap-2 sm:flex-row sm:items-center">
                        <input type="hidden" name="page" value="1" />
                        <input type="hidden" name="pageSize" value={currentPageSize} />
                        <label htmlFor="study-search" className="sr-only">Search studies</label>
                        <div className="flex min-h-11 items-center gap-2 rounded-full border border-border bg-background px-3 transition-colors focus-within:border-ring focus-within:ring-[3px] focus-within:ring-ring/20">
                            <Search className="h-4 w-4 text-muted-foreground" aria-hidden="true" />
                            <input
                                id="study-search"
                                name="query"
                                defaultValue={searchQuery}
                                placeholder="Search studies"
                                className="w-full min-w-0 bg-transparent text-sm text-foreground outline-none placeholder:text-muted-foreground sm:w-56"
                            />
                        </div>
                        <label htmlFor="study-search-mode" className="sr-only">Search mode</label>
                        <select
                            id="study-search-mode"
                            name="searchMode"
                            defaultValue={searchMode}
                            className="min-h-11 rounded-full border border-border bg-background px-3 text-sm text-foreground"
                            aria-label="Study search mode"
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
                                href={`/blog?page=1&pageSize=${currentPageSize}`}
                                className="inline-flex min-h-11 items-center justify-center gap-2 rounded-full border border-border px-4 py-2 text-sm font-medium text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
                            >
                                <X className="h-4 w-4" aria-hidden="true" />
                                Clear
                            </Link>
                        ) : null}
                    </form>
                    <PublicAdminLink href="/admin/blog" label="글 관리" canShow={canShowAdminAffordances} variant="manage" />
                </div>
            </div>
            {canShowAdminAffordances && (
                <InlineBlogEditorSection
                    triggerLabel="새 글 쓰기"
                    title="Study Inline Create"
                    description="Create a new study note inline without leaving the current public page."
                    afterSaveHref={buildBlogListHref({
                        page: 1,
                        pageSize: currentPageSize,
                    })}
                />
            )}
            <div data-testid="blog-grid" className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
                {pagedBlogs && pagedBlogs.length > 0 ? (
                    pagedBlogs.map((blog) => (
                        <Link
                            key={blog.id}
                            href={`/blog/${blog.slug}?returnTo=${returnTo}&relatedPage=${page}`}
                            prefetch={false}
                            className="group/card block h-full min-w-0"
                            data-testid="blog-card"
                        >
                            <Card className="responsive-feed-card flex h-full min-w-0 flex-col gap-0 overflow-hidden rounded-2xl border-border/80 bg-background py-0 shadow-sm transition hover:border-primary/30 hover:shadow-md">
                                <div
                                    data-testid="blog-card-accent-stripe"
                                    className="study-card-stripe h-1 w-full rounded-t-2xl"
                                />
                                <CardHeader className="min-w-0 px-4 pt-4 pb-0 sm:px-5 sm:pt-5">
                                    <div className="mb-2 flex flex-wrap items-center gap-2">
                                        <Badge variant="secondary" className="rounded-full bg-brand-navy px-2.5 py-0.5 text-xs text-white hover:bg-brand-navy/90">
                                            {formatPublishedDate(blog.publishedAt)}
                                        </Badge>
                                        {blog.tags?.slice(0, 2).map((tag) => (
                                            <span
                                                key={tag}
                                                className="max-w-full rounded-full bg-muted px-2 py-0.5 text-xs font-medium text-muted-foreground [overflow-wrap:anywhere]"
                                            >
                                                {tag}
                                            </span>
                                        ))}
                                    </div>
                                    <CardTitle className="responsive-feed-title line-clamp-2 min-w-0 break-words text-lg font-heading font-bold leading-tight text-foreground transition-colors [overflow-wrap:anywhere] group-hover/card:text-brand-accent sm:text-xl">
                                        {blog.title}
                                    </CardTitle>
                                </CardHeader>
                                <CardContent className="flex min-w-0 flex-1 flex-col px-4 pb-4 pt-3 sm:px-5 sm:pb-5">
                                    {blog.excerpt ? (
                                        <p className="responsive-feed-copy line-clamp-3 min-w-0 flex-1 break-words text-sm leading-relaxed text-muted-foreground [overflow-wrap:anywhere] sm:text-base">
                                            {blog.excerpt}
                                        </p>
                                    ) : null}
                                </CardContent>
                            </Card>
                        </Link>
                    ))
                ) : (
                    <div className="py-20 text-center text-muted-foreground">
                        {searchQuery ? 'No studies found.' : 'No blog posts found.'}
                    </div>
                )}
            </div>
            <div className="mt-6 rounded-2xl border border-border/70 bg-background/80 p-4 shadow-sm">
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
