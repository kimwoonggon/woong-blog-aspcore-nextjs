"use client"

import { Pause, Play } from 'lucide-react'
import { useEffect, useMemo, useRef, useState } from 'react'
import type { WorkVideo } from '@/lib/api/works'

interface WorkVideoPlayerProps {
  video: WorkVideo
  allowDesktopResize?: boolean
}

const hlsMimeType = 'application/vnd.apple.mpegurl'
const defaultAspectRatio = 16 / 9
const progressBarHeightPx = 10
export const timelinePreviewDisplayScale = 0.55

interface TimelinePreviewCue {
  start: number
  end: number
  x: number
  y: number
  width: number
  height: number
}

type DesktopSizeMode = 'fit' | 'wide' | 'theater'
const previewBottomOffsetPx = 56

function parseTimestampToSeconds(value: string) {
  const [clock, millisecondsRaw] = value.trim().split('.')
  const parts = clock.split(':').map((item) => Number.parseInt(item, 10))
  const milliseconds = Number.parseInt(millisecondsRaw ?? '0', 10)
  if (parts.some((item) => Number.isNaN(item)) || Number.isNaN(milliseconds)) {
    return Number.NaN
  }

  if (parts.length === 3) {
    return (parts[0] * 3600) + (parts[1] * 60) + parts[2] + (milliseconds / 1000)
  }

  if (parts.length === 2) {
    return (parts[0] * 60) + parts[1] + (milliseconds / 1000)
  }

  return Number.NaN
}

function resolvePreviewCue(cues: TimelinePreviewCue[], time: number) {
  return cues.find((cue) => time >= cue.start && time <= cue.end) ?? null
}

function formatTimeLabel(value: number) {
  if (!Number.isFinite(value) || value < 0) {
    return '0:00'
  }

  const totalSeconds = Math.floor(value)
  const minutes = Math.floor(totalSeconds / 60)
  const seconds = totalSeconds % 60

  if (minutes >= 60) {
    const hours = Math.floor(minutes / 60)
    const remainingMinutes = minutes % 60
    return `${hours}:${String(remainingMinutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`
  }

  return `${minutes}:${String(seconds).padStart(2, '0')}`
}

export function parseTimelinePreviewVtt(payload: string) {
  const cues: TimelinePreviewCue[] = []
  const blocks = payload.split(/\r?\n\r?\n/g)

  for (const block of blocks) {
    const lines = block.split(/\r?\n/).map((line) => line.trim()).filter(Boolean)
    const timingLine = lines.find((line) => line.includes('-->'))
    if (!timingLine) {
      continue
    }

    const [rawStart, rawEnd] = timingLine.split('-->').map((line) => line.trim())
    const start = parseTimestampToSeconds(rawStart)
    const end = parseTimestampToSeconds(rawEnd)
    const xywhLine = lines.find((line) => line.includes('#xywh='))
    if (!xywhLine || Number.isNaN(start) || Number.isNaN(end)) {
      continue
    }

    const xywhSegment = xywhLine.split('#xywh=')[1] ?? ''
    const [x, y, width, height] = xywhSegment.split(',').map((item) => Number.parseInt(item, 10))
    if ([x, y, width, height].some((item) => Number.isNaN(item)) || width <= 0 || height <= 0) {
      continue
    }

    cues.push({ start, end, x, y, width, height })
  }

  return cues
}

function desktopSizeClass(sizeMode: DesktopSizeMode, allowDesktopResize: boolean) {
  if (!allowDesktopResize) {
    return ''
  }

  if (sizeMode === 'wide') {
    return 'lg:w-[min(100vw-8rem,72rem)] lg:max-w-none lg:relative lg:left-1/2 lg:-translate-x-1/2'
  }

  if (sizeMode === 'theater') {
    return 'lg:w-[min(100vw-4rem,86rem)] lg:max-w-none lg:relative lg:left-1/2 lg:-translate-x-1/2'
  }

  return ''
}

