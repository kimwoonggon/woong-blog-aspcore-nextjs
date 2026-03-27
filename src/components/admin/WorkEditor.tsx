"use client"

import Image from 'next/image'
import { useState } from 'react'
import { useRouter } from 'next/navigation'
import { AIFixDialog } from '@/components/admin/AIFixDialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Checkbox } from '@/components/ui/checkbox'
import { fetchWithCsrf } from '@/lib/api/auth'
import { toast } from 'sonner'
import { getBrowserApiBaseUrl } from '@/lib/api/browser'

interface Work {
    id?: string
    title?: string
    excerpt?: string
    slug?: string
    category?: string
    tags?: string[]
    published?: boolean
    publishedAt?: string | null
    updatedAt?: string
    content?: { html?: string }
    period?: string | null
    all_properties?: Record<string, unknown>
    thumbnail_asset_id?: string | null
    icon_asset_id?: string | null
    thumbnail_url?: string
    icon_url?: string
}

interface WorkEditorProps {
    initialWork?: Work
    inlineMode?: boolean
}

const DEFAULT_WORK_CATEGORY = 'Uncategorized'

function normalizeTagsInput(tags: string) {
    return tags.split(',').map((tag) => tag.trim()).filter(Boolean)
}

function normalizeJsonInput(value: string) {
    if (!value.trim()) {
        return '{}'
    }

    try {
        return JSON.stringify(JSON.parse(value))
    } catch {
        return value.trim()
    }
}

function buildWorkSnapshot({
    title,
    category,
    period,
    tags,
    published,
    html,
    allProperties,
    thumbnailAssetId,
    iconAssetId,
}: {
    title: string
    category: string
    period: string
    tags: string
    published: boolean
    html: string
    allProperties: string
    thumbnailAssetId: string
    iconAssetId: string
}) {
    return JSON.stringify({
        title: title.trim(),
        category: category.trim(),
        period: period.trim(),
        tags: normalizeTagsInput(tags),
        published,
        html: html.trim(),
        allProperties: normalizeJsonInput(allProperties),
        thumbnailAssetId: thumbnailAssetId.trim(),
        iconAssetId: iconAssetId.trim(),
    })
}

