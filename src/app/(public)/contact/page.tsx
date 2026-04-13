import { BlockRenderer } from '@/components/content/BlockRenderer'
import { InlineAdminEditorShell } from '@/components/admin/InlineAdminEditorShell'
import { PageEditor } from '@/components/admin/PageEditor'
import { InteractiveRenderer } from '@/components/content/InteractiveRenderer'
import { isBlockPageContent, isHtmlPageContent, parsePageContentJson } from '@/lib/content/page-content'
import { fetchServerSession } from '@/lib/api/server'
import { fetchPublicPageBySlug } from '@/lib/api/pages'

export const revalidate = 60

export default async function ContactPage() {
    const page = await fetchPublicPageBySlug('contact')
    const session = await fetchServerSession()
    const title = page?.title || 'Contact'
    const parsedContent = parsePageContentJson(page?.contentJson)
    const fallbackEmail = 'woong@example.com'
    const hasMailtoLink = JSON.stringify(parsedContent ?? {}).toLowerCase().includes('mailto:')

    return (
        <div data-testid="static-public-shell" className="container mx-auto max-w-3xl px-4 py-8 md:px-6 md:py-12">
            <header className="mb-8 rounded-[2rem] border border-border/70 bg-background px-5 py-6 shadow-sm md:px-6">
                <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">
                    Get in touch
                </p>
                <h1 className="text-3xl font-heading font-bold text-foreground md:text-4xl">{title}</h1>
                <p className="mt-3 max-w-2xl text-sm leading-relaxed text-muted-foreground">
                    Use this page for a direct introduction, a project inquiry, or a quick follow-up after reading through the work.
                </p>
            </header>

            <div className="prose prose-lg max-w-none dark:prose-invert">
                {isHtmlPageContent(parsedContent) ? (
                    <InteractiveRenderer html={parsedContent.html} />
                ) : isBlockPageContent(parsedContent) ? (
                    <BlockRenderer blocks={parsedContent.blocks} />
                ) : (
                    <div className="rounded-[2rem] border border-border/70 bg-background px-5 py-5 shadow-sm md:px-6">
                        <p className="text-sm leading-relaxed text-foreground/80">
                            You can reach me directly at{' '}
                            <a
                                href="mailto:john@example.com"
                                className="font-medium text-primary underline underline-offset-4 transition-colors hover:text-brand-cyan"
                            >
                                john@example.com
                            </a>
                            .
                        </p>
                    </div>
                )}
            </div>

            {!hasMailtoLink && (
                <div className="mt-8 rounded-[2rem] border border-border/70 bg-muted/30 px-5 py-5 shadow-sm md:px-6">
                    <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">
                        Direct email
                    </p>
                    <p className="mt-2 text-sm font-medium text-foreground">
                        Prefer a plain email thread?
                    </p>
                    <p className="mt-2 text-sm text-muted-foreground">
                        <a
                            href={`mailto:${fallbackEmail}`}
                            className="font-medium text-primary underline underline-offset-4 transition-colors hover:text-brand-cyan"
                        >
                            {fallbackEmail}
                        </a>
                    </p>
                </div>
            )}

            {session.authenticated && session.role === 'admin' && page && (
                <InlineAdminEditorShell
                    triggerLabel="문의글 수정"
                    title="Contact Inline Editor"
                    description="현재 페이지에서 바로 문의 페이지 내용을 수정합니다."
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
