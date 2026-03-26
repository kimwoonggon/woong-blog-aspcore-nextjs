import { getServerApiBaseUrl } from '@/lib/api/server'

export interface PublicSiteSettings {
  ownerName: string
  tagline: string
  facebookUrl: string
  instagramUrl: string
  twitterUrl: string
  linkedInUrl: string
  gitHubUrl: string
}

export interface ResumePayload {
  publicUrl: string
  fileName: string
}

export async function fetchPublicSiteSettings() {
  const apiBaseUrl = await getServerApiBaseUrl()
  const response = await fetch(`${apiBaseUrl}/public/site-settings`, {
    cache: 'no-store',
  })

  if (!response.ok) {
    return null
  }

  return response.json() as Promise<PublicSiteSettings>
}

export async function fetchResume() {
  const apiBaseUrl = await getServerApiBaseUrl()
  const response = await fetch(`${apiBaseUrl}/public/resume`, {
    cache: 'no-store',
  })

  if (!response.ok) {
    return null
  }

  return response.json() as Promise<ResumePayload>
}
