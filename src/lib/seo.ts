import type { Metadata } from 'next'

const DEFAULT_METADATA_BASE = 'http://localhost:3000'
const DEFAULT_AUTHOR = 'Woonggon Kim'

function trimTrailingSlash(value: string) {
  return value.replace(/\/+$/, '')
}

function normalizeMetadataText(value: string, fallback = '') {
  const normalized = typeof value === 'string' ? value.trim() : ''
  return normalized || fallback
}

export function getMetadataBaseUrl() {
  if (process.env.NEXT_PUBLIC_SITE_URL) {
    return trimTrailingSlash(process.env.NEXT_PUBLIC_SITE_URL)
  }

  if (process.env.VERCEL_URL) {
    return `https://${trimTrailingSlash(process.env.VERCEL_URL)}`
  }

  return DEFAULT_METADATA_BASE
}

export function createPublicMetadata({
  title,
  description,
  path,
  type = 'website',
  images,
}: {
  title: string
  description: string
  path: string
  type?: 'website' | 'article'
  images?: string | string[] | null
}): Metadata {
  const safeTitle = normalizeMetadataText(title, DEFAULT_AUTHOR)
  const safeDescription = normalizeMetadataText(description)
  const safePath = normalizeMetadataText(path, '/')
  const normalizedPath = safePath.startsWith('/') ? safePath : `/${safePath}`
  const imageList = Array.isArray(images)
    ? images.map((image) => image?.trim()).filter((image): image is string => Boolean(image))
    : images
      ? [images.trim()].filter(Boolean)
      : undefined

  return {
    title: safeTitle,
    description: safeDescription,
    alternates: {
      canonical: normalizedPath,
    },
    openGraph: {
      title: safeTitle,
      description: safeDescription,
      url: normalizedPath,
      type,
      siteName: DEFAULT_AUTHOR,
      ...(imageList?.length ? { images: imageList } : {}),
    },
    twitter: {
      card: imageList?.length ? 'summary_large_image' : 'summary',
      title: safeTitle,
      description: safeDescription,
      ...(imageList?.length ? { images: imageList } : {}),
    },
  }
}
