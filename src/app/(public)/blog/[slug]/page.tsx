
import { notFound } from 'next/navigation'
import { AdminErrorPanel } from '@/components/admin/AdminErrorPanel'
import { BlogEditor } from '@/components/admin/BlogEditor'
import { InlineAdminEditorShell } from '@/components/admin/InlineAdminEditorShell'
import { RelatedContentList } from '@/components/content/RelatedContentList'
import { InteractiveRenderer } from '@/components/content/InteractiveRenderer'
import { Badge } from '@/components/ui/badge'
import { Metadata } from 'next'
import { fetchServerSession } from '@/lib/api/server'
import { resolveBlogRenderableHtml } from '@/lib/content/blog-content'
import { fetchAdminBlogById, fetchAllPublicBlogs, fetchPublicBlogBySlug } from '@/lib/api/blogs'

export const dynamic = 'force-dynamic'

interface PageProps {
    params: Promise<{ slug: string }>
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

export default async function BlogDetailPage({ params }: PageProps) {
    const { slug } = await params
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

    const relatedBlogs = (await fetchAllPublicBlogs())
        .filter((item) => item.id !== blog.id)
    const renderedContent = resolveBlogRenderableHtml(blog.contentJson)

    // Format date
    const publishDate = blog.publishedAt
        ? new Date(blog.publishedAt).toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'long',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        })
        : 'Unknown Date'

    return (
        <article className="container mx-auto max-w-6xl px-4 py-8 md:px-6 md:py-12">
            <div className="mx-auto max-w-3xl">
                <header className="mb-8">
                <h1 className="mb-4 text-3xl font-heading font-bold md:text-4xl text-gray-900 dark:text-gray-50 leading-tight">
                    {blog.title}
                </h1>
                <div className="mb-6 flex flex-wrap items-center gap-4 text-gray-500 dark:text-gray-400 font-medium">
                    <Badge variant="secondary" className="rounded-full bg-[#F3434F] px-3 text-white hover:bg-[#F3434F]/90">
                        <time dateTime={blog.publishedAt ?? undefined}>{publishDate}</time>
                    </Badge>
                    <div className="flex flex-wrap gap-2 font-mono text-sm">
                        {blog.tags?.map((tag: string) => (
                            <span key={tag} className="rounded-full border border-border/80 px-2.5 py-1 hover:text-[#F3434F] transition-colors cursor-default">#{tag}</span>
                        ))}
                    </div>
                </div>
                {blog.excerpt && (
                    <p className="rounded-r-lg border-l-4 border-[#F3434F] bg-gray-50 py-2 pl-4 text-xl leading-relaxed text-gray-600 dark:bg-gray-900 dark:text-gray-300">
                        {blog.excerpt}
                    </p>
                )}
                </header>

                <div className="mt-8">
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
            </div>

            <RelatedContentList
                heading="다른 게시물"
                hrefBase="/blog"
                items={relatedBlogs}
                desktopPageSize={8}
                tabletPageSize={4}
                mobilePageSize={2}
                testIdBase="related-blog"
            />
        </article>
    )
}
