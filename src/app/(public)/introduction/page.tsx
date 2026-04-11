
import { BlockRenderer } from '@/components/content/BlockRenderer'
import { InlineAdminEditorShell } from '@/components/admin/InlineAdminEditorShell'
import { PageEditor } from '@/components/admin/PageEditor'
import { InteractiveRenderer } from '@/components/content/InteractiveRenderer'
import { isBlockPageContent, isHtmlPageContent, parsePageContentJson } from '@/lib/content/page-content'
import { fetchServerSession } from '@/lib/api/server'
import { fetchPublicPageBySlug } from '@/lib/api/pages'

export const revalidate = 60

export default async function IntroductionPage() {
    const page = await fetchPublicPageBySlug('introduction')
    const session = await fetchServerSession()
    const title = page?.title || 'Introduction'
    const parsedContent = parsePageContentJson(page?.contentJson)

    return (
        <div data-testid="static-public-shell" className="container mx-auto max-w-3xl px-4 py-8 md:px-6 md:py-12">
            <header className="mb-8">
                <h1 className="text-3xl font-heading font-bold md:text-4xl text-gray-900 dark:text-gray-50">{title}</h1>
            </header>

            <div className="prose prose-lg max-w-none dark:prose-invert">
                {isHtmlPageContent(parsedContent) ? (
                    <InteractiveRenderer html={parsedContent.html} />
                ) : isBlockPageContent(parsedContent) ? (
                    <BlockRenderer blocks={parsedContent.blocks} />
                ) : (
                    <div className="space-y-4">
                        <p>
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
