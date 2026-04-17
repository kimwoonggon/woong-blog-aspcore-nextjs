import { getServerApiBaseUrl, getServerForwardingHeaders } from '@/lib/api/server'
import { throwPublicApiError } from '@/lib/api/public-errors'

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
  id: string
  publicUrl: string
  fileName: string
  path: string
}

export async function fetchPublicSiteSettings() {
  const apiBaseUrl = await getServerApiBaseUrl()
  const response = await fetch(`${apiBaseUrl}/public/site-settings`, {
    cache: 'no-store',
    headers: await getServerForwardingHeaders(),
  })

  if (!response.ok) {
    await throwPublicApiError(response, 'Failed to load public site settings.')
  }

  return response.json() as Promise<PublicSiteSettings>
}

export async function fetchResume() {
  const apiBaseUrl = await getServerApiBaseUrl()
  const response = await fetch(`${apiBaseUrl}/public/resume`, {
    cache: 'no-store',
    headers: await getServerForwardingHeaders(),
  })

  if (response.status === 404) {
    return null
  }

  if (!response.ok) {
    await throwPublicApiError(response, 'Failed to load public resume.')
  }

  return response.json() as Promise<ResumePayload>
}
