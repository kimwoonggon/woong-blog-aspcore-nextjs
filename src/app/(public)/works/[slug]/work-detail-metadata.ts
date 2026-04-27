import type { WorkDetail } from '@/lib/api/works'
import { createPublicMetadata } from '@/lib/seo'

function resolveYouTubeThumbnail(sourceKey: string) {
    const videoId = sourceKey.trim()
    return videoId ? `https://img.youtube.com/vi/${encodeURIComponent(videoId)}/hqdefault.jpg` : null
}

function resolveWorkMetadataImage(work: WorkDetail | null) {
    if (!work) {
        return null
    }

    if (work.thumbnailUrl) {
        return work.thumbnailUrl
    }

    const youtubeVideo = work.videos.find((video) => video.sourceType === 'youtube' && video.sourceKey)
    return youtubeVideo ? resolveYouTubeThumbnail(youtubeVideo.sourceKey) : null
}

function resolveWorkMetadataDescription(work: WorkDetail | null) {
    if (!work) {
        return ''
    }

    const shareMessage = work.socialShareMessage?.trim()
    return shareMessage ? shareMessage : work.excerpt
}

export function buildWorkDetailMetadata(work: WorkDetail) {
    return createPublicMetadata({
        title: work.title,
        description: resolveWorkMetadataDescription(work),
        path: `/works/${work.slug}`,
        type: 'article',
        images: resolveWorkMetadataImage(work),
    })
}
