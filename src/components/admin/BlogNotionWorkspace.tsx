"use client"

import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { useEffect, useMemo, useRef, useState } from 'react'
import { AIFixDialog } from '@/components/admin/AIFixDialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Checkbox } from '@/components/ui/checkbox'
import { Badge } from '@/components/ui/badge'
import { TiptapEditor } from '@/components/admin/TiptapEditor'
import { fetchWithCsrf } from '@/lib/api/auth'
import { getBrowserApiBaseUrl } from '@/lib/api/browser'
import { toast } from 'sonner'

interface BlogWorkspaceListItem {
    id: string
    title: string
    slug: string
    published: boolean
    publishedAt?: string | null
    updatedAt?: string
    tags?: string[]
}

interface BlogWorkspaceRecord extends BlogWorkspaceListItem {
    excerpt: string
    content: { html: string }
}

interface BlogNotionWorkspaceProps {
    blogs: BlogWorkspaceListItem[]
    activeBlog: BlogWorkspaceRecord
}

function normalizeTagsInput(tags: string) {
    return tags.split(',').map((tag) => tag.trim()).filter(Boolean)
}

function formatTimestamp(value?: string | null) {
    if (!value) {
        return '—'
    }

    return new Date(value).toLocaleString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
    })
}

type SaveState = 'idle' | 'saving' | 'saved' | 'error'

