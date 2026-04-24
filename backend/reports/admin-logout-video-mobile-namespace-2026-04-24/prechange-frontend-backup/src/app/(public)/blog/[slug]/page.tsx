import { notFound } from 'next/navigation'
import { PublicBlogDetailAdminActions } from '@/components/admin/PublicBlogDetailAdminActions'
import { PublicDetailAdjacentLink } from '@/components/content/PublicDetailAdjacentLink'
import { RelatedContentList } from '@/components/content/RelatedContentList'
import { InteractiveRenderer } from '@/components/content/InteractiveRenderer'
import { TableOfContents } from '@/components/content/TableOfContents'
import { Badge } from '@/components/ui/badge'
import { Metadata } from 'next'
import { resolveBlogRenderableHtml } from '@/lib/content/blog-content'
import { fetchAllPublicBlogs, fetchPublicBlogBySlug } from '@/lib/api/blogs'
import { createPublicMetadata } from '@/lib/seo'
import { formatDetailPublishDate } from './blog-detail-helpers'

export const revalidate = 60

interface PageProps {
    params: Promise<{ slug: string }>
}

export async function generateStaticParams() {
    const blogs = await fetchAllPublicBlogs().catch(() => [])
    return blogs.map((blog) => ({ slug: blog.slug }))
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
    const { slug } = await params
    const decodedSlug = decodeURIComponent(slug)
    const blog = await fetchPublicBlogBySlug(decodedSlug)

    if (!blog) return {}

    return createPublicMetadata({
        title: blog.title,
        description: blog.excerpt,
        path: `/blog/${blog.slug}`,
        type: 'article',
    })
}

export default async function BlogDetailPage({ params }: PageProps) {
    const { slug } = await params
    const decodedSlug = decodeURIComponent(slug)
    const [blog, allBlogs] = await Promise.all([
        fetchPublicBlogBySlug(decodedSlug),
        fetchAllPublicBlogs(),
    ])

    if (!blog) {
        notFound()
    }

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

    return (
        <article className="mx-auto w-full px-4 py-8 md:px-6 md:py-12">
            <div data-testid="blog-article-content-layout" className="mx-auto xl:grid xl:grid-cols-[minmax(0,1fr)_minmax(0,48rem)_minmax(0,1fr)] xl:items-start xl:gap-12">
                <div data-testid="blog-detail-body" className="mx-auto min-w-0 w-full max-w-3xl xl:col-start-2">
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
                    </header>

                    <PublicBlogDetailAdminActions blogId={blog.id} />

                    <div id="blog-detail-content" className="mt-8">
                        {renderedContent && (
                            <InteractiveRenderer html={renderedContent} />
                        )}
                    </div>

                </div>

                <aside className="hidden xl:col-start-3 xl:block xl:w-full xl:max-w-80 xl:justify-self-start xl:self-start xl:pl-10">
                    <TableOfContents contentRootId="blog-detail-content" />
                </aside>
            </div>

            <div className="mx-auto max-w-3xl">
                {(olderBlog || newerBlog) && (
                    <nav
                        aria-label="Study navigation"
                        data-testid="blog-prev-next"
                        className="mt-12 grid gap-3 border-t border-border/70 pt-8 sm:grid-cols-2"
                    >
                        {newerBlog ? (
                            <PublicDetailAdjacentLink hrefBase="/blog" slug={newerBlog.slug} label="Next" title={newerBlog.title} />
                        ) : (
                            <div aria-hidden="true" />
                        )}
                        {olderBlog ? (
                            <PublicDetailAdjacentLink hrefBase="/blog" slug={olderBlog.slug} label="Previous" title={olderBlog.title} alignEnd />
                        ) : null}
                    </nav>
                )}

                <div data-testid="blog-related-shell" className="mt-16 border-t pt-12">
                    <RelatedContentList
                        heading="More Studies"
                        hrefBase="/blog"
                        items={sortedBlogs}
                        currentItemId={blog.id}
                        desktopPageSize={9}
                        tabletPageSize={4}
                        mobilePageSize={2}
                        testIdBase="related-blog"
                    />
                </div>
            </div>
        </article>
    )
}
