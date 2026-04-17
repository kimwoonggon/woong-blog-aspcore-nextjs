import { BlockRenderer } from '@/components/content/BlockRenderer'
import { InlineAdminEditorShell } from '@/components/admin/InlineAdminEditorShell'
import { PageEditor } from '@/components/admin/PageEditor'
import { InteractiveRenderer } from '@/components/content/InteractiveRenderer'
import { isBlockPageContent, isHtmlPageContent, parsePageContentJson } from '@/lib/content/page-content'
import { fetchServerSession } from '@/lib/api/server'
import { fetchPublicPageBySlug } from '@/lib/api/pages'
import { headers } from 'next/headers'

export const dynamic = 'force-dynamic'

interface PageProps {
    searchParams?: Promise<{ __qaBroken?: string }>
}

export default async function IntroductionPage({ searchParams }: PageProps) {
    const resolvedSearchParams = await searchParams
    const headerStore = await headers()
    const requestHost = (headerStore.get('x-forwarded-host') ?? headerStore.get('host') ?? '').toLowerCase()
    const qaBrokenPage = resolvedSearchParams?.__qaBroken === '1' && /localhost|127\.0\.0\.1/.test(requestHost)

    if (qaBrokenPage) {
        return (
            <div className="container mx-auto flex min-h-[60vh] flex-col gap-6 px-4 py-8 md:px-6 md:py-12">
                <p className="text-sm font-medium uppercase tracking-[0.24em] text-muted-foreground">Public pages</p>
                <h2 className="text-3xl font-semibold text-foreground">This page could not be loaded.</h2>
                <p className="max-w-2xl text-sm text-muted-foreground">
                    The public content request failed. Retry once the backend is healthy.
                </p>
                <button
                    type="button"
                    className="rounded-full bg-foreground px-5 py-2 text-sm font-medium text-background transition-opacity hover:opacity-90"
                >
                    Retry
                </button>
            </div>
        )
    }

    const page = await fetchPublicPageBySlug('introduction')
    const session = await fetchServerSession()
    const title = page?.title || 'Introduction'
    const parsedContent = parsePageContentJson(page?.contentJson)

    return (
        <div data-testid="static-public-shell" className="container mx-auto max-w-3xl px-4 py-7 md:px-6 md:py-10">
            <header className="mb-6">
                <h1 className="text-3xl font-heading font-bold text-foreground md:text-4xl">{title}</h1>
            </header>

            <div className="prose prose-lg max-w-none dark:prose-invert">
                {isHtmlPageContent(parsedContent) ? (
                    <InteractiveRenderer html={parsedContent.html} />
                ) : isBlockPageContent(parsedContent) ? (
                    <BlockRenderer blocks={parsedContent.blocks} />
                ) : (
                    <div className="rounded-[2rem] border border-border/70 bg-background px-5 py-5 shadow-sm md:px-6">
                        <p className="text-sm leading-relaxed text-foreground/80">
                            Hello! I&apos;m Woonggon Kim, IT Technician.
                        </p>
                    </div>
                )}
            </div>

            {session.authenticated && session.role === 'admin' && page && (
                <InlineAdminEditorShell
                    triggerLabel="소개글 수정"
                    title="Introduction Inline Editor"
                    description="현재 페이지를 벗어나지 않고 소개글을 바로 수정합니다."
                >
                    <PageEditor
                        inlineMode
                        page={{
                            id: page.id,
                            title: page.title,
                            slug: page.slug,
                            content: parsedContent ?? { html: '' },
                        }}
                    />
                </InlineAdminEditorShell>
            )}
        </div>
    )
}
