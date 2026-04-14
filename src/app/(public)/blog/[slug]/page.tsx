
import Link from 'next/link'
import { notFound } from 'next/navigation'
import { AdminErrorPanel } from '@/components/admin/AdminErrorPanel'
import { BlogEditor } from '@/components/admin/BlogEditor'
import { InlineAdminEditorShell } from '@/components/admin/InlineAdminEditorShell'
import { RelatedContentList } from '@/components/content/RelatedContentList'
import { InteractiveRenderer } from '@/components/content/InteractiveRenderer'
import { TableOfContents } from '@/components/content/TableOfContents'
import { Badge } from '@/components/ui/badge'
import { Metadata } from 'next'
import { fetchServerSession } from '@/lib/api/server'
import { resolveBlogRenderableHtml } from '@/lib/content/blog-content'
import { fetchAdminBlogById, fetchAllPublicBlogs, fetchPublicBlogBySlug } from '@/lib/api/blogs'
import { formatDetailPublishDate } from './blog-detail-helpers'

export const dynamic = 'force-dynamic'

interface PageProps {
    params: Promise<{ slug: string }>
    searchParams?: Promise<{ relatedPage?: string }>
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
    const { slug } = await params
    const decodedSlug = decodeURIComponent(slug)
    const blog = await fetchPublicBlogBySlug(decodedSlug)

    if (!blog) return {}

    return {
        title: blog.title,
        description: blog.excerpt,
    }
}

export default async function BlogDetailPage({ params, searchParams }: PageProps) {
    const { slug } = await params
    const resolvedSearchParams = await searchParams
    const decodedSlug = decodeURIComponent(slug)
    const blog = await fetchPublicBlogBySlug(decodedSlug)

    if (!blog) {
        notFound()
    }

    const session = await fetchServerSession()
    let adminBlog = null
    let adminLoadFailed = false

    if (session.authenticated && session.role === 'admin') {
        try {
            adminBlog = await fetchAdminBlogById(blog.id)
        } catch {
            adminLoadFailed = true
        }
    }

    const allBlogs = await fetchAllPublicBlogs()
    const relatedBlogs = allBlogs.filter((item) => item.id !== blog.id)
    const renderedContent = resolveBlogRenderableHtml(blog.contentJson)

    const publishDate = formatDetailPublishDate(blog.publishedAt)
    const sortedBlogs = [...allBlogs].sort((left, right) => {
        const leftTime = left.publishedAt ? new Date(left.publishedAt).getTime() : 0
        const rightTime = right.publishedAt ? new Date(right.publishedAt).getTime() : 0

        if (leftTime !== rightTime) {
            return rightTime - leftTime
        }

        return left.title.localeCompare(right.title)
    })
    const currentIndex = sortedBlogs.findIndex((item) => item.id === blog.id)
    const newerBlog = currentIndex > 0 ? sortedBlogs[currentIndex - 1] : null
    const olderBlog = currentIndex >= 0 && currentIndex < sortedBlogs.length - 1 ? sortedBlogs[currentIndex + 1] : null
    const relatedPageSuffix = resolvedSearchParams?.relatedPage
        ? `?relatedPage=${encodeURIComponent(resolvedSearchParams.relatedPage)}`
        : ''

    return (
        <article className="container mx-auto max-w-[min(100%,112rem)] px-4 py-8 md:px-6 md:py-12">
            <div className="relative mx-auto max-w-[min(100%,112rem)]">
                <div data-testid="blog-detail-body" className="mx-auto min-w-0 max-w-3xl">
                    <header className="mb-8">
                        <h1 className="mb-4 text-3xl font-heading font-bold leading-tight text-foreground text-balance md:text-4xl">
                            {blog.title}
                        </h1>
                        <div className="mb-6 flex flex-wrap items-center gap-4 font-medium text-muted-foreground">
                            <Badge variant="secondary" className="rounded-full bg-brand-navy px-3 text-white hover:bg-brand-navy/90">
                                <time dateTime={blog.publishedAt ?? undefined}>{publishDate}</time>
                            </Badge>
                            <ul aria-label="Post tags" className="flex flex-wrap gap-2 font-mono text-sm">
                                {blog.tags?.map((tag: string) => (
                                    <li key={tag}>
                                        <span className="cursor-default rounded-full bg-muted px-2 py-0.5 text-xs font-medium text-muted-foreground transition-colors hover:text-brand-accent">
                                            #{tag}
                                        </span>
                                    </li>
                                ))}
                            </ul>
                        </div>
                        {blog.excerpt && (
                            <p className="rounded-r-lg border-l-4 border-brand-navy bg-brand-section-bg py-2 pl-4 text-xl leading-relaxed text-foreground/80 text-pretty">
                                {blog.excerpt}
                            </p>
                        )}
                    </header>

                    <div id="blog-detail-content" className="mt-8">
                        {renderedContent && (
                            <InteractiveRenderer html={renderedContent} />
                        )}
                    </div>

                    {session.authenticated && session.role === 'admin' && (
                        adminLoadFailed || !adminBlog ? (
                            <div className="mt-8">
                                <AdminErrorPanel
                                    title="Inline blog editor is unavailable"
                                    message="The public blog view loaded, but the admin edit payload could not be loaded. Please retry after the backend is healthy."
                                />
                            </div>
                        ) : (
                            <InlineAdminEditorShell
                                triggerLabel="글 수정"
                                title="Blog Inline Editor"
                                description="현재 게시물 뷰를 유지한 채 바로 수정합니다."
                            >
                                <BlogEditor initialBlog={adminBlog} inlineMode />
                            </InlineAdminEditorShell>
                        )
                    )}

                    {(olderBlog || newerBlog) && (
                        <nav
                            aria-label="Blog post navigation"
                            data-testid="blog-prev-next"
                            className="mt-12 grid gap-3 border-t border-border/70 pt-8 sm:grid-cols-2"
                        >
                            {olderBlog ? (
                                <Link
                                    href={`/blog/${olderBlog.slug}${relatedPageSuffix}`}
                                    className="group rounded-2xl border border-border/80 bg-background p-4 transition hover:border-primary/30 hover:shadow-sm"
                                >
                                    <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">Previous</p>
                                    <p className="mt-2 text-base font-semibold text-foreground text-balance transition-colors group-hover:text-brand-accent">{olderBlog.title}</p>
                                </Link>
                            ) : (
                                <div aria-hidden="true" />
                            )}
                            {newerBlog ? (
                                <Link
                                    href={`/blog/${newerBlog.slug}${relatedPageSuffix}`}
                                    className="group rounded-2xl border border-border/80 bg-background p-4 text-left transition hover:border-primary/30 hover:shadow-sm sm:justify-self-end"
                                >
                                    <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">Next</p>
                                    <p className="mt-2 text-base font-semibold text-foreground text-balance transition-colors group-hover:text-brand-accent">{newerBlog.title}</p>
                                </Link>
                            ) : null}
                        </nav>
                    )}

                    <div data-testid="blog-related-shell" className="mx-auto mt-16 max-w-3xl border-t pt-12">
                        <RelatedContentList
                            heading="More Posts"
                            hrefBase="/blog"
                            items={relatedBlogs}
                            desktopPageSize={8}
                            tabletPageSize={4}
                            mobilePageSize={2}
                            testIdBase="related-blog"
                        />
                    </div>
                </div>

                <aside className="hidden xl:absolute xl:right-0 xl:top-0 xl:block xl:w-72">
                    <TableOfContents contentRootId="blog-detail-content" />
                </aside>
            </div>
        </article>
    )
}
