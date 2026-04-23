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
}: {
  title: string
  description: string
  path: string
  type?: 'website' | 'article'
}): Metadata {
  const normalizedPath = path.startsWith('/') ? path : `/${path}`

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
    },
    twitter: {
      card: 'summary',
      title,
      description,
    },
  }
}
