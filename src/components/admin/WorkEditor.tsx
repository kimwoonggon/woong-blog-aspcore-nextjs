"use client"

import Image from 'next/image'
import { useMemo, useState } from 'react'
import { useRouter, useSearchParams } from 'next/navigation'
import { AIFixDialog } from '@/components/admin/AIFixDialog'
import { TiptapEditor } from '@/components/admin/TiptapEditor'
import { WorkVideoPlayer } from '@/components/content/WorkVideoPlayer'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Checkbox } from '@/components/ui/checkbox'
import { fetchWithCsrf } from '@/lib/api/auth'
import { getBrowserApiBaseUrl } from '@/lib/api/browser'
import type { WorkVideo } from '@/lib/api/works'
import { extractVideoFrameThumbnailBlob, fetchRemoteImageBlob } from '@/lib/content/work-auto-thumbnail'
import {
    extractWorkVideoEmbedIds,
    getWorkVideoDisplayLabel,
    removeWorkVideoEmbedReferences,
} from '@/lib/content/work-video-embeds'
import {
    buildYouTubeThumbnailUrl,
    normalizeYouTubeVideoId,
    resolveDraftThumbnailSource,
    resolveWorkThumbnailSource,
    shouldReplaceWorkThumbnailSource,
    type WorkThumbnailSourceKind,
} from '@/lib/content/work-thumbnail-resolution'
import type {
    ThumbnailCandidate,
    StagedVideoResult,
    UploadedAssetPayload,
    UploadTargetPayload,
    VideoDraft,
    VideoInsertRequest,
    VideoMutationPayload,
    WorkEditorProps,
    WorkSaveResponsePayload,
} from '@/components/admin/work-editor/types'
import {
    buildWorkSnapshot,
    getNextVideosVersion,
    getResponseError,
    inferThumbnailSourceKind,
    normalizeJsonInput,
    normalizeTagsInput,
    resolveWorkSaveSlug,
    validateFlexibleMetadata,
} from '@/components/admin/work-editor/utils'
import { toast } from 'sonner'

const DEFAULT_WORK_CATEGORY = 'Uncategorized'

