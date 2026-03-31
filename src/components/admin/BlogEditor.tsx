"use client"

import { useState } from 'react'
import { usePathname, useRouter, useSearchParams } from 'next/navigation'
import { AuthoringCapabilityHints } from '@/components/admin/AuthoringCapabilityHints'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Checkbox } from '@/components/ui/checkbox'
import { TiptapEditor } from '@/components/admin/TiptapEditor'
import { AIFixDialog } from '@/components/admin/AIFixDialog'
import { fetchWithCsrf } from '@/lib/api/auth'
import { getBrowserApiBaseUrl } from '@/lib/api/browser'
import { normalizeBlogHtmlForSave } from '@/lib/content/blog-content'
import { toast } from 'sonner'

interface Blog {
    id?: string
    title?: string
    excerpt?: string
    slug?: string
    tags?: string[]
    published?: boolean
    content?: { html?: string }
    publishedAt?: string | null
    updatedAt?: string
}

interface BlogEditorProps {
    initialBlog?: Blog
    inlineMode?: boolean
}

function normalizeTagsInput(tags: string) {
    return tags.split(',').map((tag) => tag.trim()).filter(Boolean)
}

function buildBlogSnapshot({
    title,
    tags,
    published,
    html,
}: {
    title: string
    tags: string
    published: boolean
    html: string
}) {
    return JSON.stringify({
        title: title.trim(),
        tags: normalizeTagsInput(tags),
        published,
        html: html.trim(),
    })
}

