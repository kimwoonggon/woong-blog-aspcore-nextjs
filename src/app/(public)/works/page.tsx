
import Link from 'next/link'
import Image from 'next/image'
import { BriefcaseBusiness } from 'lucide-react'
import { PublicWorksInlineCreateShell } from '@/components/admin/PublicWorksInlineCreateShell'
import { PublicAdminLink } from '@/components/admin/PublicAdminLink'
import { EdgePaginationNav } from '@/components/layout/EdgePaginationNav'
import { PublicPagination } from '@/components/layout/PublicPagination'
import { ResponsivePageSizeSync } from '@/components/layout/ResponsivePageSizeSync'
import { Badge } from '@/components/ui/badge'
import { headers } from 'next/headers'
import { fetchServerSession } from '@/lib/api/server'
import { fetchPublicWorks } from '@/lib/api/works'

export const dynamic = 'force-dynamic'

interface PageProps {
    searchParams?: Promise<{ page?: string; pageSize?: string; __qaEmpty?: string; __qaNoImage?: string }>
}

const DESKTOP_PAGE_SIZE = 8
const TABLET_PAGE_SIZE = 4
const MOBILE_PAGE_SIZE = 2

function formatPublishedMonth(publishedAt?: string | null) {
    return publishedAt
        ? new Date(publishedAt).toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'short',
        })
        : 'Unknown Date'
}

