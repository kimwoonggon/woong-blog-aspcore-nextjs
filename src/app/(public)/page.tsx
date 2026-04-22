
import Link from 'next/link'
import Image from 'next/image'
import { ArrowRight, BriefcaseBusiness, FileText, User } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { headers } from 'next/headers'
import { fetchPublicHome } from '@/lib/api/home'
import { parsePageContentJson, toHomeContent } from '@/lib/content/page-content'

export const revalidate = 60

interface PageProps {
  searchParams?: Promise<{ __qaNoImage?: string; __qaSlow?: string }>
}

function formatPublishedMonth(publishedAt?: string | null) {
  return publishedAt
    ? new Date(publishedAt).toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
      })
    : 'Unknown Date'
}

export default async function HomePage({ searchParams }: PageProps) {
  const resolvedSearchParams = await searchParams
  const hasLocalQaFlag = resolvedSearchParams?.__qaSlow === '1' || resolvedSearchParams?.__qaNoImage === '1'
  const isLocalQaRequest = hasLocalQaFlag ? await isLocalRequest() : false
  if (resolvedSearchParams?.__qaSlow === '1' && isLocalQaRequest) {
    await new Promise((resolve) => setTimeout(resolve, 2000))
  }
  const payload = await fetchPublicHome()
  const homeContent = toHomeContent(parsePageContentJson(payload?.homePage?.contentJson))

  const headline = homeContent.headline || 'Hi, I am John, Creative Technologist'
  const introText = homeContent.introText || 'Amet minim mollit non deserunt ullamco est sit aliqua dolor do amet sint. Velit officia consequat duis enim velit mollit. Exercitation veniam consequat sunt nostrud amet.'
  const profileImageUrl = homeContent.profileImageUrl || ''
  const recentPosts = payload?.recentPosts || []
  const featuredWorks = payload?.featuredWorks || []
  const qaNoImageFeaturedWorks = resolvedSearchParams?.__qaNoImage === '1' && isLocalQaRequest
  const visibleFeaturedWorks = qaNoImageFeaturedWorks
    ? featuredWorks.map((work) => ({ ...work, thumbnailUrl: null }))
    : featuredWorks

  return (
    <div className="container mx-auto max-w-7xl flex flex-col gap-12 px-4 py-8 md:px-6 md:py-10">
      <section className="animate-fade-in-up mx-auto flex w-full max-w-5xl flex-col-reverse items-center gap-8 md:grid md:grid-cols-[minmax(0,40rem)_15rem] md:items-center md:justify-center md:gap-12" style={{ animationDelay: '0ms' }}>
        <div className="flex min-w-0 max-w-full flex-col items-center text-center md:items-start md:text-left">
          <h1
            className="mb-4 max-w-full break-words text-4xl font-heading font-bold tracking-tight text-foreground md:text-5xl lg:text-6xl animate-fade-in-up [text-wrap:balance]"
            style={{ animationDelay: '100ms' }}
          >
            {headline}
          </h1>
          <p
            className="mb-8 max-w-[600px] text-lg text-muted-foreground animate-fade-in-up"
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
              Read Study
            </Link>
          </div>
        </div>
        <div className="flex-shrink-0 animate-fade-in-up" style={{ animationDelay: '0ms' }}>
          <div className="relative h-60 w-60 overflow-hidden rounded-full bg-muted shadow-xl">
            {profileImageUrl ? (
              <Image
                src={profileImageUrl}
                alt={headline}
                fill
                className="object-cover"
                priority
                sizes="240px"
                unoptimized
              />
            ) : (
              <div
                role="img"
                aria-label={headline}
                className="flex h-full w-full items-center justify-center text-muted-foreground"
              >
                <User className="h-16 w-16" aria-hidden="true" />
              </div>
            )}
          </div>
        </div>
      </section>

      <section
        data-testid="featured-works-section"
        className="animate-fade-in-up -mx-4 rounded-[2rem] border border-border/60 bg-brand-section-bg px-4 py-7 md:-mx-6 md:px-6"
        style={{ animationDelay: '350ms' }}
      >
        <div className="mb-6 flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <h2 className="text-xl font-heading font-bold text-foreground md:text-2xl">
              Works
            </h2>
          </div>
          <Link
            href="/works"
            className="inline-flex items-center gap-2 text-sm font-medium text-brand-cyan transition-colors hover:text-brand-cyan hover:underline"
          >
            View all
            <ArrowRight className="h-4 w-4" aria-hidden="true" />
          </Link>
        </div>
        <div data-testid="featured-works-grid" className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
          {visibleFeaturedWorks.length > 0 ? (
            visibleFeaturedWorks.map((work) => {
              const thumbnailUrl = work.thumbnailUrl || null
              const publishDate = formatPublishedMonth(work.publishedAt)

              return (
                <Link key={work.id} href={`/works/${work.slug}`} data-testid="featured-work-card" className="group block h-full">
                  <Card className="flex h-full flex-col overflow-hidden rounded-2xl border-border/80 bg-background py-0 shadow-sm transition hover:border-primary/30 hover:shadow-md">
                    <div className="relative aspect-[4/3] overflow-hidden bg-muted">
                      {thumbnailUrl ? (
                        <Image
                          src={thumbnailUrl}
                          alt={work.title}
                          fill
                          className="object-cover transition-transform duration-500 group-hover:scale-105"
                          unoptimized
                        />
                      ) : (
                        <div
                          data-testid="featured-work-no-image-placeholder"
                          className="flex h-full w-full flex-col items-center justify-center gap-2 bg-gradient-to-br from-muted to-muted/80 text-muted-foreground"
                        >
                          <BriefcaseBusiness className="h-8 w-8" aria-hidden="true" />
                          <span className="text-xs font-medium">No Image</span>
                        </div>
                      )}
                    </div>
                    <CardContent className="flex flex-1 flex-col p-4 sm:p-5">
                      <div className="mb-2 flex flex-wrap items-center gap-2">
                        <span className="rounded-full bg-brand-navy px-2.5 py-0.5 text-xs font-bold text-white">
                          {publishDate}
                        </span>
                        <span className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                          {work.category}
                        </span>
                      </div>
                      <h3 className="line-clamp-2 text-lg font-heading font-bold leading-tight text-foreground transition-colors group-hover:text-brand-accent sm:text-xl">
                        {work.title}
                      </h3>
                      <p className="mt-2 line-clamp-2 flex-1 text-sm leading-relaxed text-foreground/80">
                        {work.excerpt}
                      </p>
                    </CardContent>
                  </Card>
                </Link>
              )
            })
          ) : (
            <div className="col-span-full py-8 text-center text-muted-foreground">
              No featured works found.
            </div>
          )}
        </div>
      </section>

      <section
        data-testid="recent-posts-section"
        className="animate-fade-in-up rounded-[2rem] border border-border/70 bg-background px-5 py-7 shadow-sm dark:bg-card md:px-6"
        style={{ animationDelay: '450ms' }}
      >
        <div className="mb-6 flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <h2 className="text-xl font-heading font-bold text-foreground md:text-2xl">
              Study Notes
            </h2>
          </div>
          <Link
            href="/blog"
            className="inline-flex items-center gap-2 text-sm font-medium text-brand-cyan transition-colors hover:text-brand-cyan hover:underline"
          >
            View all
            <ArrowRight className="h-4 w-4" aria-hidden="true" />
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
                <Link
                  key={post.id}
                  href={`/blog/${post.slug}`}
                  className="group block h-full"
                  data-testid="recent-post-card"
                >
                  <Card className="flex h-full flex-col gap-0 overflow-hidden rounded-2xl border-border/80 bg-background py-0 shadow-sm transition hover:border-primary/30 hover:shadow-md">
                    <div className="study-card-stripe h-1 w-full rounded-t-2xl" />
                    <CardHeader className="px-4 pt-4 pb-0 sm:px-5 sm:pt-5">
                      <div className="mb-2 flex flex-wrap items-center gap-2">
                        <span className="rounded-full bg-brand-navy px-2.5 py-0.5 text-xs font-bold text-white">
                          {publishDate}
                        </span>
                        <span className="rounded-full bg-muted px-2.5 py-0.5 text-xs font-medium text-muted-foreground">
                          {post.tags?.[0] || 'Untagged'}
                        </span>
                      </div>
                      <CardTitle className="text-lg font-heading font-bold leading-tight text-foreground transition-colors group-hover:text-brand-accent sm:text-xl">
                        {post.title}
                      </CardTitle>
                    </CardHeader>
                    <CardContent className="flex flex-1 flex-col px-4 pb-4 pt-3 sm:px-5 sm:pb-5">
                      {post.excerpt ? (
                        <p className="line-clamp-3 flex-1 text-sm leading-relaxed text-foreground/80 sm:text-base">
                          {post.excerpt}
                        </p>
                      ) : null}
                    </CardContent>
                  </Card>
                </Link>
              )
            })
          ) : (
            <div className="col-span-2 py-8 text-center text-muted-foreground">
              No recent posts found.
            </div>
          )}
        </div>
      </section>

      <section data-testid="home-navigation-section" className="animate-fade-in-up rounded-[2rem] border border-border/70 bg-background px-5 py-5 shadow-sm md:px-6" style={{ animationDelay: '550ms' }}>
        <div className="grid gap-3 md:grid-cols-3">
          {[
            {
              href: '/works',
              label: 'Works',
              icon: BriefcaseBusiness,
            },
            {
              href: '/blog',
              label: 'Study',
              icon: FileText,
            },
            {
              href: '/introduction',
              label: 'Introduction',
              icon: User,
            },
          ].map(({ href, label, icon: Icon }) => (
            <Link
              key={href}
              href={href}
              className="group rounded-2xl border border-border/80 bg-background p-4 transition hover:border-primary/30 hover:shadow-sm"
            >
              <div className="flex items-start gap-3">
                <span className="rounded-full bg-muted p-2 text-muted-foreground">
                  <Icon className="h-4 w-4" aria-hidden="true" />
                </span>
                <div>
                  <p className="text-base font-semibold text-foreground transition-colors group-hover:text-brand-accent">
                    {label}
                  </p>
                </div>
              </div>
            </Link>
          ))}
        </div>
      </section>
    </div>
  )
}

async function isLocalRequest() {
  const headerStore = await headers()
  const requestHost = (headerStore.get('x-forwarded-host') ?? headerStore.get('host') ?? '').toLowerCase()
  return /localhost|127\.0\.0\.1/.test(requestHost)
}
