import type { Metadata } from 'next'

const DEFAULT_METADATA_BASE = 'http://localhost:3000'
const DEFAULT_AUTHOR = 'Woonggon Kim'

function trimTrailingSlash(value: string) {
  return value.replace(/\/+$/, '')
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
  const normalizedPath = path.startsWith('/') ? path : `/${path}`
  const imageList = Array.isArray(images)
    ? images.filter(Boolean)
    : images
      ? [images]
      : undefined

  return {
    title,
    description,
    alternates: {
      canonical: normalizedPath,
    },
    openGraph: {
      title,
      description,
      url: normalizedPath,
      type,
      siteName: DEFAULT_AUTHOR,
      ...(imageList?.length ? { images: imageList } : {}),
    },
    twitter: {
      card: imageList?.length ? 'summary_large_image' : 'summary',
      title,
      description,
      ...(imageList?.length ? { images: imageList } : {}),
    },
  }
}