export function WorkEditor({ initialWork, inlineMode = false }: WorkEditorProps) {
    const router = useRouter()
    const isEditing = Boolean(initialWork?.id)
    const defaultPublished = initialWork?.published ?? true

    const [title, setTitle] = useState(initialWork?.title || '')
    const [category, setCategory] = useState(initialWork?.category || DEFAULT_WORK_CATEGORY)
    const [period, setPeriod] = useState(initialWork?.period || '')
    const [tags, setTags] = useState(initialWork?.tags?.join(', ') || '')
    const [published, setPublished] = useState(defaultPublished)
    const [html, setHtml] = useState(initialWork?.content?.html || '')
    const [allProperties, setAllProperties] = useState(
        JSON.stringify(initialWork?.all_properties || {}, null, 2)
    )
    const [thumbnailAssetId, setThumbnailAssetId] = useState(initialWork?.thumbnail_asset_id || '')
    const [thumbnailUrl, setThumbnailUrl] = useState(initialWork?.thumbnail_url || '')
    const [iconAssetId, setIconAssetId] = useState(initialWork?.icon_asset_id || '')
    const [iconUrl, setIconUrl] = useState(initialWork?.icon_url || '')
    const [isSaving, setIsSaving] = useState(false)
    const [uploadingTarget, setUploadingTarget] = useState<'thumbnail' | 'icon' | null>(null)
    const initialSnapshot = buildWorkSnapshot({
        title: initialWork?.title || '',
        category: initialWork?.category || DEFAULT_WORK_CATEGORY,
        period: initialWork?.period || '',
        tags: initialWork?.tags?.join(', ') || '',
        published: Boolean(initialWork?.published),
        html: initialWork?.content?.html || '',
        allProperties: JSON.stringify(initialWork?.all_properties || {}, null, 2),
        thumbnailAssetId: initialWork?.thumbnail_asset_id || '',
        iconAssetId: initialWork?.icon_asset_id || '',
    })
    const currentSnapshot = buildWorkSnapshot({
        title,
        category,
        period,
        tags,
        published,
        html,
        allProperties,
        thumbnailAssetId,
        iconAssetId,
    })
    const isDirty = !isEditing || initialSnapshot !== currentSnapshot

    const formatDate = (dateString?: string | null) => {
        if (!dateString) return 'Not yet'
        return new Date(dateString).toLocaleString('en-US', {
            year: 'numeric',
            month: 'long',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
        })
    }

    async function uploadWorkImage(
        event: React.ChangeEvent<HTMLInputElement>,
        target: 'thumbnail' | 'icon'
    ) {
        const file = event.target.files?.[0]
        if (!file) return

        setUploadingTarget(target)

        const formData = new FormData()
        formData.append('file', file)
        formData.append('bucket', target === 'thumbnail' ? 'work-thumbnails' : 'work-icons')

        try {
            const response = await fetchWithCsrf(`${getBrowserApiBaseUrl()}/uploads`, {
                method: 'POST',
                body: formData,
            })

            const payload = await response.json()
            if (!response.ok) {
                throw new Error(payload.error || 'Upload failed')
            }

            if (target === 'thumbnail') {
                setThumbnailAssetId(payload.id)
                setThumbnailUrl(payload.url)
            } else {
                setIconAssetId(payload.id)
                setIconUrl(payload.url)
            }
        } catch (error) {
            const message = error instanceof Error ? error.message : 'Upload failed'
            toast.error(`Failed to upload ${target}: ${message}`)
        } finally {
            setUploadingTarget(null)
            event.target.value = ''
        }
    }

    function removeWorkImage(target: 'thumbnail' | 'icon') {
        if (target === 'thumbnail') {
            setThumbnailAssetId('')
            setThumbnailUrl('')
            return
        }

        setIconAssetId('')
        setIconUrl('')
    }

    async function saveWork() {
        try {
            if (allProperties) JSON.parse(allProperties)
        } catch {
            toast.error('Invalid JSON in Flexible Metadata field')
            return
        }

        setIsSaving(true)

        const normalizedTags = normalizeTagsInput(tags)
        const payload = {
            title,
            category: category.trim() || DEFAULT_WORK_CATEGORY,
            period,
            tags: normalizedTags,
            published,
            contentJson: JSON.stringify({ html }),
            allPropertiesJson: normalizeJsonInput(allProperties),
            thumbnailAssetId: thumbnailAssetId || null,
            iconAssetId: iconAssetId || null,
        }

        try {
            const apiBaseUrl = getBrowserApiBaseUrl()
            const response = await fetchWithCsrf(
                isEditing && initialWork?.id
                    ? `${apiBaseUrl}/admin/works/${encodeURIComponent(initialWork.id)}`
                    : `${apiBaseUrl}/admin/works`,
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
                toast.error(message || 'Failed to save work.')
                return
            }

            toast.success(isEditing ? 'Work updated successfully' : 'Work created successfully')
            if (!inlineMode) {
                router.push('/admin/works')
            }
            router.refresh()
        } finally {
            setIsSaving(false)
        }
    }

    return (
        <div className="space-y-8 max-w-4xl">
            <div className="grid gap-6 rounded-2xl border border-border/80 bg-card p-6 shadow-sm md:grid-cols-2">
                <div className="space-y-2">
                    <Label htmlFor="title">Title</Label>
                    <Input
                        id="title"
                        name="title"
                        required
                        value={title}
                        onChange={(e) => setTitle(e.target.value)}
                    />
                </div>
                <div className="space-y-2">
                    <Label htmlFor="category">Category</Label>
                    <Input
                        id="category"
                        name="category"
                        value={category}
                        onChange={(e) => setCategory(e.target.value)}
                        placeholder={DEFAULT_WORK_CATEGORY}
                    />
                    <p className="text-xs text-muted-foreground">
                        New works default to <span className="font-medium">{DEFAULT_WORK_CATEGORY}</span> so create is never blocked by categorization.
                    </p>
                </div>
                <div className="space-y-2">
                    <Label htmlFor="period">Project Period</Label>
                    <Input
                        id="period"
                        name="period"
                        value={period}
                        onChange={(e) => setPeriod(e.target.value)}
                        placeholder="YYYY.MM - YYYY.MM"
                    />
                </div>
                <div className="space-y-2">
                    <Label htmlFor="tags">Tags (comma separated)</Label>
                    <Input
                        id="tags"
                        name="tags"
                        value={tags}
                        onChange={(e) => setTags(e.target.value)}
                    />
                </div>
                <div className="space-y-2 md:col-span-2">
                    <Label htmlFor="all_properties">Flexible Metadata (JSON)</Label>
                    <Textarea
                        id="all_properties"
                        name="all_properties"
                        value={allProperties}
                        onChange={(e) => setAllProperties(e.target.value)}
                        placeholder='{"key": "value"}'
                        className="font-mono text-xs min-h-[120px]"
                    />
                </div>
                <div className="space-y-4 md:col-span-2 rounded-2xl border border-border/80 bg-background p-5 dark:border-gray-800">
                    <div>
                        <h3 className="text-lg font-medium">Work Media</h3>
                        <p className="text-sm text-gray-500">
                            Add thumbnail and icon assets with clear click-to-upload fields. Dragging a file onto the input still works, but it is no longer the only obvious path.
                        </p>
                    </div>
                    <div className="grid gap-6 md:grid-cols-2">
                        <div className="space-y-3">
                            <Label htmlFor="work-thumbnail-upload">Thumbnail Image</Label>
                            <div className="relative h-40 overflow-hidden rounded-md border bg-gray-100 dark:border-gray-800 dark:bg-gray-900">
                                {thumbnailUrl ? (
                                    <Image
                                        src={thumbnailUrl}
                                        alt="Work thumbnail preview"
                                        fill
                                        unoptimized
                                        className="object-cover"
                                    />
                                ) : (
                                    <div className="flex h-full items-center justify-center text-sm text-gray-500">
                                        No thumbnail uploaded
                                    </div>
                                )}
                            </div>
                            <div className="rounded-xl border border-dashed border-sky-300 bg-sky-50/70 p-4 dark:border-sky-900 dark:bg-sky-950/20">
                                <p className="mb-2 text-sm font-medium text-sky-900 dark:text-sky-100">Choose a thumbnail image</p>
                                <p className="mb-3 text-xs text-sky-900/80 dark:text-sky-100/80">
                                    Best for public cards. Click to browse, or drop an image onto the picker.
                                </p>
                                <Input
                                    id="work-thumbnail-upload"
                                    type="file"
                                    accept="image/*"
                                    onChange={(event) => void uploadWorkImage(event, 'thumbnail')}
                                    disabled={uploadingTarget !== null}
                                />
                            </div>
                            <div className="flex gap-2">
                                <Button
                                    type="button"
                                    variant="outline"
                                    onClick={() => removeWorkImage('thumbnail')}
                                    disabled={!thumbnailUrl}
                                >
                                    Remove Thumbnail
                                </Button>
                                {uploadingTarget === 'thumbnail' && (
                                    <span className="text-sm text-gray-500">Uploading...</span>
                                )}
                            </div>
                        </div>
                        <div className="space-y-3">
                            <Label htmlFor="work-icon-upload">Icon Image</Label>
                            <div className="relative h-40 overflow-hidden rounded-md border bg-gray-100 dark:border-gray-800 dark:bg-gray-900">
                                {iconUrl ? (
                                    <Image
                                        src={iconUrl}
                                        alt="Work icon preview"
                                        fill
                                        unoptimized
                                        className="object-cover"
                                    />
                                ) : (
                                    <div className="flex h-full items-center justify-center text-sm text-gray-500">
                                        No icon uploaded
                                    </div>
                                )}
                            </div>
                            <div className="rounded-xl border border-dashed border-sky-300 bg-sky-50/70 p-4 dark:border-sky-900 dark:bg-sky-950/20">
                                <p className="mb-2 text-sm font-medium text-sky-900 dark:text-sky-100">Choose an icon image</p>
                                <p className="mb-3 text-xs text-sky-900/80 dark:text-sky-100/80">
                                    Use a square or simple mark for compact surfaces and metadata-driven sections.
                                </p>
                                <Input
                                    id="work-icon-upload"
                                    type="file"
                                    accept="image/*"
                                    onChange={(event) => void uploadWorkImage(event, 'icon')}
                                    disabled={uploadingTarget !== null}
                                />
                            </div>
                            <div className="flex gap-2">
                                <Button
                                    type="button"
                                    variant="outline"
                                    onClick={() => removeWorkImage('icon')}
                                    disabled={!iconUrl}
                                >
                                    Remove Icon
                                </Button>
                                {uploadingTarget === 'icon' && (
                                    <span className="text-sm text-gray-500">Uploading...</span>
                                )}
                            </div>
                        </div>
                    </div>
                </div>
                <div className="flex flex-wrap gap-6 pt-2 md:col-span-2">
                    <div className="space-y-1">
                        <span className="text-xs font-medium text-gray-500 uppercase tracking-wider">Visibility</span>
                        <p className="text-sm text-gray-700 dark:text-gray-300 font-mono">
                            {isEditing ? formatDate(initialWork?.publishedAt) : 'Publishes immediately'}
                        </p>
                    </div>
                    {initialWork?.updatedAt && (
                        <div className="space-y-1">
                            <span className="text-xs font-medium text-gray-500 uppercase tracking-wider">Last Modified</span>
                            <p className="text-sm text-gray-700 dark:text-gray-300 font-mono">
                                {formatDate(initialWork?.updatedAt)}
                            </p>
                        </div>
                    )}
                    {!isEditing && (
                        <div className="rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-900 dark:border-emerald-900/60 dark:bg-emerald-950/30 dark:text-emerald-100 md:ml-auto">
                            New works publish immediately when you save. You can switch them back to draft later from the edit screen.
                        </div>
                    )}
                </div>
            </div>

            <div className="space-y-4 rounded-2xl border border-border/80 bg-card p-6 shadow-sm dark:border-gray-800">
                <div className="flex items-center justify-between mb-2">
                    <h3 className="text-lg font-medium">Content (HTML/Text)</h3>
                    <div className="flex items-center gap-2">
                        <AIFixDialog
                            content={html}
                            onApply={setHtml}
                            apiEndpoint="/api/admin/ai/work-enrich"
                            title="AI Enrich"
                            extraBodyParams={{ title }}
                        />
                        {isEditing && (
                            <div className="flex items-center space-x-2 rounded-full border bg-gray-50 px-3 py-1.5 dark:bg-gray-900">
                                <Checkbox
                                    id="published"
                                    name="published"
                                    checked={published}
                                    onCheckedChange={(value) => setPublished(Boolean(value))}
                                />
                                <Label htmlFor="published" className="text-sm cursor-pointer">Published</Label>
                            </div>
                        )}
                    </div>
                </div>
                <div className="rounded-xl border border-dashed border-sky-300 bg-sky-50/70 px-4 py-3 text-sm text-sky-900 dark:border-sky-900 dark:bg-sky-950/20 dark:text-sky-100">
                    Write the public-facing project story here. New works save live immediately, so keep the summary and body ready before hitting create.
                </div>
                <Textarea
                    id="content"
                    name="content"
                    value={html}
                    onChange={(e) => setHtml(e.target.value)}
                    placeholder="<p>Describe the project...</p>"
                    className="min-h-[280px] font-mono text-sm"
                />
                <p className="text-sm text-gray-500">
                    For now this editor is intentionally simplified so create/publish/readback is stable.
                </p>
            </div>

            <div className="flex flex-col gap-3 border-t pt-8 sm:flex-row sm:items-center sm:justify-end">
                {!isEditing && (
                    <p className="text-sm text-muted-foreground sm:mr-auto">
                        Saving creates a live work immediately, then returns you to the works list so you can keep organizing the library.
                    </p>
                )}
                {!inlineMode && (
                    <Button type="button" variant="outline" onClick={() => router.back()}>
                        Cancel
                    </Button>
                )}
                <Button
                    type="button"
                    onClick={saveWork}
                    disabled={isSaving || !isDirty || !title.trim()}
                    className="bg-[#142850] hover:bg-[#142850]/90 text-white font-medium px-8 transition-all hover:scale-[1.02]"
                >
                    {isSaving ? 'Saving...' : isEditing ? 'Update Work' : 'Create Work'}
                </Button>
            </div>
        </div>
    )
}
