
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

    return (
        <div className="container mx-auto max-w-3xl px-4 py-8 md:px-6 md:py-12">
            <header className="mb-8">
                <h1 className="text-3xl font-heading font-bold md:text-4xl text-gray-900 dark:text-gray-50">{title}</h1>
            </header>

            <div className="prose prose-lg max-w-none dark:prose-invert">
                {isHtmlPageContent(parsedContent) ? (
                    <InteractiveRenderer html={parsedContent.html} />
                ) : isBlockPageContent(parsedContent) ? (
                    <BlockRenderer blocks={parsedContent.blocks} />
                ) : (
                    <p>
                        You can reach me at: <a href="mailto:john@example.com" className="text-blue-600 hover:underline dark:text-blue-400">john@example.com</a>
                    </p>
                )}
            </div>

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
