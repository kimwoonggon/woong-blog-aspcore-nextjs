"use client"

import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { useEffect, useMemo, useRef, useState } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Checkbox } from '@/components/ui/checkbox'
import { Badge } from '@/components/ui/badge'
import { TiptapEditor } from '@/components/admin/TiptapEditor'
import { fetchWithCsrf } from '@/lib/api/auth'
import { getBrowserApiBaseUrl } from '@/lib/api/browser'
import { fetchAdminAiRuntimeConfigBrowser, type AdminAiRuntimeConfig } from '@/lib/api/admin-ai'
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
    const [selectedBlogIds, setSelectedBlogIds] = useState<string[]>([])
    const [isBatchFixing, setIsBatchFixing] = useState(false)
    const [batchFixSummary, setBatchFixSummary] = useState<string | null>(null)
    const [runtimeConfig, setRuntimeConfig] = useState<AdminAiRuntimeConfig | null>(null)
    const [codexModel, setCodexModel] = useState('gpt-5.4')
    const [codexReasoningEffort, setCodexReasoningEffort] = useState('medium')
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

    useEffect(() => {
        setSelectedBlogIds((current) => {
            if (current.length === 0) {
                return current
            }

            const blogIds = new Set(blogs.map((blog) => blog.id))
            const next = current.filter((blogId) => blogIds.has(blogId))
            return next.length === current.length ? current : next
        })
    }, [blogs])

    useEffect(() => {
        let cancelled = false
        const savedModel = typeof window !== 'undefined' ? window.localStorage.getItem('admin-ai-codex-model') : null
        const savedReasoning = typeof window !== 'undefined' ? window.localStorage.getItem('admin-ai-codex-reasoning') : null

        void fetchAdminAiRuntimeConfigBrowser()
            .then((config: AdminAiRuntimeConfig) => {
                if (cancelled) {
                    return
                }

                setRuntimeConfig(config)
                setCodexModel(savedModel || config.codexModel || 'gpt-5.4')
                setCodexReasoningEffort(savedReasoning || config.codexReasoningEffort || 'medium')
            })
            .catch((error: unknown) => {
                if (!cancelled) {
                    toast.error(error instanceof Error ? error.message : 'Failed to load AI runtime config')
                }
            })

        return () => {
            cancelled = true
        }
    }, [])

    const metaDirty = useMemo(() => (
        title.trim() !== lastSavedRef.current.title
        || published !== lastSavedRef.current.published
        || JSON.stringify(normalizeTagsInput(tagsInput)) !== JSON.stringify(lastSavedRef.current.tags)
    ), [published, tagsInput, title])

    const selectedCount = selectedBlogIds.length
    const selectedBlogTitles = blogs
        .filter((blog) => selectedBlogIds.includes(blog.id))
        .map((blog) => blog.title)

    function toggleSelectedBlog(blogId: string) {
        setSelectedBlogIds((current) => (
            current.includes(blogId)
                ? current.filter((id) => id !== blogId)
                : [...current, blogId]
        ))
    }

    function selectAllBlogs() {
        setSelectedBlogIds(blogs.map((blog) => blog.id))
    }

    function clearSelection() {
        setSelectedBlogIds([])
    }

    async function applyBatchAiFix() {
        if (selectedBlogIds.length === 0) {
            return
        }

        setIsBatchFixing(true)
        setBatchFixSummary(null)

        try {
            const response = await fetchWithCsrf(
                `${getBrowserApiBaseUrl()}/admin/ai/blog-fix-batch`,
                {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({
                        blogIds: selectedBlogIds,
                        all: false,
                        apply: true,
                        codexModel,
                        codexReasoningEffort,
                    }),
                },
            )

            const payload = await response.json()
            if (!response.ok) {
                throw new Error(payload.error || 'Batch AI Fix failed')
            }

            const results = Array.isArray(payload.results) ? payload.results : []
            const fixedCount = results.filter((item: { status?: string }) => item.status === 'fixed').length
            const failedCount = results.filter((item: { status?: string }) => item.status === 'failed').length
            const summary = failedCount > 0
                ? `AI Fix applied to ${fixedCount} posts, ${failedCount} failed.`
                : `AI Fix applied to ${fixedCount} posts.`

            setBatchFixSummary(summary)
            toast.success(summary)
            router.refresh()
        } catch (error) {
            const message = error instanceof Error ? error.message : 'Batch AI Fix failed'
            setBatchFixSummary(message)
            toast.error(message)
        } finally {
            setIsBatchFixing(false)
        }
    }

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
                    <div className="mt-3 flex flex-wrap gap-2">
                        <Button size="sm" variant="outline" type="button" onClick={selectAllBlogs} disabled={blogs.length === 0}>
                            Select all
                        </Button>
                        <Button size="sm" variant="ghost" type="button" onClick={clearSelection} disabled={selectedCount === 0}>
                            Clear selection
                        </Button>
                        <div className="flex flex-wrap items-center gap-2 rounded-md border border-input bg-background px-3 py-2 text-sm">
                            <span className="text-xs text-muted-foreground">Provider</span>
                            <span data-testid="batch-ai-provider" className="font-medium uppercase">{runtimeConfig?.provider ?? 'loading'}</span>
                            {runtimeConfig?.provider === 'codex' ? (
                                <>
                                    <Label htmlFor="batch-codex-model" className="sr-only">Codex model</Label>
                                    <select
                                        id="batch-codex-model"
                                        aria-label="Batch AI codex model"
                                        value={codexModel}
                                        onChange={(event) => {
                                            setCodexModel(event.target.value)
                                            if (typeof window !== 'undefined') {
                                                window.localStorage.setItem('admin-ai-codex-model', event.target.value)
                                            }
                                        }}
                                        className="bg-transparent text-sm outline-none"
                                    >
                                        {(runtimeConfig.allowedCodexModels || []).map((model: string) => (
                                            <option key={model} value={model}>{model}</option>
                                        ))}
                                    </select>
                                    <Label htmlFor="batch-codex-reasoning" className="sr-only">Codex reasoning</Label>
                                    <select
                                        id="batch-codex-reasoning"
                                        aria-label="Batch AI codex reasoning"
                                        value={codexReasoningEffort}
                                        onChange={(event) => {
                                            setCodexReasoningEffort(event.target.value)
                                            if (typeof window !== 'undefined') {
                                                window.localStorage.setItem('admin-ai-codex-reasoning', event.target.value)
                                            }
                                        }}
                                        className="bg-transparent text-sm outline-none"
                                    >
                                        {(runtimeConfig.allowedCodexReasoningEfforts || []).map((effort: string) => (
                                            <option key={effort} value={effort}>{effort}</option>
                                        ))}
                                    </select>
                                </>
                            ) : null}
                        </div>
                        <Button size="sm" type="button" onClick={() => void applyBatchAiFix()} disabled={selectedCount === 0 || isBatchFixing}>
                            {isBatchFixing ? 'Applying AI Fix...' : 'AI Fix selected'}
                        </Button>
                        <Link href="/admin/blog/new">
                            <Button size="sm" type="button">New Post</Button>
                        </Link>
                    </div>
                    <div className="mt-3 rounded-2xl border border-dashed border-border/80 bg-muted/20 px-4 py-3">
                        <p data-testid="batch-selection-count" className="text-sm font-medium text-gray-900 dark:text-gray-100" aria-live="polite">
                            Selected {selectedCount} {selectedCount === 1 ? 'post' : 'posts'}
                        </p>
                        <p data-testid="batch-selection-summary" className="mt-1 text-xs text-muted-foreground">
                            {selectedCount === 0
                                ? 'No posts selected yet. Use the checkboxes to stage a batch later.'
                                : `Ready for future batch actions: ${selectedBlogTitles.join(' · ')}`}
                        </p>
                        {batchFixSummary ? (
                            <p data-testid="batch-ai-status" className="mt-2 text-xs text-muted-foreground">
                                {batchFixSummary}
                            </p>
                        ) : null}
                    </div>
                </div>
                <div className="max-h-[calc(100vh-16rem)] space-y-2 overflow-y-auto p-3">
                    {blogs.map((blog) => {
                        const isActive = blog.id === activeBlog.id
                        const isSelected = selectedBlogIds.includes(blog.id)

                        return (
                            <div
                                key={blog.id}
                                className={`rounded-2xl border px-4 py-3 transition ${
                                    isActive
                                        ? 'border-primary/40 bg-primary/5 shadow-sm'
                                        : isSelected
                                            ? 'border-primary/20 bg-primary/5'
                                        : 'border-transparent hover:border-border hover:bg-muted/40'
                                }`}
                            >
                                <div className="flex items-start gap-3">
                                    <Checkbox
                                        aria-label={`Select ${blog.title}`}
                                        checked={isSelected}
                                        onCheckedChange={() => toggleSelectedBlog(blog.id)}
                                        className="mt-1"
                                    />
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