export function BlogEditor({ initialBlog, inlineMode = false }: BlogEditorProps) {
    const router = useRouter()
    const pathname = usePathname()
    const searchParams = useSearchParams()
    const isEditing = Boolean(initialBlog?.id)
    const defaultPublished = initialBlog?.published ?? true
    const [title, setTitle] = useState(initialBlog?.title || '')
    const [tagsInput, setTagsInput] = useState(initialBlog?.tags?.join(', ') || '')
    const [published, setPublished] = useState(defaultPublished)
    const [html, setHtml] = useState<string>(initialBlog?.content?.html || '')
    const [isSaving, setIsSaving] = useState(false)

    const initialSnapshot = buildBlogSnapshot({
        title: initialBlog?.title || '',
        tags: initialBlog?.tags?.join(', ') || '',
        published: Boolean(initialBlog?.published),
        html: initialBlog?.content?.html || '',
    })
    const currentSnapshot = buildBlogSnapshot({
        title,
        tags: tagsInput,
        published,
        html,
    })
    const isDirty = !isEditing || initialSnapshot !== currentSnapshot

    const formatDate = (dateString?: string) => {
        if (!dateString) return 'Not yet'
        return new Date(dateString).toLocaleString('en-US', {
            year: 'numeric',
            month: 'long',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        })
    }

    async function saveBlog() {
        const tags = normalizeTagsInput(tagsInput)
        const apiBaseUrl = getBrowserApiBaseUrl()
        const normalizedHtml = normalizeBlogHtmlForSave(html)
        const payload = {
            title,
            tags,
            published,
            contentJson: JSON.stringify({ html: normalizedHtml }),
        }

        setIsSaving(true)

        try {
            const response = await fetchWithCsrf(
                isEditing && initialBlog?.id
                    ? `${apiBaseUrl}/admin/blogs/${encodeURIComponent(initialBlog.id)}`
                    : `${apiBaseUrl}/admin/blogs`,
                {
                    method: isEditing ? 'PUT' : 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify(payload),
                }
            )

            if (!response.ok) {
                const message = await response.text()
                toast.error(message || 'Failed to save blog post.')
                return
            }

            const result = await response.json().catch(() => null) as { id?: string; slug?: string } | null
            const nextSlug = result?.slug ?? initialBlog?.slug ?? null

            toast.success(isEditing ? 'Blog post updated successfully' : 'Blog post created successfully')
            if (normalizedHtml !== html) {
                setHtml(normalizedHtml)
            }

            if (inlineMode) {
                const relatedPage = searchParams.get('relatedPage')
                const relatedPageQuery = relatedPage ? `?relatedPage=${encodeURIComponent(relatedPage)}` : ''

                if (!isEditing && pathname === '/blog' && nextSlug) {
                    window.location.assign(`/blog/${encodeURIComponent(nextSlug)}`)
                    return
                }

                if (isEditing && pathname.startsWith('/blog/')) {
                    if (nextSlug) {
                        window.location.assign(`/blog/${encodeURIComponent(nextSlug)}${relatedPageQuery}`)
                    } else {
                        router.refresh()
                    }
                    return
                }

                router.refresh()
                return
            }

            router.push('/admin/blog')
        } finally {
            setIsSaving(false)
        }
    }

    return (
        <form
            className="space-y-8 max-w-4xl"
            onSubmit={(event) => {
                event.preventDefault()
                void saveBlog()
            }}
        >
            <div className="grid gap-6 rounded-2xl border border-border/80 bg-card p-6 shadow-sm md:grid-cols-2">
                <div className="space-y-2">
                    <Label htmlFor="title">Title</Label>
                    <Input id="title" name="title" required value={title} onChange={(event) => setTitle(event.target.value)} />
                </div>
                <div className="space-y-2">
                    <Label htmlFor="tags">Tags (comma separated)</Label>
                    <Input id="tags" name="tags" value={tagsInput} onChange={(event) => setTagsInput(event.target.value)} />
                </div>

                <div className="flex flex-wrap gap-6 pt-2 md:col-span-2">
                    <div className="space-y-1">
                        <span className="text-xs font-medium text-gray-500 uppercase tracking-wider">Visibility</span>
                        <p className="text-sm text-gray-700 dark:text-gray-300 font-mono">
                            {isEditing ? formatDate(initialBlog?.publishedAt ?? undefined) : 'Publishes immediately'}
                        </p>
                    </div>
                    {initialBlog?.updatedAt && (
                        <div className="space-y-1">
                            <span className="text-xs font-medium text-gray-500 uppercase tracking-wider">Last Modified</span>
                            <p className="text-sm text-gray-700 dark:text-gray-300 font-mono">
                                {formatDate(initialBlog.updatedAt)}
                            </p>
                        </div>
                    )}
                    {!isEditing && (
                        <div className="rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-900 dark:border-emerald-900/60 dark:bg-emerald-950/30 dark:text-emerald-100 md:ml-auto">
                            New posts publish immediately when you save. You can switch them back to draft later from the edit screen.
                        </div>
                    )}
                </div>
            </div>

            <div className="space-y-4 rounded-2xl border border-border/80 bg-card p-6 shadow-sm">
                <div className="mb-2 flex items-center justify-between">
                    <h3 className="text-lg font-medium">Content</h3>
                    <div className="flex items-center gap-2">
                        <AIFixDialog content={html} onApply={setHtml} />
                        {isEditing && (
                            <>
                                <div className="mx-1 h-6 w-px bg-gray-200 dark:bg-gray-800" />
                                <div className="flex items-center space-x-2 rounded-full border bg-gray-50 px-3 py-1.5 dark:bg-gray-900">
                                    <Checkbox id="published" name="published" checked={published} onCheckedChange={(value) => setPublished(Boolean(value))} />
                                    <Label htmlFor="published" className="cursor-pointer text-sm">Published</Label>
                                </div>
                            </>
                        )}
                    </div>
                </div>
                <AuthoringCapabilityHints />
                <TiptapEditor content={html} onChange={setHtml} />
            </div>

            <div className="flex flex-col gap-3 border-t pt-8 sm:flex-row sm:items-center sm:justify-end">
                {!isEditing && (
                    <p className="text-sm text-muted-foreground sm:mr-auto">
                        Saving creates a live post immediately, then returns you to the blog list so you can keep editing the library.
                    </p>
                )}
                {!inlineMode && (
                    <Button type="button" variant="outline" onClick={() => router.back()}>Cancel</Button>
                )}
                <Button
                    type="submit"
                    disabled={isSaving || !isDirty || !title.trim()}
                    className="bg-[#142850] px-8 font-medium text-white transition-all hover:scale-[1.02] hover:bg-[#142850]/90"
                >
                    {isSaving ? 'Saving...' : isEditing ? 'Update Post' : 'Create Post'}
                </Button>
            </div>
        </form>
    )
}