export default async function WorksPage({ searchParams }: PageProps) {
    const resolvedSearchParams = await searchParams
    const headerStore = await headers()
    const requestHost = (headerStore.get('x-forwarded-host') ?? headerStore.get('host') ?? '').toLowerCase()
    const qaEmptyWorks = resolvedSearchParams?.__qaEmpty === '1' && /localhost|127\.0\.0\.1/.test(requestHost)
    const qaNoImageWorks = resolvedSearchParams?.__qaNoImage === '1' && /localhost|127\.0\.0\.1/.test(requestHost)
    const currentPage = Math.max(1, Number.parseInt(resolvedSearchParams?.page ?? '1', 10) || 1)
    const currentPageSize = Math.max(1, Number.parseInt(resolvedSearchParams?.pageSize ?? String(DESKTOP_PAGE_SIZE), 10) || DESKTOP_PAGE_SIZE)
    const worksPayload = qaEmptyWorks
        ? { items: [], page: 1, pageSize: currentPageSize, totalItems: 0, totalPages: 1 }
        : await fetchPublicWorks(currentPage, currentPageSize)
    const session = await fetchServerSession()
    const totalPages = Math.max(1, worksPayload.totalPages)
    const page = worksPayload.page
    const pagedWorks = qaNoImageWorks
        ? worksPayload.items.map((work) => ({
            ...work,
            thumbnailUrl: null,
        }))
        : worksPayload.items
    const returnTo = encodeURIComponent(`/works?page=${page}&pageSize=${currentPageSize}`)

    return (
        <div className="container mx-auto max-w-7xl px-4 py-8 md:px-6 md:py-12">
            <EdgePaginationNav
                pathname="/works"
                currentPage={page}
                totalPages={totalPages}
                pageSize={currentPageSize}
            />
            <ResponsivePageSizeSync
                desktopPageSize={DESKTOP_PAGE_SIZE}
                tabletPageSize={TABLET_PAGE_SIZE}
                mobilePageSize={MOBILE_PAGE_SIZE}
            />
            <div className="mb-8 flex flex-col gap-4 rounded-[2rem] border border-border/70 bg-brand-section-bg px-5 py-6 md:px-6">
                <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
                    <div>
                        <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">Project archive</p>
                        <h1 className="mt-2 text-3xl font-heading font-bold text-foreground md:text-4xl">Works</h1>
                        <p className="mt-3 max-w-2xl text-sm leading-relaxed text-muted-foreground">
                            A curated archive of shipped interfaces, experiments, and platform work. Each card is meant to read quickly and still invite a deeper dive.
                        </p>
                    </div>
                    <div className="flex flex-wrap gap-2">
                        <Link
                            href="/contact"
                            className="inline-flex min-h-10 items-center rounded-full border border-border bg-background px-4 py-2 text-sm font-medium text-foreground transition-colors hover:bg-muted"
                        >
                            Start a conversation
                        </Link>
                        <Link
                            href="/blog"
                            className="inline-flex min-h-10 items-center rounded-full px-4 py-2 text-sm font-medium text-brand-cyan transition-colors hover:text-brand-cyan hover:underline"
                        >
                            Read the notes
                        </Link>
                    </div>
                </div>
                <div className="flex flex-wrap gap-2">
                    <PublicAdminLink href="/admin/works" label="작업 관리" variant="manage" />
                </div>
            </div>
            {session.authenticated && session.role === 'admin' && (
                <PublicWorksInlineCreateShell />
            )}
            <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-4">
                {pagedWorks && pagedWorks.length > 0 ? (
                    pagedWorks.map((work) => {
                        const thumbnailUrl = work.thumbnailUrl ?? null
                        const publishDate = formatPublishedMonth(work.publishedAt)

                        return (
                            <Link
                                key={work.id}
                                href={`/works/${work.slug}?returnTo=${returnTo}&relatedPage=${page}`}
                                className="group/card block h-full"
                                data-testid="work-card"
                            >
                                <article className="responsive-feed-card works-feed-card flex h-full flex-col overflow-hidden rounded-2xl border border-border/80 bg-background shadow-sm transition hover:border-primary/30 hover:shadow-md">
                                    <div className="relative aspect-[4/3] overflow-hidden bg-muted">
                                        {thumbnailUrl ? (
                                            <Image
                                                src={thumbnailUrl}
                                                alt={work.title}
                                                fill
                                                unoptimized
                                                sizes="(max-width: 768px) 100vw, (max-width: 1200px) 50vw, 25vw"
                                                className="responsive-feed-image object-cover transition-transform duration-500 group-hover/card:scale-105"
                                            />
                                        ) : (
                                            <div
                                                data-testid="work-card-no-image-placeholder"
                                                className="flex h-full w-full flex-col items-center justify-center gap-2 bg-gradient-to-br from-muted to-muted/80 text-muted-foreground"
                                            >
                                                <BriefcaseBusiness className="h-8 w-8" aria-hidden="true" />
                                                <span className="text-xs font-medium">No Image</span>
                                            </div>
                                        )}
                                    </div>
                                    <div className="flex flex-1 flex-col p-4 sm:p-5">
                                        <div className="mb-3 flex flex-wrap items-center gap-2">
                                            <Badge variant="secondary" className="rounded-full bg-brand-navy px-2.5 py-0.5 text-xs text-white hover:bg-brand-navy/90">
                                                {publishDate}
                                            </Badge>
                                            <span className="responsive-feed-copy text-xs font-medium uppercase tracking-wide text-muted-foreground">
                                                {work.category}
                                            </span>
                                        </div>
                                        <h2 className="responsive-feed-title works-feed-title line-clamp-2 text-lg font-heading font-bold leading-tight text-foreground transition-colors group-hover/card:text-brand-accent sm:text-xl">
                                            {work.title}
                                        </h2>
                                        <p className="responsive-feed-copy works-feed-excerpt mt-2 line-clamp-3 flex-1 text-sm leading-relaxed text-foreground/80">
                                            {work.excerpt}
                                        </p>
                                        <div className="works-feed-tags mt-4 flex flex-wrap content-start gap-1.5 overflow-hidden">
                                            {work.tags?.slice(0, 3).map((tag: string) => (
                                                <span key={tag} className="rounded-full bg-muted px-2 py-0.5 text-xs font-medium text-muted-foreground">
                                                    {tag}
                                                </span>
                                            ))}
                                        </div>
                                    </div>
                                </article>
                            </Link>
                        )
                    })
                ) : (
                    <div className="col-span-full rounded-[2rem] border border-dashed border-border/80 bg-muted/30 px-6 py-20 text-center text-muted-foreground">
                        No works found.
                    </div>
                )}
            </div>
            <div className="mt-6 rounded-2xl border border-border/70 bg-background/80 p-4 shadow-sm">
                <PublicPagination
                    pathname="/works"
                    currentPage={page}
                    totalPages={totalPages}
                    pageSize={currentPageSize}
                    ariaLabel="Works pagination"
                />
            </div>
            <section className="mt-8 rounded-[2rem] border border-border/70 bg-background px-5 py-6 shadow-sm md:px-6">
                <div className="flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
                    <div>
                        <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">Keep exploring</p>
                        <h2 className="mt-2 text-xl font-heading font-bold text-foreground">Use the portfolio like a guided path</h2>
                    </div>
                    <p className="max-w-2xl text-sm leading-relaxed text-muted-foreground">
                        If you want more context than a card can hold, move into the writing or reach out directly.
                    </p>
                </div>
                <div className="mt-5 grid gap-3 md:grid-cols-3">
                    {[
                        {
                            href: '/blog',
                            label: 'Blog',
                            description: 'Read the process notes behind specific product and engineering decisions.',
                        },
                        {
                            href: '/introduction',
                            label: 'Introduction',
                            description: 'Get the personal framing behind the type of work I choose to do.',
                        },
                        {
                            href: '/contact',
                            label: 'Contact',
                            description: 'Open a conversation if a project here overlaps with what you are building.',
                        },
                    ].map(({ href, label, description }) => (
                        <Link
                            key={href}
                            href={href}
                            className="group rounded-2xl border border-border/80 bg-background p-4 transition hover:border-primary/30 hover:shadow-sm"
                        >
                            <p className="text-base font-semibold text-foreground transition-colors group-hover:text-brand-accent">
                                {label}
                            </p>
                            <p className="mt-1 text-sm leading-relaxed text-muted-foreground">
                                {description}
                            </p>
                        </Link>
                    ))}
                </div>
            </section>
        </div>
    )
}
