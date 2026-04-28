import { notFound } from 'next/navigation'
import { Badge } from '@/components/ui/badge'
import { PublicWorkDetailAdminActions } from '@/components/admin/PublicWorkDetailAdminActions'
import { PublicDetailAdjacentLink } from '@/components/content/PublicDetailAdjacentLink'
import { RelatedContentList } from '@/components/content/RelatedContentList'
import { InteractiveRenderer } from '@/components/content/InteractiveRenderer'
import { WorkTableOfContentsRail } from '@/components/content/WorkTableOfContentsRail'
import { WorkVideoPlayer } from '@/components/content/WorkVideoPlayer'
import { Metadata } from 'next'
import { Suspense } from 'react'
import { fetchAllPublicWorks, fetchPublicWorkBySlug } from '@/lib/api/works'
import { hasWorkVideoEmbeds } from '@/lib/content/work-video-embeds'
import { buildWorkDetailMetadata } from './work-detail-metadata'
import { formatDetailPublishDate, parseWorkContentHtml } from './work-detail-helpers'

export const revalidate = 60

interface PageProps {
    params: Promise<{ slug: string }>
}

function safeDecodeSlug(slug: string) {
    try {
        return decodeURIComponent(slug)
    } catch {
        return null
    }
}

function normalizeStaticParamSlug(slug: unknown) {
    if (typeof slug !== 'string') {
        return null
    }

    const cleanedSlug = slug.trim()
    if (!cleanedSlug || cleanedSlug.includes('/') || cleanedSlug.includes('?') || cleanedSlug.includes('#')) {
        return null
    }

    return cleanedSlug
}

export async function generateStaticParams() {
    const works = await fetchAllPublicWorks().catch(() => [])
    return works.flatMap((work) => {
        const slug = normalizeStaticParamSlug(work.slug)
        return slug ? [{ slug }] : []
    })
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
    const { slug } = await params
    const decodedSlug = safeDecodeSlug(slug)
    if (!decodedSlug) return {}

    const work = await fetchPublicWorkBySlug(decodedSlug).catch(() => null)

    if (!work) return {}

    return buildWorkDetailMetadata(work)
}

export default async function WorkDetailPage({ params }: PageProps) {
    const { slug } = await params
    const decodedSlug = decodeURIComponent(slug)
    const [work, allWorks] = await Promise.all([
        fetchPublicWorkBySlug(decodedSlug),
        fetchAllPublicWorks(),
    ])

    if (!work) {
        notFound()
    }

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
    return (
        <article className="mx-auto w-full px-4 py-8 md:px-6 md:py-12">
            <div id="work-detail-toc-start" aria-hidden="true" className="h-0" />
            <div data-testid="work-article-content-layout" className="mx-auto xl:grid xl:grid-cols-[minmax(0,1fr)_minmax(0,48rem)_minmax(0,1fr)] xl:items-start xl:gap-12">
                <div data-testid="work-detail-body" className="mx-auto min-w-0 w-full max-w-3xl xl:col-start-2">
                    <header className="mb-8">
                        <h1 className="mb-4 text-3xl font-heading font-bold leading-tight text-foreground text-balance md:text-4xl">
                            <span data-testid="work-detail-title">{work.title}</span>
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

                    <PublicWorkDetailAdminActions workId={work.id} />

                    <div id="work-detail-content" className="mt-8">
                        {orderedVideos.length > 0 && !hasInlineVideoEmbeds && (
                            <div className="mb-8 space-y-4">
                                <div data-testid="work-lead-video">
                                    <WorkVideoPlayer video={orderedVideos[0]} allowDesktopResize />
                                </div>
                                {orderedVideos.length > 1 && (
                                    <details data-testid="work-more-videos" className="rounded-2xl border border-border/80 p-4">
                                        <summary className="cursor-pointer text-sm font-medium text-muted-foreground">
                                            More videos ({orderedVideos.length - 1})
                                        </summary>
                                        <div className="mt-4 space-y-4">
                                            {orderedVideos.slice(1).map((video) => (
                                                <WorkVideoPlayer key={video.id} video={video} allowDesktopResize />
                                            ))}
                                        </div>
                                    </details>
                                )}
                            </div>
                        )}
                        {contentHtml && (
                            <InteractiveRenderer
                                html={contentHtml}
                                workVideos={orderedVideos}
                                enableWorksDetailUploadedVideoPresentation
                            />
                        )}
                    </div>

                </div>

                <aside className="hidden xl:sticky xl:top-28 xl:col-start-3 xl:block xl:w-full xl:max-w-80 xl:justify-self-start xl:self-start xl:pl-10">
                    <WorkTableOfContentsRail
                        contentRootId="work-detail-content"
                        title="On This Work"
                        rangeStartId="work-detail-toc-start"
                        rangeEndId="work-detail-toc-end"
                    />
                </aside>
            </div>

            <div id="work-detail-toc-end" aria-hidden="true" className="h-0" />
            <div className="mx-auto max-w-3xl">
                {(olderWork || newerWork) && (
                    <Suspense fallback={null}>
                        <nav
                            aria-label="Work navigation"
                            data-testid="work-prev-next"
                            className="mt-12 grid gap-3 border-t border-border/70 pt-8 sm:grid-cols-2"
                        >
                            {newerWork ? (
                                <PublicDetailAdjacentLink hrefBase="/works" slug={newerWork.slug} label="Next" title={newerWork.title} />
                            ) : (
                                <div aria-hidden="true" />
                            )}
                            {olderWork ? (
                                <PublicDetailAdjacentLink hrefBase="/works" slug={olderWork.slug} label="Previous" title={olderWork.title} alignEnd />
                            ) : null}
                        </nav>
                    </Suspense>
                )}

                <div data-testid="work-related-shell" className="mt-16 border-t pt-12">
                    <Suspense fallback={null}>
                        <RelatedContentList
                            heading="More Works"
                            hrefBase="/works"
                            items={sortedWorks}
                            currentItemId={work.id}
                            desktopPageSize={9}
                            tabletPageSize={4}
                            mobilePageSize={2}
                            testIdBase="related-work"
                        />
                    </Suspense>
                </div>
            </div>
        </article>
    )
}