export function WorkEditor({ initialWork, inlineMode = false, onSaved }: WorkEditorProps) {
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
    const [thumbnailSourceKind, setThumbnailSourceKind] = useState<WorkThumbnailSourceKind>(() => inferThumbnailSourceKind(initialWork))
    const [videosVersion, setVideosVersion] = useState(initialWork?.videos_version || 0)
    const [videos, setVideos] = useState<WorkVideo[]>(initialWork?.videos || [])
    const [stagedVideos, setStagedVideos] = useState<VideoDraft[]>([])
    const [youtubeUrlInput, setYoutubeUrlInput] = useState('')
    const [isSaving, setIsSaving] = useState(false)
    const [uploadingTarget, setUploadingTarget] = useState<'thumbnail' | 'icon' | null>(null)
    const [isVideoBusy, setIsVideoBusy] = useState(false)
    const [isAutoGeneratingThumbnail, setIsAutoGeneratingThumbnail] = useState(false)
    const [insertVideoRequest, setInsertVideoRequest] = useState<VideoInsertRequest | null>(null)
    const shouldContinueInlinePlacement = searchParams.get('videoInline') === '1'
    const embeddedVideoIds = useMemo(() => extractWorkVideoEmbedIds(html), [html])
    const embeddedVideoIdSet = useMemo(() => new Set(embeddedVideoIds), [embeddedVideoIds])
    const orphanEmbeddedVideoIds = useMemo(
        () => embeddedVideoIds.filter((videoId, index) => embeddedVideoIds.indexOf(videoId) === index && !videos.some((video) => video.id === videoId)),
        [embeddedVideoIds, videos],
    )
    const resolvedThumbnailSource = useMemo(
        () => resolveWorkThumbnailSource({ thumbnailAssetId, videos, html }),
        [thumbnailAssetId, videos, html],
    )
    const effectiveThumbnailPreviewUrl = useMemo(() => {
        if (thumbnailUrl) {
            return thumbnailUrl
        }

        if (resolvedThumbnailSource.kind === 'youtube' && resolvedThumbnailSource.video) {
            return buildYouTubeThumbnailUrl(resolvedThumbnailSource.video.sourceKey)
        }

        if (resolvedThumbnailSource.kind === 'content-image') {
            return resolvedThumbnailSource.imageUrl ?? ''
        }

        return ''
    }, [resolvedThumbnailSource, thumbnailUrl])

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
        const nextVersion = getNextVideosVersion(payload, videosVersion)
        const nextVideos = Array.isArray(payload.videos) ? payload.videos : videos

        setVideosVersion(nextVersion)
        setVideos(nextVideos)
    }

    function buildWorkMutationPayload(nextThumbnailAssetId: string = thumbnailAssetId, nextIconAssetId: string = iconAssetId) {
        const normalizedTags = normalizeTagsInput(tags)

        return {
            title,
            category: category.trim() || DEFAULT_WORK_CATEGORY,
            period,
            tags: normalizedTags,
            published,
            contentJson: JSON.stringify({ html }),
            allPropertiesJson: normalizeJsonInput(allProperties),
            thumbnailAssetId: nextThumbnailAssetId || null,
            iconAssetId: nextIconAssetId || null,
        }
    }

    async function uploadAssetFile(file: File, bucket: string) {
        const formData = new FormData()
        formData.append('file', file)
        formData.append('bucket', bucket)

        const response = await fetchWithCsrf(`${getBrowserApiBaseUrl()}/uploads`, {
            method: 'POST',
            body: formData,
        })

        const payload = await response.json() as UploadedAssetPayload & { error?: string }
        if (!response.ok) {
            throw new Error(payload.error || 'Upload failed')
        }

        return payload
    }

    async function uploadGeneratedThumbnail(blob: Blob, fileName: string) {
        const file = new File([blob], fileName, { type: blob.type || 'image/jpeg' })
        return await uploadAssetFile(file, 'work-thumbnails')
    }

    function applyThumbnailSelection(asset: UploadedAssetPayload, sourceKind: WorkThumbnailSourceKind) {
        setThumbnailAssetId(asset.id)
        setThumbnailUrl(asset.url)
        setThumbnailSourceKind(sourceKind)
    }

    async function tryAutoGenerateThumbnailFromUploadedVideo(file: File) {
        const thumbnailBlob = await extractVideoFrameThumbnailBlob(file)
        const uploadedThumbnail = await uploadGeneratedThumbnail(
            thumbnailBlob,
            `${file.name.replace(/\.[^.]+$/, '') || 'video'}-thumbnail.jpg`,
        )
        applyThumbnailSelection(uploadedThumbnail, 'uploaded-video')
        return uploadedThumbnail
    }

    async function tryAutoGenerateThumbnailFromYouTube(videoId: string) {
        const thumbnailBlob = await fetchRemoteImageBlob(buildYouTubeThumbnailUrl(videoId))
        const uploadedThumbnail = await uploadGeneratedThumbnail(thumbnailBlob, `${videoId}-thumbnail.jpg`)
        applyThumbnailSelection(uploadedThumbnail, 'youtube')
        return uploadedThumbnail
    }

    async function maybeApplyAutoThumbnailForCandidate(candidate: ThumbnailCandidate) {
        if (!shouldReplaceWorkThumbnailSource(thumbnailSourceKind, candidate.kind)) {
            return null
        }

        setIsAutoGeneratingThumbnail(true)

        try {
            if (candidate.kind === 'uploaded-video' && candidate.file) {
                return await tryAutoGenerateThumbnailFromUploadedVideo(candidate.file)
            }

            if (candidate.kind === 'youtube' && candidate.youtubeVideoId) {
                return await tryAutoGenerateThumbnailFromYouTube(candidate.youtubeVideoId)
            }

            return null
        } catch (error) {
            if (candidate.kind === 'youtube') {
                setThumbnailSourceKind('youtube')
                setThumbnailUrl(buildYouTubeThumbnailUrl(candidate.youtubeVideoId ?? ''))
            }

            throw error
        } finally {
            setIsAutoGeneratingThumbnail(false)
        }
    }

    async function persistThumbnailSelectionForWork(workId: string, nextThumbnailAssetId: string) {
        const response = await fetchWithCsrf(`${getBrowserApiBaseUrl()}/admin/works/${encodeURIComponent(workId)}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(buildWorkMutationPayload(nextThumbnailAssetId)),
        })

        if (!response.ok) {
            throw new Error(await getResponseError(response, 'Failed to persist the generated thumbnail.'))
        }

        return await response.json().catch(() => null) as { id?: string; slug?: string; Slug?: string } | null
    }

    async function uploadWorkImage(
        event: React.ChangeEvent<HTMLInputElement>,
        target: 'thumbnail' | 'icon'
    ) {
        const file = event.target.files?.[0]
        if (!file) return

        setUploadingTarget(target)

        try {
            const payload = await uploadAssetFile(file, target === 'thumbnail' ? 'work-thumbnails' : 'work-icons')

            if (target === 'thumbnail') {
                setThumbnailAssetId(payload.id)
                setThumbnailUrl(payload.url)
                setThumbnailSourceKind('manual')
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
            setThumbnailSourceKind(resolveWorkThumbnailSource({
                thumbnailAssetId: null,
                videos,
                html,
            }).kind)
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

            const payload = await response.json() as VideoMutationPayload
            syncVideos(payload)
            setYoutubeUrlInput('')
            const normalizedVideoId = normalizeYouTubeVideoId(youtubeUrlOrId)
            if (normalizedVideoId) {
                try {
                    await maybeApplyAutoThumbnailForCandidate({ kind: 'youtube', youtubeVideoId: normalizedVideoId })
                } catch (error) {
                    toast.error(error instanceof Error ? error.message : 'Failed to auto-generate a YouTube thumbnail.')
                }
            }
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
            try {
                await maybeApplyAutoThumbnailForCandidate({ kind: 'uploaded-video', file })
            } catch (error) {
                toast.error(error instanceof Error ? error.message : 'Failed to auto-generate a video thumbnail.')
            }
            toast.success('Video uploaded.')
        } catch (error) {
            toast.error(error instanceof Error ? error.message : 'Failed to upload video.')
        } finally {
            setIsVideoBusy(false)
        }
    }

    async function removeSavedVideo(videoId: string) {
        if (!initialWork?.id) return

        if (embeddedVideoIdSet.has(videoId)) {
            toast.error('Remove this video from the body before deleting it.')
            return
        }

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

    async function addStagedYoutubeVideo(workId: string, draft: VideoDraft, currentVersion: number): Promise<StagedVideoResult> {
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
        return {
            currentVersion: getNextVideosVersion(payload, currentVersion + 1),
            latestPayload: payload,
        }
    }

    async function addStagedUploadedVideo(workId: string, draft: VideoDraft, currentVersion: number): Promise<StagedVideoResult> {
        if (!draft.file) {
            return {
                currentVersion,
                latestPayload: null,
            }
        }

        const target = await requestUploadTarget(workId, draft.file, currentVersion)
        await uploadToTarget(workId, draft.file, target)
        const payload = await confirmVideoUpload(workId, target.uploadSessionId, currentVersion)

        return {
            currentVersion: getNextVideosVersion(payload, currentVersion + 1),
            latestPayload: payload,
        }
    }

    async function processStagedVideos(workId: string) {
        let currentVersion = 0
        let latestPayload: VideoMutationPayload | null = null

        for (const draft of stagedVideos) {
            if (draft.kind === 'youtube' && draft.youtubeUrl) {
                const result = await addStagedYoutubeVideo(workId, draft, currentVersion)
                currentVersion = result.currentVersion
                latestPayload = result.latestPayload
                continue
            }

            if (draft.kind === 'file' && draft.file) {
                const result = await addStagedUploadedVideo(workId, draft, currentVersion)
                currentVersion = result.currentVersion
                latestPayload = result.latestPayload
            }
        }

        return {
            latestPayload,
            currentVersion,
        }
    }

    async function submitWorkPayload(payload: ReturnType<typeof buildWorkMutationPayload>) {
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
            return null
        }

        return await response.json().catch(() => null) as WorkSaveResponsePayload | null
    }

    function finishInlineSave(responsePayload: WorkSaveResponsePayload | null, nextSlug: string | null, editing: boolean) {
        if (!inlineMode) {
            return false
        }

        if (onSaved) {
            onSaved({ id: responsePayload?.id, slug: nextSlug, isEditing: editing })
            return true
        }

        router.refresh()
        return true
    }

    function finishUpdateSave(responsePayload: WorkSaveResponsePayload | null, nextSlug: string | null) {
        toast.success('Work updated successfully')
        if (finishInlineSave(responsePayload, nextSlug, true)) {
            return
        }

        router.push(returnTo)
    }

    function finishCreateSave(responsePayload: WorkSaveResponsePayload | null, nextSlug: string | null) {
        toast.success('Work created successfully')
        if (finishInlineSave(responsePayload, nextSlug, false)) {
            return
        }

        router.push(returnTo)
    }

    async function handleCreatedWorkWithVideos(responsePayload: WorkSaveResponsePayload, nextSlug: string | null) {
        if (!responsePayload.id) {
            finishCreateSave(responsePayload, nextSlug)
            return
        }

        try {
            const stagedResult = await processStagedVideos(responsePayload.id)
            if (stagedResult.latestPayload) {
                syncVideos(stagedResult.latestPayload)
            }

            const stagedThumbnailCandidate = resolveDraftThumbnailSource(stagedVideos)
            if (stagedThumbnailCandidate.kind !== 'none') {
                try {
                    const uploadedThumbnail = await maybeApplyAutoThumbnailForCandidate(stagedThumbnailCandidate)
                    if (uploadedThumbnail) {
                        await persistThumbnailSelectionForWork(responsePayload.id, uploadedThumbnail.id)
                    }
                } catch (error) {
                    toast.error(error instanceof Error ? error.message : 'Failed to auto-generate a work thumbnail.')
                }
            }

            toast.success('Work and videos created successfully')
            if (inlineMode && onSaved) {
                onSaved({ id: responsePayload.id, slug: nextSlug, isEditing: false })
                return
            }

            router.push(`/admin/works/${responsePayload.id}?videoInline=1`)
        } catch (error) {
            toast.error(error instanceof Error ? error.message : 'Work was created, but some videos failed to attach.')
            router.push(`/admin/works/${responsePayload.id}`)
        }
    }

    async function saveWork(mode: 'default' | 'with-videos' = 'default') {
        try {
            validateFlexibleMetadata(allProperties)
        } catch {
            toast.error('Invalid JSON in Flexible Metadata field')
            return
        }

        setIsSaving(true)

        try {
            const responsePayload = await submitWorkPayload(buildWorkMutationPayload())
            if (!responsePayload) {
                return
            }

            const nextSlug = resolveWorkSaveSlug({
                payload: responsePayload,
                title,
                initialSlug: initialWork?.slug,
            })

            if (isEditing) {
                finishUpdateSave(responsePayload, nextSlug)
                return
            }

            if (mode === 'with-videos' && stagedVideos.length > 0) {
                await handleCreatedWorkWithVideos(responsePayload, nextSlug)
                return
            }

            finishCreateSave(responsePayload, nextSlug)
        } finally {
            setIsSaving(false)
        }
    }

    function insertSavedVideoIntoBody(videoId: string) {
        if (embeddedVideoIdSet.has(videoId)) {
            toast.error('This video is already placed in the body.')
            return
        }

        setInsertVideoRequest({ videoId, nonce: Date.now() })
    }

    function removeSavedVideoFromBody(videoId: string) {
        if (!embeddedVideoIdSet.has(videoId)) {
            return
        }

        setHtml((current) => removeWorkVideoEmbedReferences(current, videoId))
        toast.success('Inline video removed from the body.')
    }

    function handleVideoInsertHandled(result: { inserted: boolean; reason?: 'duplicate' | 'missing' }) {
        setInsertVideoRequest(null)

        if (result.inserted) {
            toast.success('Video inserted into the body.')
            return
        }

        if (result.reason === 'duplicate') {
            toast.error('This video is already placed in the body.')
            return
        }

        if (result.reason === 'missing') {
            toast.error('This video is no longer available in the saved video list.')
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
                                {effectiveThumbnailPreviewUrl ? (
                                    <Image
                                        src={effectiveThumbnailPreviewUrl}
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
                            <p className="text-xs text-muted-foreground" data-testid="work-thumbnail-source">
                                {thumbnailSourceKind === 'manual'
                                    ? 'Thumbnail source: manual'
                                    : thumbnailSourceKind === 'uploaded-video'
                                        ? 'Thumbnail source: uploaded video'
                                        : thumbnailSourceKind === 'youtube'
                                            ? 'Thumbnail source: YouTube'
                                            : thumbnailSourceKind === 'content-image'
                                                ? 'Thumbnail source: content image'
                                                : 'Thumbnail source: none'}
                            </p>
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
                                    disabled={!thumbnailAssetId}
                                >
                                    Remove Thumbnail
                                </Button>
                                {uploadingTarget === 'thumbnail' && (
                                    <span className="text-sm text-gray-500">Uploading...</span>
                                )}
                                {isAutoGeneratingThumbnail && (
                                    <span className="text-sm text-gray-500">Generating thumbnail...</span>
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
                                            {embeddedVideoIdSet.has(video.id) && (
                                                <div className="rounded-xl border border-emerald-300 bg-emerald-50 px-3 py-2 text-xs text-emerald-900 dark:border-emerald-900/60 dark:bg-emerald-950/20 dark:text-emerald-100">
                                                    Placed in body. Remove it from the body before deleting the saved video.
                                                </div>
                                            )}
                                            <div className="flex flex-wrap items-center justify-between gap-3">
                                                <div className="space-y-1">
                                                    <p className="text-sm font-medium">{getWorkVideoDisplayLabel(video)}</p>
                                                    <p className="text-xs text-muted-foreground">
                                                        {video.sourceType.toUpperCase()} · order {video.sortOrder + 1} · {embeddedVideoIdSet.has(video.id) ? 'Placed in body' : 'Not placed'}
                                                    </p>
                                                </div>
                                                <div className="flex flex-wrap gap-2">
                                                    <Button
                                                        type="button"
                                                        variant="outline"
                                                        onClick={() => insertSavedVideoIntoBody(video.id)}
                                                        disabled={isVideoBusy || embeddedVideoIdSet.has(video.id)}
                                                    >
                                                        Insert Into Body
                                                    </Button>
                                                    <Button
                                                        type="button"
                                                        variant="outline"
                                                        onClick={() => removeSavedVideoFromBody(video.id)}
                                                        disabled={isVideoBusy || !embeddedVideoIdSet.has(video.id)}
                                                    >
                                                        Remove From Body
                                                    </Button>
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
                            {orphanEmbeddedVideoIds.length > 0 && (
                                <div className="rounded-xl border border-amber-300 bg-amber-50 px-4 py-3 text-sm text-amber-900 dark:border-amber-900/60 dark:bg-amber-950/20 dark:text-amber-100">
                                    Body references missing videos: {orphanEmbeddedVideoIds.join(', ')}
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
                {shouldContinueInlinePlacement && isEditing && videos.length > 0 && (
                    <div className="rounded-xl border border-emerald-300 bg-emerald-50 px-4 py-3 text-sm text-emerald-900 dark:border-emerald-900/60 dark:bg-emerald-950/20 dark:text-emerald-100">
                        Videos were saved. Continue by placing them inline inside the body wherever they should appear.
                    </div>
                )}
                <TiptapEditor
                    content={html}
                    onChange={setHtml}
                    placeholder="Describe the project story and place saved videos inline where they belong..."
                    workVideos={videos}
                    insertVideoEmbedRequest={insertVideoRequest}
                    onVideoInsertHandled={handleVideoInsertHandled}
                />
                {orphanEmbeddedVideoIds.length > 0 && (
                    <p className="text-sm text-amber-700 dark:text-amber-300">
                        Some inline video references no longer exist in the saved list. Remove those inline blocks before publishing.
                    </p>
                )}
                <p className="text-sm text-gray-500">
                    Use the saved video cards above to place each video inline inside the story.
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