export function WorkVideoPlayer({ video, allowDesktopResize = false }: WorkVideoPlayerProps) {
  const videoRef = useRef<HTMLVideoElement>(null)
  const frameRef = useRef<HTMLDivElement>(null)
  const [aspectRatio, setAspectRatio] = useState(() => {
    if (typeof video.width === 'number' && typeof video.height === 'number' && video.width > 0 && video.height > 0) {
      return video.width / video.height
    }

    return defaultAspectRatio
  })
  const [duration, setDuration] = useState(video.durationSeconds ?? 0)
  const [currentTime, setCurrentTime] = useState(0)
  const [isPlaying, setIsPlaying] = useState(false)
  const [previewCues, setPreviewCues] = useState<TimelinePreviewCue[]>([])
  const [previewTime, setPreviewTime] = useState<number | null>(null)
  const [previewLeft, setPreviewLeft] = useState(0)
  const [previewCue, setPreviewCue] = useState<TimelinePreviewCue | null>(null)
  const [desktopSizeMode, setDesktopSizeMode] = useState<DesktopSizeMode>(allowDesktopResize ? 'wide' : 'fit')
  const isHlsVideo = useMemo(() => {
    return video.sourceType === 'hls'
      || video.mimeType === hlsMimeType
      || video.playbackUrl?.toLowerCase().endsWith('.m3u8') === true
  }, [video.mimeType, video.playbackUrl, video.sourceType])
  const supportsTimelinePreview = useMemo(() => {
    return video.sourceType !== 'youtube'
      && Boolean(video.timelinePreviewSpriteUrl)
      && Boolean(video.timelinePreviewVttUrl)
  }, [video.sourceType, video.timelinePreviewSpriteUrl, video.timelinePreviewVttUrl])
  const previewSpriteSize = useMemo(() => {
    if (previewCues.length === 0) {
      return { width: 0, height: 0 }
    }

    return previewCues.reduce((accumulator, cue) => ({
      width: Math.max(accumulator.width, cue.x + cue.width),
      height: Math.max(accumulator.height, cue.y + cue.height),
    }), { width: 0, height: 0 })
  }, [previewCues])
  const progressPercent = duration > 0 ? (currentTime / duration) * 100 : 0
  useEffect(() => {
    if (!isHlsVideo || !video.playbackUrl) {
      return
    }

    const element = videoRef.current
    if (!element) {
      return
    }

    if (element.canPlayType(hlsMimeType)) {
      element.src = video.playbackUrl
      return () => {
        element.removeAttribute('src')
      }
    }

    let disposed = false
    let hls: { loadSource: (source: string) => void; attachMedia: (media: HTMLMediaElement) => void; destroy: () => void } | null = null

    void import('hls.js').then(({ default: Hls }) => {
      if (disposed || !Hls.isSupported()) {
        return
      }

      hls = new Hls()
      hls.loadSource(video.playbackUrl!)
      hls.attachMedia(element)
    })

    return () => {
      disposed = true
      hls?.destroy()
    }
  }, [isHlsVideo, video.playbackUrl])

  useEffect(() => {
    setAspectRatio(() => {
      if (typeof video.width === 'number' && typeof video.height === 'number' && video.width > 0 && video.height > 0) {
        return video.width / video.height
      }

      return defaultAspectRatio
    })
    setDuration(video.durationSeconds ?? 0)
  }, [video.durationSeconds, video.height, video.width])

  useEffect(() => {
    if (!supportsTimelinePreview || !video.timelinePreviewVttUrl) {
      setPreviewCues([])
      return
    }

    let cancelled = false

    void fetch(video.timelinePreviewVttUrl)
      .then((response) => {
        if (!response.ok) {
          throw new Error('timeline preview vtt fetch failed')
        }

        return response.text()
      })
      .then((text) => {
        if (!cancelled) {
          const cues = parseTimelinePreviewVtt(text)
          setPreviewCues(cues)
          if ((!Number.isFinite(duration) || duration <= 0) && cues.length > 0) {
            setDuration(cues[cues.length - 1].end)
          }
        }
      })
      .catch(() => {
        if (!cancelled) {
          setPreviewCues([])
        }
      })

    return () => {
      cancelled = true
    }
  }, [duration, supportsTimelinePreview, video.timelinePreviewVttUrl])

  function syncMetadata(element: HTMLVideoElement) {
    if (element.videoWidth > 0 && element.videoHeight > 0) {
      setAspectRatio(element.videoWidth / element.videoHeight)
    }

    if (Number.isFinite(element.duration) && element.duration > 0) {
      setDuration(element.duration)
    }
  }

  function updatePreview(clientX: number, barElement: HTMLDivElement) {
    if (!supportsTimelinePreview || previewCues.length === 0 || duration <= 0 || !video.timelinePreviewSpriteUrl) {
      setPreviewCue(null)
      setPreviewTime(null)
      return
    }

    const barRect = barElement.getBoundingClientRect()
    if (barRect.width <= 0) {
      return
    }

    const offsetX = Math.max(0, Math.min(clientX - barRect.left, barRect.width))
    const percent = offsetX / barRect.width
    const targetTime = percent * duration
    const cue = resolvePreviewCue(previewCues, targetTime)

    if (!cue) {
      setPreviewCue(null)
      setPreviewTime(null)
      return
    }

    const frameRect = frameRef.current?.getBoundingClientRect()
    const rawPreviewLeft = frameRect
      ? (barRect.left - frameRect.left) + offsetX
      : offsetX
    const previewHalfWidth = (cue.width * timelinePreviewDisplayScale) / 2
    const previewLeft = frameRect
      ? frameRect.width > (previewHalfWidth * 2)
        ? Math.max(previewHalfWidth, Math.min(rawPreviewLeft, frameRect.width - previewHalfWidth))
        : frameRect.width / 2
      : rawPreviewLeft

    setPreviewCue(cue)
    setPreviewTime(targetTime)
    setPreviewLeft(previewLeft)
  }

  async function togglePlayback() {
    if (!videoRef.current) {
      return
    }

    if (videoRef.current.paused) {
      await videoRef.current.play().catch(() => undefined)
      return
    }

    videoRef.current.pause()
  }

  function seekToClientX(clientX: number, barElement: HTMLDivElement) {
    if (!videoRef.current || duration <= 0) {
      return
    }

    const rect = barElement.getBoundingClientRect()
    if (rect.width <= 0) {
      return
    }

    const offsetX = Math.max(0, Math.min(clientX - rect.left, rect.width))
    const nextTime = (offsetX / rect.width) * duration
    videoRef.current.currentTime = nextTime
    setCurrentTime(nextTime)
  }

  if (video.sourceType === 'youtube') {
    return (
      <div
        data-testid="work-video-player"
        data-size-mode="fit"
        className="mx-auto w-full"
      >
        <div
          className="relative w-full overflow-hidden rounded-xl border border-border/70 bg-black"
          style={{
            aspectRatio: String(defaultAspectRatio),
            maxHeight: 'clamp(16rem, 72vh, 42rem)',
          }}
        >
          <iframe
            src={`https://www.youtube-nocookie.com/embed/${video.sourceKey}?playsinline=1&rel=0`}
            title={video.originalFileName ?? `YouTube video ${video.sourceKey}`}
            allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
            allowFullScreen
            className="h-full w-full"
          />
        </div>
      </div>
    )
  }

  return (
    <div
      data-testid="work-video-player"
      data-size-mode={desktopSizeMode}
      data-preview-ready={supportsTimelinePreview && previewCues.length > 0 ? 'true' : 'false'}
      className={`mx-auto w-full ${desktopSizeClass(desktopSizeMode, allowDesktopResize)}`}
    >
      {allowDesktopResize ? (
        <div className="mb-3 hidden items-center justify-end gap-2 lg:flex">
          <button
            type="button"
            data-testid="work-video-size-fit"
            onClick={() => setDesktopSizeMode('fit')}
            className={`rounded-full border px-3 py-1.5 text-xs font-medium transition-colors ${desktopSizeMode === 'fit' ? 'border-foreground bg-foreground text-background' : 'border-border bg-background text-foreground hover:bg-muted'}`}
          >
            Fit
          </button>
          <button
            type="button"
            data-testid="work-video-size-wide"
            onClick={() => setDesktopSizeMode('wide')}
            className={`rounded-full border px-3 py-1.5 text-xs font-medium transition-colors ${desktopSizeMode === 'wide' ? 'border-foreground bg-foreground text-background' : 'border-border bg-background text-foreground hover:bg-muted'}`}
          >
            Wide
          </button>
          <button
            type="button"
            data-testid="work-video-size-theater"
            onClick={() => setDesktopSizeMode('theater')}
            className={`rounded-full border px-3 py-1.5 text-xs font-medium transition-colors ${desktopSizeMode === 'theater' ? 'border-foreground bg-foreground text-background' : 'border-border bg-background text-foreground hover:bg-muted'}`}
          >
            Theater
          </button>
        </div>
      ) : null}

      <div
        ref={frameRef}
        data-testid="work-video-frame"
        className="relative w-full overflow-hidden rounded-xl border border-border/70 bg-black"
        style={{
          aspectRatio: String(aspectRatio),
          maxHeight: allowDesktopResize
            ? desktopSizeMode === 'theater'
              ? 'clamp(20rem, 84vh, 56rem)'
              : desktopSizeMode === 'wide'
                ? 'clamp(18rem, 80vh, 50rem)'
                : 'clamp(16rem, 72vh, 42rem)'
            : 'clamp(16rem, 72vh, 42rem)',
        }}
      >
        <video
          ref={videoRef}
          preload="metadata"
          playsInline
          controlsList="nodownload noremoteplayback"
          disablePictureInPicture
          onLoadedMetadata={(event) => syncMetadata(event.currentTarget)}
          onDurationChange={(event) => syncMetadata(event.currentTarget)}
          onTimeUpdate={(event) => setCurrentTime(event.currentTarget.currentTime)}
          onPlay={() => setIsPlaying(true)}
          onPause={() => setIsPlaying(false)}
          onContextMenu={(event) => event.preventDefault()}
          className="h-full w-full bg-black"
        >
          {!isHlsVideo ? <source src={video.playbackUrl ?? undefined} type={video.mimeType ?? 'video/mp4'} /> : null}
        </video>

        {!isPlaying ? (
          <div className="pointer-events-none absolute inset-0 z-10 flex items-center justify-center">
            <button
              type="button"
              data-testid="work-video-center-play"
              aria-label="Play video"
              onClick={() => void togglePlayback()}
              className="pointer-events-auto inline-flex h-16 w-16 items-center justify-center rounded-full border border-white/20 bg-black/55 text-white shadow-lg backdrop-blur transition-colors hover:bg-black/70"
            >
              <Play className="h-7 w-7 translate-x-[2px]" />
            </button>
          </div>
        ) : null}

        <div
          className="absolute inset-x-0 z-10 bg-gradient-to-t from-black/70 via-black/30 to-transparent px-4 pb-3 pt-10"
          style={{
            bottom: 0,
          }}
        >
          <div className="flex items-center gap-3">
            <button
              type="button"
              data-testid="work-video-play-toggle"
              aria-label={isPlaying ? 'Pause video' : 'Play video'}
              onClick={() => void togglePlayback()}
              className="inline-flex h-9 w-9 shrink-0 items-center justify-center rounded-full border border-white/20 bg-black/50 text-white backdrop-blur transition-colors hover:bg-black/70"
            >
              {isPlaying ? <Pause className="h-4 w-4" /> : <Play className="h-4 w-4 translate-x-[1px]" />}
            </button>

            <div className="min-w-0 flex-1 space-y-1">
              <div className="flex items-center justify-between text-[11px] font-medium tabular-nums text-white/90">
                <span data-testid="work-video-current-time">{formatTimeLabel(currentTime)}</span>
                <span data-testid="work-video-duration">{formatTimeLabel(duration)}</span>
              </div>

              <div
                data-testid="work-video-progress-overlay"
                className="relative cursor-pointer"
                style={{ height: `${progressBarHeightPx}px` }}
                onMouseMove={(event) => {
                  if (event.currentTarget instanceof HTMLDivElement) {
                    updatePreview(event.clientX, event.currentTarget)
                  }
                }}
                onMouseLeave={() => {
                  setPreviewCue(null)
                  setPreviewTime(null)
                }}
                onClick={(event) => {
                  if (event.currentTarget instanceof HTMLDivElement) {
                    seekToClientX(event.clientX, event.currentTarget)
                  }
                }}
              >
                <div className="absolute inset-x-0 top-1/2 h-1.5 -translate-y-1/2 rounded-full bg-white/20" />
                <div
                  className="absolute left-0 top-1/2 h-1.5 -translate-y-1/2 rounded-full bg-brand-cyan"
                  style={{ width: `${Math.max(0, Math.min(progressPercent, 100))}%` }}
                />
                <div
                  className="absolute top-1/2 h-3.5 w-3.5 -translate-y-1/2 rounded-full border border-white/30 bg-white shadow-sm"
                  style={{ left: `calc(${Math.max(0, Math.min(progressPercent, 100))}% - 0.4375rem)` }}
                />
              </div>
            </div>
          </div>
        </div>

        {previewCue && previewTime !== null ? (
          <div
            data-testid="work-video-timeline-preview"
            className="pointer-events-none absolute z-20 -translate-x-1/2 rounded-md border border-border/80 bg-black/90 p-1"
            style={{
              left: previewLeft,
              bottom: `${previewBottomOffsetPx}px`,
            }}
          >
            {video.timelinePreviewSpriteUrl ? (
              <div
                style={{
                  width: `${previewCue.width * timelinePreviewDisplayScale}px`,
                  height: `${previewCue.height * timelinePreviewDisplayScale}px`,
                  backgroundImage: `url(${video.timelinePreviewSpriteUrl})`,
                  backgroundPosition: `-${previewCue.x * timelinePreviewDisplayScale}px -${previewCue.y * timelinePreviewDisplayScale}px`,
                  backgroundRepeat: 'no-repeat',
                  backgroundSize: `${previewSpriteSize.width * timelinePreviewDisplayScale}px ${previewSpriteSize.height * timelinePreviewDisplayScale}px`,
                }}
              />
            ) : null}
            <p className="mt-1 text-center text-[11px] tabular-nums text-white/90">
              {formatTimeLabel(previewTime)}
            </p>
          </div>
        ) : null}
      </div>
    </div>
  )
}