export function BlogNotionWorkspace({ blogs, activeBlog }: BlogNotionWorkspaceProps) {
    const router = useRouter()
    const [title, setTitle] = useState(activeBlog.title)
    const [tagsInput, setTagsInput] = useState(activeBlog.tags?.join(', ') ?? '')
    const [published, setPublished] = useState(activeBlog.published)
    const [html, setHtml] = useState(activeBlog.content.html ?? '')
    const [saveState, setSaveState] = useState<SaveState>('idle')
    const [isSavingMeta, setIsSavingMeta] = useState(false)
    const lastSavedRef = useRef({
        title: activeBlog.title,
        tags: activeBlog.tags ?? [],
        published: activeBlog.published,
        html: activeBlog.content.html ?? '',
    })
    const skipAutosaveRef = useRef(true)

    useEffect(() => {
        setTitle(activeBlog.title)
        setTagsInput(activeBlog.tags?.join(', ') ?? '')
        setPublished(activeBlog.published)
        setHtml(activeBlog.content.html ?? '')
        setSaveState('idle')
        lastSavedRef.current = {
            title: activeBlog.title,
            tags: activeBlog.tags ?? [],
            published: activeBlog.published,
            html: activeBlog.content.html ?? '',
        }
        skipAutosaveRef.current = true
    }, [activeBlog])

    useEffect(() => {
        if (skipAutosaveRef.current) {
            skipAutosaveRef.current = false
            return
        }

        if (html === lastSavedRef.current.html) {
            return
        }

        const controller = new AbortController()
        const timeout = window.setTimeout(async () => {
            setSaveState('saving')

            try {
                const response = await fetchWithCsrf(
                    `${getBrowserApiBaseUrl()}/admin/blogs/${encodeURIComponent(activeBlog.id)}`,
                    {
                        method: 'PUT',
                        headers: {
                            'Content-Type': 'application/json',
                        },
                        body: JSON.stringify({
                            title: lastSavedRef.current.title,
                            tags: lastSavedRef.current.tags,
                            published: lastSavedRef.current.published,
                            contentJson: JSON.stringify({ html }),
                        }),
                        signal: controller.signal,
                    },
                )

                if (!response.ok) {
                    const message = await response.text()
                    throw new Error(message || 'Autosave failed')
                }

                lastSavedRef.current = {
                    ...lastSavedRef.current,
                    html,
                }
                setSaveState('saved')
            } catch (error) {
                if (controller.signal.aborted) {
                    return
                }

                setSaveState('error')
                toast.error(error instanceof Error ? error.message : 'Autosave failed')
            }
        }, 700)

        return () => {
            controller.abort()
            window.clearTimeout(timeout)
        }
    }, [activeBlog.id, html])

    const metaDirty = useMemo(() => (
        title.trim() !== lastSavedRef.current.title
        || published !== lastSavedRef.current.published
        || JSON.stringify(normalizeTagsInput(tagsInput)) !== JSON.stringify(lastSavedRef.current.tags)
    ), [published, tagsInput, title])

    async function saveMetadata() {
        setIsSavingMeta(true)

        try {
            const nextTags = normalizeTagsInput(tagsInput)
            const response = await fetchWithCsrf(
                `${getBrowserApiBaseUrl()}/admin/blogs/${encodeURIComponent(activeBlog.id)}`,
                {
                    method: 'PUT',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({
                        title,
                        tags: nextTags,
                        published,
                        contentJson: JSON.stringify({ html }),
                    }),
                },
            )

            if (!response.ok) {
                const message = await response.text()
                throw new Error(message || 'Failed to save metadata')
            }

            lastSavedRef.current = {
                title: title.trim(),
                tags: nextTags,
                published,
                html,
            }
            setSaveState('saved')
            toast.success('Blog details saved')
        } catch (error) {
            setSaveState('error')
            toast.error(error instanceof Error ? error.message : 'Failed to save metadata')
        } finally {
            setIsSavingMeta(false)
        }
    }

    return (
        <div className="grid min-h-[calc(100vh-12rem)] gap-6 lg:grid-cols-[320px_minmax(0,1fr)]">
            <aside className="overflow-hidden rounded-3xl border border-border/80 bg-background shadow-sm">
                <div className="border-b border-border/80 px-5 py-4">
                    <div className="flex items-center justify-between gap-3">
                        <div>
                            <p className="text-sm font-medium text-gray-900 dark:text-gray-100">Blog library</p>
                            <p className="text-xs text-muted-foreground">Select a document and stage posts for future batch actions.</p>
                        </div>
                    </div>
                </div>
                <div className="max-h-[calc(100vh-16rem)] space-y-2 overflow-y-auto p-3">
                    {blogs.map((blog) => {
                        const isActive = blog.id === activeBlog.id

                        return (
                            <div
                                key={blog.id}
                                className={`rounded-2xl border px-4 py-3 transition ${
                                    isActive
                                        ? 'border-primary/40 bg-primary/5 shadow-sm'
                                        : 'border-transparent hover:border-border hover:bg-muted/40'
                                }`}
                            >
                                <div className="min-w-0 flex-1">
                                    <div className="flex items-start justify-between gap-3">
                                        <Link
                                            href={`/admin/blog/notion?id=${encodeURIComponent(blog.id)}`}
                                            data-testid="notion-blog-list-item"
                                            className="block min-w-0"
                                        >
                                            <p className="line-clamp-2 text-sm font-medium text-gray-900 underline-offset-4 hover:underline dark:text-gray-100">
                                                {blog.title}
                                            </p>
                                        </Link>
                                        <Badge variant="secondary" className={blog.published ? 'bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-300' : 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/40 dark:text-yellow-300'}>
                                            {blog.published ? 'Published' : 'Draft'}
                                        </Badge>
                                    </div>
                                    <p className="mt-2 text-xs text-muted-foreground">
                                        Updated {formatTimestamp(blog.updatedAt ?? blog.publishedAt)}
                                    </p>
                                    {blog.tags?.length ? (
                                        <p className="mt-2 line-clamp-1 text-xs text-muted-foreground">
                                            {blog.tags.join(' · ')}
                                        </p>
                                    ) : null}
                                </div>
                            </div>
                        )
                    })}
                </div>
            </aside>

            <section className="overflow-hidden rounded-3xl border border-border/80 bg-background shadow-sm">
                <div className="border-b border-border/80 px-6 py-5">
                    <div className="flex flex-wrap items-start justify-between gap-4">
                        <div>
                            <h1 className="text-2xl font-semibold tracking-tight text-gray-900 dark:text-gray-50">Blog Notion View</h1>
                            <p className="mt-1 text-sm text-muted-foreground">
                                Content autosaves after a short pause. Title, tags, and publish state stay explicit so high-impact changes remain deliberate.
                            </p>
                        </div>
                        <div className="flex items-center gap-2">
                            <span
                                data-testid="notion-save-state"
                                className={`rounded-full border px-3 py-1 text-xs font-medium ${
                                    saveState === 'saving'
                                        ? 'border-sky-200 bg-sky-50 text-sky-700 dark:border-sky-900 dark:bg-sky-950/30 dark:text-sky-200'
                                        : saveState === 'saved'
                                            ? 'border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-900 dark:bg-emerald-950/30 dark:text-emerald-200'
                                            : saveState === 'error'
                                                ? 'border-red-200 bg-red-50 text-red-700 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200'
                                                : 'border-border text-muted-foreground'
                                }`}
                            >
                                {saveState === 'saving' ? 'Saving...' : saveState === 'saved' ? 'Saved' : saveState === 'error' ? 'Error' : 'Waiting'}
                            </span>
                            <AIFixDialog content={html} onApply={setHtml} />
                            <Link href={`/admin/blog/${activeBlog.id}`}>
                                <Button variant="outline">Open full editor</Button>
                            </Link>
                        </div>
                    </div>
                </div>

                <div className="grid gap-6 px-6 py-6 xl:grid-cols-[minmax(0,1fr)_260px]">
                    <div className="space-y-5">
                        <div className="grid gap-4 md:grid-cols-2">
                            <div className="space-y-2 md:col-span-2">
                                <Label htmlFor="notion-blog-title">Title</Label>
                                <Input
                                    id="notion-blog-title"
                                    value={title}
                                    onChange={(event) => setTitle(event.target.value)}
                                />
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="notion-blog-tags">Tags</Label>
                                <Input
                                    id="notion-blog-tags"
                                    value={tagsInput}
                                    onChange={(event) => setTagsInput(event.target.value)}
                                    placeholder="react, portfolio, notes"
                                />
                            </div>
                            <div className="flex items-end">
                                <div className="flex items-center space-x-2 rounded-2xl border border-border/80 px-4 py-3">
                                    <Checkbox
                                        id="notion-blog-published"
                                        checked={published}
                                        onCheckedChange={(value) => setPublished(Boolean(value))}
                                    />
                                    <Label htmlFor="notion-blog-published" className="cursor-pointer">Published</Label>
                                </div>
                            </div>
                        </div>

                        <div
                            data-testid="tiptap-capability-hint"
                            className="rounded-2xl border border-dashed border-sky-300 bg-sky-50/70 px-4 py-3 text-sm text-sky-900 dark:border-sky-900 dark:bg-sky-950/20 dark:text-sky-100"
                        >
                            Reuse the existing Tiptap stack here: Type <span className="font-medium">/</span> for commands, insert <span className="font-medium">code blocks</span> for snippets, drag/drop or paste images directly into the canvas, and keep HTML / 3D blocks available through the existing toolbar controls.
                        </div>

                        <TiptapEditor
                            content={html}
                            onChange={setHtml}
                            placeholder="Start writing. Content autosaves here while metadata stays explicit."
                        />
                    </div>

                    <aside className="space-y-4 rounded-2xl border border-border/80 bg-muted/20 p-4">
                        <div>
                            <p className="text-sm font-medium text-gray-900 dark:text-gray-100">Document info</p>
                            <dl className="mt-3 space-y-3 text-sm">
                                <div>
                                    <dt className="text-muted-foreground">Published</dt>
                                    <dd>{formatTimestamp(activeBlog.publishedAt)}</dd>
                                </div>
                                <div>
                                    <dt className="text-muted-foreground">Last updated</dt>
                                    <dd>{formatTimestamp(activeBlog.updatedAt)}</dd>
                                </div>
                                <div>
                                    <dt className="text-muted-foreground">Slug</dt>
                                    <dd className="break-all">{activeBlog.slug}</dd>
                                </div>
                            </dl>
                        </div>

                        <Button
                            type="button"
                            onClick={() => void saveMetadata()}
                            disabled={isSavingMeta || !metaDirty || !title.trim()}
                            className="w-full"
                        >
                            {isSavingMeta ? 'Saving post settings...' : 'Save Post Settings'}
                        </Button>
                        <p className="text-xs text-muted-foreground">
                            Members list stays out of this modernization pass for now; this view is intentionally blog-first and content-first.
                        </p>
                    </aside>
                </div>
            </section>
        </div>
    )
}
