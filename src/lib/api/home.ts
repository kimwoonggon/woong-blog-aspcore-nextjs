import { getServerApiBaseUrl } from '@/lib/api/server'

export interface HomePagePayload {
  title: string
  contentJson: string
}

export interface HomeSiteSettingsPayload {
  ownerName: string
  tagline: string
  gitHubUrl: string
  linkedInUrl: string
  resumePublicUrl: string
}

export interface HomeFeaturedWork {
  id: string
  slug: string
  title: string
  excerpt: string
  category: string
  period?: string | null
  tags: string[]
  thumbnailUrl?: string
  iconUrl?: string
  publishedAt?: string | null
}

export interface HomeRecentPost {
  id: string
  slug: string
  title: string
  excerpt: string
  tags: string[]
  coverUrl?: string
  publishedAt?: string | null
}

export interface HomePayload {
  homePage: HomePagePayload
  siteSettings: HomeSiteSettingsPayload
  featuredWorks: HomeFeaturedWork[]
  recentPosts: HomeRecentPost[]
}

export async function fetchPublicHome() {
  const apiBaseUrl = await getServerApiBaseUrl()
  const response = await fetch(`${apiBaseUrl}/public/home`, { cache: 'no-store' })

  if (!response.ok) {
    return null
  }

  return response.json() as Promise<HomePayload>
}
