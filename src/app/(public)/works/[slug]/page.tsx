
import { notFound } from 'next/navigation'
import { Badge } from '@/components/ui/badge'
import { AdminErrorPanel } from '@/components/admin/AdminErrorPanel'
import { InlineAdminEditorShell } from '@/components/admin/InlineAdminEditorShell'
import { WorkEditor } from '@/components/admin/WorkEditor'
import { RelatedContentList } from '@/components/content/RelatedContentList'
import { InteractiveRenderer } from '@/components/content/InteractiveRenderer'
import { WorkVideoPlayer } from '@/components/content/WorkVideoPlayer'
import { Metadata } from 'next'
import { fetchServerSession } from '@/lib/api/server'
import { fetchAdminWorkById, fetchAllPublicWorks, fetchPublicWorkBySlug } from '@/lib/api/works'
import { hasWorkVideoEmbeds } from '@/lib/content/work-video-embeds'

export const dynamic = 'force-dynamic'

interface PageProps {
    params: Promise<{ slug: string }>
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
    const { slug } = await params
    const decodedSlug = decodeURIComponent(slug)
    const work = await fetchPublicWorkBySlug(decodedSlug)

    if (!work) return {}

    return {
        title: work.title,
        description: work.excerpt,
    }
}

export default async function WorkDetailPage({ params }: PageProps) {
    const { slug } = await params
    const decodedSlug = decodeURIComponent(slug)
    const work = await fetchPublicWorkBySlug(decodedSlug)

    if (!work) {
        notFound()
    }

    const session = await fetchServerSession()
    let adminWork = null
    let adminLoadFailed = false

    if (session.authenticated && session.role === 'admin') {
        try {
            adminWork = await fetchAdminWorkById(work.id)
        } catch {
            adminLoadFailed = true
        }
    }

    const relatedWorks = (await fetchAllPublicWorks())
        .filter((item) => item.id !== work.id)
    const contentHtml = parseWorkContentHtml(work.contentJson)
    const hasInlineVideoEmbeds = hasWorkVideoEmbeds(contentHtml)

    // Format date
    const publishDate = work.publishedAt
        ? new Date(work.publishedAt).toLocaleDateString('en-US', {
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
                    {work.title}
                </h1>
                <div className="flex flex-wrap items-center gap-4 mb-6">
                    <Badge variant="secondary" className="bg-[#FF7B54] text-white hover:bg-[#FF7B54]/90 rounded-full px-3">
                        {publishDate}
                    </Badge>
                    <span className="text-gray-500 dark:text-gray-400 font-medium">{work.category}</span>
                    {work.period && (
                        <span className="text-sm border-l pl-4 text-gray-400 dark:text-gray-500 font-mono">
                            {work.period}
                        </span>
                    )}
                </div>
                <p className="text-xl text-gray-600 dark:text-gray-300 leading-relaxed border-l-4 border-[#FF7B54] pl-4 py-2 bg-gray-50 dark:bg-gray-900 rounded-r-lg">
                    {work.excerpt}
                </p>
                <div className="mt-8 flex flex-wrap gap-2 text-sm text-gray-500 dark:text-gray-400 font-mono">
                    {work.tags?.map((tag: string) => (
                        <span key={tag} className="hover:text-[#FF7B54] transition-colors cursor-default">#{tag}</span>
                    ))}
                </div>
                </header>

                <div className="mt-8">
                    {work.videos.length > 0 && !hasInlineVideoEmbeds && (
                        <div className="mb-8 space-y-4">
                            {work.videos.map((video) => (
                                <WorkVideoPlayer key={video.id} video={video} />
                            ))}
                        </div>
                    )}
                    {contentHtml && (
                        <InteractiveRenderer html={contentHtml} workVideos={work.videos} />
                    )}
                </div>

                {session.authenticated && session.role === 'admin' && (
                    adminLoadFailed || !adminWork ? (
                        <div className="mt-8">
                            <AdminErrorPanel
                                title="Inline work editor is unavailable"
                                message="The public work view loaded, but the admin edit payload could not be loaded. Please retry after the backend is healthy."
                            />
                        </div>
                    ) : (
                        <InlineAdminEditorShell
                            triggerLabel="작업 수정"
                            title="Work Inline Editor"
                            description="현재 작업 상세 뷰를 벗어나지 않고 바로 수정합니다."
                        >
                            <WorkEditor initialWork={adminWork} inlineMode />
                        </InlineAdminEditorShell>
                    )
                )}
            </div>

            <RelatedContentList
                heading="다른 작업"
                hrefBase="/works"
                items={relatedWorks}
                desktopPageSize={8}
                tabletPageSize={4}
                mobilePageSize={2}
                testIdBase="related-work"
            />
        </article>
    )
}

function parseWorkContentHtml(contentJson: string) {
    try {
        const parsed = JSON.parse(contentJson) as { html?: string }
        return parsed.html ?? ''
    } catch {
        return ''
    }
}
