
import Link from 'next/link'
import { BlogEditor } from '@/components/admin/BlogEditor'
import { InlineAdminEditorShell } from '@/components/admin/InlineAdminEditorShell'
import { PublicAdminLink } from '@/components/admin/PublicAdminLink'
import { EdgePaginationNav } from '@/components/layout/EdgePaginationNav'
import { PublicPagination } from '@/components/layout/PublicPagination'
import { ResponsivePageSizeSync } from '@/components/layout/ResponsivePageSizeSync'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { fetchServerSession } from '@/lib/api/server'
import { fetchPublicBlogs } from '@/lib/api/blogs'

export const dynamic = 'force-dynamic'

interface PageProps {
    searchParams?: Promise<{ page?: string; pageSize?: string }>
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
    const currentPage = Math.max(1, Number.parseInt(resolvedSearchParams?.page ?? '1', 10) || 1)
    const currentPageSize = Math.max(1, Number.parseInt(resolvedSearchParams?.pageSize ?? String(DESKTOP_PAGE_SIZE), 10) || DESKTOP_PAGE_SIZE)
    const blogsPayload = await fetchPublicBlogs(currentPage, currentPageSize)
    const session = await fetchServerSession()
    const totalPages = Math.max(1, blogsPayload.totalPages)
    const page = blogsPayload.page
    const pagedBlogs = blogsPayload.items

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
                <h1 className="text-3xl font-heading font-bold md:text-4xl text-gray-900 dark:text-gray-50">Blog</h1>
                <div className="flex flex-wrap gap-2">
                    <PublicAdminLink href="/admin/blog" label="글 관리" variant="manage" />
                </div>
            </div>
            {session.authenticated && session.role === 'admin' && (
                <InlineAdminEditorShell
                    triggerLabel="새 글 쓰기"
                    title="Blog Inline Create"
                    description="navbar를 유지한 채 현재 페이지 아래에서 새 글을 작성합니다."
                >
                    <BlogEditor inlineMode />
                </InlineAdminEditorShell>
            )}
            <div data-testid="blog-grid" className="grid gap-5 md:grid-cols-2 xl:grid-cols-4">
                {pagedBlogs && pagedBlogs.length > 0 ? (
                    pagedBlogs.map((blog) => (
                        <Link
                            key={blog.id}
                            href={`/blog/${blog.slug}?relatedPage=${page}`}
                            className="group/card block h-full"
                            data-testid="blog-card"
                        >
                            <Card className="responsive-feed-card !gap-0 !py-0 flex h-full flex-col overflow-hidden rounded-2xl border-border/80 bg-background shadow-sm transition hover:border-primary/30 hover:shadow-md">
                                <CardHeader className="px-4 pt-4 pb-0 sm:px-5 sm:pt-5">
                                    <CardTitle className="responsive-feed-title line-clamp-2 text-lg font-heading font-bold leading-tight text-gray-900 transition-colors group-hover/card:text-brand-accent dark:text-gray-50 sm:text-xl md:text-2xl">
                                        {blog.title}
                                    </CardTitle>
                                    <div className="responsive-feed-copy mt-2 flex flex-wrap gap-x-4 gap-y-2 text-sm text-gray-600 dark:text-gray-400">
                                        <span>{formatPublishedDate(blog.publishedAt)}</span>
                                        {blog.tags?.length ? (
                                            <span className="font-medium text-gray-500 dark:text-gray-500">
                                                {blog.tags.slice(0, 3).join(' · ')}
                                            </span>
                                        ) : null}
                                    </div>
                                </CardHeader>
                                <CardContent className="flex flex-1 flex-col px-4 pb-4 pt-3 sm:px-5 sm:pb-5">
                                    <p className="responsive-feed-copy line-clamp-3 flex-1 text-sm text-gray-600 dark:text-gray-300 sm:text-base">
                                        {blog.excerpt}
                                    </p>
                                </CardContent>
                            </Card>
                        </Link>
                    ))
                ) : (
                    <div className="py-20 text-center text-gray-500">
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
