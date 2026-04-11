
import Link from 'next/link'
import Image from 'next/image'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { fetchPublicHome } from '@/lib/api/home'
import { parsePageContentJson, toHomeContent } from '@/lib/content/page-content'

export const dynamic = 'force-dynamic'

function formatPublishedMonth(publishedAt?: string | null) {
  return publishedAt
    ? new Date(publishedAt).toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
      })
    : 'Unknown Date'
}

export default async function HomePage() {
  const payload = await fetchPublicHome()
  const homeContent = toHomeContent(parsePageContentJson(payload?.homePage?.contentJson))

  const headline = homeContent.headline || 'Hi, I am John, Creative Technologist'
  const introText = homeContent.introText || 'Amet minim mollit non deserunt ullamco est sit aliqua dolor do amet sint. Velit officia consequat duis enim velit mollit. Exercitation veniam consequat sunt nostrud amet.'
  const profileImageUrl = homeContent.profileImageUrl || ''
  const recentPosts = payload?.recentPosts || []
  const featuredWorks = payload?.featuredWorks || []
  const ownerName = payload?.siteSettings?.ownerName?.trim()
  const sanitizedHeadlineName = headline
    .replace(/^hi,?\s*i\s*am\s*/i, '')
    .split(',')[0]
    ?.trim()
  const profileAltText = ownerName
    ? `Profile photo of ${ownerName}`
    : sanitizedHeadlineName
      ? `Profile photo of ${sanitizedHeadlineName}`
      : 'Profile photo'

  return (
    <div className="container mx-auto max-w-7xl flex flex-col gap-16 px-4 py-8 md:px-6 md:py-12">
      <section className="flex flex-col-reverse items-center justify-between gap-8 md:flex-row md:items-start md:gap-12">
        <div className="flex flex-1 flex-col items-center text-center md:items-start md:text-left">
          <h1
            className="mb-4 text-4xl font-heading font-bold tracking-tight text-gray-900 md:text-5xl lg:text-6xl dark:text-gray-50 animate-fade-in-up [text-wrap:balance]"
            style={{ animationDelay: '100ms' }}
          >
            {headline}
          </h1>
          <p
            className="mb-8 max-w-[600px] text-lg text-gray-600 dark:text-gray-400 animate-fade-in-up"
            style={{ animationDelay: '200ms' }}
          >
            {introText}
          </p>
          <div
            className="flex flex-wrap gap-4 animate-fade-in-up"
            style={{ animationDelay: '300ms' }}
          >
            <Link
              href="/works"
              className="inline-flex min-h-11 items-center rounded-full bg-foreground px-6 py-3 text-sm font-semibold text-background transition-colors hover:bg-foreground/90"
            >
              View My Works
            </Link>
            <Link
              href="/blog"
              className="inline-flex min-h-11 items-center rounded-full border border-border px-6 py-3 text-sm font-semibold text-foreground transition-colors hover:bg-muted"
            >
              Read Blog
            </Link>
          </div>
        </div>
        <div className="flex-shrink-0 animate-fade-in-up" style={{ animationDelay: '0ms' }}>
          <div className="relative h-60 w-60 overflow-hidden rounded-full bg-gray-200 shadow-xl dark:bg-gray-800">
            {profileImageUrl ? (
              <Image
                src={profileImageUrl}
                alt={profileAltText}
                fill
                className="object-cover"
                unoptimized
              />
            ) : (
              <div className="flex h-full w-full items-center justify-center text-gray-400">
                Avatar
              </div>
            )}
          </div>
        </div>
      </section>

      <section data-testid="featured-works-section" className="-mx-4 bg-brand-section-bg px-4 py-8 md:-mx-6 md:px-6">
        <div className="mb-6 flex items-center justify-between">
          <h2 className="text-xl font-bold text-gray-900 md:text-2xl dark:text-gray-50">
            Featured works
          </h2>
          <Link
            href="/works"
            className="text-sm font-medium text-brand-cyan transition-colors hover:text-brand-cyan hover:underline"
          >
            View all
          </Link>
        </div>
        <div data-testid="featured-works-grid" className="grid gap-5 md:grid-cols-2 lg:grid-cols-3">
          {featuredWorks.length > 0 ? (
            featuredWorks.map((work) => {
              const thumbnailUrl = work.thumbnailUrl || null
              const publishDate = formatPublishedMonth(work.publishedAt)

              return (
                <Link key={work.id} href={`/works/${work.slug}`} data-testid="featured-work-card" className="group block h-full">
                  <Card className="flex h-full flex-col overflow-hidden rounded-2xl border-border/80 bg-background py-0 shadow-sm transition hover:border-primary/30 hover:shadow-md">
                    <div className="relative aspect-[4/3] overflow-hidden bg-gray-100 dark:bg-gray-800">
                      {thumbnailUrl ? (
                        <Image
                          src={thumbnailUrl}
                          alt={work.title}
                          fill
                          className="object-cover transition-transform duration-500 group-hover:scale-105"
                          unoptimized
                        />
                      ) : (
                        <div className="flex h-full w-full items-center justify-center text-sm font-medium text-gray-400">
                          No Image
                        </div>
                      )}
                    </div>
                    <CardContent className="flex flex-1 flex-col p-4 sm:p-5">
                      <div className="mb-2 flex flex-wrap items-center gap-2">
                        <span className="rounded-full bg-brand-navy px-2.5 py-0.5 text-xs font-bold text-white">
                          {publishDate}
                        </span>
                        <span className="text-xs font-medium uppercase tracking-wide text-gray-500 dark:text-gray-400">
                          {work.category}
                        </span>
                      </div>
                      <h3 className="line-clamp-2 text-lg font-heading font-bold leading-tight text-gray-900 transition-colors group-hover:text-brand-accent dark:text-gray-50">
                        {work.title}
                      </h3>
                      <p className="mt-2 line-clamp-2 flex-1 text-sm leading-relaxed text-gray-600 dark:text-gray-300">
                        {work.excerpt || 'Click to view details'}
                      </p>
                    </CardContent>
                  </Card>
                </Link>
              )
            })
          ) : (
            <div className="col-span-full py-8 text-center text-gray-500">
              No featured works found.
            </div>
          )}
        </div>
      </section>

      <section data-testid="recent-posts-section" className="bg-background">
        <div className="mb-6 flex items-center justify-between">
          <h2 className="text-xl font-bold text-gray-900 md:text-2xl dark:text-gray-50">
            Recent posts
          </h2>
          <Link
            href="/blog"
            className="text-sm font-medium text-brand-cyan transition-colors hover:text-brand-cyan hover:underline"
          >
            View all
          </Link>
        </div>
        <div className="grid gap-6 md:grid-cols-2">
          {recentPosts.length > 0 ? (
            recentPosts.map((post) => {
              const publishDate = post.publishedAt
                ? new Date(post.publishedAt).toLocaleDateString('en-US', {
                    year: 'numeric',
                    month: 'long',
                    day: 'numeric',
                  })
                : 'Unknown Date'

              return (
                <Card key={post.id} data-testid="recent-post-card" className="overflow-hidden rounded-2xl border-border/80 bg-background shadow-sm transition hover:border-primary/30 hover:shadow-md">
                  <CardHeader>
                    <CardTitle className="text-xl font-bold">
                      <Link href={`/blog/${post.slug}`} className="transition-colors hover:text-brand-cyan">
                        {post.title}
                      </Link>
                    </CardTitle>
                    <div className="flex flex-wrap items-center gap-3 text-base text-gray-600 dark:text-gray-400">
                      <span>{publishDate}</span>
                      <span className="rounded-full bg-muted px-2.5 py-0.5 text-xs font-medium text-muted-foreground">
                        {post.tags?.[0] || 'Untagged'}
                      </span>
                    </div>
                  </CardHeader>
                  <CardContent>
                    <p className="line-clamp-3 text-gray-600 dark:text-gray-300">
                      {post.excerpt}
                    </p>
                  </CardContent>
                </Card>
              )
            })
          ) : (
            <div className="col-span-2 py-8 text-center text-gray-500">
              No recent posts found.
            </div>
          )}
        </div>
      </section>
    </div>
  )
}
