import Link from 'next/link'
import { notFound } from 'next/navigation'
import { Badge } from '@/components/ui/badge'
import { AdminErrorPanel } from '@/components/admin/AdminErrorPanel'
import { InlineAdminEditorShell } from '@/components/admin/InlineAdminEditorShell'
import { WorkEditor } from '@/components/admin/WorkEditor'
import { RelatedContentList } from '@/components/content/RelatedContentList'
import { InteractiveRenderer } from '@/components/content/InteractiveRenderer'
import { TableOfContents } from '@/components/content/TableOfContents'
import { WorkVideoPlayer } from '@/components/content/WorkVideoPlayer'
import { Metadata } from 'next'
import { fetchServerSession } from '@/lib/api/server'
import { fetchAdminWorkById, fetchAllPublicWorks, fetchPublicWorkBySlug } from '@/lib/api/works'
import { hasWorkVideoEmbeds } from '@/lib/content/work-video-embeds'
import { formatDetailPublishDate, parseWorkContentHtml } from './work-detail-helpers'

export const dynamic = 'force-dynamic'

interface PageProps {
    params: Promise<{ slug: string }>
    searchParams?: Promise<{ relatedPage?: string }>
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

export default async function WorkDetailPage({ params, searchParams }: PageProps) {
    const { slug } = await params
    const resolvedSearchParams = await searchParams
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

    const allWorks = await fetchAllPublicWorks()
    const relatedWorks = allWorks.filter((item) => item.id !== work.id)
    const contentHtml = parseWorkContentHtml(work.contentJson)
    const orderedVideos = [...work.videos].sort((left, right) => left.sortOrder - right.sortOrder)
    const hasInlineVideoEmbeds = hasWorkVideoEmbeds(contentHtml)

    const publishDate = formatDetailPublishDate(work.publishedAt)
    const sortedWorks = [...allWorks].sort((left, right) => {
        const leftTime = left.publishedAt ? new Date(left.publishedAt).getTime() : 0
        const rightTime = right.publishedAt ? new Date(right.publishedAt).getTime() : 0

        if (leftTime !== rightTime) {
            return rightTime - leftTime
        }

        return left.title.localeCompare(right.title)
    })
    const currentIndex = sortedWorks.findIndex((item) => item.id === work.id)
    const newerWork = currentIndex > 0 ? sortedWorks[currentIndex - 1] : null
    const olderWork = currentIndex >= 0 && currentIndex < sortedWorks.length - 1 ? sortedWorks[currentIndex + 1] : null
    const relatedPageSuffix = resolvedSearchParams?.relatedPage
        ? `?relatedPage=${encodeURIComponent(resolvedSearchParams.relatedPage)}`
        : ''

    return (
        <article className="container mx-auto max-w-6xl px-4 py-8 md:px-6 md:py-12">
            <div className="relative mx-auto max-w-6xl">
                <div data-testid="work-detail-body" className="mx-auto max-w-3xl min-w-0">
                    <header className="mb-8">
                        <h1 className="mb-4 text-3xl font-heading font-bold leading-tight text-foreground text-balance md:text-4xl">
                            {work.title}
                        </h1>
                        <div className="mb-6 flex flex-wrap items-center gap-4 text-muted-foreground">
                            <Badge variant="secondary" className="rounded-full bg-brand-navy px-3 text-white hover:bg-brand-navy/90">
                                <time dateTime={work.publishedAt ?? undefined}>{publishDate}</time>
                            </Badge>
                            <span className="font-medium text-muted-foreground">{work.category}</span>
                            {work.period && (
                                <span className="border-l border-border pl-4 font-mono text-sm text-muted-foreground">
                                    {work.period}
                                </span>
                            )}
                        </div>
                        {work.excerpt ? (
                            <p className="rounded-r-lg border-l-4 border-brand-navy bg-brand-section-bg py-2 pl-4 text-xl leading-relaxed text-foreground/80 text-pretty">
                                {work.excerpt}
                            </p>
                        ) : null}
                        {work.tags?.length ? (
                            <ul aria-label="Work tags" className="mt-8 flex flex-wrap gap-2">
                                {work.tags.map((tag: string) => (
                                    <li key={tag}>
                                        <span className="cursor-default rounded-full bg-muted px-2 py-0.5 text-xs font-medium text-muted-foreground transition-colors hover:text-brand-accent">
                                            #{tag}
                                        </span>
                                    </li>
                                ))}
                            </ul>
                        ) : null}
                    </header>

                    <div id="work-detail-content" className="mt-8">
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

                    {(olderWork || newerWork) && (
                        <nav
                            aria-label="Work navigation"
                            data-testid="work-prev-next"
                            className="mt-12 grid gap-3 border-t border-border/70 pt-8 sm:grid-cols-2"
                        >
                            {olderWork ? (
                                <Link
                                    href={`/works/${olderWork.slug}${relatedPageSuffix}`}
                                    className="group rounded-2xl border border-border/80 bg-background p-4 transition hover:border-primary/30 hover:shadow-sm"
                                >
                                    <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">Previous</p>
                                    <p className="mt-2 text-base font-semibold text-foreground text-balance transition-colors group-hover:text-brand-accent">{olderWork.title}</p>
                                </Link>
                            ) : (
                                <div aria-hidden="true" />
                            )}
                            {newerWork ? (
                                <Link
                                    href={`/works/${newerWork.slug}${relatedPageSuffix}`}
                                    className="group rounded-2xl border border-border/80 bg-background p-4 text-left transition hover:border-primary/30 hover:shadow-sm sm:justify-self-end"
                                >
                                    <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">Next</p>
                                    <p className="mt-2 text-base font-semibold text-foreground text-balance transition-colors group-hover:text-brand-accent">{newerWork.title}</p>
                                </Link>
                            ) : null}
                        </nav>
                    )}

                    <div data-testid="work-related-shell" className="mx-auto mt-16 max-w-3xl border-t pt-12">
                        <RelatedContentList
                            heading="More Works"
                            hrefBase="/works"
                            items={relatedWorks}
                            desktopPageSize={8}
                            tabletPageSize={4}
                            mobilePageSize={2}
                            testIdBase="related-work"
                        />
                    </div>
                </div>

                <aside className="hidden xl:absolute xl:right-0 xl:top-0 xl:block xl:w-64">
                    <TableOfContents contentRootId="work-detail-content" />
                </aside>
            </div>
        </article>
    )
}
