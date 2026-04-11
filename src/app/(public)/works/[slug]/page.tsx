
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
import { formatDetailPublishDate, parseWorkContentHtml } from './work-detail-helpers'

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
    const orderedVideos = [...work.videos].sort((left, right) => left.sortOrder - right.sortOrder)
    const hasInlineVideoEmbeds = hasWorkVideoEmbeds(contentHtml)

    const publishDate = formatDetailPublishDate(work.publishedAt)

    return (
        <article className="container mx-auto max-w-6xl px-4 py-8 md:px-6 md:py-12">
            <div data-testid="work-detail-body" className="mx-auto max-w-3xl">
                <header className="mb-8">
                <h1 className="mb-4 text-3xl font-heading font-bold md:text-4xl text-gray-900 dark:text-gray-50 leading-tight">
                    {work.title}
                </h1>
                <div className="flex flex-wrap items-center gap-4 mb-6">
                    <Badge variant="secondary" className="rounded-full bg-brand-navy px-3 text-white hover:bg-brand-navy/90">
                        {publishDate}
                    </Badge>
                    <span className="text-gray-500 dark:text-gray-400 font-medium">{work.category}</span>
                    {work.period && (
                        <span className="text-sm border-l pl-4 text-gray-400 dark:text-gray-400 font-mono">
                            {work.period}
                        </span>
                    )}
                </div>
                <p className="rounded-r-lg border-l-4 border-brand-navy bg-gray-50 py-2 pl-4 text-xl leading-relaxed text-gray-600 dark:bg-gray-900 dark:text-gray-300">
                    {work.excerpt}
                </p>
                <div className="mt-8 flex flex-wrap gap-2 text-sm text-gray-500 dark:text-gray-400 font-mono">
                    {work.tags?.map((tag: string) => (
                        <span key={tag} className="cursor-default transition-colors hover:text-brand-accent">#{tag}</span>
                    ))}
                </div>
                </header>

                <div className="mt-8">
                    {orderedVideos.length > 0 && !hasInlineVideoEmbeds && (
                        <div className="mb-8 space-y-4">
                            <div data-testid="work-lead-video">
                                <WorkVideoPlayer video={orderedVideos[0]} />
                            </div>
                            {orderedVideos.length > 1 && (
                                <details data-testid="work-more-videos" className="rounded-2xl border border-border/80 p-4">
                                    <summary className="cursor-pointer text-sm font-medium text-muted-foreground">
                                        More videos ({orderedVideos.length - 1})
                                    </summary>
                                    <div className="mt-4 space-y-4">
                                        {orderedVideos.slice(1).map((video) => (
                                            <WorkVideoPlayer key={video.id} video={video} />
                                        ))}
                                    </div>
                                </details>
                            )}
                        </div>
                    )}
                    {contentHtml && (
                        <InteractiveRenderer html={contentHtml} workVideos={orderedVideos} />
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

            <div data-testid="work-related-shell" className="mx-auto mt-16 max-w-3xl border-t pt-12">
                <RelatedContentList
                    heading="다른 작업"
                    hrefBase="/works"
                    items={relatedWorks}
                    desktopPageSize={8}
                    tabletPageSize={4}
                    mobilePageSize={2}
                    testIdBase="related-work"
                />
            </div>
        </article>
    )
}
