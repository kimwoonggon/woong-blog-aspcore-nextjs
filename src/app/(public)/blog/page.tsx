
import Link from 'next/link'
import { BlogEditor } from '@/components/admin/BlogEditor'
import { InlineAdminEditorShell } from '@/components/admin/InlineAdminEditorShell'
import { PublicAdminLink } from '@/components/admin/PublicAdminLink'
import { EdgePaginationNav } from '@/components/layout/EdgePaginationNav'
import { PublicPagination } from '@/components/layout/PublicPagination'
import { ResponsivePageSizeSync } from '@/components/layout/ResponsivePageSizeSync'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { headers } from 'next/headers'
import { fetchServerSession } from '@/lib/api/server'
import { fetchPublicBlogs } from '@/lib/api/blogs'

export const dynamic = 'force-dynamic'

interface PageProps {
    searchParams?: Promise<{ page?: string; pageSize?: string; __qaTagged?: string; __qaEmpty?: string }>
}

const DESKTOP_PAGE_SIZE = 12
const TABLET_PAGE_SIZE = 4
const MOBILE_PAGE_SIZE = 2

function formatPublishedDate(publishedAt?: string | null) {
    return publishedAt
        ? new Date(publishedAt).toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
        })
        : 'Unknown Date'
}

export default async function BlogPage({ searchParams }: PageProps) {
    const resolvedSearchParams = await searchParams
    const headerStore = await headers()
    const requestHost = (headerStore.get('x-forwarded-host') ?? headerStore.get('host') ?? '').toLowerCase()
    const qaEmptyBlogs = resolvedSearchParams?.__qaEmpty === '1' && /localhost|127\.0\.0\.1/.test(requestHost)
    const qaTaggedBlogs = resolvedSearchParams?.__qaTagged === '1' && /localhost|127\.0\.0\.1/.test(requestHost)
    const currentPage = Math.max(1, Number.parseInt(resolvedSearchParams?.page ?? '1', 10) || 1)
    const currentPageSize = Math.max(1, Number.parseInt(resolvedSearchParams?.pageSize ?? String(DESKTOP_PAGE_SIZE), 10) || DESKTOP_PAGE_SIZE)
    const blogsPayload = qaEmptyBlogs
        ? { items: [], page: 1, pageSize: currentPageSize, totalItems: 0, totalPages: 1 }
        : await fetchPublicBlogs(currentPage, currentPageSize)
    const session = await fetchServerSession()
    const totalPages = Math.max(1, blogsPayload.totalPages)
    const page = blogsPayload.page
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
            />
            <ResponsivePageSizeSync
                desktopPageSize={DESKTOP_PAGE_SIZE}
                tabletPageSize={TABLET_PAGE_SIZE}
                mobilePageSize={MOBILE_PAGE_SIZE}
            />
            <div className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                <h1 className="text-3xl font-heading font-bold text-foreground md:text-4xl">Blog</h1>
                <div className="flex flex-wrap gap-2">
                    <PublicAdminLink href="/admin/blog" label="글 관리" variant="manage" />
                </div>
            </div>
            {session.authenticated && session.role === 'admin' && (
                <InlineAdminEditorShell
                    triggerLabel="새 글 쓰기"
                    title="Blog Inline Create"
                    description="Create a new post inline without leaving the current public page."
                >
                    <BlogEditor inlineMode />
                </InlineAdminEditorShell>
            )}
            <div data-testid="blog-grid" className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
                {pagedBlogs && pagedBlogs.length > 0 ? (
                    pagedBlogs.map((blog) => (
                        <Link
                            key={blog.id}
                            href={`/blog/${blog.slug}?relatedPage=${page}`}
                            className="group/card block h-full"
                            data-testid="blog-card"
                        >
                            <Card className="responsive-feed-card flex h-full flex-col gap-0 overflow-hidden rounded-2xl border-border/80 bg-background py-0 shadow-sm transition hover:border-primary/30 hover:shadow-md">
                                <div
                                    data-testid="blog-card-accent-stripe"
                                    className="h-1 w-full rounded-t-2xl bg-gradient-to-r from-brand-accent to-brand-cyan"
                                />
                                <CardHeader className="px-4 pt-4 pb-0 sm:px-5 sm:pt-5">
                                    <div className="mb-2 flex flex-wrap items-center gap-2">
                                        <Badge variant="secondary" className="rounded-full bg-brand-navy px-2.5 py-0.5 text-xs text-white hover:bg-brand-navy/90">
                                            {formatPublishedDate(blog.publishedAt)}
                                        </Badge>
                                        {blog.tags?.slice(0, 2).map((tag) => (
                                            <span
                                                key={tag}
                                                className="rounded-full bg-muted px-2 py-0.5 text-xs font-medium text-muted-foreground"
                                            >
                                                {tag}
                                            </span>
                                        ))}
                                    </div>
                                    <CardTitle className="responsive-feed-title line-clamp-2 text-lg font-heading font-bold leading-tight text-foreground transition-colors group-hover/card:text-brand-accent sm:text-xl">
                                        {blog.title}
                                    </CardTitle>
                                </CardHeader>
                                <CardContent className="flex flex-1 flex-col px-4 pb-4 pt-3 sm:px-5 sm:pb-5">
                                    {blog.excerpt ? (
                                        <p className="responsive-feed-copy line-clamp-3 flex-1 text-sm leading-relaxed text-muted-foreground sm:text-base">
                                            {blog.excerpt}
                                        </p>
                                    ) : null}
                                </CardContent>
                            </Card>
                        </Link>
                    ))
                ) : (
                    <div className="py-20 text-center text-muted-foreground">
                        No blog posts found.
                    </div>
                )}
            </div>
            <div className="mt-6 rounded-2xl border border-border/70 bg-background/80 p-4 shadow-sm">
                <PublicPagination
                    pathname="/blog"
                    currentPage={page}
                    totalPages={totalPages}
                    pageSize={currentPageSize}
                    ariaLabel="Blog pagination"
                />
            </div>
        </div>
    )
}
