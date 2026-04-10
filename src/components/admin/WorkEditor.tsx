"use client"

import Image from 'next/image'
import { useState } from 'react'
import { useRouter, useSearchParams } from 'next/navigation'
import { AIFixDialog } from '@/components/admin/AIFixDialog'
import { WorkVideoPlayer } from '@/components/content/WorkVideoPlayer'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Checkbox } from '@/components/ui/checkbox'
import { fetchWithCsrf } from '@/lib/api/auth'
import { getBrowserApiBaseUrl } from '@/lib/api/browser'
import type { WorkVideo } from '@/lib/api/works'
import { toast } from 'sonner'

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
    videos_version?: number
    videos?: WorkVideo[]
}

interface WorkEditorProps {
    initialWork?: Work
    inlineMode?: boolean
}

interface VideoDraft {
    tempId: string
    kind: 'youtube' | 'file'
    label: string
    youtubeUrl?: string
    file?: File
}

interface VideoMutationPayload {
    videos_version?: number
    videosVersion?: number
    videos?: WorkVideo[]
}

interface UploadTargetPayload {
    uploadSessionId: string
    uploadMethod: 'PUT' | 'POST'
    uploadUrl: string
    storageKey: string
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

async function getResponseError(response: Response, fallback: string) {
    const contentType = response.headers.get('content-type') ?? ''
    if (contentType.includes('application/json')) {
        const payload = await response.json().catch(() => null) as { error?: string } | null
        if (payload?.error) {
            return payload.error
        }
    }

    const text = await response.text().catch(() => '')
    return text || fallback
}

export function WorkEditor({ initialWork, inlineMode = false }: WorkEditorProps) {
    const router = useRouter()
    const searchParams = useSearchParams()
    const isEditing = Boolean(initialWork?.id)
    const defaultPublished = initialWork?.published ?? true
    const requestedReturnTo = searchParams.get('returnTo')
    const returnTo = requestedReturnTo && requestedReturnTo.startsWith('/')
        ? requestedReturnTo
        : '/admin/works'

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
    const [videosVersion, setVideosVersion] = useState(initialWork?.videos_version || 0)
    const [videos, setVideos] = useState<WorkVideo[]>(initialWork?.videos || [])
    const [stagedVideos, setStagedVideos] = useState<VideoDraft[]>([])
    const [youtubeUrlInput, setYoutubeUrlInput] = useState('')
    const [isSaving, setIsSaving] = useState(false)
    const [uploadingTarget, setUploadingTarget] = useState<'thumbnail' | 'icon' | null>(null)
    const [isVideoBusy, setIsVideoBusy] = useState(false)

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

    const syncVideos = (payload: VideoMutationPayload) => {
        const nextVersion = typeof payload.videos_version === 'number'
            ? payload.videos_version
            : typeof payload.videosVersion === 'number'
                ? payload.videosVersion
                : videosVersion
        const nextVideos = Array.isArray(payload.videos) ? payload.videos : videos

        setVideosVersion(nextVersion)
        setVideos(nextVideos)
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

    function addYouTubeDraft() {
        const trimmed = youtubeUrlInput.trim()
        if (!trimmed) {
            toast.error('Paste a YouTube URL or video ID first.')
            return
        }

        if (isEditing) {
            void addYouTubeForExistingWork(trimmed)
            return
        }

        setStagedVideos((current) => [...current, {
            tempId: crypto.randomUUID(),
            kind: 'youtube',
            label: trimmed,
            youtubeUrl: trimmed,
        }])
        setYoutubeUrlInput('')
    }

    function handleStageVideoFile(event: React.ChangeEvent<HTMLInputElement>) {
        const file = event.target.files?.[0]
        if (!file) return

        if (isEditing) {
            void uploadVideoForExistingWork(file)
            event.target.value = ''
            return
        }

        setStagedVideos((current) => [...current, {
            tempId: crypto.randomUUID(),
            kind: 'file',
            label: file.name,
            file,
        }])
        event.target.value = ''
    }

    function moveStagedVideo(tempId: string, direction: -1 | 1) {
        setStagedVideos((current) => {
            const index = current.findIndex((item) => item.tempId === tempId)
            if (index < 0) return current

            const nextIndex = index + direction
            if (nextIndex < 0 || nextIndex >= current.length) return current

            const next = [...current]
            const [item] = next.splice(index, 1)
            next.splice(nextIndex, 0, item)
            return next
        })
    }

    function removeStagedVideo(tempId: string) {
        setStagedVideos((current) => current.filter((item) => item.tempId !== tempId))
    }

    async function addYouTubeForExistingWork(youtubeUrlOrId: string) {
        if (!initialWork?.id) return

        setIsVideoBusy(true)

        try {
            const response = await fetchWithCsrf(`${getBrowserApiBaseUrl()}/admin/works/${encodeURIComponent(initialWork.id)}/videos/youtube`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    youtubeUrlOrId,
                    expectedVideosVersion: videosVersion,
                }),
            })

            if (!response.ok) {
                throw new Error(await getResponseError(response, 'Failed to add YouTube video.'))
            }

            syncVideos(await response.json() as VideoMutationPayload)
            setYoutubeUrlInput('')
            toast.success('YouTube video added.')
        } catch (error) {
            toast.error(error instanceof Error ? error.message : 'Failed to add YouTube video.')
        } finally {
            setIsVideoBusy(false)
        }
    }

    async function requestUploadTarget(workId: string, file: File, expectedVersion: number) {
        const response = await fetchWithCsrf(`${getBrowserApiBaseUrl()}/admin/works/${encodeURIComponent(workId)}/videos/upload-url`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                fileName: file.name,
                contentType: file.type,
                size: file.size,
                expectedVideosVersion: expectedVersion,
            }),
        })

        if (!response.ok) {
            throw new Error(await getResponseError(response, 'Failed to prepare a video upload.'))
        }

        return await response.json() as UploadTargetPayload
    }

    async function uploadToTarget(workId: string, file: File, target: UploadTargetPayload) {
        if (target.uploadMethod === 'PUT') {
            let response: Response
            try {
                response = await fetch(target.uploadUrl, {
                    method: 'PUT',
                    headers: {
                        'Content-Type': file.type,
                    },
                    body: file,
                })
            } catch {
                throw new Error('Browser upload to Cloudflare R2 failed. Check bucket CORS for Origin, PUT, and Content-Type.')
            }

            if (!response.ok) {
                throw new Error(await getResponseError(response, 'Failed to upload the video file.'))
            }

            return
        }

        const formData = new FormData()
        formData.append('file', file)

        const response = await fetchWithCsrf(`${getBrowserApiBaseUrl()}/admin/works/${encodeURIComponent(workId)}/videos/upload?uploadSessionId=${encodeURIComponent(target.uploadSessionId)}`, {
            method: 'POST',
            body: formData,
        })

        if (!response.ok) {
            throw new Error(await getResponseError(response, 'Failed to upload the video file.'))
        }
    }

    async function confirmVideoUpload(workId: string, uploadSessionId: string, expectedVersion: number) {
        const response = await fetchWithCsrf(`${getBrowserApiBaseUrl()}/admin/works/${encodeURIComponent(workId)}/videos/confirm`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                uploadSessionId,
                expectedVideosVersion: expectedVersion,
            }),
        })

        if (!response.ok) {
            throw new Error(await getResponseError(response, 'Failed to confirm the video upload.'))
        }

        return await response.json() as VideoMutationPayload
    }

    async function uploadVideoForExistingWork(file: File) {
        if (!initialWork?.id) return

        setIsVideoBusy(true)

        try {
            const target = await requestUploadTarget(initialWork.id, file, videosVersion)
            await uploadToTarget(initialWork.id, file, target)
            const payload = await confirmVideoUpload(initialWork.id, target.uploadSessionId, videosVersion)
            syncVideos(payload)
            toast.success('Video uploaded.')
        } catch (error) {
            toast.error(error instanceof Error ? error.message : 'Failed to upload video.')
        } finally {
            setIsVideoBusy(false)
        }
    }

    async function removeSavedVideo(videoId: string) {
        if (!initialWork?.id) return

        setIsVideoBusy(true)

        try {
            const response = await fetchWithCsrf(`${getBrowserApiBaseUrl()}/admin/works/${encodeURIComponent(initialWork.id)}/videos/${encodeURIComponent(videoId)}?expectedVideosVersion=${videosVersion}`, {
                method: 'DELETE',
            })

            if (!response.ok) {
                throw new Error(await getResponseError(response, 'Failed to remove video.'))
            }

            syncVideos(await response.json() as VideoMutationPayload)
            toast.success('Video removed.')
        } catch (error) {
            toast.error(error instanceof Error ? error.message : 'Failed to remove video.')
        } finally {
            setIsVideoBusy(false)
        }
    }

    async function reorderSavedVideo(videoId: string, direction: -1 | 1) {
        if (!initialWork?.id) return

        const index = videos.findIndex((video) => video.id === videoId)
        const nextIndex = index + direction
        if (index < 0 || nextIndex < 0 || nextIndex >= videos.length) {
            return
        }

        const reordered = [...videos]
        const [item] = reordered.splice(index, 1)
        reordered.splice(nextIndex, 0, item)

        setIsVideoBusy(true)

        try {
            const response = await fetchWithCsrf(`${getBrowserApiBaseUrl()}/admin/works/${encodeURIComponent(initialWork.id)}/videos/order`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    orderedVideoIds: reordered.map((video) => video.id),
                    expectedVideosVersion: videosVersion,
                }),
            })

            if (!response.ok) {
                throw new Error(await getResponseError(response, 'Failed to reorder videos.'))
            }

            syncVideos(await response.json() as VideoMutationPayload)
        } catch (error) {
            toast.error(error instanceof Error ? error.message : 'Failed to reorder videos.')
        } finally {
            setIsVideoBusy(false)
        }
    }

    async function processStagedVideos(workId: string) {
        let currentVersion = 0

        for (const draft of stagedVideos) {
            if (draft.kind === 'youtube' && draft.youtubeUrl) {
                const response = await fetchWithCsrf(`${getBrowserApiBaseUrl()}/admin/works/${encodeURIComponent(workId)}/videos/youtube`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({
                        youtubeUrlOrId: draft.youtubeUrl,
                        expectedVideosVersion: currentVersion,
                    }),
                })

                if (!response.ok) {
                    throw new Error(await getResponseError(response, `Failed to add YouTube video: ${draft.label}`))
                }

                const payload = await response.json() as VideoMutationPayload
                currentVersion = typeof payload.videos_version === 'number' ? payload.videos_version : currentVersion + 1
                continue
            }

            if (draft.kind === 'file' && draft.file) {
                const target = await requestUploadTarget(workId, draft.file, currentVersion)
                await uploadToTarget(workId, draft.file, target)
                const payload = await confirmVideoUpload(workId, target.uploadSessionId, currentVersion)
                currentVersion = typeof payload.videos_version === 'number' ? payload.videos_version : currentVersion + 1
            }
        }
    }

    async function saveWork(mode: 'default' | 'with-videos' = 'default') {
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
                toast.error(await getResponseError(response, 'Failed to save work.'))
                return
            }

            const responsePayload = await response.json().catch(() => null) as { id?: string } | null

            if (isEditing) {
                toast.success('Work updated successfully')
                if (inlineMode) {
                    router.refresh()
                    return
                }

                router.push(returnTo)
                return
            }

            if (mode === 'with-videos' && stagedVideos.length > 0 && responsePayload?.id) {
                try {
                    await processStagedVideos(responsePayload.id)
                    toast.success('Work and videos created successfully')
                    router.push(returnTo)
                    return
                } catch (error) {
                    toast.error(error instanceof Error ? error.message : 'Work was created, but some videos failed to attach.')
                    router.push(`/admin/works/${responsePayload.id}`)
                    return
                }
            }

            toast.success('Work created successfully')
            router.push(returnTo)
        } finally {
            setIsSaving(false)
        }
    }

    const hasStagedVideos = stagedVideos.length > 0

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

                <div className="space-y-4 md:col-span-2 rounded-2xl border border-border/80 bg-background p-5 dark:border-gray-800">
                    <div>
                        <h3 className="text-lg font-medium">Work Videos</h3>
                        <p className="text-sm text-gray-500">
                            {isEditing
                                ? 'You can add YouTube videos and MP4 uploads immediately. Ordering and removal update the work separately from the main form.'
                                : 'You can stage videos while creating a work. The app creates the work first, then attaches the staged videos in order.'}
                        </p>
                    </div>

                    <div className="grid gap-4 md:grid-cols-[1fr_auto] md:items-end">
                        <div className="space-y-2">
                            <Label htmlFor="youtube-video-input">YouTube URL or ID</Label>
                            <Input
                                id="youtube-video-input"
                                value={youtubeUrlInput}
                                onChange={(event) => setYoutubeUrlInput(event.target.value)}
                                placeholder="https://youtu.be/... or dQw4w9WgXcQ"
                                disabled={isVideoBusy}
                            />
                        </div>
                        <Button
                            type="button"
                            variant="outline"
                            onClick={addYouTubeDraft}
                            disabled={isVideoBusy || !youtubeUrlInput.trim() || (!isEditing && stagedVideos.length >= 10)}
                        >
                            Add YouTube Video
                        </Button>
                    </div>

                    <div className="space-y-2">
                        <Label htmlFor="work-video-upload">Upload MP4 Video</Label>
                        <Input
                            id="work-video-upload"
                            type="file"
                            accept="video/mp4,.mp4"
                            onChange={handleStageVideoFile}
                            disabled={isVideoBusy || (!isEditing && stagedVideos.length >= 10)}
                        />
                        <p className="text-xs text-muted-foreground">
                            MP4 only, up to 200MB, maximum 10 videos per work.
                        </p>
                    </div>

                    {isEditing ? (
                        <div className="space-y-4">
                            <p className="text-xs uppercase tracking-wide text-muted-foreground">
                                Saved videos version {videosVersion}
                            </p>
                            {videos.length === 0 ? (
                                <div className="rounded-xl border border-dashed border-border/70 px-4 py-6 text-sm text-muted-foreground">
                                    No videos attached yet.
                                </div>
                            ) : (
                                <div className="space-y-4">
                                    {videos.map((video, index) => (
                                        <div key={video.id} className="space-y-3 rounded-xl border border-border/70 p-4">
                                            <div className="flex flex-wrap items-center justify-between gap-3">
                                                <div className="space-y-1">
                                                    <p className="text-sm font-medium">
                                                        {video.sourceType === 'youtube'
                                                            ? `YouTube ${video.sourceKey}`
                                                            : video.originalFileName || video.sourceKey}
                                                    </p>
                                                    <p className="text-xs text-muted-foreground">
                                                        {video.sourceType.toUpperCase()} · order {video.sortOrder + 1}
                                                    </p>
                                                </div>
                                                <div className="flex flex-wrap gap-2">
                                                    <Button
                                                        type="button"
                                                        variant="outline"
                                                        onClick={() => void reorderSavedVideo(video.id, -1)}
                                                        disabled={isVideoBusy || index === 0}
                                                    >
                                                        Move Up
                                                    </Button>
                                                    <Button
                                                        type="button"
                                                        variant="outline"
                                                        onClick={() => void reorderSavedVideo(video.id, 1)}
                                                        disabled={isVideoBusy || index === videos.length - 1}
                                                    >
                                                        Move Down
                                                    </Button>
                                                    <Button
                                                        type="button"
                                                        variant="outline"
                                                        onClick={() => void removeSavedVideo(video.id)}
                                                        disabled={isVideoBusy}
                                                    >
                                                        Remove
                                                    </Button>
                                                </div>
                                            </div>
                                            <WorkVideoPlayer video={video} />
                                        </div>
                                    ))}
                                </div>
                            )}
                        </div>
                    ) : (
                        <div className="space-y-4">
                            {stagedVideos.length === 0 ? (
                                <div className="rounded-xl border border-dashed border-border/70 px-4 py-6 text-sm text-muted-foreground">
                                    No staged videos yet.
                                </div>
                            ) : (
                                stagedVideos.map((video, index) => (
                                    <div key={video.tempId} className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-border/70 p-4">
                                        <div>
                                            <p className="text-sm font-medium">{video.label}</p>
                                            <p className="text-xs text-muted-foreground">
                                                {video.kind === 'youtube' ? 'YouTube draft' : 'MP4 draft'} · order {index + 1}
                                            </p>
                                        </div>
                                        <div className="flex flex-wrap gap-2">
                                            <Button
                                                type="button"
                                                variant="outline"
                                                onClick={() => moveStagedVideo(video.tempId, -1)}
                                                disabled={index === 0}
                                            >
                                                Move Up
                                            </Button>
                                            <Button
                                                type="button"
                                                variant="outline"
                                                onClick={() => moveStagedVideo(video.tempId, 1)}
                                                disabled={index === stagedVideos.length - 1}
                                            >
                                                Move Down
                                            </Button>
                                            <Button
                                                type="button"
                                                variant="outline"
                                                onClick={() => removeStagedVideo(video.tempId)}
                                            >
                                                Remove
                                            </Button>
                                        </div>
                                    </div>
                                ))
                            )}
                        </div>
                    )}
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
                            New works publish immediately when you save. If you stage videos, use <span className="font-medium">Create And Add Videos</span> so the work is created first and the videos attach safely after.
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
                        Saving creates a live work immediately, then returns you to the works list unless you choose the staged video flow.
                    </p>
                )}
                {!inlineMode && (
                    <Button type="button" variant="outline" onClick={() => router.push(returnTo)}>
                        Cancel
                    </Button>
                )}

                {isEditing ? (
                    <Button
                        type="button"
                        onClick={() => void saveWork('default')}
                        disabled={isSaving || !isDirty || !title.trim()}
                        className="bg-[#142850] hover:bg-[#142850]/90 text-white font-medium px-8 transition-all hover:scale-[1.02]"
                    >
                        {isSaving ? 'Saving...' : 'Update Work'}
                    </Button>
                ) : (
                    <>
                        <Button
                            type="button"
                            variant="outline"
                            onClick={() => void saveWork('default')}
                            disabled={isSaving || !isDirty || !title.trim() || hasStagedVideos}
                        >
                            {isSaving ? 'Saving...' : 'Create Work'}
                        </Button>
                        <Button
                            type="button"
                            onClick={() => void saveWork('with-videos')}
                            disabled={isSaving || !isDirty || !title.trim() || !hasStagedVideos}
                            className="bg-[#142850] hover:bg-[#142850]/90 text-white font-medium px-8 transition-all hover:scale-[1.02]"
                        >
                            {isSaving ? 'Creating...' : 'Create And Add Videos'}
                        </Button>
                    </>
                )}
            </div>
        </div>
    )
}
